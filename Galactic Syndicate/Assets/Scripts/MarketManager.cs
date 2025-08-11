// MarketManager.cs - NİHAİ HALİ (Fiyatları sunucudan çekiyor)

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using Firebase.Firestore;
using Firebase.Functions;

// Sunucudan gelen fiyat verisini tutmak için yeni bir struct
public struct MarketPriceData
{
    public int BuyPrice;
    public int SellPrice;
}

public class MarketManager : MonoBehaviour
{
    public static MarketManager instance;

    [Header("UI Link")]
    public MarketUI marketUI;

    public event Action<bool, MarketDisplayData> OnMarketUpdate;

    private Planet currentPlanet;
    // DEĞİŞİKLİK: Artık arz/talep yerine doğrudan fiyatları saklıyoruz.
    private Dictionary<string, MarketPriceData> currentMarketPrices;
    private ItemData[] allGameItems;
    
    // YENİ: Firestore dinleyicisini yönetmek için.
    private ListenerRegistration economyListener;
    private bool isTransactionPending = false;

    #region Core Logic
    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        allGameItems = Resources.LoadAll<ItemData>("");
        if (marketUI != null)
        {
            marketUI.Initialize(this);
        }
    }

    public void OpenMarket(Planet planet)
    {
        // --- YENİ GÜVENLİK KONTROLÜ ---
        // Oyuncu verisi (kredi vb.) yüklenmeden marketin açılmasını engelle.
        // Bu, mobildeki yavaş yüklemelerden kaynaklanan yarış durumlarını (race condition) çözer.
        if (!SaveManager.IsInitialLoadComplete)
        {
            Debug.LogWarning("Market cannot be opened yet. Initial data load is not complete.");
            // İsteğe bağlı: Kullanıcıya bir "Lütfen bekleyin..." mesajı gösterilebilir.
            return;
        }
        // --------------------------------

        currentPlanet = planet;
        // DEĞİŞİKLİK: Fiyatları bir kere çekmek yerine, gezegenin ekonomi dökümanını dinlemeye başlıyoruz.
        // Bu, fiyatlar her değiştiğinde OnEconomySnapshot metodunun otomatik olarak çağrılmasını sağlar.
        DocumentReference economyDocRef = FirebaseFirestore.DefaultInstance.Collection("economies").Document(currentPlanet.name);
        // HATA DÜZELTİLDİ: Unity Firebase SDK'sında Listen metodu sadece tek parametre alır

        // --- İYİLEŞTİRME: Paneli anında aç, veriyi sonra yükle ---
        OnMarketUpdate?.Invoke(true, new MarketDisplayData()); // Paneli boş olarak hemen göster.
        // ---------------------------------------------------------
        economyListener = economyDocRef.Listen(OnEconomySnapshot);
    }

    public void CloseMarket()
    {
        currentPlanet = null;
        currentMarketPrices = null;
        // YENİ: Market kapatıldığında dinleyiciyi durdurmak ÇOK ÖNEMLİDİR.
        economyListener?.Stop();
        OnMarketUpdate?.Invoke(false, new MarketDisplayData());
    }

    public async Task BuyItem(ItemData item, int price)
    {
        if (isTransactionPending || currentPlanet == null) return;

        if (PlayerStats.instance.credits < price)
        {
            Debug.LogWarning("Insufficient credits (client-side check).");
            UIManager.instance?.ShowNotification("Yetersiz Kredi!");
            return;
        }

        if (!InventoryManager.instance.CanAddItem(item))
        {
            Debug.LogWarning("Inventory is full (client-side check).");
            UIManager.instance?.ShowNotification("Envanter Dolu!");
            return;
        }
        
        // --- HATA DÜZELTMESİ: Yarış Durumu (Race Condition) Önlemi ---
        // Metot içinde "await" kullanacağımız için, marketin kapatılması ihtimaline karşı
        // gezegen ismini yerel bir değişkende saklıyoruz.
        string planetNameForAnalytics = currentPlanet.name;

        // --- İYİMSER GÜNCELLEME BAŞLANGICI ---
        // Sunucuyu beklemeden, işlemi yerelde hemen gerçekleştir.
        PlayerStats.instance.RemoveCredits(price);
        InventoryManager.instance.AddItem(item, 1);
        PlayerStats.instance.onStatsChanged.Invoke();

        // UI'ı anında güncelle.
        UpdateAndNotifyUI();
        // ------------------------------------

        isTransactionPending = true;
        marketUI?.SetLoadingIndicator(true); // Yükleniyor göstergesini AÇ
        Debug.Log($"[BuyItem] '{item.itemName}' alımı deneniyor...");

        // Gerçek sunucu işlemini arka planda yap.
        try
        {
            var functions = FirebaseFunctions.DefaultInstance;
            var function = functions.GetHttpsCallable("buyItem");

            var data = new Dictionary<string, object>
            {
                { "planetName", planetNameForAnalytics },
                { "itemName", item.itemName }
            };

            var result = await function.CallAsync(data);
            var resultDict = ParseFunctionResult(result.Data);

            if (resultDict != null && resultDict.ContainsKey("success") && (bool)resultDict["success"])
            {
                Debug.Log($"[BuyItem] Sunucu onayı başarılı: {resultDict["message"]}");

                // --- ANALYTICS OLAYI ---
                AnalyticsManager.instance?.LogEvent("buy_item", new Dictionary<string, object>
                {
                    { "item_name", item.itemName },
                    { "planet_name", planetNameForAnalytics },
                    { "price", price }
                });
                // -------------------------

                // İşlem başarılı olduğu için başka bir şey yapmaya gerek yok.
                // Sadece oyunu kaydet.
                _ = SaveManager.instance.RequestSave();
            }
            else
            {
                // --- GELİŞTİRİLMİŞ HATA LOGLAMA ---
                // Sunucudan gelen hata mesajını ve tüm cevabı loglayarak sorunu daha net görelim.
                string serverMessage = resultDict?["message"]?.ToString() ?? "Sunucudan mesaj gelmedi.";
                string fullError = $"[BuyItem] SUNUCU İŞLEMİ REDDETTİ. Sebep: {serverMessage}.";
                UIManager.instance?.ShowNotification($"İşlem Başarısız: {serverMessage}");
                Debug.LogError(fullError, this);
                // ------------------------------------
                // --- İYİMSER GÜNCELLEMEYİ GERİ AL ---
                PlayerStats.instance.AddCredits(price);
                InventoryManager.instance.RemoveItem(item, 1);
                // ------------------------------------
            }
        }
        catch (Exception e)
        {
            // --- GELİŞTİRİLMİŞ HATA LOGLAMA ---
            // Hatayı daha detaylı bir şekilde loglayarak sorunun kaynağını bulalım.
            string fullError = e is FunctionsException functionsException ?
                $"[BuyItem] CLOUD FUNCTION ÇÖKTÜ. Kod: {functionsException.ErrorCode}. Mesaj: {functionsException.Message}" :
                $"[BuyItem] BEKLENMEDİK İSTEMCİ HATASI. Tip: {e.GetType()}. Mesaj: {e.Message}\n{e.StackTrace}";
            UIManager.instance?.ShowNotification("Ağ Hatası!");
            Debug.LogError(fullError, this);
            // ------------------------------------
            // --- İYİMSER GÜNCELLEMEYİ GERİ AL ---
            PlayerStats.instance.AddCredits(price);
            InventoryManager.instance.RemoveItem(item, 1);
            // ------------------------------------
        }
        finally
        {
            isTransactionPending = false;
            marketUI?.SetLoadingIndicator(false); // Yükleniyor göstergesini KAPAT
            // --- YENİ HATA DÜZELTMESİ: Kilitlenen Buton Sorunu ---
            // İşlem başarılı da olsa, başarısız da olsa, UI'ı her zaman en güncel
            // veriyle yenileyerek butonların doğru durumda (aktif/pasif) olmasını garantile.
            UpdateAndNotifyUI();
        }
    }
    #endregion
        #region Helper Methods & Updated Logic
    public async Task SellItem(ItemData item, int price)
    {
        if (isTransactionPending || currentPlanet == null) return;

        // --- HATA DÜZELTMESİ: Yarış Durumu (Race Condition) Önlemi ---
        // Metot içinde "await" kullanacağımız için, marketin kapatılması ihtimaline karşı
        // gezegen ismini yerel bir değişkende saklıyoruz.
        string planetNameForAnalytics = currentPlanet.name;
        
        // --- İYİMSER GÜNCELLEME BAŞLANGICI ---
        // Sunucuyu beklemeden, işlemi yerelde hemen gerçekleştir.
        if (!InventoryManager.instance.RemoveItem(item, 1)) return; // Satacak mal yoksa çık.
        PlayerStats.instance.AddCredits(price);
        PlayerStats.instance.onStatsChanged.Invoke();

        // UI'ı anında güncelle.
        UpdateAndNotifyUI();
        // ------------------------------------
        
        isTransactionPending = true;
        marketUI?.SetLoadingIndicator(true); // Yükleniyor göstergesini AÇ
        Debug.Log($"[SellItem] '{item.itemName}' satışı deneniyor...");

        // Gerçek sunucu işlemini arka planda yap.
        try
        {
            var functions = FirebaseFunctions.DefaultInstance;
            var function = functions.GetHttpsCallable("sellItem");

            var data = new Dictionary<string, object>
            {
                { "planetName", planetNameForAnalytics },
                { "itemName", item.itemName }
            };

            var result = await function.CallAsync(data);
            var resultDict = ParseFunctionResult(result.Data);

            if (resultDict != null && resultDict.ContainsKey("success") && (bool)resultDict["success"])
            {
                Debug.Log($"[SellItem] Sunucu onayı başarılı: {resultDict["message"]}");

                // --- ANALYTICS OLAYI ---
                AnalyticsManager.instance?.LogEvent("sell_item", new Dictionary<string, object>
                {
                    { "item_name", item.itemName },
                    { "planet_name", planetNameForAnalytics },
                    { "price", price }
                });
                // -------------------------

                // İşlem başarılı olduğu için başka bir şey yapmaya gerek yok.
                // Sadece oyunu kaydet ve sendika verisini yenile.
                _ = SaveManager.instance.RequestSave();
                _ = SyndicateManager.instance.RefreshCurrentSyndicateData();
            }
            else
            {
                Debug.LogError($"[SellItem] Fonksiyon başarısız oldu: {resultDict?["message"] ?? "Unknown server error."}");
                UIManager.instance?.ShowNotification("Satış Başarısız!");
                // --- İYİMSER GÜNCELLEMEYİ GERİ AL ---
                InventoryManager.instance.AddItem(item, 1);
                PlayerStats.instance.RemoveCredits(price);
                // ------------------------------------
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SellItem] Bir hata oluştu: {e.Message}");
            UIManager.instance?.ShowNotification("Ağ Hatası!");
            // --- İYİMSER GÜNCELLEMEYİ GERİ AL ---
            InventoryManager.instance.AddItem(item, 1);
            PlayerStats.instance.RemoveCredits(price);
            // ------------------------------------
        }
        finally
        {
            isTransactionPending = false;
            marketUI?.SetLoadingIndicator(false); // Yükleniyor göstergesini KAPAT
            // --- YENİ HATA DÜZELTMESİ: Kilitlenen Buton Sorunu ---
            // Satış işleminden sonra da UI'ı her zaman en güncel veriyle yenile.
            UpdateAndNotifyUI();
        }
    }

    private IDictionary<string, object> ParseFunctionResult(object data)
    {
        if (data == null) return null;
        if (data is IDictionary<string, object> directDict) return directDict;
        if (data is IDictionary<object, object> objectDict)
        {
            return objectDict.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
        }
        return null;
    }

    // YENİ METOT: Firestore dinleyicisi her veri aldığında bu metot çağrılır.
    // HATA DÜZELTİLDİ: Unity Firebase SDK'sında sadece DocumentSnapshot parametresi var
    private async void OnEconomySnapshot(DocumentSnapshot snapshot)
    {
        // Unity Firebase SDK'sında hata kontrolü snapshot üzerinden yapılır
        if (snapshot == null || !snapshot.Exists)
        {
            Debug.LogWarning($"[MarketListener] Gezegenin ekonomi verisi bulunamadı veya silindi: {currentPlanet?.name}");
            CloseMarket();
            return;
        }

        // Fiyatları sunucudan çekme işlemi artık burada, dinleyici içinde yapılıyor.
        // Bu, market açıkken fiyatların canlı olarak güncellenmesini sağlar.
        bool success = await FetchMarketPrices();
        if (success)
        {
            UpdateAndNotifyUI();
        }
        else
        {
            Debug.LogError($"{currentPlanet.name} için pazar fiyatları dinleyici tarafından çekilemedi.");
            CloseMarket();
        }
    }

    // YENİ METOT: Fiyatları sunucudan çeker.
    private async Task<bool> FetchMarketPrices()
    {
        if (currentPlanet == null) return false;
        
        try
        {
            var functions = FirebaseFunctions.DefaultInstance;
            var function = functions.GetHttpsCallable("getMarketPrices");
            var data = new Dictionary<string, object> { { "planetName", currentPlanet.name } };
            
            var result = await function.CallAsync(data);
            var resultDict = ParseFunctionResult(result.Data);

            if (resultDict != null && resultDict.ContainsKey("success") && (bool)resultDict["success"])
            {
                // --- NİHAİ ÇÖZÜM: Sunucudan gelen veriyi daha sağlam bir şekilde ayrıştırma ---
                // Sorun, Firebase'in veriyi C#'ın beklemediği bir tipte (IDictionary<object, object>)
                // göndermesi ve 'as' operatörünün sessizce başarısız olmasıydı.
                // Bu yeni kod, gelen veri tipine karşı daha esnektir.
                currentMarketPrices = new Dictionary<string, MarketPriceData>();
                if (resultDict.TryGetValue("prices", out object pricesObject) && pricesObject is IDictionary<object, object> pricesDict)
                {
                    foreach (var pair in pricesDict)
                    {
                        var itemName = pair.Key.ToString();
                        if (pair.Value is IDictionary<object, object> priceDataDict)
                        {
                            try
                            {
                                var buyPrice = Convert.ToInt32(priceDataDict["buyPrice"]);
                                var sellPrice = Convert.ToInt32(priceDataDict["sellPrice"]);
                                
                                currentMarketPrices[itemName] = new MarketPriceData
                                {
                                    BuyPrice = buyPrice,
                                    SellPrice = sellPrice
                                };
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarning($"'{itemName}' için fiyat ayrıştırılamadı. Hata: {e.Message}");
                            }
                        }
                    }
                }
                Debug.Log($"Market prices for {currentPlanet.name} fetched. Parsed {currentMarketPrices.Count} items.");

                // --- YENİ NİHAİ DEBUG ADIMI ---
                PerformMismatchAnalysis();
                // --------------------------------

                return true;
            }
            else
            {
                Debug.LogError($"getMarketPrices function failed: {resultDict?["message"] ?? "Unknown error"}");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching market prices: {e.Message}");
            return false;
        }
    }
    
    // YENİ DEBUG METODU: Sunucu ve istemci verilerini karşılaştırır ve detaylı bir rapor yazdırır.
    private void PerformMismatchAnalysis()
    {
        if (currentMarketPrices == null || allGameItems == null)
        {
            Debug.LogError("[Mismatch Analysis] Veri analize hazır değil.");
            return;
        }

        var serverKeys = new HashSet<string>(currentMarketPrices.Keys);
        var clientNames = new HashSet<string>(allGameItems.Select(item => item.itemName));

        var matchingItems = serverKeys.Intersect(clientNames).ToList();
        var serverOnlyItems = serverKeys.Except(clientNames).ToList();
        var clientOnlyItems = clientNames.Except(serverKeys).ToList();

        string debugMessage = "\n\n--- PAZAR VERİ ANALİZİ ---\n\n";
        debugMessage += $"SUNUCU {serverKeys.Count} ürün fiyatı gönderdi.\nİSİMLER: [{string.Join(", ", serverKeys)}]\n\n";
        debugMessage += $"İSTEMCİ (UNITY) {clientNames.Count} ürüne sahip.\nİSİMLER: [{string.Join(", ", clientNames)}]\n\n";

        if (matchingItems.Count > 0)
        {
            debugMessage += $"✅ EŞLEŞEN ÜRÜNLER ({matchingItems.Count}): [{string.Join(", ", matchingItems)}]\n";
        }
        else
        {
            debugMessage += $"❌ HİÇ EŞLEŞEN ÜRÜN BULUNAMADI! Sorun büyük ihtimalle isim uyuşmazlığı.\n";
        }

        if (serverOnlyItems.Count > 0)
        {
            debugMessage += $"⚠️ SADECE SUNUCUDA VAR (Unity'de bu isimde ItemData asset'i eksik): [{string.Join(", ", serverOnlyItems)}]\n";
        }

        if (clientOnlyItems.Count > 0)
        {
            debugMessage += $"⚠️ SADECE İSTEMCİDE VAR (Sunucu bu ürünler için fiyat göndermedi): [{string.Join(", ", clientOnlyItems)}]\n";
        }

        debugMessage += "\n--- ANALİZ SONU ---\n";

        Debug.Log(debugMessage);
    }

    // GÜNCELLENMİŞ METOT: Artık fiyat hesaplamıyor, hazır fiyatları kullanıyor.
    private void UpdateAndNotifyUI()
    {
        if (currentPlanet == null || currentMarketPrices == null) return;
        
        var planetItems = new List<MarketItemInfo>();
        var playerItems = new List<MarketItemInfo>();

        // Gezegenin sattığı ürünler listesi
        foreach (var itemData in allGameItems)
        {
            if (currentMarketPrices.ContainsKey(itemData.itemName))
            {
                int price = currentMarketPrices[itemData.itemName].BuyPrice;
                int playerQuantity = InventoryManager.instance.inventory
                    .FirstOrDefault(slot => slot.item == itemData)?.quantity ?? 0;
                
                planetItems.Add(new MarketItemInfo { Item = itemData, Price = price, PlayerQuantity = playerQuantity });
            }
        }

        // Oyuncunun satabileceği ürünler listesi
        foreach (var inventorySlot in InventoryManager.instance.inventory)
        {
            if (currentMarketPrices.ContainsKey(inventorySlot.item.itemName))
            {
                int price = currentMarketPrices[inventorySlot.item.itemName].SellPrice;
                playerItems.Add(new MarketItemInfo { Item = inventorySlot.item, Price = price, PlayerQuantity = inventorySlot.quantity });
            }
        }

        MarketDisplayData displayData = new MarketDisplayData
        {
            PlanetItems = planetItems,
            PlayerItems = playerItems
        };

        OnMarketUpdate?.Invoke(true, displayData);
    }
    
    // ARTIK KULLANILMIYOR: Bu metotlar artık sunucuda olduğu için istemcide gerekmiyor.
    // private int CalculatePrice(...) { ... }
    // private async Task<bool> FetchEconomyData() { ... }
    // private async Task<bool> CreateEconomyForPlanet(Planet planet) { ... }
    
    #endregion
}
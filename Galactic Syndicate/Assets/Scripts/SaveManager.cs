// SaveManager.cs - NİHAİ VE DOĞRU HALİ

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Firestore;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;
    [Header("Auto-Save Settings")]
    [Tooltip("Oyuncu ilerlemesini sunucuya ne sıklıkla otomatik olarak kaydedeceğimiz (saniye).")]
    [SerializeField] private float autoSaveInterval = 20f;
    private bool isSaving = false;
    private bool saveRequestedWhileBusy = false; // YENİ: Kaydetme işlemi sırasında yeni bir istek gelip gelmediğini takip eder.
    
    // --- YENİ EKLENEN SATIRLAR ---
    // Bu bayrak, oyunun ilk yüklemesi bitene kadar market gibi sistemlerin açılmasını engeller.
    public static bool IsInitialLoadComplete { get; private set; } = false;
    private bool isLoading = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Periyodik olarak otomatik kaydetmeyi başlat.
        InvokeRepeating(nameof(RequestSave), autoSaveInterval, autoSaveInterval);
    }

    /// <summary>
    /// Güvenli bir şekilde oyun kaydı başlatma isteği gönderir.
    /// Zaten bir kayıt veya yükleme işlemi devam ediyorsa yenisini başlatmaz.
    /// </summary>
    public async Task RequestSave()
    {
        // Yükleme sırasında veya kullanıcı girişi yapılmamışsa kaydetme.
        if (isLoading || FirebaseManager.instance?.user == null) return;

        // Eğer zaten bir kaydetme işlemi devam ediyorsa, bir istek olduğunu işaretle ve çık.
        // Devam eden işlem bittiğinde bu bayrağı kontrol edip tekrar çalışacak.
        if (isSaving)
        {
            saveRequestedWhileBusy = true;
            Debug.Log("Save requested while another save was in progress. Queuing request.");
            return;
        }

        isSaving = true;

        // Ana kaydetme döngüsü. Bu, meşgulken gelen isteklerin de işlenmesini sağlar.
        do
        {
            // Döngünün başında bayrağı sıfırla.
            saveRequestedWhileBusy = false;

            Debug.Log("Performing save to Firestore...");
            try
            {
                await PerformSave();
                Debug.Log("Game data successfully saved to Firestore.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save to Firestore: {e.Message}");
            }

            // Eğer bu kaydetme sırasında yeni bir istek geldiyse, döngü devam edecek.
            if (saveRequestedWhileBusy)
            {
                Debug.Log("A new save was requested during the last one. Running save again.");
            }
        } while (saveRequestedWhileBusy);

        isSaving = false;
    }

    // Uygulama duraklatıldığında veya kapatıldığında otomatik kayıt yapar.
    private async void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("Application is pausing. Requesting final save...");
            await RequestSave();
        }
    }

    private async void OnApplicationQuit()
    {
        Debug.Log("Application is quitting. Requesting final save...");
        await RequestSave();
    }

    /// <summary>
    /// Oyundaki tüm verileri toplayıp Firestore'a yazan ana metot.
    /// </summary>
    private async Task PerformSave()
    {
        // --- YENİ GÜVENLİK KONTROLÜ ---
        // Herhangi bir kayıt işlemi yapmadan önce, geçerli bir kullanıcı ID'miz olduğundan emin ol.
        // Bu, "An empty string was provided" hatasını tamamen önler.
        if (string.IsNullOrEmpty(FirebaseManager.instance.UserID) || FirebaseManager.instance.UserID == "N/A")
        {
            Debug.LogWarning("PerformSave: UserID is not available yet. Aborting save.");
            return;
        }

        // Eğer ana oyun sahnesinde değilsek (PlayerStats yoksa), hiçbir şey kaydetme.
        if (PlayerStats.instance == null)
        {
            Debug.LogWarning("PerformSave: PlayerStats.instance is null. Aborting save to prevent overwriting with empty data.");
            return;
        }

        // --- GÜNCELLEME: Veriyi esnek bir sözlük olarak hazırlıyoruz ---
        // Bu, FieldValue.ServerTimestamp gibi özel sunucu değerlerini eklememizi sağlar.
        var data = new Dictionary<string, object>();

        // --- Veri Toplama ---
        if (PlayerStats.instance != null)
        {
            data["credits"] = PlayerStats.instance.credits;
            data["moveSpeed"] = PlayerStats.instance.moveSpeed;
            data["maxInventorySlots"] = PlayerStats.instance.maxInventorySlots;
            data["thrustUpgradeCost"] = PlayerStats.instance.thrustUpgradeCost;
            data["inventoryUpgradeCost"] = PlayerStats.instance.inventoryUpgradeCost;
        }

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            data["playerPosition"] = new GeoPoint(player.transform.position.x, player.transform.position.y);
            var rot = player.transform.rotation;
            data["playerRotation"] = new List<float> { rot.x, rot.y, rot.z, rot.w };
        }

        if (InventoryManager.instance != null)
        {
            data["inventory"] = InventoryManager.instance.inventory;
        }

        if (QuestManager.instance != null)
        {
            data["activeQuests"] = QuestManager.instance.activeQuests
                .Select(quest => new QuestData(quest))
                .ToList();
        }
        
        GalaxyGenerator galaxyGen = FindObjectOfType<GalaxyGenerator>();
        if (galaxyGen != null)
        {
            data["galaxySeed"] = galaxyGen.currentGalaxySeed;
        }

        if (SyndicateManager.instance != null)
        {
            data["syndicateId"] = SyndicateManager.instance.CurrentSyndicateId;
        }

        if (PlayerProfileManager.instance != null)
        {
            data["username"] = PlayerProfileManager.instance.PlayerUsername;
            data["profilePictureUrl"] = PlayerProfileManager.instance.ProfilePictureURL;
        }

        // --- YENİ ADIM: Aktivite takibi için son görülme zaman damgasını ekle ---
        data["lastSeen"] = FieldValue.ServerTimestamp;

        // --- Veritabanına Yazma ---
        DocumentReference docRef = FirebaseManager.instance.db.Collection("users").Document(FirebaseManager.instance.UserID);
        await docRef.SetAsync(data); // SetAsync, dökümanı bu yeni veriyle tamamen günceller.
    }

    /// <summary>
    /// Firestore'dan oyun verisini yükler.
    /// </summary>
    /// <returns>Kayıt bulunup yüklendiyse true, aksi halde false döner.</returns>
    public async Task<bool> LoadGame()
    {
        // Yükleme başladığında bayrağı sıfırla.
        IsInitialLoadComplete = false;

        if (isLoading || FirebaseManager.instance?.user == null) return false;

        isLoading = true;
        Debug.Log("Attempting to load game from Firestore...");

        try
        {
            DocumentReference docRef = FirebaseManager.instance.db.Collection("users").Document(FirebaseManager.instance.UserID);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                Debug.Log("Save data found in Firestore. Applying data...");
                GameData data = snapshot.ConvertTo<GameData>();
                ApplyDataToGame(data);
                return true; // Başarılı yükleme
            }
            else
            {
                Debug.Log("No save data found for this user. Starting a new game.");
                
                // --- YENİ EKLENEN BLOK ---
                // Yeni bir oyuncu için varsayılan bir pilot adı ayarla.
                // Bu, profil panelinde "N/A" gösterilmesini engeller.
                if (PlayerProfileManager.instance != null)
                {
                    await PlayerProfileManager.instance.FetchUserProfile();
                }
                return false; // Kayıt yok
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load from Firestore: {e.Message}");
            return false; // Hata oluştu
        }
        finally
        {
            isLoading = false;
            // İşlem başarılı da olsa, başarısız da olsa, ilk yüklemenin bittiğini işaretle.
            IsInitialLoadComplete = true;
        }
    }

    /// <summary>
    /// Yüklenen veriyi oyundaki ilgili sistemlere uygular.
    /// </summary>
    private async void ApplyDataToGame(GameData data)
    {
        // --- Veri Uygulama ---
        GalaxyGenerator galaxyGen = FindObjectOfType<GalaxyGenerator>();
        if (galaxyGen != null)
        {
            galaxyGen.GenerateGalaxyFromSeed(data.galaxySeed);
        }
        
        if (PlayerStats.instance != null)
        {
            PlayerStats stats = PlayerStats.instance;
            stats.credits = data.credits;
            stats.moveSpeed = data.moveSpeed;
            stats.maxInventorySlots = data.maxInventorySlots;
            stats.thrustUpgradeCost = data.thrustUpgradeCost;
            stats.inventoryUpgradeCost = data.inventoryUpgradeCost;
            stats.onStatsChanged.Invoke(); // UI'ı güncellemek için olayı tetikle
        }

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null && data.playerPosition != null && data.playerRotation != null && data.playerRotation.Count == 4)
        {
            player.transform.position = new Vector3((float)data.playerPosition.Latitude, (float)data.playerPosition.Longitude, 0);
            player.transform.rotation = new Quaternion(data.playerRotation[0], data.playerRotation[1], data.playerRotation[2], data.playerRotation[3]);
        }

        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.LoadInventoryFromData(data.inventory);
        }

        if (QuestManager.instance != null)
        {
            QuestManager.instance.LoadQuestsFromData(data.activeQuests);
        }
        
        if (SyndicateManager.instance != null)
        {
            _ = SyndicateManager.instance.FetchPlayerSyndicateData(data.syndicateId);
        }

        // YENİ: Kaydedilmiş kullanıcı adını yükle
        if (PlayerProfileManager.instance != null)
        {
// YENİ HALİ:
            await PlayerProfileManager.instance.FetchUserProfile();        }

        Debug.Log("Game data applied successfully.");
    }
}
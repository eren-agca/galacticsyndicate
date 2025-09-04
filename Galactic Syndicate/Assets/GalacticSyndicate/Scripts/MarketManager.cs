// MarketManager.cs - TEK OYUNCULU VERSİYON

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

// Fiyat verisini tutmak için struct
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
    
    private Planet currentPlanet;
    private readonly Dictionary<string, MarketPriceData> currentMarketPrices = new Dictionary<string, MarketPriceData>();
    private ItemData[] allGameItems;

    public event Action<bool, MarketDisplayData> OnMarketUpdate;

    #region Core Logic
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Oyundaki tüm ItemData'ları bir kere yükle
        allGameItems = Resources.LoadAll<ItemData>("");
        if (marketUI != null)
        {
            marketUI.Initialize(this);
        }
    }

    /// <summary>
    /// Belirtilen gezegen için market panelini açar.
    /// </summary>
    public void OpenMarket(Planet planet)
    {
        currentPlanet = planet;
        OnMarketUpdate?.Invoke(true, new MarketDisplayData()); // Paneli boş olarak hemen göster.
        
        // Yerel fiyatları hesapla ve UI'ı güncelle
        CalculateLocalPrices();
        UpdateAndNotifyUI();
    }

    /// <summary>
    /// Market panelini kapatır.
    /// </summary>
    public void CloseMarket()
    {
        currentPlanet = null;
        currentMarketPrices.Clear();

        // --- YENİ MANTIK ---
        // Market kapatıldığında, oyuncunun tekrar etkileşime girebilmesi için DockingPanel'i göster.
        DockingPanelUI dockingPanel = FindObjectOfType<DockingPanelUI>(true); // true: inaktif olanları da bul.
        PlayerInteraction playerInteraction = FindObjectOfType<PlayerInteraction>(); // PlayerInteraction'ı sahnede bul.

        if (dockingPanel != null && playerInteraction != null && playerInteraction.GetClosestPlanet() != null)
        {
            dockingPanel.Show(playerInteraction.GetClosestPlanet());
        }

        OnMarketUpdate?.Invoke(false, new MarketDisplayData());
    }

    /// <summary>
    /// Bir ürün satın alma işlemini gerçekleştirir.
    /// </summary>
    public Task BuyItem(ItemData item, int price)
    {
        if (PlayerStats.instance.credits < price)
        {
            UIManager.instance?.ShowNotification("Yetersiz Kredi!");
            return Task.CompletedTask;
        }

        if (!InventoryManager.instance.CanAddItem(item))
        {
            UIManager.instance?.ShowNotification("Envanter Dolu!");
            return Task.CompletedTask;
        }

        PlayerStats.instance.RemoveCredits(price);
        InventoryManager.instance.AddItem(item, 1);
        
        UpdateAndNotifyUI();
        SaveManager.instance.SaveGame();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Bir ürün satma işlemini gerçekleştirir.
    /// </summary>
    public Task SellItem(ItemData item, int price)
    {
        if (!InventoryManager.instance.RemoveItem(item, 1))
        {
            UIManager.instance?.ShowNotification("Satacak ürün yok!");
            return Task.CompletedTask;
        }

        PlayerStats.instance.AddCredits(price);
        
        UpdateAndNotifyUI();
        SaveManager.instance.SaveGame();
        return Task.CompletedTask;
    }

    #endregion

    #region Helper Methods & Updated Logic

    /// <summary>
    /// Gezegenin türüne göre yerel alım-satım fiyatlarını hesaplar.
    /// </summary>
    private void CalculateLocalPrices()
    {
        currentMarketPrices.Clear();
        if (currentPlanet == null) return;

        foreach (var item in allGameItems)
        {
            int buyPrice = item.baseValue;
            int sellPrice = item.baseValue;

            // Gezegen bu ürünü üretiyorsa, ucuza satar.
            if (currentPlanet.type.producedItems.Contains(item))
            {
                buyPrice = Mathf.RoundToInt(item.baseValue * 0.7f); // %30 daha ucuz
                sellPrice = Mathf.RoundToInt(item.baseValue * 0.6f);
            }
            // Gezegen bu ürünü tüketiyorsa, pahalıya alır.
            else if (currentPlanet.type.consumedItems.Contains(item))
            {
                buyPrice = Mathf.RoundToInt(item.baseValue * 1.5f); // %50 daha pahalı
                sellPrice = Mathf.RoundToInt(item.baseValue * 1.4f);
            }

            currentMarketPrices[item.itemName] = new MarketPriceData
            {
                BuyPrice = buyPrice,
                SellPrice = sellPrice
            };
        }
    }

    /// <summary>
    /// Hesaplanan fiyatlar ve envanter durumuna göre Market UI'ını günceller.
    /// </summary>
    private void UpdateAndNotifyUI()
    {
        if (currentPlanet == null || currentMarketPrices == null) return;
        
        var planetItems = new List<MarketItemInfo>();
        var playerItems = new List<MarketItemInfo>();

        // Gezegenin sattığı ürünler listesi
        foreach (var itemData in allGameItems)
        {
            if (currentMarketPrices.TryGetValue(itemData.itemName, out MarketPriceData priceData))
            {
                int playerQuantity = InventoryManager.instance.inventory
                    .FirstOrDefault(slot => slot.item == itemData)?.quantity ?? 0;
                
                planetItems.Add(new MarketItemInfo { Item = itemData, Price = priceData.BuyPrice, PlayerQuantity = playerQuantity });
            }
        }

        // Oyuncunun satabileceği ürünler listesi
        foreach (var inventorySlot in InventoryManager.instance.inventory)
        {
            if (currentMarketPrices.TryGetValue(inventorySlot.item.itemName, out MarketPriceData priceData))
            {
                playerItems.Add(new MarketItemInfo { Item = inventorySlot.item, Price = priceData.SellPrice, PlayerQuantity = inventorySlot.quantity });
            }
        }

        MarketDisplayData displayData = new MarketDisplayData
        {
            PlanetItems = planetItems,
            PlayerItems = playerItems
        };

        OnMarketUpdate?.Invoke(true, displayData);
    }

    #endregion
}
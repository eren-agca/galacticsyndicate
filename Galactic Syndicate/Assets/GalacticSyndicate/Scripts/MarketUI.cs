// MarketUI.cs - GÜNCELLENDİ (Kaydırma Pozisyonunu Koruma Eklendi)

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using System.Threading.Tasks; // Task kullanmak için eklendi

// Bu struct'lar değişmeden kalıyor
public struct MarketDisplayData
{
    public List<MarketItemInfo> PlanetItems;
    public List<MarketItemInfo> PlayerItems;
}

public struct MarketItemInfo
{
    public ItemData Item;
    public int Price;
    public int PlayerQuantity;
}

public class MarketUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject marketPanel;
    public Transform planetSellsGrid;
    public Transform playerSellsGrid;
    public GameObject marketSlotPrefab;
    public GameObject loadingIndicator; // YENİ: Yükleniyor görseli için referans

    private MarketManager marketManager;

    public void Initialize(MarketManager manager)
    {
        this.marketManager = manager;
        this.marketManager.OnMarketUpdate += UpdateDisplay;
        marketPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (marketManager != null)
        {
            marketManager.OnMarketUpdate -= UpdateDisplay;
        }
    }

    private void UpdateDisplay(bool isOpen, MarketDisplayData data)
    {
        marketPanel.SetActive(isOpen);
        if (!isOpen) return;

        // --- İYİLEŞTİRME: Yükleniyor durumunu yönet ---
        // Eğer veri henüz gelmemişse (panel ilk açıldığında), yükleniyor göstergesini aktif et.
        bool hasData = data.PlanetItems != null && data.PlayerItems != null;
        SetLoadingIndicator(!hasData);
        if (!hasData) return;
        // ---------------------------------------------

        StartCoroutine(RebuildLayoutCoroutine(planetSellsGrid, data.PlanetItems, true));
        StartCoroutine(RebuildLayoutCoroutine(playerSellsGrid, data.PlayerItems, false));
    }

    private IEnumerator RebuildLayoutCoroutine(Transform grid, List<MarketItemInfo> items, bool isBuyList)
    {
        var scrollRect = grid.parent.parent.GetComponent<ScrollRect>();
        float savedVerticalPosition = 1f; 
        if (scrollRect != null)
        {
            savedVerticalPosition = scrollRect.verticalNormalizedPosition;
        }

        foreach (Transform child in grid)
        {
            Destroy(child.gameObject);
        }

        foreach (var itemInfo in items)
        {
            CreateSlot(itemInfo, grid, isBuyList);
        }

        yield return new WaitForEndOfFrame();

        var scrollRectTransform = grid.parent.parent.GetComponent<RectTransform>();
        if (scrollRectTransform != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRectTransform);
        }
        
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = savedVerticalPosition;
        }
    }

    private void CreateSlot(MarketItemInfo info, Transform grid, bool isBuySlot)
    {
        GameObject newSlot = Instantiate(marketSlotPrefab, grid);
        MarketSlotUI slotUI = newSlot.GetComponent<MarketSlotUI>();
        
        // --- DEĞİŞİKLİK: Action<T> yerine Func<T, Task> kullanıyoruz. ---
        Func<ItemData, Task> action = isBuySlot 
            ? (itemData) => marketManager.BuyItem(itemData, info.Price) 
            : (itemData) => marketManager.SellItem(itemData, info.Price);
        
        slotUI.Setup(info.Item, info.Price, info.PlayerQuantity, action, isBuySlot);
    }
    
    public void RequestCloseMarket()
    {
        if (marketManager != null)
        {
            marketManager.CloseMarket();
        }
    }

    /// <summary>
    /// Yükleniyor göstergesinin görünürlüğünü ayarlar.
    /// </summary>
    public void SetLoadingIndicator(bool isActive)
    {
        if (loadingIndicator != null) 
            loadingIndicator.SetActive(isActive);
    }
}
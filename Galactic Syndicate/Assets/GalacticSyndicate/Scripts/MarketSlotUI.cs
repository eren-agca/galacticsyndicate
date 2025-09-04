using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks; // Task kullanmak için eklendi

public class MarketSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image icon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI playerQuantityText;
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;

    // --- DEĞİŞİKLİK: Action<T> yerine Func<T, Task> kullanıyoruz. ---
    public void Setup(ItemData item, int price, int playerQuantity, Func<ItemData, Task> onActionPressed, bool isBuySlot)
    {
        icon.sprite = item.icon;
        itemNameText.text = item.itemName;
        playerQuantityText.text = $"Sende: {playerQuantity}";

        actionButtonText.text = isBuySlot ? "AL" : "SAT";

        // --- NİHAİ ÇÖZÜM: Butonun durumunu doğru koşullara göre ayarla ---
        bool canAfford = PlayerStats.instance.credits >= price;

        if (isBuySlot)
        {
            actionButton.interactable = canAfford;
            priceText.text = canAfford ? $"{price}c" : $"<color=red>{price}c</color>";
        }
        else // Satış slotu ise
        {
            actionButton.interactable = playerQuantity > 0;
            priceText.text = $"{price}c";
        }

        actionButton.onClick.RemoveAllListeners();
        // --- DEĞİŞİKLİK: Butonun listener'ı artık async. ---
        actionButton.onClick.AddListener(async () => 
        {
            // İşlem bitene kadar butonu pasif yap
            actionButton.interactable = false; 
            await onActionPressed(item);
            // İşlem bittikten sonra butonun durumu UI güncellemesi ile tekrar ayarlanacak.
        });
    }
}
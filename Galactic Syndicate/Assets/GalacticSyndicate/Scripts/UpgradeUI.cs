// UpgradeUI.cs - NIHAI HALI

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks; // Task kullanmak için eklendi

public class UpgradeUI : MonoBehaviour
{
    [System.Serializable]
    public class UpgradeSlotUI
    {
        public Image upgradeIcon;
        public TextMeshProUGUI currentValueText;
        public TextMeshProUGUI effectText;
        public Button upgradeButton;
        public TextMeshProUGUI buttonCostText;
    }

    [Header("UI Slots")]
    public UpgradeSlotUI thrustUpgradeSlot;
    public UpgradeSlotUI inventoryUpgradeSlot;

    [Header("Panel Navigation")]
    [Tooltip("Bu panel kapatıldığında geri dönülecek olan panel.")]
    public GameObject dockingPanel;

    void OnEnable()
    {
        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.onStatsChanged.AddListener(UpdateAllUI);
        }
        UpdateAllUI();
    }

    void OnDisable()
    {
        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.onStatsChanged.RemoveListener(UpdateAllUI);
        }
    }

    void UpdateAllUI()
    {
        if (PlayerStats.instance == null || InventoryManager.instance == null) return;

        UpdateThrustUI();
        UpdateInventoryUI();
    }

    private void UpdateThrustUI()
    {
        PlayerStats stats = PlayerStats.instance;
        var slot = thrustUpgradeSlot;

        slot.currentValueText.text = $"Mevcut Hız: {stats.moveSpeed:F0}";

        if (stats.moveSpeed >= stats.maxMoveSpeed)
        {
            slot.effectText.text = "<color=orange>MAX SEVİYE</color>";
            slot.buttonCostText.text = "---";
            slot.upgradeButton.interactable = false;
        }
        else
        {
            slot.effectText.text = $"<color=green>+{stats.thrustUpgradeIncrement:F0}</color>";
            slot.buttonCostText.text = $"{stats.thrustUpgradeCost}c";
            slot.upgradeButton.interactable = stats.credits >= stats.thrustUpgradeCost;
        }
    }

    private void UpdateInventoryUI()
    {
        PlayerStats stats = PlayerStats.instance;
        var slot = inventoryUpgradeSlot;

        slot.currentValueText.text = $"Mevcut Slot: {InventoryManager.instance.inventory.Count} / {stats.maxInventorySlots}";

        if (stats.maxInventorySlots >= stats.maxInventoryCapacity)
        {
            slot.effectText.text = "<color=orange>MAX SEVİYE</color>";
            slot.buttonCostText.text = "---";
            slot.upgradeButton.interactable = false;
        }
        else
        {
            slot.effectText.text = $"<color=green>+{stats.inventoryUpgradeIncrement}</color>";
            slot.buttonCostText.text = $"{stats.inventoryUpgradeCost}c";
            slot.upgradeButton.interactable = stats.credits >= stats.inventoryUpgradeCost;
        }
    }

    // --- DEĞİŞİKLİK: Metot artık async void. ---
    public void OnUpgradeThrustClicked()
    {
        if (PlayerStats.instance != null)
        {
            // Geliştirme başarılı olduysa, kaydet.
            if (PlayerStats.instance.TryUpgradeThrust())
            {
                SaveManager.instance.SaveGame();
            }
        }
    }

    // --- DEĞİŞİKLİK: Metot artık async void. ---
    public void OnUpgradeInventoryClicked()
    {
        if (PlayerStats.instance != null)
        {
            // Geliştirme başarılı olduysa, kaydet.
            if (PlayerStats.instance.TryUpgradeInventory())
            {
                SaveManager.instance.SaveGame();
            }
        }
    }

    public void CloseUpgradePanel()
    {
        if (dockingPanel != null)
        {
            dockingPanel.SetActive(true);
        }
        gameObject.SetActive(false);
    }
}
// QuestLogSlotUI.cs - NIHAI HALI

using UnityEngine;
using TMPro;

public class QuestLogSlotUI : MonoBehaviour
{
    [Header("UI References")]
    // public TextMeshProUGUI titleText; // BU SATIR SİLİNDİ
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI objectiveText;

    public void Setup(Quest quest)
    {
        if (quest == null) return;

        // titleText.text = quest.Title; // BU SATIR SİLİNDİ
        descriptionText.text = quest.Description;

        // Hedefi net bir şekilde yazdır: "5 Demir Cevheri -> Vulkan Gezegeni"
        objectiveText.text = $"HEDEF: {quest.RequiredQuantity} {quest.TargetItem.itemName} -> {quest.OriginPlanet.name}";
    }
}
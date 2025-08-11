using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image icon;
    public TextMeshProUGUI quantityText;

    public void DisplaySlot(InventorySlot slot)
    {
        if (slot != null && slot.item != null)
        {
            icon.sprite = slot.item.icon;
            icon.enabled = true;
            quantityText.text = slot.quantity.ToString();
        }
        else
        {
            // Eğer slot boşsa, ikonu ve yazıyı gizle
            icon.enabled = false;
            quantityText.text = "";
        }
    }
}
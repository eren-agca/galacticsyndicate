// LeaderboardSlotUI.cs - NİHAİ HALİ

using UnityEngine;
using TMPro;

public class LeaderboardSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI valueText;

    /// <summary>
    /// Liderlik tablosundaki bir satırı verilerle doldurur.
    /// </summary>
    /// <param name="rank">Oyuncunun veya sendikanın sırası.</param>
    /// <param name="entryName">Oyuncunun veya sendikanın adı.</param>
    /// <param name="entryValue">Sıralama değeri (kredi, hazine vb.).</param>
    public void Setup(int rank, string entryName, long entryValue)
    {
        if (rankText != null)
        {
            rankText.text = $"{rank}.";
        }
        
        if (nameText != null)
        {
            nameText.text = entryName;
        }

        if (valueText != null)
        {
            // Sayıyı binlik ayraçlarla formatla (örn: 1,234,567)
            valueText.text = $"{entryValue:N0}"; 
        }
    }
}
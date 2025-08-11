// C:/Users/sdsof/OneDrive/Desktop/GitHub/galacticsyndicate/Galactic Syndicate/Assets/Scripts/SyndicateBrowserSlotUI.cs

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using System;

public class SyndicateBrowserSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI tagText;
    [SerializeField] private TextMeshProUGUI memberCountText;
    [SerializeField] private Button joinButton;

    // Bu metot, ana UI script'i tarafından çağrılarak bu slotu doldurur.
    public void Setup(SyndicateManager.PublicSyndicateInfo info, Func<string, Task> onJoinClicked)
    {
        nameText.text = info.Name;
        tagText.text = $"[{info.Tag}]";
        memberCountText.text = $"{info.MemberCount} Üye";

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(async () =>
        {
            // Butona basıldığında tekrar basılmasını engelle
            joinButton.interactable = false;
            // Ana UI'daki metoda sendika ID'sini gönder
            await onJoinClicked(info.Id);
            // İşlem bittikten sonra butonun durumu ana panel tarafından yönetilecek.
        });
    }
}
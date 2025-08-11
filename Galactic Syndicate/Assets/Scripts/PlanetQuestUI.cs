// PlanetQuestUI.cs - NIHAI HALI

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlanetQuestUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI questDescriptionText;
    public TextMeshProUGUI questRewardText;
    public Button acceptButton;

    private Quest currentQuest;

    public void DisplayQuest(Quest quest)
    {
        currentQuest = quest;

        if (currentQuest != null)
        {
            gameObject.SetActive(true);
            questDescriptionText.text = currentQuest.Description;
            questRewardText.text = $"Ödül: {currentQuest.Reward}c";
            
            bool isQuestLogFull = QuestManager.instance.activeQuests.Count >= QuestManager.instance.maxActiveQuests;
            
            acceptButton.interactable = !isQuestLogFull && currentQuest.Status == QuestStatus.Available;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Bu fonksiyonu "Kabul Et" butonunun OnClick event'ine ata.
    /// </summary>
    public void OnAcceptButtonPressed()
    {
        if (currentQuest != null)
        {
            QuestManager.instance.AddQuest(currentQuest);
            
            // Görev kabul edildikten sonra paneli kapat.
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Bu fonksiyonu "Reddet" veya "Kapat" butonunun OnClick event'ine ata.
    /// </summary>
    public void OnRejectButtonPressed()
    {
        // Reddetmek sadece paneli kapatır.
        gameObject.SetActive(false);
    }
}
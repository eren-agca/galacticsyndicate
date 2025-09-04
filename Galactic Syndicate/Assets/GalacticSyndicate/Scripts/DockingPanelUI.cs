// DockingPanelUI.cs - NIHAI HALI

using UnityEngine;
using UnityEngine.UI;

public class DockingPanelUI : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject upgradePanel;
    public GameObject planetQuestPanel;

    [Header("Button References")]
    public Button questButton;

    private Planet currentPlanet;

    public void Show(Planet planet)
    {
        this.currentPlanet = planet;
        gameObject.SetActive(true);

        // --- GÜNCELLENEN MANTIK ---
        // 1. Gezegenin sunabileceği, "Mevcut" (Available) durumda bir görevi var mı?
        // HATA DÜZELTİLDİ: 'Quest.QuestStatus.Inactive' yerine 'QuestStatus.Available' kullanılıyor.
        bool hasAvailableQuest = planet.currentQuest != null && planet.currentQuest.Status == QuestStatus.Available;
        
        // 2. Oyuncunun görev günlüğünde yer var mı?
        bool canAcceptMoreQuests = QuestManager.instance.activeQuests.Count < QuestManager.instance.maxActiveQuests;

        // Butonun görünmesi için HER İKİ koşulun da doğru olması gerekir.
        // --- HATA DÜZELTMESİ: NullReferenceException'ı önlemek için kontrol eklendi ---
        if (questButton != null)
        {
            questButton.gameObject.SetActive(hasAvailableQuest && canAcceptMoreQuests);
        }
        else
        {
            Debug.LogWarning("DockingPanelUI: Quest Button referansı atanmamış!", this.gameObject);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void OnMarketButtonClicked()
    {
        if (currentPlanet != null && MarketManager.instance != null)
        {
            Hide(); // ÖNCE bu paneli kapatarak yarış durumunu (race condition) engelle.
            MarketManager.instance.OpenMarket(currentPlanet); // SONRA marketi açma komutunu ver.
        }
    }

    public void OnUpgradeButtonClicked()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
            Hide();
        }
    }

    public void OnQuestButtonClicked()
    {
        if (planetQuestPanel != null && currentPlanet.currentQuest != null)
        {
            var questUI = planetQuestPanel.GetComponent<PlanetQuestUI>();
            if (questUI != null)
            {
                questUI.DisplayQuest(currentPlanet.currentQuest);
            }
            planetQuestPanel.SetActive(true);
            Hide();
        }
    }

    public void OnLeaveButtonClicked()
    {
        Hide();
    }
}
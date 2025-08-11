    // QuestLogUI.cs - NIHAI HALI (YENİ SCRİPT)

    using UnityEngine;
    using System.Collections.Generic;

    public class QuestLogUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject questLogPanel;
        public Transform questGrid; // ScrollView'ın içindeki "Content" objesi
        public GameObject questSlotPrefab; // Az önce oluşturduğumuz prefab

        void OnEnable()
        {
            // Panel her açıldığında listeyi güncel tutmak için event'e abone ol
            if (QuestManager.instance != null)
            {
                QuestManager.instance.OnQuestLogUpdated += UpdateQuestList;
            }
            // Paneli açar açmaz listeyi bir kere doldur
            UpdateQuestList();
        }

        void OnDisable()
        {
            // Panel kapandığında event aboneliğini iptal et (hafıza sızıntısını önler)
            if (QuestManager.instance != null)
            {
                QuestManager.instance.OnQuestLogUpdated -= UpdateQuestList;
            }
        }

        private void UpdateQuestList()
        {
            // Önce mevcut listeyi temizle
            foreach (Transform child in questGrid)
            {
                Destroy(child.gameObject);
            }

            // QuestManager'dan aktif görev listesini al ve UI'ı doldur
            List<Quest> activeQuests = QuestManager.instance.activeQuests;

            if (activeQuests.Count == 0)
            {
                // Eğer görev yoksa bilgilendirici bir yazı gösterilebilir (isteğe bağlı)
                // Örneğin: CreateSlotWithMessage("Aktif görevin yok.");
                return;
            }

            foreach (Quest quest in activeQuests)
            {
                GameObject newSlot = Instantiate(questSlotPrefab, questGrid);
                newSlot.GetComponent<QuestLogSlotUI>().Setup(quest);
            }
        }

        // Bu metot, ana UI'daki "Görevler" butonu tarafından çağrılacak
        public void TogglePanel()
        {
            if (questLogPanel != null)
            {
                bool isActive = !questLogPanel.activeSelf;
                questLogPanel.SetActive(isActive);
            }
        }
    }
    
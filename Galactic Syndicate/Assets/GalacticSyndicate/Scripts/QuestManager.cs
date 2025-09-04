// QuestManager.cs - NİHAİ HALİ

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using System.Threading.Tasks; // Task kullanmak için eklendi

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    public List<Quest> activeQuests = new List<Quest>();
    public int maxActiveQuests = 5;
    public UnityAction OnQuestLogUpdated;

    private ItemData[] allGameItems;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        allGameItems = Resources.LoadAll<ItemData>("");
        if (allGameItems.Length == 0)
        {
            Debug.LogError("No ItemData found in Resources folder! Quests cannot be loaded or created properly.");
        }
    }

    public void AddQuest(Quest quest)
    {
        if (quest != null && activeQuests.Count < maxActiveQuests)
        {
            quest.Status = QuestStatus.Active;
            activeQuests.Add(quest);
            OnQuestLogUpdated?.Invoke();
        }
        else if (quest != null)
        {
            Debug.LogWarning("Quest log is full. Cannot accept new quest.");
        }
    }

    // --- DEĞİŞİKLİK: Metot artık void yerine async Task döndürüyor. ---
    public async Task TryCompleteQuests(Planet destinationPlanet)
    {
        List<Quest> completedQuests = new List<Quest>();
        foreach (var quest in activeQuests.ToList())
        {
            if (quest.OriginPlanet == destinationPlanet)
            {
                if (InventoryManager.instance.HasItem(quest.TargetItem, quest.RequiredQuantity))
                {
                    InventoryManager.instance.RemoveItem(quest.TargetItem, quest.RequiredQuantity);
                    PlayerStats.instance.AddCredits(quest.Reward);
                    
                    quest.Status = QuestStatus.Completed;

                    // Görevin tamamlandığı gezegene yeni bir görev oluşturmasını söyle.
                    quest.OriginPlanet.GenerateNewQuest();

                    completedQuests.Add(quest);
                    Debug.Log($"Görev tamamlandı: {quest.Description}");
                }
            }
        }

        foreach (var quest in completedQuests)
        {
            activeQuests.Remove(quest);
        }

        if (completedQuests.Count > 0)
        {
            OnQuestLogUpdated?.Invoke();
            // --- DEĞİŞİKLİK: Kaydetme işleminin bitmesini bekle. ---
            SaveManager.instance.SaveGame();
        }
    }
    
    public void LoadQuestsFromData(List<QuestData> questDataList)
    {
        activeQuests.Clear();
        Planet[] allPlanets = FindObjectsOfType<Planet>();

        foreach (var data in questDataList)
        {
            Planet originPlanet = allPlanets.FirstOrDefault(p => p.gameObject.name == data.originPlanetName);
            ItemData targetItem = allGameItems.FirstOrDefault(i => i.itemName == data.targetItemName);

            if (originPlanet != null && targetItem != null)
            {
                Quest newQuest = new Quest(originPlanet, targetItem, data.requiredQuantity, data.reward);
                newQuest.Status = QuestStatus.Active; 
                activeQuests.Add(newQuest);

                // Görevi yükledikten sonra, o görevin ait olduğu gezegeni de bilgilendiriyoruz.
                originPlanet.AssignLoadedQuest(newQuest);
            }
            else
            {
                Debug.LogWarning($"Could not load quest: Planet '{data.originPlanetName}' or Item '{data.targetItemName}' not found.");
            }
        }

        OnQuestLogUpdated?.Invoke();
    }
}
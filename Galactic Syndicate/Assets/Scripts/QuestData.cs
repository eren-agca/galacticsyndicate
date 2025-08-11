using Firebase.Firestore;

[FirestoreData]
public class QuestData
{
    [FirestoreProperty] public string originPlanetName { get; set; }
    [FirestoreProperty] public string targetItemName { get; set; }
    [FirestoreProperty] public int requiredQuantity { get; set; }
    [FirestoreProperty] public int reward { get; set; }

    // Firestore için boş bir yapıcı metot gereklidir.
    public QuestData() { }

    public QuestData(Quest quest)
    {
        this.originPlanetName = quest.OriginPlanet.name;
        this.targetItemName = quest.TargetItem.itemName;
        this.requiredQuantity = quest.RequiredQuantity;
        this.reward = quest.Reward;
    }
}
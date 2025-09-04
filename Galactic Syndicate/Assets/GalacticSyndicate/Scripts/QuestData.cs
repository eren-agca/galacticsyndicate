[System.Serializable] // JsonUtility için gerekli
public class QuestData
{
    public string originPlanetName;
    public string targetItemName;
    public int requiredQuantity;
    public int reward;

    // JSON serileştirme için boş bir yapıcı metot gereklidir.
    public QuestData() { }

    public QuestData(Quest quest)
    {
        this.originPlanetName = quest.OriginPlanet.name;
        this.targetItemName = quest.TargetItem.itemName;
        this.requiredQuantity = quest.RequiredQuantity;
        this.reward = quest.Reward;
    }
}
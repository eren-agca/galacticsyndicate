// Quest.cs - NIHAI HALI

// Görevin durumunu takip etmek için bir enum.
public enum QuestStatus
{
    Available, // Gezegende mevcut, henüz alınmamış.
    Active,    // Oyuncu tarafından kabul edilmiş.
    Completed  // Tamamlanmış.
}

public class Quest
{
    // Dışarıdan okunabilir ama sadece bu sınıf içinden ayarlanabilir özellikler.
    public Planet OriginPlanet { get; private set; }
    public ItemData TargetItem { get; private set; }
    public int RequiredQuantity { get; private set; }
    public int Reward { get; private set; }
    public string Description { get; private set; }

    // Görevin mevcut durumunu tutan özellik.
    public QuestStatus Status { get; set; }

    // Yapıcı Metot (Constructor)
    public Quest(Planet origin, ItemData item, int quantity, int reward)
    {
        this.OriginPlanet = origin;
        this.TargetItem = item;
        this.RequiredQuantity = quantity;
        this.Reward = reward;

        // Görev oluşturulduğunda durumu "Mevcut" (Available) olarak ayarla.
        this.Status = QuestStatus.Available;

        // Görev tanımını otomatik oluştur.
        this.Description = $"{quantity} birim {item.itemName} ürününü {origin.name} gezegenine teslim et.";
    }
}
// GameData.cs - NİHAİ HALİ

using System.Collections.Generic;
using Firebase.Firestore;

[FirestoreData]
public class GameData
{
    // Player Stats
    [FirestoreProperty] public int credits { get; set; }
    [FirestoreProperty] public float moveSpeed { get; set; }
    [FirestoreProperty] public int maxInventorySlots { get; set; }
    [FirestoreProperty] public int thrustUpgradeCost { get; set; }
    [FirestoreProperty] public int inventoryUpgradeCost { get; set; }

    // Player State
    [FirestoreProperty] public GeoPoint playerPosition { get; set; }
    [FirestoreProperty] public List<float> playerRotation { get; set; }

    // --- YENİ EKLENEN ALAN ---
    // Oyuncunun kişisel kullanıcı adı
    [FirestoreProperty] public string username { get; set; }

    // --- YENİ EKLENEN ALAN ---
    [FirestoreProperty] public string profilePictureUrl { get; set; }

    // Inventory
    [FirestoreProperty] public List<InventorySlot> inventory { get; set; }

    // Quests
    [FirestoreProperty] public List<QuestData> activeQuests { get; set; }
    
    // World State
    [FirestoreProperty] public int galaxySeed { get; set; }
    
    // Syndicate State
    [FirestoreProperty] public string syndicateId { get; set; }

    /// <summary>
    /// Firestore'un bu sınıfı başlatabilmesi için parametresiz bir yapıcı metot gereklidir.
    /// Yeni bir oyunun başlangıç değerleri, doğrudan sahnedeki PlayerStats gibi
    /// yönetici bileşenlerinin Inspector'daki değerlerinden gelir ve SaveManager tarafından buraya atanır.
    /// Bu yapıcı metot, sadece NullReferenceException'ları önlemek için listeleri başlatır.
    /// </summary>
    public GameData()
    {
        // NullReferenceException'ları önlemek için listeleri ve referans tiplerini başlat.
        // Başlangıç değerleri (kredi, hız vb.) burada ayarlanmaz.
        this.inventory = new List<InventorySlot>();
        this.activeQuests = new List<QuestData>();
        this.playerRotation = new List<float>();
        this.username = null; // Başlangıçta null olması, yeni oyuncu olduğunu gösterir.
        this.profilePictureUrl = null;
    }
}
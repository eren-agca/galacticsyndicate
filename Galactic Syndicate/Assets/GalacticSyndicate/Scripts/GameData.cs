// GameData.cs - NİHAİ HALİ

using System.Collections.Generic;
using UnityEngine; // Vector3 ve Quaternion için eklendi

[System.Serializable] // Bu sınıfın JsonUtility tarafından serileştirilebilmesi için gerekli.
public class GameData
{
    // Player Stats
    public int credits;
    public float moveSpeed;
    public int maxInventorySlots;
    public int thrustUpgradeCost;
    public int inventoryUpgradeCost;

    // Player State
    public Vector3 playerPosition;
    public Quaternion playerRotation;

    // Oyuncunun kişisel kullanıcı adı
    public string username;

    public string profilePictureUrl;

    // Inventory
    public List<InventorySlot> inventory;

    // Quests
    public List<QuestData> activeQuests;
    
    // World State
    public int galaxySeed;
    
    /// <summary>
    /// JsonUtility'nin bu sınıfı başlatabilmesi için parametresiz bir yapıcı metot gereklidir.
    /// Yeni bir oyunun başlangıç değerleri, doğrudan sahnedeki PlayerStats gibi
    /// yönetici bileşenlerinin Inspector'daki değerlerinden gelir ve SaveManager tarafından buraya atanır.
    /// Bu yapıcı metot, sadece NullReferenceException'ları önlemek için listeleri başlatır.
    /// </summary>
    public GameData()
    {
        this.inventory = new List<InventorySlot>();
        this.activeQuests = new List<QuestData>();
        this.username = null; // Başlangıçta null olması, yeni oyuncu olduğunu gösterir.
        this.profilePictureUrl = null;
    }
}
// Planet.cs - NIHAI HALI

using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Planet : MonoBehaviour
{
    public PlanetType type;
    public Quest currentQuest { get; private set; } // Gezegenin mevcut görevi

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Setup(PlanetType newType)
    {
        type = newType;
        gameObject.name = type.planetName;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = type.planetSprite;
        }
            
        GenerateNewQuest(); // Gezegen oluşturulurken görevini de oluştursun
    }

    // Rastgele görev oluşturur
    public void GenerateNewQuest()
    {
        // Eğer gezegenin tükettiği bir mal yoksa, görev oluşturma.
        if (type.consumedItems == null || type.consumedItems.Length == 0)
        {
            currentQuest = null;
            return;
        }

        // Tüketilen mallar arasından rastgele birini seç
        ItemData targetItem = type.consumedItems[Random.Range(0, type.consumedItems.Length)];
            
        // Rastgele bir miktar belirle
        int quantity = Random.Range(3, 8); // 3 ila 7 arası birim iste
        
        // --- GÜNCELLENEN KISIM ---
        // Ödülü hesapla ve ardından 100-250 aralığında kalmasını sağla.
        int reward = Mathf.Clamp(Mathf.RoundToInt(targetItem.baseValue * quantity * 1.8f), 100, 250);

        currentQuest = new Quest(this, targetItem, quantity, reward);
        Debug.Log($"{name} yeni görev oluşturdu: {currentQuest.Description}");
    }
    public void AssignLoadedQuest(Quest loadedQuest)
    {
        // Gezegenin mevcut görevini, kayıttan gelen görevle değiştiriyoruz.
        // Bu, gezegenin görevinin durumunun "Active" olmasını sağlar.
        this.currentQuest = loadedQuest;
    }
}
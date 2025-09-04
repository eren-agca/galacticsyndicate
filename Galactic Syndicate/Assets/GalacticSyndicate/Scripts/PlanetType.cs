using UnityEngine;

[CreateAssetMenu(fileName = "New PlanetType", menuName = "Astro-Trader/Planet Type")]
public class PlanetType : ScriptableObject
{
    [Header("Planet Info")]
    public string planetName;
    public Sprite planetSprite;

    [Header("Economy")]
    [Tooltip("Bu gezegenin ürettiği ve ucuza sattığı ürünler.")]
    public ItemData[] producedItems;
    
    [Tooltip("Bu gezegenin ihtiyaç duyduğu ve pahalıya aldığı ürünler.")]
    public ItemData[] consumedItems;
}
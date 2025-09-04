using UnityEngine;

// Bu satır, Unity'nin "Assets/Create" menüsüne yeni bir seçenek ekler.
[CreateAssetMenu(fileName = "New ItemData", menuName = "Astro-Trader/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public Sprite icon;

    [Header("Trade Info")]
    public int baseValue;

    [Header("Description")]
    [TextArea(3, 5)]
    public string description;
}
// InventorySlot.cs - NİHAİ VE HATASIZ HALİ

using UnityEngine;
using System;

[System.Serializable] // JsonUtility için gerekli
public class InventorySlot
{
    public string itemName;
    public int quantity;

    // Oyun içinde kullanılacak olan, ItemData'ya doğrudan referans.
    // [NonSerialized] niteliği, bu alanın JSON'a kaydedilmesini engeller.
    [NonSerialized]
    public ItemData item;

    // JSON serileştirme için gereken boş yapıcı metot.
    public InventorySlot() { }

    // Oyun içinde yeni bir slot oluştururken kullanılan yapıcı metot
    public InventorySlot(ItemData item, int quantity)
    {
        this.item = item;
        this.itemName = item.itemName;
        this.quantity = quantity;
    }
}
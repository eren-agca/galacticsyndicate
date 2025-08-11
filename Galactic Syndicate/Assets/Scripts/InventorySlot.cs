// InventorySlot.cs - NİHAİ VE HATASIZ HALİ

using Firebase.Firestore;
using UnityEngine;
using System;

[FirestoreData]
public class InventorySlot
{
    // Firestore'a kaydedilecek olan özellikler.
    [FirestoreProperty] public string itemName { get; set; }
    [FirestoreProperty] public int quantity { get; set; }

    // Oyun içinde kullanılacak olan, ItemData'ya doğrudan referans.
    // [NonSerialized] niteliği, bu alanın veritabanına kaydedilmesini engeller.
    // HATA DÜZELTMESİ: Property, field'a dönüştürüldü.
    [NonSerialized]
    public ItemData item;

    // Firestore'un objeyi okuyabilmesi için gereken boş yapıcı metot.
    public InventorySlot() { }

    // Oyun içinde yeni bir slot oluştururken kullanılan yapıcı metot.
    public InventorySlot(ItemData item, int quantity)
    {
        this.item = item;
        this.itemName = item.itemName;
        this.quantity = quantity;
    }
}
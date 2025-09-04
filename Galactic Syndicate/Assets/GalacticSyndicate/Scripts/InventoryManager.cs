// InventoryManager.cs - NİHAİ HALİ

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    [Header("Data")]
    public List<InventorySlot> inventory = new List<InventorySlot>();
    
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Transform slotGridParent;
    public GameObject inventorySlotPrefab;

    private ItemData[] allGameItems;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Oyundaki tüm ItemData'ları bir kere yükleyip hafızada tutuyoruz.
        allGameItems = Resources.LoadAll<ItemData>("");
        if (allGameItems.Length == 0)
        {
            Debug.LogError("No ItemData found in Resources folder! Inventory cannot function properly.");
        }

        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        UpdateInventoryUI();
    }

    public void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        }
    }

    public bool CanAddItem(ItemData itemToAdd)
    {
        bool itemExists = inventory.Any(slot => slot.item == itemToAdd);
        if (itemExists)
        {
            return true;
        }

        if (inventory.Count < PlayerStats.instance.maxInventorySlots)
        {
            return true;
        }
        
        return false;
    }

    public void AddItem(ItemData itemToAdd, int amount)
    {
        InventorySlot existingSlot = inventory.FirstOrDefault(slot => slot.item == itemToAdd);

        if (existingSlot != null)
        {
            existingSlot.quantity += amount;
        }
        else
        {
            if (inventory.Count >= PlayerStats.instance.maxInventorySlots)
            {
                Debug.Log("Inventory is full! Cannot add new item type.");
                return;
            }
            inventory.Add(new InventorySlot(itemToAdd, amount));
        }
        UpdateInventoryUI();
    }

    public bool RemoveItem(ItemData itemToRemove, int amount)
    {
        InventorySlot slotToRemoveFrom = inventory.FirstOrDefault(slot => slot.item == itemToRemove);

        if (slotToRemoveFrom == null || slotToRemoveFrom.quantity < amount) return false;

        slotToRemoveFrom.quantity -= amount;

        if (slotToRemoveFrom.quantity <= 0)
        {
            inventory.Remove(slotToRemoveFrom);
        }

        UpdateInventoryUI();
        return true;
    }

    public void LoadInventoryFromData(List<InventorySlot> savedSlots)
    {
        inventory.Clear();
        foreach (var savedSlot in savedSlots)
        {
            // Kayıtlı item ismine göre Resources'tan gerçek ItemData'yı bul.
            ItemData itemData = allGameItems.FirstOrDefault(i => i.itemName == savedSlot.itemName);
            if (itemData != null)
            {
                // Yeni slotu oluştururken hem ItemData referansını hem de diğer bilgileri ata.
                InventorySlot newSlot = new InventorySlot(itemData, savedSlot.quantity);
                inventory.Add(newSlot);
            }
            else
            {
                Debug.LogWarning($"Could not find item '{savedSlot.itemName}' while loading inventory.");
            }
        }
        UpdateInventoryUI();
    }

    public void UpdateInventoryUI()
    {
        if (slotGridParent == null || inventorySlotPrefab == null) return;

        foreach (Transform child in slotGridParent)
        {
            Destroy(child.gameObject);
        }

        foreach (InventorySlot slot in inventory)
        {
            GameObject newSlot = Instantiate(inventorySlotPrefab, slotGridParent);
            InventorySlotUI slotUI = newSlot.GetComponent<InventorySlotUI>();
            slotUI.DisplaySlot(slot);
        }
    }
    
    public bool HasItem(ItemData itemToCheck, int quantity)
    {
        var slot = inventory.FirstOrDefault(s => s.item == itemToCheck);
        return slot != null && slot.quantity >= quantity;
    }
}
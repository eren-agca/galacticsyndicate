// PlayerStats.cs - NIHAI HALI

using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic; // Dictionary için eklendi

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats instance;

    [Header("Player Resources")]
    public int credits = 500;

    [Header("Ship Stats - Current Values")]
    public float moveSpeed = 50f;
    public int maxInventorySlots = 5;

    [Header("Ship Stats - Upgrade Limits & Increments")]
    public float maxMoveSpeed = 100f;
    public int maxInventoryCapacity = 10;
    public float thrustUpgradeIncrement = 10f;
    public int inventoryUpgradeIncrement = 1;

    [Header("Upgrade Costs")]
    public int thrustUpgradeCost = 150;
    public int inventoryUpgradeCost = 250;
    [Tooltip("Her geliştirmeden sonra maliyetin çarpılacağı oran.")]
    public float costIncreaseMultiplier = 1.5f;

    // UI'ın güncellenmesi gerektiğinde bu olay tetiklenir.
    public UnityEvent onStatsChanged;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddCredits(int amount)
    {
        credits += amount;
        onStatsChanged.Invoke();
    }

    public bool RemoveCredits(int amount)
    {
        if (credits >= amount)
        {
            credits -= amount;
            onStatsChanged.Invoke();
            return true;
        }
        return false;
    }

    public bool TryUpgradeThrust()
    {
        // GÜNCELLENDİ: Seviye sınırını kontrol et
        if (moveSpeed >= maxMoveSpeed)
        {
            Debug.Log("Thrust is already at max level.");
            return false;
        }

        if (RemoveCredits(thrustUpgradeCost))
        {
            moveSpeed += thrustUpgradeIncrement;
            // Hızın sınırı geçmediğinden emin ol
            moveSpeed = Mathf.Min(moveSpeed, maxMoveSpeed); 

            thrustUpgradeCost = Mathf.RoundToInt(thrustUpgradeCost * costIncreaseMultiplier);
            onStatsChanged.Invoke();
            return true;
        }
        return false;
    }

    public bool TryUpgradeInventory()
    {
        // GÜNCELLENDİ: Seviye sınırını kontrol et
        if (maxInventorySlots >= maxInventoryCapacity)
        {
            Debug.Log("Inventory is already at max capacity.");
            return false;
        }

        if (RemoveCredits(inventoryUpgradeCost))
        {
            maxInventorySlots += inventoryUpgradeIncrement;
            // Kapasitenin sınırı geçmediğinden emin ol
            maxInventorySlots = Mathf.Min(maxInventorySlots, maxInventoryCapacity);

            inventoryUpgradeCost = Mathf.RoundToInt(inventoryUpgradeCost * costIncreaseMultiplier);
            onStatsChanged.Invoke();
            return true;
        }
        return false;
    }
}
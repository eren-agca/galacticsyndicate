// SaveManager.cs - TEK OYUNCULU VERSİYON

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO; // Dosya işlemleri için eklendi

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;
    [Header("Auto-Save Settings")]
    [Tooltip("Oyuncu ilerlemesini ne sıklıkla otomatik olarak kaydedeceğimiz (saniye).")]
    [SerializeField] private float autoSaveInterval = 20f;
    
    // Bu bayrak, oyunun ilk yüklemesi bitene kadar market gibi sistemlerin açılmasını engeller
    public static bool IsInitialLoadComplete { get; private set; } = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Periyodik olarak otomatik kaydetmeyi başlat.
        InvokeRepeating(nameof(SaveGame), autoSaveInterval, autoSaveInterval);
    }

    /// <summary>
    /// Oyundaki tüm verileri toplayıp yerel bir dosyaya yazar.
    /// </summary>
    public void SaveGame()
    {
        // Eğer ana oyun sahnesinde değilsek (PlayerStats yoksa), hiçbir şey kaydetme.
        if (PlayerStats.instance == null)
        {
            Debug.LogWarning("SaveGame: PlayerStats.instance is null. Aborting save to prevent overwriting with empty data.");
            return;
        }

        GameData data = new GameData();

        // --- Veri Toplama ---
        data.credits = PlayerStats.instance.credits;
        data.moveSpeed = PlayerStats.instance.moveSpeed;
        data.maxInventorySlots = PlayerStats.instance.maxInventorySlots;
        data.thrustUpgradeCost = PlayerStats.instance.thrustUpgradeCost;
        data.inventoryUpgradeCost = PlayerStats.instance.inventoryUpgradeCost;

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            data.playerPosition = player.transform.position;
            data.playerRotation = player.transform.rotation;
        }

        if (InventoryManager.instance != null)
        {
            data.inventory = InventoryManager.instance.inventory;
        }

        if (QuestManager.instance != null)
        {
            data.activeQuests = QuestManager.instance.activeQuests
                .Select(quest => new QuestData(quest))
                .ToList();
        }
        
        GalaxyGenerator galaxyGen = FindObjectOfType<GalaxyGenerator>();
        if (galaxyGen != null)
        {
            data.galaxySeed = galaxyGen.currentGalaxySeed;
        }

        if (PlayerProfileManager.instance != null)
        {
            data.username = PlayerProfileManager.instance.PlayerUsername;
            data.profilePictureUrl = PlayerProfileManager.instance.ProfilePictureURL;
        }

        SaveSystem.Save(data);
    }

    /// <summary>
    /// Yerel dosyadan oyun verisini yükler.
    /// </summary>
    /// <returns>Kayıt bulunup yüklendiyse true, aksi halde false döner.</returns>
    public bool LoadGame()
    {
        IsInitialLoadComplete = false;
        Debug.Log("Attempting to load game from local file...");

        GameData data = SaveSystem.Load();

        if (data != null)
        {
            Debug.Log("Save data found. Applying data...");
            ApplyDataToGame(data);
            IsInitialLoadComplete = true;
            return true; // Başarılı yükleme
        }
        else
        {
            Debug.Log("No save data found. Starting a new game.");
            IsInitialLoadComplete = true;
            return false; // Hata oluştu
        }
    }

    /// <summary>
    /// Yüklenen veriyi oyundaki ilgili sistemlere uygular.
    /// </summary>
    private void ApplyDataToGame(GameData data)
    {
        GalaxyGenerator galaxyGen = FindObjectOfType<GalaxyGenerator>();
        if (galaxyGen != null)
        {
            galaxyGen.GenerateGalaxyFromSeed(data.galaxySeed);
        }
        
        PlayerStats.instance.credits = data.credits;
        PlayerStats.instance.moveSpeed = data.moveSpeed;
        PlayerStats.instance.maxInventorySlots = data.maxInventorySlots;
        PlayerStats.instance.thrustUpgradeCost = data.thrustUpgradeCost;
        PlayerStats.instance.inventoryUpgradeCost = data.inventoryUpgradeCost;
        PlayerStats.instance.onStatsChanged.Invoke();

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.transform.position = data.playerPosition;
            player.transform.rotation = data.playerRotation;
        }

        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.LoadInventoryFromData(data.inventory);
        }

        if (QuestManager.instance != null)
        {
            QuestManager.instance.LoadQuestsFromData(data.activeQuests);
        }

        if (PlayerProfileManager.instance != null)
        {
            PlayerProfileManager.instance.LoadProfileFromData(data.username, data.profilePictureUrl);
        }

        Debug.Log("Game data applied successfully.");
    }
}
// SaveSystem.cs - NIHAI HALI

using UnityEngine;
using System.IO;

public static class SaveSystem
{
    // Mobil cihazlarda ve PC'de güvenli bir kayıt konumu
    private static readonly string SAVE_PATH = Path.Combine(Application.persistentDataPath, "savegame.json");

    public static void Save(GameData data)
    {
        string json = JsonUtility.ToJson(data, true); // 'true' JSON'ı okunabilir formatlar
        File.WriteAllText(SAVE_PATH, json);
        Debug.Log("Game Saved to: " + SAVE_PATH);
    }

    public static GameData Load()
    {
        if (File.Exists(SAVE_PATH))
        {
            string json = File.ReadAllText(SAVE_PATH);
            GameData data = JsonUtility.FromJson<GameData>(json);
            Debug.Log("Game Loaded from: " + SAVE_PATH);
            return data;
        }
        else
        {
            // Bu bir hata değil, normal bir durum. O yüzden Warning yerine Log kullanabiliriz.
            Debug.Log("Save file not found. This is a new game session.");
            return null; // Kayıt dosyası yoksa null döneriz.
        }
    }

    // --- HATAYI GİDEREN YENİ METOT ---
    /// <summary>
    /// Mevcut kayıt dosyasını diskten siler.
    /// </summary>
    public static void DeleteSave()
    {
        if (File.Exists(SAVE_PATH))
        {
            File.Delete(SAVE_PATH);
            Debug.Log("Save file deleted.");
        }
    }
}
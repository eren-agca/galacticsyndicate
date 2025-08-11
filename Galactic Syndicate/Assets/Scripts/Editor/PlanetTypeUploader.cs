using UnityEngine;
using UnityEditor; // HATA DÜZELTMESİ: Bu satır eklendi.
using System.Linq;
using System.Collections.Generic;
using Firebase.Firestore;
using System;

/// <summary>
/// Bu sınıfın çalışması için "Assets/Scripts/Editor" klasöründe olması gerekir.
/// </summary>
public class PlanetTypeUploader
{
    // Bu satır, Unity'nin üst menüsüne yeni bir seçenek ekler.
    [MenuItem("Astro-Trader/Tools/Upload Planet Types to Firestore")]
    public static async void UploadPlanetTypes()
    {
        // Firebase'in hazır olduğundan emin olmalıyız. Bu yüzden bu aracı
        // sadece "Play Mode"dayken çalıştırmak en güvenlisidir.
        if (!Application.isPlaying || FirebaseManager.instance == null || !FirebaseManager.instance.IsInitialized)
        {
            Debug.LogError("Bu aracı kullanmak için lütfen Play Mode'a girin ve Firebase'in başlatılmasını bekleyin.");
            return;
        }

        Debug.Log("Uploading all PlanetType assets to Firestore...");

        try
        {
            FirebaseFirestore db = FirebaseManager.instance.db;

            // Projedeki tüm PlanetType asset'lerini bul.
            string[] guids = AssetDatabase.FindAssets("t:PlanetType");
            if (guids.Length == 0)
            {
                Debug.LogWarning("Projede hiç PlanetType asset'i bulunamadı.");
                return;
            }

            int successCount = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PlanetType planetType = AssetDatabase.LoadAssetAtPath<PlanetType>(path);

                if (planetType == null || string.IsNullOrEmpty(planetType.planetName))
                {
                    Debug.LogWarning($"Asset at path '{path}' is invalid or has no planet name. Skipping.");
                    continue;
                }

                // Sunucunun beklediği veri yapısını oluştur.
                var planetData = new Dictionary<string, object>
                {
                    // HATA DÜZELTİLDİ: Sadece null olmayan ItemData'ları ve onların isimlerini al.
                    // Bu, bozuk veya eksik asset referanslarının sunucuya "null" olarak gitmesini engeller.
                    { "producedItems", planetType.producedItems.Where(item => item != null && !string.IsNullOrEmpty(item.itemName)).Select(item => item.itemName).ToList() },
                    { "consumedItems", planetType.consumedItems.Where(item => item != null && !string.IsNullOrEmpty(item.itemName)).Select(item => item.itemName).ToList() }
                };

                // Veriyi Firestore'a yaz. Döküman ID'si olarak gezegenin adını kullanıyoruz.
                // SetAsync, döküman yoksa oluşturur, varsa üzerine yazar.
                await db.Collection("planet_types").Document(planetType.planetName).SetAsync(planetData);
                
                Debug.Log($"Successfully uploaded '{planetType.planetName}'.");
                successCount++;
            }

            Debug.Log($"Upload complete! {successCount}/{guids.Length} planet types were successfully uploaded.");
        }
        catch (Exception e)
        {
            Debug.LogError($"An error occurred during upload: {e.Message}");
        }
    }
}
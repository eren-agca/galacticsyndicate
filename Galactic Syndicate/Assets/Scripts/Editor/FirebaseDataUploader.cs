// FirebaseDataUploader.cs - DOĞRU EDİTÖR SCRİPTİ

using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Firebase.Firestore;
using System.Threading.Tasks;

public class FirebaseDataUploader
{
    // Bu [MenuItem] niteliği, Unity'nin üst menüsüne yeni bir seçenek ekler.
    [MenuItem("Galactic Syndicate/Upload Game Definitions to Firebase")]
    public static async void UploadGameDefinitions()
    {
        if (!EditorUtility.DisplayDialog("Confirm Upload",
            "This will overwrite 'game_definitions' in Firestore with data from your local ScriptableObjects. This is needed for the scheduled news function. Are you sure?",
            "Yes, Upload", "Cancel"))
        {
            return;
        }

        Debug.Log("Starting upload of game definitions...");

        try
        {
            // Editörde Firebase'in başlatıldığından emin olmak için oyunu en az bir kez çalıştırmış olmanız gerekebilir.
            var db = FirebaseFirestore.DefaultInstance;
            if (db == null)
            {
                Debug.LogError("Firestore instance is not available. Is Firebase configured correctly and have you run the game in the editor at least once?");
                return;
            }

            // 1. Tüm ItemData ScriptableObject'larının isimlerini yükle
            var allItems = Resources.LoadAll<ItemData>("");
            var itemNames = allItems.Select(item => item.itemName).ToList();
            var itemsDocRef = db.Collection("game_definitions").Document("items");
            await itemsDocRef.SetAsync(new Dictionary<string, object> { { "allItemNames", itemNames } });
            Debug.Log($"Successfully uploaded {itemNames.Count} item names.");

            // 2. Tüm PlanetType ScriptableObject'larının isimlerini yükle
            var allPlanetTypes = Resources.LoadAll<PlanetType>("");
            var planetNames = allPlanetTypes.Select(pt => pt.planetName).ToList();
            var planetsDocRef = db.Collection("game_definitions").Document("planets");
            await planetsDocRef.SetAsync(new Dictionary<string, object> { { "allPlanetNames", planetNames } });
            Debug.Log($"Successfully uploaded {planetNames.Count} planet names.");

            EditorUtility.DisplayDialog("Upload Complete", "Game definitions have been successfully uploaded to Firestore.", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"An error occurred during upload: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("Upload Failed", $"An error occurred: {e.Message}", "OK");
        }
    }
}

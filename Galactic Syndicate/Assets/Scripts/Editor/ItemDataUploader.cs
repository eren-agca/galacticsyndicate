// Bu yeni bir dosyadır: Assets/Scripts/Editor/ItemDataUploader.cs

using UnityEngine;
using UnityEditor; // HATA DÜZELTMESİ: Bu satır eklendi.
using Firebase.Firestore;
using System.Threading.Tasks;

/// <summary>
/// Bu sınıf, Unity Editor'e özel menü seçenekleri ekleyerek
/// ScriptableObject verilerini Firestore'a yüklemek için kullanılır.
/// Bu scriptin çalışması için "Assets/Scripts/Editor" klasöründe olması gerekir.
/// </summary>
public class ItemDataUploader
{
    // Bu nitelik, Unity'nin üst menüsüne yeni bir buton ekler.
    [MenuItem("Astro-Trader/Tools/Upload All ItemData to Firestore")]
    public static async void UploadAllItems()
    {
        Debug.Log("Starting ItemData upload to Firestore...");

        // Resources klasöründeki tüm ItemData asset'lerini bul.
        ItemData[] allItems = Resources.LoadAll<ItemData>("");
        if (allItems.Length == 0)
        {
            Debug.LogError("No ItemData found in Resources folder. Aborting upload.");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        WriteBatch batch = db.StartBatch();

        foreach (var item in allItems)
        {
            if (string.IsNullOrEmpty(item.itemName))
            {
                Debug.LogWarning($"Skipping an ItemData asset with no name: {item.name}", item);
                continue;
            }

            // 'items' koleksiyonuna, asset'in adıyla bir döküman oluştur.
            DocumentReference docRef = db.Collection("items").Document(item.itemName);
            var data = new { baseValue = item.baseValue };
            batch.Set(docRef, data);
        }

        try
        {
            await batch.CommitAsync();
            Debug.Log($"<color=green>Successfully uploaded {allItems.Length} items to Firestore.</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to upload items: {e.Message}");
        }
    }
}
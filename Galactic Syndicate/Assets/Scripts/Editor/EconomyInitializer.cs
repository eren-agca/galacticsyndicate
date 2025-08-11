// Bu yeni bir dosyadır: Assets/Scripts/Editor/EconomyInitializer.cs

using UnityEngine;
using UnityEditor; // HATA DÜZELTMESİ: Bu satır eklendi.
using System.Collections.Generic;
using Firebase.Firestore;
using System;
using System.Threading.Tasks;

/// <summary>
/// Bu sınıf, Unity Editor'e özel menü seçenekleri ekleyerek
/// tüm gezegenlerin ekonomilerini Firestore'da başlatmak için kullanılır.
/// Bu scriptin çalışması için "Assets/Scripts/Editor" klasöründe olması gerekir.
/// </summary>
public class EconomyInitializer
{
    [MenuItem("Astro-Trader/Tools/Initialize All Economies in Firestore")]
    public static async void InitializeAllEconomies()
    {
        if (!Application.isPlaying || FirebaseManager.instance == null || !FirebaseManager.instance.IsInitialized)
        {
            Debug.LogError("Bu aracı kullanmak için lütfen Play Mode'a girin ve Firebase'in başlatılmasını bekleyin.");
            return;
        }

        Debug.Log("Starting to initialize all planet economies in Firestore...");

        try
        {
            FirebaseFirestore db = FirebaseManager.instance.db;
            string[] guids = AssetDatabase.FindAssets("t:PlanetType");
            WriteBatch batch = db.StartBatch();

            // --- YENİ: Tüm ItemData'ları bir kere yükle ---
            ItemData[] allItems = Resources.LoadAll<ItemData>("");
            if (allItems.Length == 0)
            {
                Debug.LogError("Hiç ItemData asset'i bulunamadı. Lütfen ürünleri Resources klasörüne ekleyin.");
                return;
            }
            // -------------------------------------------

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PlanetType planetType = AssetDatabase.LoadAssetAtPath<PlanetType>(path);

                if (planetType != null && !string.IsNullOrEmpty(planetType.planetName))
                {
                    DocumentReference docRef = db.Collection("economies").Document(planetType.planetName);
                    
                    // --- GÜNCELLENMİŞ MANTIK: Ekonomiyi doğru verilerle doldur ---
                    var itemsMap = new Dictionary<string, object>();

                    foreach (var itemData in allItems)
                    {
                        // --- YENİ GÜVENLİK KONTROLÜ ---
                        // Eğer bir ItemData'nın ismi boşsa, bu hatalı bir konfigürasyondur.
                        // Hata verip işlemi durdurmak yerine, bu ürünü atlayıp devam ediyoruz.
                        if (string.IsNullOrEmpty(itemData.itemName))
                        {
                            Debug.LogWarning($"Skipping an ItemData asset with no name: {itemData.name}", itemData);
                            continue;
                        }

                        // Varsayılan arz ve talep değerleri
                        int supply = 50;
                        int demand = 50;

                        // Eğer gezegen bu ürünü üretiyorsa, arz yüksek, talep düşük olur.
                        if (Array.Exists(planetType.producedItems, item => item == itemData))
                        {
                            supply = 100;
                            demand = 25;
                        }
                        // Eğer gezegen bu ürünü tüketiyorsa, arz düşük, talep yüksek olur.
                        else if (Array.Exists(planetType.consumedItems, item => item == itemData))
                        {
                            supply = 25;
                            demand = 100;
                        }
                        
                        var economyItem = new EconomyItemData { Supply = supply, Demand = demand };
                        itemsMap[itemData.itemName] = economyItem;
                    }

                    var initialEconomyData = new Dictionary<string, object> { { "Items", itemsMap } };
                    // ----------------------------------------------------------------
                    
                    Debug.Log($"[EconomyInitializer] Preparing to set economy for '{planetType.planetName}' with {itemsMap.Count} items.");
                    // SetOptions.MergeAll kullanmak, döküman zaten varsa diğer alanları silmeden
                    // sadece "Items" alanını günceller. Bu, daha güvenli bir yöntemdir.
                    batch.Set(docRef, initialEconomyData, SetOptions.MergeAll);
                }
            }

            await batch.CommitAsync();
            Debug.Log($"<color=green>Successfully initialized or merged {guids.Length} economy documents with full item data.</color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"An error occurred during economy initialization: {e.Message}");
        }
    }
}
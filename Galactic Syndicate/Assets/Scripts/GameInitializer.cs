// GameInitializer.cs - NİHAİ VE HATASIZ HALİ

using UnityEngine;
using System.Threading.Tasks;

public class GameInitializer : MonoBehaviour
{
    // Start metodunu "async void" yapıyoruz çünkü içinde bekleme gerektiren işlemler var.
    async void Start()
    {
        // --- YENİ GÜVENLİK KONTROLÜ ---
        // Gerekli yöneticilerin (manager) hazır olmasını bekle.
        // Bu, NullReferenceException hatalarını önler.
        await WaitForManagers();
        // --------------------------------

        // SaveManager'dan verileri yüklemesini iste ve işlemin bitmesini bekle.
        bool hasSaveData = await SaveManager.instance.LoadGame();

        // Eğer yüklenecek bir kayıt dosyası YOKSA, yeni bir galaksi oluştur.
        // Eğer kayıt dosyası VARSA, LoadGame metodu zaten doğru galaksiyi oluşturmuştur.
        if (!hasSaveData)
        {
            GalaxyGenerator galaxyGen = FindObjectOfType<GalaxyGenerator>();
            if (galaxyGen != null)
            {
                galaxyGen.GenerateNewGalaxy();
            }
            else
            {
                Debug.LogError("GameInitializer could not find a GalaxyGenerator in the scene!");
            }

            // YENİ EKLENEN SATIR: Yeni bir oyun başladığında, oyuncunun veritabanında
            // hemen görünür olması için bir ilk kayıt oluştur.
            _ = SaveManager.instance.RequestSave();
        }
    }

    /// <summary>
    /// Gerekli tüm yönetici singleton'larının yüklenip hazır hale gelmesini bekler.
    /// </summary>
    private async Task WaitForManagers()
    {
        Debug.Log("GameInitializer is waiting for managers to be ready...");
        
        // FirebaseManager ve SaveManager'ın hazır olmasını bekle.
        // FirebaseManager'ın IsInitialized bayrağının true olmasını da kontrol ediyoruz.
        while (SaveManager.instance == null || FirebaseManager.instance == null || !FirebaseManager.instance.IsInitialized)
        {
            // Henüz hazır değillerse, bir sonraki frame'e kadar bekle ve döngüye devam et.
            await Task.Yield(); 
        }

        Debug.Log("All managers are ready. Proceeding with game initialization.");
    }
}
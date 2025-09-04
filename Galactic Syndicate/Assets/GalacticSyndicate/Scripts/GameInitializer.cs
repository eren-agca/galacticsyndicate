// GameInitializer.cs - TEK OYUNCULU VERSİYON

using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    void Start()
    {
        // SaveManager'dan verileri yüklemesini iste.
        bool hasSaveData = SaveManager.instance.LoadGame();

        // Eğer kayıt dosyası yoksa, yeni bir galaksi oluştur.
        // Varsa, LoadGame metodu zaten doğru galaksiyi oluşturmuştur.
        if (!hasSaveData)
        {
            GalaxyGenerator galaxyGen = FindObjectOfType<GalaxyGenerator>();
            if (galaxyGen != null)
            {
                galaxyGen.GenerateNewGalaxy();
                // Yeni oyun başladığında, oyuncunun veritabanında
                // hemen görünür olması için bir ilk kayıt oluştur.
                SaveManager.instance.SaveGame();
            }
            else
            {
                Debug.LogError("GameInitializer could not find a GalaxyGenerator in the scene!");
            }
        }
    }
}
// GhostPlayerManager.cs (YENİ SCRİPT - TAMAMLANMIŞ HALİ)
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Functions;
using System.Linq;

/// <summary>
/// Diğer oyuncuların "hayaletlerini" periyodik olarak sunucudan çeker ve sahnede yönetir.
/// </summary>
public class GhostPlayerManager : MonoBehaviour
{
    public static GhostPlayerManager instance;

    [Header("Ayarlar")]
    [Tooltip("Hayalet oyuncu verilerini sunucudan ne sıklıkla çekeceğimiz (saniye).")]
    [SerializeField] private float updateInterval = 5f;
    [Tooltip("Hayalet oyuncuyu temsil edecek olan prefab.")]
    [SerializeField] private GameObject ghostPrefab;

    private Dictionary<string, GhostPlayerController> activeGhosts = new Dictionary<string, GhostPlayerController>();
    private bool isRunning = false;

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

    void Start()
    {
        // Sadece ana oyun sahnesinde çalışmasını sağla (PlayerStats varsa)
        if (PlayerStats.instance != null)
        {
            StartGhostSystem();
        }
    }

    public void StartGhostSystem()
    {
        if (isRunning) return;
        isRunning = true;
        StartCoroutine(UpdateGhostsCoroutine());
        Debug.Log("[GhostManager] Hayalet sistemi başlatıldı.");
    }

    private IEnumerator UpdateGhostsCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);
            if (FirebaseManager.instance != null && FirebaseManager.instance.IsInitialized)
            {
                // HATA DÜZELTMESİ: Bir Coroutine içinde doğrudan 'await' kullanılamaz.
                // Görevi sadece başlatıp arka planda çalışmasına izin veriyoruz.
                _ = FetchAndProcessGhosts();
            }
        }
    }

    private async Task FetchAndProcessGhosts()
    {
        try
        {
            var functions = FirebaseFunctions.DefaultInstance;
            var function = functions.GetHttpsCallable("getGhostPlayers");
            var result = await function.CallAsync();
            // HATA DÜZELTMESİ: Bağımlılığı kaldırmak için SyndicateManager yerine
            // PlayerProfileManager'daki genel ayrıştırıcıyı kullanıyoruz.
            // Bu, kodun daha temiz ve modüler olmasını sağlar.
            var resultDict = PlayerProfileManager.instance.ParseFunctionResult(result.Data);

            if (resultDict != null && resultDict.TryGetValue("success", out var successVal) && (bool)successVal)
            {
                if (resultDict.TryGetValue("players", out object listObj) && listObj is IList<object> ghostList)
                {
                    ProcessGhostData(ghostList);
                }
            }
        }
        catch (System.Exception e)
        {
            // Bu kritik bir hata değil, sadece bir uyarı olmalı.
            Debug.LogWarning($"[GhostManager] Hayalet verisi çekilirken hata: {e.Message}");
        }
    }


    private void ProcessGhostData(IList<object> ghostDataList)
    {
        var receivedGhostUids = new HashSet<string>();

        // HATA DÜZELTMESİ: Döngü değişkeni 'playerList' yerine 'ghostDataList' olmalı.
        foreach (var item in ghostDataList)
        {
            // --- YENİ GÜVENLİK VE HATA AYIKLAMA ADIMI ---
            // Her bir oyuncunun verisini işlerken hata oluşursa, döngünün
            // tamamının çökmesini engellemek için try-catch bloğu ekliyoruz.
            // Bu, bozuk veriye sahip bir oyuncunun diğerlerini etkilemesini önler.
            string uid = "N/A";
            try
            {
                var dict = PlayerProfileManager.instance.ParseFunctionResult(item);
                if (dict == null || !dict.ContainsKey("uid"))
                {
                    Debug.LogWarning("[GhostManager] Gelen oyuncu verisi geçersiz veya UID içermiyor. Atlanıyor.");
                    continue; // Bu oyuncuyu atla ve bir sonrakine geç.
                }

                uid = dict["uid"].ToString();
                receivedGhostUids.Add(uid);

                string username = dict["username"].ToString();

                // --- NİHAİ ÇÖZÜM ---
                // Teşhis logu, 'position' verisinin Dictionary<object, object> olarak geldiğini,
                // ParseFunctionResult ile başarıyla Dictionary<string, object>'e çevrildiğini
                // ve anahtarların '_latitude' ve '_longitude' olduğunu kesin olarak doğruladı.
                // Artık bu bilgiye dayanarak veriyi doğrudan işleyebiliriz.
                object posObj;
                dict.TryGetValue("position", out posObj);

                var positionDict = PlayerProfileManager.instance.ParseFunctionResult(posObj);
                var rotData = dict["rotation"] as List<object>;

                // Veri doğrulama: Teşhis logunda gördüğümüz '_latitude' ve '_longitude' anahtarlarını kullanıyoruz.
                if (positionDict == null || !positionDict.ContainsKey("_latitude") || !positionDict.ContainsKey("_longitude") || rotData == null || rotData.Count != 4)
                {
                    Debug.LogWarning($"[GhostManager] Oyuncu {uid} için konum/rotasyon verisi bozuk veya anlaşılamadı. Atlanıyor.");
                    continue;
                }

                // Sözlükten enlem ve boylamı alıyoruz. System.Convert.ToSingle, double'dan float'a güvenli çevrim yapar.
                Vector3 position = new Vector3(
                    System.Convert.ToSingle(positionDict["_latitude"]),
                    System.Convert.ToSingle(positionDict["_longitude"]),
                    0);

                Quaternion rotation = new Quaternion(
                    System.Convert.ToSingle(rotData[0]),
                    System.Convert.ToSingle(rotData[1]),
                    System.Convert.ToSingle(rotData[2]),
                    System.Convert.ToSingle(rotData[3]));

                if (activeGhosts.TryGetValue(uid, out GhostPlayerController existingGhost))
                {
                    existingGhost.UpdateData(position, rotation);
                }
                else
                {
                    GameObject newGhostGO = Instantiate(ghostPrefab, position, rotation);
                    GhostPlayerController newGhost = newGhostGO.GetComponent<GhostPlayerController>();
                    newGhost.Initialize(username, position, rotation);
                    activeGhosts.Add(uid, newGhost);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GhostManager] Oyuncu verisi işlenirken hata oluştu (UID: {uid}). Hata: {e.Message}\nStack Trace: {e.StackTrace}");
            }
        }

        var uidsToRemove = activeGhosts.Keys.Where(uid => !receivedGhostUids.Contains(uid)).ToList();
        foreach (var uid in uidsToRemove)
        {
            if (activeGhosts.TryGetValue(uid, out GhostPlayerController ghostToRemove))
            {
                ghostToRemove.FadeOutAndDestroy();
            }
            activeGhosts.Remove(uid);
        }
    }
}
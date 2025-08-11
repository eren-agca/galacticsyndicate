// LeaderboardManager.cs - NİHAİ VE HATASIZ HALİ

using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Functions;
using System;
using System.Linq;

public class LeaderboardEntry
{
    public string Name;
    public long Value;
}

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager instance;

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

    /// <summary>
    /// Belirtilen türdeki liderlik tablosu verilerini sunucudan çeker.
    /// Başarılı olursa liste, başarısız olursa null döndürür.
    /// </summary>
    public async Task<List<LeaderboardEntry>> GetLeaderboardData(string boardType)
    {
        // Bu kontroller, bir geliştirici hatası (sahnede eksik yönetici) olduğunu belirtir.
        // Bu durumda bir istisna fırlatmak, sorunun kaynağını anında bulmayı sağlar.
        if (FirebaseManager.instance == null || !FirebaseManager.instance.IsInitialized)
        {
            throw new InvalidOperationException("LeaderboardManager cannot fetch data: Firebase is not initialized yet.");
        }
        if (SyndicateManager.instance == null)
        {
            throw new InvalidOperationException("LeaderboardManager cannot fetch data: SyndicateManager.instance is not available.");
        }

        try
        {
            var functions = FirebaseFunctions.DefaultInstance;
            var function = functions.GetHttpsCallable("getLeaderboards");

            var data = new Dictionary<string, object>
            {
                { "boardType", boardType },
                { "limit", 50 }
            };

            var result = await function.CallAsync(data);
            var resultDict = SyndicateManager.instance.ParseFunctionResult(result.Data);

            if (resultDict != null && resultDict.TryGetValue("success", out var successVal) && (bool)successVal)
            {
                var entries = new List<LeaderboardEntry>();
                if (resultDict.TryGetValue("leaderboard", out object listObj) && listObj is IList<object> leaderboardList)
                {
                    foreach (var item in leaderboardList)
                    {
                        var entryDict = SyndicateManager.instance.ParseFunctionResult(item);
                        if (entryDict != null)
                        {
                            entries.Add(new LeaderboardEntry
                            {
                                Name = entryDict["name"]?.ToString(),
                                Value = Convert.ToInt64(entryDict["value"])
                            });
                        }
                    }
                }
                return entries; // Başarılı: Veri listesini döndür.
            }
            else
            {
                // Sunucu "başarısız" dediğinde.
                string serverMessage = resultDict?["message"]?.ToString() ?? "Unknown server error, success was false.";
                Debug.LogWarning($"Leaderboard fetch was not successful according to server: {serverMessage}");
                return null; // Başarısız: null döndür.
            }
        }
        catch (Exception e)
        {
            // Ağ hatası veya başka bir beklenmedik hata durumunda.
            Debug.LogError($"Exception while getting leaderboard for '{boardType}': {e.Message}\n{e.StackTrace}");
            return null; // Başarısız: null döndür.
        }
    }
}
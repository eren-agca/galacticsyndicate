// SyndicateManager.cs - NİHAİ HALİ

using UnityEngine;
using Firebase.Firestore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using Firebase.Storage;
using Firebase.Functions;

public class SyndicateManager : MonoBehaviour
{
    public static SyndicateManager instance;

    // Sendika tarayıcısında gösterilecek veriyi tutan sınıf
    public class PublicSyndicateInfo
    {
        public string Id;
        public string Name;
        public string Tag;
        public int MemberCount;
    }

    public class SyndicateMemberInfo
    {
        public string Uid;
        public string Name;
        public bool IsLeader;
        public string ProfilePictureUrl;
    }

    public SyndicateData CurrentSyndicate { get; private set; }
    public string CurrentSyndicateId { get; private set; }

    public event Action OnSyndicateDataUpdated;

    private FirebaseFirestore db;
    private bool isBusy = false;
    private bool isInitialized = false;

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

    async void Start()
    {
        while (FirebaseManager.instance == null || !FirebaseManager.instance.IsInitialized)
        {
            await Task.Yield();
        }

        db = FirebaseManager.instance.db;
        isInitialized = true;
        Debug.Log("SyndicateManager initialized successfully.");
    }

    public async Task FetchPlayerSyndicateData(string syndicateId)
    {
        if (!isInitialized) return;

        if (string.IsNullOrEmpty(syndicateId))
        {
            CurrentSyndicate = null;
            CurrentSyndicateId = null;
            OnSyndicateDataUpdated?.Invoke();
            return;
        }

        DocumentReference docRef = db.Collection("syndicates").Document(syndicateId);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            CurrentSyndicate = snapshot.ConvertTo<SyndicateData>();
            CurrentSyndicateId = snapshot.Id;
            Debug.Log($"[SyndicateManager] Syndicate data for '{CurrentSyndicate.SyndicateName}' refreshed. New Treasury: {CurrentSyndicate.Treasury}");
        }
        else
        {
            Debug.LogWarning($"Player is assigned to a non-existent syndicate (ID: {syndicateId}). Clearing assignment.");
            CurrentSyndicate = null;
            CurrentSyndicateId = null;
        }
        OnSyndicateDataUpdated?.Invoke();
    }

    public async Task RefreshCurrentSyndicateData()
    {
        if (!isInitialized || string.IsNullOrEmpty(CurrentSyndicateId))
        {
            Debug.LogWarning("[SyndicateManager] Cannot refresh data: not in a syndicate or not initialized.");
            return;
        }
        await FetchPlayerSyndicateData(CurrentSyndicateId);
    }

    public async Task<bool> CreateSyndicate(string syndicateName, string tag, string description)
    {
        if (!isInitialized || isBusy || CurrentSyndicate != null)
        {
            Debug.LogError("İşlem yapılamıyor: Yönetici hazır değil, zaten bir sendikadasınız veya başka bir işlem devam ediyor.");
            return false;
        }

        isBusy = true;
        
        try
        {
            var functions = FirebaseFunctions.DefaultInstance;
            var function = functions.GetHttpsCallable("createSyndicate");

            var data = new Dictionary<string, object>
            {
                { "name", syndicateName },
                { "tag", tag },
                { "description", description }
            };

            var result = await function.CallAsync(data);
            var resultDict = ParseFunctionResult(result.Data);

            if (resultDict != null && resultDict.TryGetValue("success", out var successVal) && (bool)successVal)
            {
                // Sunucudan gelen veriyi ayrıştır ve lokal durumu güncelle
                var syndicateId = resultDict["syndicateId"].ToString();
                var syndicateDataDict = ParseFunctionResult(resultDict["syndicateData"]);

                if (syndicateDataDict != null)
                {
                    CurrentSyndicate = new SyndicateData
                    {
                        SyndicateName = syndicateDataDict["SyndicateName"].ToString(),
                        Tag = syndicateDataDict["Tag"].ToString(),
                        Description = syndicateDataDict["Description"].ToString(),
                        LeaderID = syndicateDataDict["LeaderID"].ToString(),
                        MemberIDs = (syndicateDataDict["MemberIDs"] as List<object>).Select(o => o.ToString()).ToList(),
                        Treasury = Convert.ToInt32(syndicateDataDict["Treasury"]),
                        TradeBuffLevel = Convert.ToInt32(syndicateDataDict["TradeBuffLevel"]),
                        EmblemURL = syndicateDataDict["EmblemURL"]?.ToString() ?? ""
                    };
                    CurrentSyndicateId = syndicateId;

                    OnSyndicateDataUpdated?.Invoke();
                    Debug.Log($"Sendika '{syndicateName}' başarıyla oluşturuldu! (Sunucu tarafından)");
                    
                    AnalyticsManager.instance?.LogEvent("syndicate_created", new Dictionary<string, object>
                    {
                        { "syndicate_name", syndicateName }
                    });
                    return true;
                }
            }
            
            string errorMessage = resultDict?["message"]?.ToString() ?? "Bilinmeyen sunucu hatası.";
            Debug.LogError($"Sendika oluşturulurken sunucu hatası: {errorMessage}");
            UIManager.instance?.ShowNotification($"Hata: {errorMessage}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Sendika oluşturulurken istemci hatası: {e.Message}");
            UIManager.instance?.ShowNotification("Ağ Hatası!");
            return false;
        }
        finally
        {
            isBusy = false;
        }
    }

    public async Task<bool> LeaveSyndicate()
    {
        if (!isInitialized || isBusy || CurrentSyndicate == null)
        {
            Debug.LogError("Bir sendikada değilsiniz veya başka bir işlem devam ediyor.");
            return false;
        }

        isBusy = true;
        string playerId = FirebaseManager.instance.UserID;
        string syndicateId = CurrentSyndicateId;
        string syndicateName = CurrentSyndicate.SyndicateName;

        try
        {
            var functions = FirebaseFunctions.DefaultInstance;
            var function = functions.GetHttpsCallable("leaveSyndicate");
            await function.CallAsync();

            CurrentSyndicate = null;
            CurrentSyndicateId = null;

            OnSyndicateDataUpdated?.Invoke();
            Debug.Log("Sendikadan başarıyla ayrıldınız.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Sendikadan ayrılırken hata: {e.Message}");
            return false;
        }
        finally
        {
            isBusy = false;
        }
    }
    
    public async Task AddCreditsToTreasury(int amount)
    {
        if (!isInitialized || CurrentSyndicate == null || string.IsNullOrEmpty(CurrentSyndicateId) || amount <= 0)
        {
            return;
        }

        try
        {
            DocumentReference syndicateDocRef = db.Collection("syndicates").Document(CurrentSyndicateId);
            await syndicateDocRef.UpdateAsync("Treasury", FieldValue.Increment(amount));
            CurrentSyndicate.Treasury += amount;
            OnSyndicateDataUpdated?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Sendika hazinesi güncellenirken hata: {e.Message}");
        }
    }

    public async Task<List<PublicSyndicateInfo>> FindPublicSyndicates()
    {
        if (!isInitialized) return new List<PublicSyndicateInfo>();

        try
        {
            var functions = FirebaseFunctions.DefaultInstance;
            var function = functions.GetHttpsCallable("getPublicSyndicates");

            var result = await function.CallAsync();
            var resultDict = ParseFunctionResult(result.Data);

            if (resultDict != null && resultDict.TryGetValue("success", out var successVal) && (bool)successVal)
            {
                var syndicates = new List<PublicSyndicateInfo>();
                if (resultDict.TryGetValue("syndicates", out object listObj) && listObj is IList<object> syndicateList)
                {
                    foreach (var item in syndicateList)
                    {
                        var dict = ParseFunctionResult(item);
                        if (dict != null)
                        {
                            syndicates.Add(new PublicSyndicateInfo
                            {
                                Id = dict["id"]?.ToString(),
                                Name = dict["name"]?.ToString(),
                                Tag = dict["tag"]?.ToString(),
                                MemberCount = Convert.ToInt32(dict["memberCount"])
                            });
                        }
                    }
                }
                return syndicates;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Sendika listesi çekilirken hata: {e.Message}");
        }
        return new List<PublicSyndicateInfo>();
    }

    public async Task<bool> JoinSyndicate(string syndicateId)
    {
        if (!isInitialized || isBusy) return false;
        isBusy = true;
        try
        {
            var functions = FirebaseFunctions.DefaultInstance;
            var function = functions.GetHttpsCallable("joinSyndicate");
            var data = new Dictionary<string, object> { { "syndicateId", syndicateId } };
            var result = await function.CallAsync(data);
            var resultDict = ParseFunctionResult(result.Data);

            if (resultDict != null && resultDict.TryGetValue("success", out var successVal) && (bool)successVal)
            {
                await FetchPlayerSyndicateData(syndicateId);

                AnalyticsManager.instance?.LogEvent("syndicate_joined", new Dictionary<string, object>
                {
                    { "syndicate_id", syndicateId }
                });
                return true;
            }
            string errorMessage = resultDict?["message"]?.ToString() ?? "Bilinmeyen hata";
            Debug.LogError($"Sendikaya katılamadı: {errorMessage}");
            UIManager.instance?.ShowNotification($"Hata: {errorMessage}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Sendikaya katılırken hata: {e.Message}");
            UIManager.instance?.ShowNotification("Ağ Hatası!");
            return false;
        }
        finally { isBusy = false; }
    }
    
    public async Task<List<SyndicateMemberInfo>> GetSyndicateMembers()
    {
        if (!isInitialized || string.IsNullOrEmpty(CurrentSyndicateId))
        {
            return new List<SyndicateMemberInfo>();
        }

        try
        {
            var functions = FirebaseFunctions.DefaultInstance;
            var function = functions.GetHttpsCallable("getSyndicateMembers");
            var data = new Dictionary<string, object> { { "syndicateId", CurrentSyndicateId } };
            var result = await function.CallAsync(data);
            var resultDict = ParseFunctionResult(result.Data);

            if (resultDict != null && resultDict.TryGetValue("success", out var successVal) && (bool)successVal)
            {
                var members = new List<SyndicateMemberInfo>();
                if (resultDict.TryGetValue("members", out object listObj) && listObj is IList<object> memberList)
                {
                    foreach (var item in memberList)
                    {
                        var dict = ParseFunctionResult(item);
                        if (dict != null)
                        {
                            members.Add(new SyndicateMemberInfo
                            {
                                Uid = dict["uid"]?.ToString(),
                                Name = dict["name"]?.ToString(),
                                IsLeader = Convert.ToBoolean(dict["isLeader"]),
                                ProfilePictureUrl = dict["profilePictureUrl"]?.ToString()
                            });
                        }
                    }
                }
                return members;
            }
            else
            {
                Debug.LogError($"Could not get members: {resultDict?["message"]?.ToString() ?? "Unknown error"}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting syndicate members: {e.Message}");
        }
        return new List<SyndicateMemberInfo>();
    }

    public async Task<bool> UploadSyndicateEmblem(string localFilePath)
    {
        if (!isInitialized || isBusy || CurrentSyndicate == null || CurrentSyndicate.LeaderID != FirebaseManager.instance.UserID)
        {
            Debug.LogError("Cannot upload emblem: Not leader, not in a syndicate, or another operation is busy.");
            return false;
        }

        isBusy = true;
        try
        {
            FirebaseStorage storage = FirebaseStorage.DefaultInstance;
            string storagePath = $"syndicate_emblems/{CurrentSyndicateId}.jpg";
            StorageReference emblemRef = storage.GetReference(storagePath);

            string prefixedPath = "file://" + localFilePath;
            Debug.Log($"Uploading emblem to: {storagePath} from local path: {prefixedPath}");

            await emblemRef.PutFileAsync(prefixedPath);

            Uri downloadUri = await emblemRef.GetDownloadUrlAsync();
            Debug.Log($"Upload finished. Download URL: {downloadUri}");

            DocumentReference syndicateDocRef = db.Collection("syndicates").Document(CurrentSyndicateId);
            await syndicateDocRef.UpdateAsync("EmblemURL", downloadUri.ToString());

            await RefreshCurrentSyndicateData();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Emblem upload failed: {e.Message}");
            return false;
        }
        finally { isBusy = false; }
    }

    public IDictionary<string, object> ParseFunctionResult(object data)
    {
        if (data == null) return null;
        if (data is IDictionary<string, object> directDict) return directDict;
        if (data is IDictionary<object, object> objectDict)
        {
            return objectDict.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
        }
        Debug.LogWarning($"Could not parse function result of type {data.GetType()}");
        return null;
    }
}
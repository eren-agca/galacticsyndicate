using UnityEngine;
using System.Threading.Tasks;
using Firebase.Functions;
using System.Collections.Generic;
using System;
using Firebase.Storage;
using System.IO;
using System.Linq;
using Firebase.Firestore;
using Firebase;
using Google;
using Firebase.Auth;

public class PlayerProfileManager : MonoBehaviour
{
    public static PlayerProfileManager instance;

    [Header("Google Sign-In Ayarları")]
    [SerializeField] private string webClientId = "YOUR-WEB-CLIENT-ID-HERE";

    public string PlayerUsername { get; private set; }
    public string ProfilePictureURL { get; private set; }

    public event Action OnProfileUpdated;

    private bool isBusy = false;
    private FirebaseAuth auth;

    /// <summary>
    /// Mevcut kullanıcının misafir (anonymous) olup olmadığını kontrol eder.
    /// </summary>
    public bool IsGuestUser => auth != null && auth.CurrentUser != null && auth.CurrentUser.IsAnonymous;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Firebase hazır olduğunda Auth servisini başlat
        InitializeAuthService();
    }

    private async void InitializeAuthService()
    {
        // Firebase'in tamamen hazır olmasını bekle
        int maxWaitTime = 10; // 10 saniye maksimum bekleme
        int waited = 0;
        
        while (FirebaseManager.instance == null || !FirebaseManager.instance.IsInitialized)
        {
            if (waited >= maxWaitTime)
            {
                Debug.LogError("Firebase initialization timeout in PlayerProfileManager");
                return;
            }
            await Task.Delay(1000);
            waited++;
        }

        auth = FirebaseAuth.DefaultInstance;
        
        // --- KESİN ÇÖZÜM: Google Sign-In yapılandırmasını daha sağlam ve daha bilgilendirici hale getir ---
        // Bu, "hiçbir şey olmuyor" hatasının en yaygın sebebini (eksik Web Client ID) tespit eder.
        if (string.IsNullOrEmpty(webClientId) || webClientId.Contains("YOUR-WEB-CLIENT-ID-HERE"))
        {
            // Hata mesajı, geliştiriciye tam olarak ne yapması gerektiğini söyler.
            Debug.LogError("!!! KRİTİK YAPILANDIRMA HATASI !!!\n'Web Client Id' alanı, Unity Editor'deki 'PlayerProfileManager' bileşeninde ayarlanmamış. Google ile giriş bu olmadan ÇALIŞAMAZ. Lütfen sahnenizdeki 'PlayerProfileManager' objesini bulun ve Firebase konsolundan aldığınız Web Client ID'yi ilgili alana yapıştırın.");
        }
        else
        {
            Debug.Log($"[PlayerProfileManager] GoogleSignIn yapılandırması şu Web Client ID ile yapılıyor: {webClientId}");
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                RequestIdToken = true,
                WebClientId = webClientId
            };
        }

        Debug.Log("PlayerProfileManager Auth service initialized");
    }

    /// <summary>
    /// Oyuncu verisi yüklendiğinde veya yeni oyun başladığında çağrılır.
    /// </summary>
    public async Task FetchUserProfile()
    {
        if (auth == null || auth.CurrentUser == null)
        {
            Debug.LogError("[FetchUserProfile] Auth service or CurrentUser is null");
            PlayerUsername = "Misafir";
            ProfilePictureURL = null;
            OnProfileUpdated?.Invoke();
            return;
        }

        if (FirebaseManager.instance == null || FirebaseManager.instance.db == null)
        {
            Debug.LogError("[FetchUserProfile] FirebaseManager or database is not initialized");
            OnProfileUpdated?.Invoke();
            return;
        }

        try
        {
            Debug.Log($"[FetchUserProfile] Fetching profile for UID: {auth.CurrentUser.UserId}");
            DocumentReference docRef = FirebaseManager.instance.db.Collection("users").Document(auth.CurrentUser.UserId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                Debug.Log("[FetchUserProfile] Document found. Parsing data...");
                snapshot.TryGetValue("username", out string fetchedUsername);
                snapshot.TryGetValue("profilePictureUrl", out string fetchedUrl);

                PlayerUsername = fetchedUsername;
                ProfilePictureURL = fetchedUrl;
                Debug.Log($"[FetchUserProfile] Data: Username='{PlayerUsername}', URL='{ProfilePictureURL}'");
            }
            else
            {
                Debug.LogWarning($"[FetchUserProfile] No document found for UID: {auth.CurrentUser.UserId}");
                PlayerUsername = null;
                ProfilePictureURL = null;
            }

            // Kullanıcı adı boşsa varsayılan değer ata
            if (string.IsNullOrEmpty(PlayerUsername))
            {
                PlayerUsername = IsGuestUser 
                    ? $"Pilot-{auth.CurrentUser.UserId.Substring(0, 5)}" 
                    : auth.CurrentUser.DisplayName ?? $"Pilot-{auth.CurrentUser.UserId.Substring(0, 5)}";
                Debug.Log($"[FetchUserProfile] Username was empty, set to: {PlayerUsername}");
            }

            // Google kullanıcısı için profil resmi URL'ini Firebase Auth'dan al
            if (string.IsNullOrEmpty(ProfilePictureURL) && !IsGuestUser && auth.CurrentUser.PhotoUrl != null)
            {
                ProfilePictureURL = auth.CurrentUser.PhotoUrl.ToString();
                Debug.Log($"[FetchUserProfile] Using Google profile picture: {ProfilePictureURL}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[FetchUserProfile] Error: {e.Message}");
        }
        finally
        {
            OnProfileUpdated?.Invoke();
        }
    }

    /// <summary>
    /// Kullanıcı adını sunucuda değiştirmeyi dener.
    /// </summary>
    public async Task<(bool success, string message)> ChangeUsername(string newUsername)
    {
        if (isBusy) return (false, "Başka bir işlem devam ediyor.");
        if (string.IsNullOrWhiteSpace(newUsername)) return (false, "Geçersiz kullanıcı adı.");
        
        isBusy = true;

        try
        {
            var functions = FirebaseFunctions.DefaultInstance;
            var function = functions.GetHttpsCallable("setPlayerUsername");
            var data = new Dictionary<string, object> { { "username", newUsername } };

            var result = await function.CallAsync(data);
            var resultDict = ParseFunctionResult(result.Data);

            if (resultDict != null && resultDict.TryGetValue("success", out var successVal) && (bool)successVal)
            {
                PlayerUsername = newUsername;
                OnProfileUpdated?.Invoke();
                return (true, "Kullanıcı adı güncellendi!");
            }

            string errorMessage = resultDict?["message"]?.ToString() ?? "Bilinmeyen sunucu hatası.";
            return (false, errorMessage);
        }
        catch (Exception e)
        {
            Debug.LogError($"Kullanıcı adı değiştirilirken hata: {e.Message}");
            if (e is FunctionsException funcEx) return (false, funcEx.Message);
            return (false, "Ağ hatası!");
        }
        finally
        {
            isBusy = false;
        }
    }

    /// <summary>
    /// Seçilen profil resmini Firebase Storage'a yükler ve URL'yi günceller.
    /// </summary>
    public async Task<(bool success, string message)> UploadProfilePicture(string path)
    {
        if (isBusy) return (false, "Başka bir işlem devam ediyor.");
        isBusy = true;

        try
        {
            byte[] resizedImageBytes = ResizeImage(path, 512, 512);
            if (resizedImageBytes == null)
            {
                throw new Exception("Resim yeniden boyutlandırma başarısız oldu.");
            }
            Debug.Log($"Resim {resizedImageBytes.Length / 1024} KB boyutuna küçültüldü.");

            FirebaseStorage storage = FirebaseStorage.DefaultInstance;
            string storagePath = $"player_profile_pictures/{FirebaseManager.instance.UserID}.jpg";
            StorageReference picRef = storage.GetReference(storagePath);

            await picRef.PutBytesAsync(resizedImageBytes);
            Uri downloadUri = await picRef.GetDownloadUrlAsync();
            ProfilePictureURL = downloadUri.ToString();

            // UI'ı anında güncelle
            OnProfileUpdated?.Invoke();

            // Database'de profil resmi URL'sini güncelle
            DocumentReference docRef = FirebaseManager.instance.db.Collection("users").Document(FirebaseManager.instance.UserID);
            await docRef.SetAsync(new Dictionary<string, object> 
            { 
                { "profilePictureUrl", ProfilePictureURL } 
            }, SetOptions.MergeAll);

            return (true, "Profil resmi güncellendi!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Profil resmi yüklenirken hata: {e.Message}");
            return (false, "Resim yüklenemedi.");
        }
        finally
        {
            isBusy = false;
        }
    }

    /// <summary>
    /// Mevcut misafir (anonymous) hesabını bir Google hesabına bağlar.
    /// </summary>
    public async Task<(bool success, string message)> LinkAccountWithGoogleAsync()
    {
        if (!IsGuestUser)
        {
            return (false, "Sadece misafir hesapları bağlanabilir.");
        }

        if (isBusy) return (false, "Başka bir işlem devam ediyor.");
        isBusy = true;

        try
        {
            Debug.Log("[LinkAccountWithGoogle] Google Sign-In başlatılıyor...");
            Task<GoogleSignInUser> signInTask = GoogleSignIn.DefaultInstance.SignIn();
            await signInTask;

            if (signInTask.IsCanceled)
            {
                return (false, "Google girişi kullanıcı tarafından iptal edildi.");
            }
            if (signInTask.IsFaulted)
            {
                Debug.LogError($"[LinkAccountWithGoogle] GoogleSignIn task failed with exception: {signInTask.Exception}");
                return (false, "Yapılandırma hatası! Lütfen Firebase konsolundaki SHA-1 parmak izini kontrol edin. (Hata Kodu: 10)");
            }

            GoogleSignInUser googleUser = signInTask.Result;
            var credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
            var user = auth.CurrentUser;
            
            Debug.Log("[LinkAccountWithGoogle] Hesap bağlanıyor...");
            await user.LinkWithCredentialAsync(credential);
            Debug.Log($"[LinkAccountWithGoogle] Başarıyla bağlandı: {user.UserId}");

            await FetchUserProfile();
            return (true, "Hesap başarıyla Google'a bağlandı!");
        }
        catch (FirebaseException e)
        {
            if (e.ErrorCode == (int)AuthError.CredentialAlreadyInUse)
            {
                return (false, "Bu Google hesabı zaten başka bir profile bağlı.");
            }
            Debug.LogError($"[LinkAccountWithGoogle] Firebase hatası: {e.Message} (Kod: {e.ErrorCode})");
            return (false, $"Hesap bağlanamadı: {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LinkAccountWithGoogle] Beklenmedik hata: {e.Message}");
            return (false, "Bilinmeyen bir hata oluştu.");
        }
        finally
        {
            isBusy = false;
        }
    }

    /// <summary>
    /// Kullanıcının Google hesabıyla Firebase'e giriş yapmasını sağlar.
    /// </summary>
    public async Task<(bool success, string message)> SignInWithGoogleAsync()
    {
        if (isBusy) return (false, "Başka bir işlem devam ediyor.");
        isBusy = true;

        Debug.Log("[SignInWithGoogle] Google ile giriş işlemi başlatılıyor...");

        try
        {
            // Google ile giriş yapmayı dene
            Debug.Log("[SignInWithGoogle] GoogleSignIn.DefaultInstance.SignIn() çağrılıyor...");
            Task<GoogleSignInUser> signInTask = GoogleSignIn.DefaultInstance.SignIn();
            await signInTask;

            if (signInTask.IsCanceled)
            {
                return (false, "Google girişi kullanıcı tarafından iptal edildi.");
            }
            if (signInTask.IsFaulted)
            {
                Debug.LogError($"[SignInWithGoogle] GoogleSignIn task failed with exception: {signInTask.Exception}");
                return (false, "Yapılandırma hatası! Lütfen Firebase konsolundaki SHA-1 parmak izini kontrol edin. (Hata Kodu: 10)");
            }

            GoogleSignInUser googleUser = signInTask.Result;

            // Google'dan alınan ID Token ile Firebase kimlik bilgisi oluştur
            Debug.Log("[SignInWithGoogle] Firebase kimlik bilgisi oluşturuluyor...");
            Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

            // Firebase'e bu kimlik bilgisiyle giriş yap
            Debug.Log("[SignInWithGoogle] Firebase'e giriş yapılıyor...");
            await auth.SignInWithCredentialAsync(credential);

            Debug.Log($"[SignInWithGoogle] Başarıyla giriş yapıldı: {auth.CurrentUser.DisplayName}");

            // Oyuncu profilini sunucudan çek
            await FetchUserProfile();

            return (true, "Giriş başarılı!");
        }
        catch (FirebaseException e)
        {
            Debug.LogError($"[SignInWithGoogle] Firebase giriş hatası: {e.Message} (Kod: {e.ErrorCode})");
            return (false, $"Giriş yapılamadı: {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SignInWithGoogle] Beklenmedik hata: {e.Message}");
            return (false, "Giriş işlemi iptal edildi veya bir hata oluştu.");
        }
        finally
        {
            isBusy = false;
        }
    }

    /// <summary>
    /// Kullanıcının oturumunu hem Firebase'den hem de Google'dan güvenli bir şekilde kapatır.
    /// </summary>
    public void SignOut()
    {
        Debug.Log("[SignOut] Oturum kapatılıyor...");

        try
        {
            // Firebase'den çıkış yap
            if (auth != null && auth.CurrentUser != null)
            {
                auth.SignOut();
                Debug.Log("[SignOut] Firebase'den çıkış yapıldı.");
            }

            // Google'dan çıkış yap
            GoogleSignIn.DefaultInstance.SignOut();
            Debug.Log("[SignOut] Google'dan çıkış yapıldı.");

            // Yerel verileri temizle
            PlayerUsername = "Misafir";
            ProfilePictureURL = null;

            // Dinleyicileri bilgilendir
            OnProfileUpdated?.Invoke();
            
            Debug.Log("[SignOut] Oturum başarıyla kapatıldı.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SignOut] Oturum kapatırken hata: {e.Message}");
        }
    }

    // Yardımcı metodlar
    private byte[] ResizeImage(string path, int maxWidth, int maxHeight)
    {
        try
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);

            if (!tex.LoadImage(fileData))
            {
                Debug.LogError("Resim yüklenemedi.");
                return null;
            }

            int width = tex.width;
            int height = tex.height;

            if (width <= maxWidth && height <= maxHeight)
            {
                return tex.EncodeToJPG(85);
            }

            float ratio = (float)width / height;
            if (width > height)
            {
                width = maxWidth;
                height = Mathf.RoundToInt(width / ratio);
            }
            else
            {
                height = maxHeight;
                width = Mathf.RoundToInt(height * ratio);
            }

            RenderTexture rt = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            Texture2D finalResult = result.height > result.width ? RotateTexture90CCW(result) : result;
            if (finalResult != result) Destroy(result);

            Destroy(tex);
            byte[] resizedBytes = finalResult.EncodeToJPG(85);
            Destroy(finalResult);
            return resizedBytes;
        }
        catch (Exception e)
        {
            Debug.LogError($"Resim boyutlandırma hatası: {e.Message}");
            return null;
        }
    }

    private Texture2D RotateTexture90CCW(Texture2D originalTexture)
    {
        Color32[] originalPixels = originalTexture.GetPixels32();
        int width = originalTexture.width;
        int height = originalTexture.height;

        int newWidth = height;
        int newHeight = width;
        Color32[] rotatedPixels = new Color32[originalPixels.Length];

        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                int oldX = y;
                int oldY = height - 1 - x;
                rotatedPixels[y * newWidth + x] = originalPixels[oldY * width + oldX];
            }
        }

        Texture2D rotatedTexture = new Texture2D(newWidth, newHeight, originalTexture.format, false);
        rotatedTexture.SetPixels32(rotatedPixels);
        rotatedTexture.Apply();
        return rotatedTexture;
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
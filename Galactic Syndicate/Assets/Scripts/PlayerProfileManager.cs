// PlayerProfileManager.cs - NİHAİ HALİ (Profil Resmi Yükleme Eklendi)

using UnityEngine;
using System.Threading.Tasks;
using Firebase.Functions;
using System.Collections.Generic;
using System;
using Firebase.Storage;
using System.IO;
using System.Linq;
using Firebase.Firestore;
using Firebase; // FirebaseException için eklendi
using Google; // GoogleSignIn için eklendi
using Firebase.Auth;

public class PlayerProfileManager : MonoBehaviour
{
    public static PlayerProfileManager instance;

    // --- İYİLEŞTİRME: Web Client ID'yi Inspector'dan ayarlanabilir yap ---
    [Header("Google Sign-In Ayarları")]
    [SerializeField] private string webClientId = "SENİN-WEB-CLIENT-ID-Nİ-BURAYA-YAPIŞTIR";

    public string PlayerUsername { get; private set; }
    public string ProfilePictureURL { get; private set; }

    public event Action OnProfileUpdated;

    private bool isBusy = false;
    private FirebaseAuth auth; // Auth servisine referans

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
            auth = FirebaseAuth.DefaultInstance; // Auth servisini başlat
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Oyuncu verisi yüklendiğinde veya yeni oyun başladığında çağrılır.
    /// </summary>
    public async Task FetchUserProfile()
    {
        if (auth.CurrentUser == null) return;

        // Kullanıcının veritabanındaki dökümanından en güncel bilgileri çek.
        DocumentReference docRef = FirebaseManager.instance.db.Collection("users").Document(auth.CurrentUser.UserId);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            PlayerUsername = snapshot.GetValue<string>("username");
            ProfilePictureURL = snapshot.GetValue<string>("profilePictureUrl");
        }

        // Eğer veritabanında isim yoksa, varsayılan bir isim oluştur.
        if (string.IsNullOrEmpty(PlayerUsername))
        {
            PlayerUsername = $"Pilot-{FirebaseManager.instance.UserID.Substring(0, 5)}";
        }

        OnProfileUpdated?.Invoke();
    }

    /// <summary>
    /// Kullanıcı adını sunucuda değiştirmeyi dener.
    /// </summary>
    public async Task<(bool success, string message)> ChangeUsername(string newUsername)
    {
        if (isBusy) return (false, "Başka bir işlem devam ediyor.");
        isBusy = true;

        try
        {
            var functions = FirebaseFunctions.DefaultInstance;
            var function = functions.GetHttpsCallable("setPlayerUsername");
            var data = new Dictionary<string, object> { { "username", newUsername } };

            var result = await function.CallAsync(data);
            var resultDict = ParseFunctionResult(result.Data); // HATA DÜZELTMESİ: SyndicateManager bağımlılığı kaldırıldı.

            if (resultDict != null && resultDict.TryGetValue("success", out var successVal) && (bool)successVal)
            {
                PlayerUsername = newUsername;
                OnProfileUpdated?.Invoke();
                return (true, "Kullanıcı adı güncellendi!");
            }

            string errorMessage = resultDict?["message"]?.ToString() ?? "Bilinmeyen sunucu hatası.";
            return (false, $"Hata: {errorMessage}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Kullanıcı adı değiştirilirken hata: {e.Message}");
            // Firebase Functions'tan gelen hatalar genellikle kullanıcı dostu mesajlar içerir.
            if (e is FunctionsException funcEx) return (false, funcEx.Message);
            return (false, "Ağ Hatası!");
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
            // --- YENİ ADIM: Resmi yeniden boyutlandır ve sıkıştır ---
            byte[] resizedImageBytes = ResizeImage(path, 512, 512);
            if (resizedImageBytes == null)
            {
                throw new Exception("Resim yeniden boyutlandırma istemcide başarısız oldu.");
            }
            Debug.Log($"Resim, orijinal boyutundan {resizedImageBytes.Length / 1024} KB boyutuna küçültüldü.");
            // ----------------------------------------------------

            FirebaseStorage storage = FirebaseStorage.DefaultInstance;
            string storagePath = $"player_profile_pictures/{FirebaseManager.instance.UserID}.jpg";
            StorageReference picRef = storage.GetReference(storagePath);

            // --- DEĞİŞİKLİK: PutFileAsync yerine PutBytesAsync kullan ---
            // Artık dosyanın kendisini değil, bellekteki küçültülmüş halini yüklüyoruz.
            await picRef.PutBytesAsync(resizedImageBytes);

            Uri downloadUri = await picRef.GetDownloadUrlAsync();
            ProfilePictureURL = downloadUri.ToString();

            // Önce yerel UI'ı anında güncelle.
            OnProfileUpdated?.Invoke();

            // --- HATAYI DÜZELTEN YENİ KOD ---
            // SaveManager'a güvenmek yerine, sadece ilgili alanı UpdateAsync ile güvenli bir şekilde güncelliyoruz.
            // Bu, hangi sahnede olursa olsun çalışır ve diğer verileri ezme riskini ortadan kaldırır.
            DocumentReference docRef = FirebaseManager.instance.db.Collection("users").Document(FirebaseManager.instance.UserID);
            await docRef.UpdateAsync("profilePictureUrl", ProfilePictureURL);

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

        try
        {
            var googleUser = await GoogleSignIn.DefaultInstance.SignIn();
            var credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

            var user = auth.CurrentUser;
            await user.LinkWithCredentialAsync(credential);

            Debug.Log($"[PlayerProfileManager] Misafir hesap {user.UserId} başarıyla Google hesabına bağlandı.");

            await FetchUserProfile();
            OnProfileUpdated?.Invoke();

            return (true, "Hesap başarıyla bağlandı!");
        }
        catch (FirebaseException e)
        {
            if (e.ErrorCode == (int)AuthError.CredentialAlreadyInUse)
            {
                return (false, "Bu Google hesabı zaten başka bir oyun profiline bağlı.");
            }
            Debug.LogError($"[PlayerProfileManager] Google ile hesap bağlama hatası: {e.Message} (Kod: {e.ErrorCode})");
            return (false, $"Hesap bağlanamadı: {e.Message}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerProfileManager] Beklenmedik hesap bağlama hatası: {e.Message}");
            return (false, "Bilinmeyen bir hata oluştu.");
        }
    }

    /// <summary>
    /// Verilen yoldaki bir resmi okur, belirtilen maksimum boyutlara yeniden boyutlandırır ve JPG olarak sıkıştırır.
    /// </summary>
    /// <returns>Yeniden boyutlandırılmış resmin byte dizisi.</returns>
    private byte[] ResizeImage(string path, int maxWidth, int maxHeight)
    {
        byte[] fileData = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);

        // LoadImage, 2x2'lik dokuyu resmin verileri ve boyutlarıyla değiştirir.
        if (!tex.LoadImage(fileData))
        {
            Debug.LogError("Resim verisi byte dizisinden yüklenemedi.");
            return null;
        }

        int width = tex.width;
        int height = tex.height;

        // Eğer resim zaten yeterince küçükse, sadece JPG'ye çevirip döndür.
        if (width <= maxWidth && height <= maxHeight)
        {
            return tex.EncodeToJPG(85); // 85 iyi bir kalite/boyut dengesidir.
        }

        // Boyut oranını koruyarak yeni boyutları hesapla.
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

        // Yüksek kaliteli yeniden boyutlandırma için RenderTexture kullan.
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(tex, rt);
        RenderTexture.active = rt;
        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        // --- YENİ ADIM: Resmi -90 derece (saat yönünün tersine) döndür ---
        // Telefonla dikey çekilen resimlerin Unity'de yan dönmüş görünmesini düzeltir.
        Texture2D rotatedResult = RotateTexture90CCW(result);
        //--------------------------------------------------------------------

        // Belleği temizle ve sonucu döndür.
        Destroy(tex);
        Destroy(result); // Artık orijinal 'result' dokusuna ihtiyacımız yok.
        byte[] resizedBytes = rotatedResult.EncodeToJPG(85);
        Destroy(rotatedResult); // Döndürülmüş son dokuyu da temizle.

        return resizedBytes;
    }

    /// <summary>
    /// Bir Texture2D'yi 90 derece saat yönünün tersine (-90 derece) döndürür.
    /// Bu, dikey çekilmiş mobil fotoğrafların doğru yönde görünmesini sağlar.
    /// </summary>
    /// <param name="originalTexture">Döndürülecek orijinal doku.</param>
    /// <returns>Döndürülmüş yeni bir Texture2D nesnesi.</returns>
    private Texture2D RotateTexture90CCW(Texture2D originalTexture)
    {
        Color32[] originalPixels = originalTexture.GetPixels32();
        int width = originalTexture.width;
        int height = originalTexture.height;

        // Yeni dokunun boyutları orijinalin tersi olacak.
        int newWidth = height;
        int newHeight = width;
        Color32[] rotatedPixels = new Color32[originalPixels.Length];

        for (int y = 0; y < newHeight; y++) // Yeni dokunun y koordinatı
        {
            for (int x = 0; x < newWidth; x++) // Yeni dokunun x koordinatı
            {
                // Yeni dokudaki (x, y) pikseli, eski dokudaki (y, height - 1 - x) pikseline karşılık gelir.
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

    /// <summary>
    /// Firebase Function'dan dönen sonucu güvenli bir şekilde IDictionary<string, object> formatına çevirir.
    /// Bu, SyndicateManager'a olan bağımlılığı ortadan kaldırır.
    /// </summary>
    // HATA DÜZELTMESİ: Bu metoda diğer Manager'ların da erişebilmesi için 'public' olmalı.
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
        /// <summary>
    /// Kullanıcının Google hesabıyla Firebase'e giriş yapmasını sağlar.
    /// Eğer bu Google hesabıyla daha önce giriş yapılmışsa mevcut hesabı açar,
    /// yapılmamışsa yeni bir Firebase hesabı oluşturur.
    /// </summary>
    /// <returns>İşlemin başarı durumunu ve mesajını içeren bir tuple.</returns>
    public async Task<(bool success, string message)> SignInWithGoogleAsync()
    {
        if (isBusy) return (false, "Başka bir işlem devam ediyor.");
        isBusy = true;

        Debug.Log("[PlayerProfileManager] Google ile giriş işlemi başlatılıyor...");

        try
        {
            // 1. GoogleSignIn'ı WebClientId ile yapılandır.
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                RequestIdToken = true,
                WebClientId = this.webClientId
            };

            // 2. Google ile giriş yapmayı dene.
            Task<GoogleSignInUser> signInTask = GoogleSignIn.DefaultInstance.SignIn();
            GoogleSignInUser googleUser = await signInTask;

            // 3. Google'dan alınan ID Token ile Firebase kimlik bilgisi oluştur.
            Debug.Log("[PlayerProfileManager] Google'dan IdToken alındı, Firebase kimlik bilgisi oluşturuluyor...");
            Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

            // 4. Firebase'e bu kimlik bilgisiyle giriş yap.
            Debug.Log("[PlayerProfileManager] Firebase'e giriş yapılıyor...");
            await auth.SignInWithCredentialAsync(credential);

            Debug.Log($"[PlayerProfileManager] Google ile başarıyla giriş yapıldı: {auth.CurrentUser.DisplayName}");

            // 5. Oyuncu profilini sunucudan çek.
            await FetchUserProfile();
            OnProfileUpdated?.Invoke();

            return (true, "Giriş başarılı!");
        }
        catch (FirebaseException e)
        {
            Debug.LogError($"[PlayerProfileManager] Google ile Firebase girişi hatası: {e.Message} (Kod: {e.ErrorCode})");
            return (false, $"Giriş yapılamadı: {e.Message}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerProfileManager] Beklenmedik Google giriş hatası: {e.Message}");
            return (false, "Giriş işlemi iptal edildi veya bir hata oluştu.");
        }
        finally
        {
            isBusy = false;
        }
    }
}
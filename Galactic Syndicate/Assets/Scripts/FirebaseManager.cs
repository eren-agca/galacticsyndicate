// Dosya: Assets/Scripts/FirebaseManager.cs

using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using System; // Exception sınıfı için eklendi

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager instance;
    public bool IsInitialized { get; private set; } = false;

    public FirebaseApp app { get; private set; }
    public FirebaseAuth auth { get; private set; }
    public FirebaseFirestore db { get; private set; }
    public FirebaseUser user { get; private set; }
    public string UserID => user?.UserId ?? "N/A";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Firebase.FirebaseApp.LogLevel = Firebase.LogLevel.Verbose;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async Task InitializeFirebase()
    {
        // --- YENİ: Hata Yakalama Bloğu ---
        // Bu blok, başlatma sırasında oluşabilecek herhangi bir ağ veya yapılandırma
        // hatasını yakalayarak oyunun kilitlenmesini önler ve sorunu net bir şekilde loglar.
        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus == DependencyStatus.Available)
            {
                app = FirebaseApp.DefaultInstance;
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance;

                if (auth.CurrentUser != null)
                {
                    user = auth.CurrentUser;
                    Debug.Log($"Firebase: Mevcut kullanıcıyla devam ediliyor. UserID: {user.UserId}, IsAnonymous: {user.IsAnonymous}");
                }
                else
                {
                    Debug.Log("Firebase: Mevcut kullanıcı yok. Anonim olarak giriş yapılıyor...");
                    AuthResult authResult = await auth.SignInAnonymouslyAsync();
                    user = authResult.User;
                    Debug.Log($"Firebase: Anonim giriş başarılı. UserID: {user.UserId}");
                }
                
                IsInitialized = true;
                Debug.LogWarning("Firebase başlatıldı. Diğer sistemler aktive ediliyor...");

                // Hazır olduğumuzu diğer yöneticilere bildir.
                AnalyticsManager.instance?.OnFirebaseInitialized();
            }
            else
            {
                Debug.LogError($"Firebase bağımlılıkları çözülemedi: {dependencyStatus}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"!!! KRİTİK HATA: Firebase başlatılamadı. Oyuncu kimliği alınamadı. Hata: {e}");
            // İsteğe bağlı: Kullanıcıya bir hata mesajı göster.
            // UIManager.instance?.ShowNotification("Sunucuya bağlanılamadı. Lütfen internet bağlantınızı kontrol edip tekrar deneyin.");
        }
    }
}
// MainMenu.cs - GÜVENLİ ASENKRON KONTROLLERİ EKLENDİ

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Firebase.RemoteConfig;
using Firebase.Functions;
using System;
using Firebase.Auth;      // YENİ: FirebaseAuth için eklendi.
using Firebase.Firestore; // HATA DÜZELTİLDİ: Eksik using direktifi eklendi.

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    public Button primaryActionButton;
    public TextMeshProUGUI primaryActionButtonText;
    public Button newGameButton;
    [Tooltip("Sadece Unity Editor'de görünen, Firebase oturumunu kapatma butonu.")]
    public Button signOutDebugButton;
    public GameObject loadingIndicator;

    [Header("Dynamic Content")]
    [Tooltip("Günün haberlerinin gösterileceği metin alanı.")]
    public TextMeshProUGUI galaxyNewsText;

    async void Start()
    {
        primaryActionButton.interactable = false;
        newGameButton.gameObject.SetActive(false);
        loadingIndicator.SetActive(true);
        if (galaxyNewsText != null) galaxyNewsText.text = "Haberler yükleniyor...";

        // --- YENİ GELİŞTİRİCİ BUTONU MANTIĞI ---
        // Eğer signOutDebugButton atanmışsa, sadece Unity Editor'de aktif et.
        // Build alınmış oyunda bu buton görünmeyecektir.
        #if UNITY_EDITOR
        if (signOutDebugButton != null) signOutDebugButton.gameObject.SetActive(true);
        #else
        if (signOutDebugButton != null) signOutDebugButton.gameObject.SetActive(false);
        #endif
        // -----------------------------------------

        await FirebaseManager.instance.InitializeFirebase();
        if (this == null) return;

        await FetchAndDisplayNews();
        if (this == null) return;

        // --- DEĞİŞİKLİK: LoadGame artık doğrudan çağrılmıyor, varlık kontrolü için kullanılıyor. ---
        DocumentReference docRef = FirebaseManager.instance.db.Collection("users").Document(FirebaseManager.instance.UserID);
        var snapshot = await docRef.GetSnapshotAsync();
        bool hasSaveData = snapshot.Exists;
        
        if (this == null) return;
        
        loadingIndicator.SetActive(false);
        
        if (hasSaveData)
        {
            // Eğer kayıt varsa, "Continue" butonunu göster ve kayıtlı kullanıcı adını yükle.
            primaryActionButtonText.text = "Continue";
            newGameButton.gameObject.SetActive(true);

            // Profili sunucudan çek. Bu, UI'ı otomatik olarak güncelleyecektir.
            await PlayerProfileManager.instance.FetchUserProfile();
        }
        else
        {
            // Eğer kayıt yoksa, "Play" butonunu göster.
            primaryActionButtonText.text = "Play";
            newGameButton.gameObject.SetActive(false);

            // --- KÖK NEDEN ÇÖZÜMÜ ---
            // Yeni oyuncu için sunucuda hemen bir kayıt oluştur.
            // Bu, profil panelinin ilk kullanımdan itibaren doğru çalışmasını sağlar
            // ve "veri bulunamadı" hatalarını tamamen ortadan kaldırır.
            Debug.Log("No save data found. Creating initial player record on the server...");
            string defaultUsername = $"Pilot-{FirebaseManager.instance.UserID.Substring(0, 5)}";
            // Bu çağrı hem sunucuda kayıt oluşturur hem de OnProfileUpdated ile yerel veriyi günceller.
            await PlayerProfileManager.instance.ChangeUsername(defaultUsername);
            Debug.Log("Initial player record created.");
        }
        
        primaryActionButton.interactable = true;
    }

    private async Task FetchAndDisplayNews()
    {
        if (galaxyNewsText == null) return;

        try
        {
            await FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync();
            string news = FirebaseRemoteConfig.DefaultInstance.GetValue("galaxy_news").StringValue;

            if (!string.IsNullOrEmpty(news))
            {
                galaxyNewsText.text = news;
            }
            else
            {
                galaxyNewsText.text = "Galakside bugün her şey sakin.";
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Remote Config verisi çekilirken hata oluştu: {e.Message}");
            galaxyNewsText.text = "Haber akışına bağlanılamadı.";
        }
    }

    public void OnPrimaryActionButtonClicked()
    {
        // Oyunu yüklemek için GameInitializer'a güveniyoruz.
        SceneManager.LoadScene("GameScene");
    }

    // --- DEĞİŞİKLİK: Metot artık Cloud Function çağırıyor. ---
    public async void OnNewGameButtonClicked()
    {
        loadingIndicator.SetActive(true);
        primaryActionButton.interactable = false;
        newGameButton.interactable = false;

        try
        {
            var functions = FirebaseFunctions.DefaultInstance;
            var function = functions.GetHttpsCallable("handleNewGame");
            await function.CallAsync();
            
            Debug.Log("New game process completed successfully on server.");

            // UI'ı yeni oyun durumuna ayarla
            primaryActionButtonText.text = "Play";
            newGameButton.gameObject.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start a new game: {e.Message}");
            // Hata durumunda butonları tekrar aktif hale getir.
            newGameButton.interactable = true;
        }
        finally
        {
            loadingIndicator.SetActive(false);
            primaryActionButton.interactable = true;
        }
    }

    /// <summary>
    /// Sadece geliştirme amaçlı kullanılan bu metot, mevcut Firebase kullanıcısının oturumunu kapatır
    /// ve yeni bir kullanıcı olarak test yapabilmek için sahneyi yeniden yükler.
    /// </summary>
    public void OnSignOutDebugButtonClicked()
    {
        if (FirebaseManager.instance?.auth != null)
        {
            Debug.LogWarning("--- DEVELOPER ACTION: Signing out current Firebase user. ---");
            FirebaseManager.instance.auth.SignOut();
            // Sahneyi yeniden yükleyerek temiz bir başlangıç yap ve yeni bir anonim kullanıcı oluşturulmasını sağla.
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public async void OnGoogleSignInButtonClicked()
    {
        loadingIndicator.SetActive(true);
        primaryActionButton.interactable = false;
        newGameButton.interactable = false;

        var (success, message) = await PlayerProfileManager.instance.SignInWithGoogleAsync();

        if (success)
        {
            Debug.Log("Google ile giriş başarılı, oyun sahnesine geçiliyor.");
            // Oyunu yüklemek için GameInitializer'a güveniyoruz.
            SceneManager.LoadScene("GameScene");
        }
        else
        {
            Debug.LogError($"Google ile giriş başarısız: {message}");
            // TODO: Hata mesajını kullanıcıya bir panelde göster.
            loadingIndicator.SetActive(false);
            primaryActionButton.interactable = true;
            newGameButton.interactable = true;
        }
    }
}
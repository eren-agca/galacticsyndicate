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

    void OnEnable()
    {
        // Merkezi profil güncelleme olayına abone ol.
        // Bu, giriş/çıkış yapıldığında menünün anında güncellenmesini sağlar.
        if (PlayerProfileManager.instance != null)
        {
            PlayerProfileManager.instance.OnProfileUpdated += OnProfileUpdatedHandler;
        }
    }

    void OnDisable()
    {
        // Bellek sızıntılarını önlemek için olay aboneliğini iptal et.
        if (PlayerProfileManager.instance != null)
        {
            PlayerProfileManager.instance.OnProfileUpdated -= OnProfileUpdatedHandler;
        }
    }

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

        // İlk UI durumunu ayarla ve haberleri çek.
        _ = UpdateMenuUIAsync();
        await FetchAndDisplayNews();
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

    public void OnSignOutDebugButtonClicked()
    {
        if (FirebaseManager.instance?.auth != null)
        {
            Debug.LogWarning("--- DEVELOPER ACTION: Signing out current Firebase user. ---");
            // DEĞİŞİKLİK: Merkezi oturum kapatma metodunu kullan.
            PlayerProfileManager.instance.SignOut();
            // Sahneyi yeniden yükleyerek temiz bir başlangıç yap ve yeni bir anonim kullanıcı oluşturulmasını sağla.
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    /// <summary>
    /// Profil güncelleme olayı tetiklendiğinde çağrılır.
    /// </summary>
    private void OnProfileUpdatedHandler()
    {
        // UI güncelleme işlemini arka planda başlat.
        _ = UpdateMenuUIAsync();
    }

    /// <summary>
    /// Oyuncunun kayıt durumuna göre ana menü butonlarını günceller.
    /// </summary>
    private async Task UpdateMenuUIAsync()
    {
        if (FirebaseManager.instance == null || !FirebaseManager.instance.IsInitialized) return;

        loadingIndicator.SetActive(true);
        primaryActionButton.interactable = false;

        DocumentReference docRef = FirebaseManager.instance.db.Collection("users").Document(FirebaseManager.instance.UserID);
        var snapshot = await docRef.GetSnapshotAsync();
        bool hasSaveData = snapshot.Exists;

        if (this == null) return;

        loadingIndicator.SetActive(false);

        if (hasSaveData)
        {
            primaryActionButtonText.text = "Continue";
            newGameButton.gameObject.SetActive(true);
        }
        else
        {
            primaryActionButtonText.text = "Play";
            newGameButton.gameObject.SetActive(false);
        }

        primaryActionButton.interactable = true;
    }
}
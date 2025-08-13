using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Google ile giriş ve profil panellerinin kullanıcı arayüzünü yönetir.
/// Arka plan işlemlerini PlayerProfileManager'a devreder.
/// </summary>
public class GoogleAuthentication : MonoBehaviour
{
    [Header("UI Referansları")]
    public TMP_Text userNameTxt;
    public TMP_Text userEmailTxt;
    public Image profilePic;
    // profilePanel referansını kaldırdık - PlayerProfileUI kendi panelini yönetecek
    public Button signInButton;
    public Button signOutButton;
    public GameObject loadingIndicator; // Loading göstergesi eklendi

    private Coroutine imageLoadCoroutine;
    private bool isProcessing = false; // Çoklu işlem önleme

    void Start()
    {
        // Başlangıçta UI'ı temiz duruma getir
        InitializeUI();

        // DEBUG: Button event kontrolü
        if (signInButton != null)
        {
            Debug.Log("[DEBUG] Sign-In button found and active");

            // Button click event'ini manuel olarak ekleyelim (eğer Inspector'da atanmamışsa)
            signInButton.onClick.RemoveAllListeners();
            signInButton.onClick.AddListener(() => {
                Debug.Log("[DEBUG] Sign-In button clicked via code listener!");
                OnSignIn();
            });
        }
        else
        {
            Debug.LogError("[DEBUG] Sign-In button is NULL!");
        }
    }

    // Ayrıca bu test metodunu da ekleyin:
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestButtonClick()
    {
        Debug.Log("[DEBUG] Manual test button click");
        OnSignIn();
    }
    void OnEnable()
    {
        // PlayerProfileManager'daki profil güncelleme olayına abone ol
        if (PlayerProfileManager.instance != null)
        {
            PlayerProfileManager.instance.OnProfileUpdated += UpdateProfileUI;
        }
        
        // Firebase hazır olduğunda profil durumunu kontrol et
        if (FirebaseManager.instance != null && FirebaseManager.instance.IsInitialized)
        {
            UpdateProfileUI();
        }
    }

    void OnDisable()
    {
        // Bellek sızıntılarını önlemek için olay aboneliğini iptal et
        if (PlayerProfileManager.instance != null)
        {
            PlayerProfileManager.instance.OnProfileUpdated -= UpdateProfileUI;
        }
    }

    private void InitializeUI()
    {
        // Başlangıç durumunda tüm UI elemanlarını temiz hale getir
        if (userNameTxt != null) userNameTxt.text = "Misafir";
        if (userEmailTxt != null) userEmailTxt.text = "";
        if (profilePic != null) 
        {
            profilePic.sprite = null;
            profilePic.color = new Color(1, 1, 1, 0.2f); // Şeffaf görünüm
        }
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        
        SetButtonsInteractable(true);
        UpdateSignInButtonVisibility(); // Sadece sign-in butonunu kontrol et
    }

    /// <summary>
    /// Google ile giriş yapma işlemini başlatır.
    /// </summary>
    public async void OnSignIn()
    {
        if (isProcessing)
        {
            Debug.Log("Zaten bir giriş işlemi devam ediyor...");
            return;
        }

        if (PlayerProfileManager.instance == null)
        {
            Debug.LogError("PlayerProfileManager bulunamadı!");
            ShowFeedback("Sistem hatası! Oyunu yeniden başlatın.", false);
            return;
        }

        isProcessing = true;
        SetButtonsInteractable(false);
        if (loadingIndicator != null) loadingIndicator.SetActive(true);

        try
        {
            // Akıllı giriş mantığı: Misafir hesabı varsa bağla, yoksa yeni giriş yap
            var (success, message) = PlayerProfileManager.instance.IsGuestUser
                ? await PlayerProfileManager.instance.LinkAccountWithGoogleAsync()
                : await PlayerProfileManager.instance.SignInWithGoogleAsync();

            if (success)
            {
                Debug.Log("Google ile giriş başarılı!");
                ShowFeedback("Giriş başarılı!", true);
                // UpdateProfileUI otomatik olarak OnProfileUpdated event'i ile çağrılacak
            }
            else
            {
                Debug.LogError($"Google giriş hatası: {message}");
                ShowFeedback(message, false);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Google Authentication exception: {e.Message}");
            ShowFeedback("Beklenmedik bir hata oluştu.", false);
        }
        finally
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            // --- KESİN ÇÖZÜM: Butonların kilitlenmesini önle ---
            // İşlem başarılı da olsa, başarısız da olsa veya bir istisna fırlatsa da
            // butonların her zaman tekrar tıklanabilir olmasını garantiler.
            SetButtonsInteractable(true);
            isProcessing = false;
        }
    }

    /// <summary>
    /// Oturumu kapatma işlemini başlatır.
    /// </summary>
    public void OnSignOut()
    {
        if (isProcessing) return;

        if (PlayerProfileManager.instance == null)
        {
            Debug.LogError("PlayerProfileManager bulunamadı!");
            return;
        }

        isProcessing = true;
        SetButtonsInteractable(false);

        try
        {
            // Asıl işi PlayerProfileManager'a devret
            PlayerProfileManager.instance.SignOut();
            
            // UI'ı misafir durumuna getir
            UpdateUIForGuestState();
            
            ShowFeedback("Çıkış yapıldı.", true);
            Debug.Log("Oturum başarıyla kapatıldı.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Sign out error: {e.Message}");
            ShowFeedback("Çıkış yaparken hata oluştu.", false);
        }
        finally
        {
            isProcessing = false;
            // Çıkış işleminden sonra da butonların tekrar aktif olmasını garantile.
            SetButtonsInteractable(true);
        }
    }

    /// <summary>
    /// PlayerProfileManager'dan gelen verilerle UI elemanlarını günceller.
    /// </summary>
    private void UpdateProfileUI()
    {
        if (PlayerProfileManager.instance == null || isProcessing) return;

        // Firebase Auth durumunu kontrol et
        bool isSignedInWithGoogle = IsUserSignedInWithGoogle();
        
        if (isSignedInWithGoogle && !string.IsNullOrEmpty(PlayerProfileManager.instance.PlayerUsername))
        {
            // Kullanıcı Google ile giriş yapmış
            UpdateUIForSignedInState();
        }
        else
        {
            // Kullanıcı misafir veya çıkış yapmış
            UpdateUIForGuestState();
        }
        
        SetButtonsInteractable(true);
    }

    private bool IsUserSignedInWithGoogle()
    {
        // Firebase Auth durumunu kontrol et
        if (FirebaseManager.instance?.auth?.CurrentUser == null) return false;
        
        var user = FirebaseManager.instance.auth.CurrentUser;
        return !user.IsAnonymous && user.ProviderId != "anonymous";
    }

    private void UpdateUIForSignedInState()
    {
        if (userNameTxt != null) 
            userNameTxt.text = PlayerProfileManager.instance.PlayerUsername ?? "Kullanıcı";
            
        if (userEmailTxt != null)
        {
            // Firebase Auth'dan email bilgisini al
            var currentUser = FirebaseManager.instance?.auth?.CurrentUser;
            userEmailTxt.text = currentUser?.Email ?? "";
        }

        // Profil resmini yükle
        LoadProfilePicture();
        
        UpdateSignInButtonVisibility(); // Sadece sign-in butonu kontrolü
    }

    private void UpdateUIForGuestState()
    {
        if (userNameTxt != null) userNameTxt.text = "Misafir";
        if (userEmailTxt != null) userEmailTxt.text = "";
        if (profilePic != null) 
        {
            profilePic.sprite = null;
            profilePic.color = new Color(1, 1, 1, 0.2f);
        }
        
        UpdateSignInButtonVisibility(); // Sadece sign-in butonu kontrolü
    }

    private void UpdateSignInButtonVisibility()
    {
        // Sadece sign-in butonunu kontrol et
        // ProfilePanel'i PlayerProfileUI yönetsin
        bool isSignedIn = IsUserSignedInWithGoogle();
        
        if (signInButton != null) 
        {
            signInButton.gameObject.SetActive(!isSignedIn);
            Debug.Log($"[GoogleAuthentication] Sign-in button visibility: {!isSignedIn}");
        }
    }

    private void LoadProfilePicture()
    {
        if (imageLoadCoroutine != null)
        {
            StopCoroutine(imageLoadCoroutine);
        }

        if (!string.IsNullOrEmpty(PlayerProfileManager.instance.ProfilePictureURL))
        {
            imageLoadCoroutine = StartCoroutine(LoadImageFromUrl(PlayerProfileManager.instance.ProfilePictureURL));
        }
        else
        {
            // Firebase Auth'dan profil resmi URL'ini almaya çalış
            var currentUser = FirebaseManager.instance?.auth?.CurrentUser;
            if (currentUser?.PhotoUrl != null)
            {
                imageLoadCoroutine = StartCoroutine(LoadImageFromUrl(currentUser.PhotoUrl.ToString()));
            }
            else
            {
                if (profilePic != null)
                {
                    profilePic.sprite = null;
                    profilePic.color = new Color(1, 1, 1, 0.2f);
                }
            }
        }
    }

    private IEnumerator LoadImageFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url) || profilePic == null) yield break;

        using (var www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                if (texture != null && profilePic != null)
                {
                    profilePic.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    profilePic.color = Color.white;
                }
            }
            else
            {
                Debug.LogWarning($"Profil resmi yüklenemedi: {www.error}");
                if (profilePic != null)
                {
                    profilePic.sprite = null;
                    profilePic.color = new Color(1, 1, 1, 0.2f);
                }
            }
        }
    }

    private void SetButtonsInteractable(bool state)
    {
        if (signInButton != null) signInButton.interactable = state;
        if (signOutButton != null) signOutButton.interactable = state;
    }

    private void ShowFeedback(string message, bool isSuccess)
    {
        // UIManager üzerinden bildirim göster
        if (UIManager.instance != null)
        {
            UIManager.instance.ShowNotification(message);
        }
        else
        {
            Debug.Log($"Feedback: {message}");
        }
    }
    
}
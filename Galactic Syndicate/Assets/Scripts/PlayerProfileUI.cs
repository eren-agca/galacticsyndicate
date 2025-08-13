// PlayerProfileUI.cs - DÜZELTİLMİŞ HALİ

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.Networking;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class PlayerProfileUI : MonoBehaviour
{
    [Header("Panel & UI References")]
    public GameObject profilePanel;
    public Image profilePicture;
    public Button changePictureButton;
    public GameObject loadingIndicator;
    
    [Header("Username Section")]
    [Tooltip("Mevcut kullanıcı adını gösteren metin alanı.")]
    public TextMeshProUGUI usernameText; 
    [Tooltip("Yeni kullanıcı adının girileceği alan.")]
    public TMP_InputField usernameInputField;
    [Tooltip("İsim değiştirme işlemini başlatan buton.")]
    public Button updateUsernameButton;
    [Tooltip("Misafir hesabını Google'a bağlayan buton.")]
    public Button linkWithGoogleButton;

    private Coroutine imageLoadCoroutine;
    private bool isLoadingProfile = false; // Panel açılırken profil yükleme durumu
    private bool isToggling = false; // Panel toggle işlemi devam ediyor mu?

    void Start()
    {
        if (profilePanel != null) profilePanel.SetActive(false);
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        
        // Debug için log ekleyelim
        Debug.Log("[PlayerProfileUI] Component initialized");
    }

    void OnEnable()
    {
        Debug.Log("[PlayerProfileUI] OnEnable called");
        if (PlayerProfileManager.instance != null)
        {
            PlayerProfileManager.instance.OnProfileUpdated += UpdateUI;
        }
        
        // OnEnable'da UpdateUI çağırmayalım çünkü panel kapalıyken tetikleniyor olabilir
        // UpdateUI sadece panel açıkken çağrılmalı
    }

    void OnDisable()
    {
        Debug.Log("[PlayerProfileUI] OnDisable called");
        if (PlayerProfileManager.instance != null)
        {
            PlayerProfileManager.instance.OnProfileUpdated -= UpdateUI;
        }
    }

    /// <summary>
    /// Profil panelini açar veya kapatır.
    /// Panel açılırken, her zaman en güncel veriyi sunucudan çeker.
    /// </summary>
    public async void TogglePanel()
    {
        Debug.Log($"[PlayerProfileUI] TogglePanel called. Current panel state: {(profilePanel != null ? profilePanel.activeSelf : "null")}, isToggling: {isToggling}");
        
        // Zaten bir toggle işlemi devam ediyorsa, yeni işlem başlatma
        if (isToggling)
        {
            Debug.LogWarning("[PlayerProfileUI] Toggle already in progress. Ignoring new toggle request.");
            return;
        }

        // Panel referansı kontrol et
        if (profilePanel == null)
        {
            Debug.LogError("[PlayerProfileUI] profilePanel is null!");
            return;
        }

        isToggling = true;

        try
        {
            bool isPanelCurrentlyActive = profilePanel.activeSelf;
            
            if (isPanelCurrentlyActive)
            {
                // Panel açıksa, sadece kapat
                Debug.Log("[PlayerProfileUI] Closing panel");
                profilePanel.SetActive(false);
            }
            else
            {
                // Panel kapalıysa, açma işlemini başlat
                Debug.Log("[PlayerProfileUI] Opening panel and loading profile data");
                await OpenPanelAndLoadProfile();
            }
        }
        finally
        {
            isToggling = false;
            Debug.Log("[PlayerProfileUI] Toggle operation completed");
        }
    }

    private async Task OpenPanelAndLoadProfile()
    {
        if (isLoadingProfile)
        {
            Debug.LogWarning("[PlayerProfileUI] Profile already loading, skipping...");
            return;
        }
        
        isLoadingProfile = true;
        
        try
        {
            // Paneli hemen göster
            Debug.Log("[PlayerProfileUI] Setting panel active to true");
            profilePanel.SetActive(true);
            
            // Kısa bir bekle - UI'ın güncellenmesi için
            await Task.Yield();
            
            // Panel gerçekten açık mı kontrol et
            if (!profilePanel.activeSelf)
            {
                Debug.LogError("[PlayerProfileUI] Panel failed to activate!");
                return;
            }
            
            // Loading göstergesini aktifleştir
            if (loadingIndicator != null) 
            {
                loadingIndicator.SetActive(true);
                Debug.Log("[PlayerProfileUI] Loading indicator activated");
            }
            
            SetButtonsInteractable(false);

            // PlayerProfileManager kontrolü
            if (PlayerProfileManager.instance == null)
            {
                Debug.LogError("[PlayerProfileUI] PlayerProfileManager.instance is null!");
                ShowFeedback("Profil sistemi bulunamadı!", false);
                profilePanel.SetActive(false);
                return;
            }

            // Profil verilerini çek
            Debug.Log("[PlayerProfileUI] Fetching user profile...");
            await PlayerProfileManager.instance.FetchUserProfile();
            Debug.Log("[PlayerProfileUI] Profile data fetched successfully");
            
            // Son kontrol: Panel hala açık mı?
            if (profilePanel != null && profilePanel.activeSelf)
            {
                // UI'ı güncelle (OnProfileUpdated event'i otomatik olarak tetiklenir)
                Debug.Log("[PlayerProfileUI] Calling UpdateUI");
                UpdateUI();
            }
            else
            {
                Debug.LogWarning("[PlayerProfileUI] Panel was closed during loading process");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerProfileUI] Error while opening panel: {e.Message}");
            Debug.LogError($"[PlayerProfileUI] Stack trace: {e.StackTrace}");
            ShowFeedback("Profil verisi yüklenemedi.", false);
            
            // Hata durumunda paneli güvenli bir şekilde kapat
            if (profilePanel != null)
            {
                profilePanel.SetActive(false);
            }
        }
        finally
        {
            // İşlem bittiğinde loading'i kapat ve butonları aktifleştir
            if (loadingIndicator != null) 
            {
                loadingIndicator.SetActive(false);
                Debug.Log("[PlayerProfileUI] Loading indicator deactivated");
            }
            
            SetButtonsInteractable(true);
            isLoadingProfile = false;
            
            Debug.Log("[PlayerProfileUI] Panel opening process completed");
        }
    }

    private void UpdateUI()
    {
        // GameObject yok edilmişse hiçbir şey yapma
        if (this == null || gameObject == null)
        {
            Debug.LogWarning("[PlayerProfileUI] GameObject is null in UpdateUI");
            return;
        }

        // Panel aktif değilse UpdateUI yapmaya gerek yok
        if (profilePanel == null || !profilePanel.activeSelf)
        {
            Debug.Log("[PlayerProfileUI] UpdateUI called but panel is not active");
            return;
        }

        if (PlayerProfileManager.instance == null) 
        {
            Debug.LogWarning("[PlayerProfileUI] PlayerProfileManager.instance is null in UpdateUI");
            return;
        }

        Debug.Log($"[PlayerProfileUI] UpdateUI - Username: {PlayerProfileManager.instance.PlayerUsername}");

        // Username'i güncelle
        if (usernameText != null)
        {
            usernameText.text = PlayerProfileManager.instance.PlayerUsername ?? "Misafir";
        }

        // Profil resmini yükle
        if (!string.IsNullOrEmpty(PlayerProfileManager.instance.ProfilePictureURL))
        {
            if (imageLoadCoroutine != null)
            {
                StopCoroutine(imageLoadCoroutine);
            }
            imageLoadCoroutine = StartCoroutine(LoadImageFromUrl(PlayerProfileManager.instance.ProfilePictureURL, profilePicture));
        }
        else
        {
            if (profilePicture != null)
            {
                profilePicture.sprite = null;
                profilePicture.color = new Color(1, 1, 1, 0.2f);
            }
        }

        // Sadece misafir kullanıcılar için "Google ile Kaydet" butonunu göster
        if (linkWithGoogleButton != null)
        {
            bool shouldShowLinkButton = PlayerProfileManager.instance.IsGuestUser;
            linkWithGoogleButton.gameObject.SetActive(shouldShowLinkButton);
            Debug.Log($"[PlayerProfileUI] Link button visibility: {shouldShowLinkButton} (IsGuest: {PlayerProfileManager.instance.IsGuestUser})");
        }
    }

    public void OnChangePictureClicked()
    {
        Debug.Log("[PlayerProfileUI] Change picture button clicked");
        
        #if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
            if (UIManager.instance != null) UIManager.instance.ShowNotification("Lütfen depolama izni verin.");
            return;
        }
        #endif

        if (NativeFilePicker.IsFilePickerBusy()) return;

        string[] fileTypes = { "image/png", "image/jpeg" };

        NativeFilePicker.PickFile(async (path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("Dosya seçimi iptal edildi.");
                return;
            }
            
            if (loadingIndicator != null) loadingIndicator.SetActive(true);
            SetButtonsInteractable(false);
            try
            {
                var (success, message) = await PlayerProfileManager.instance.UploadProfilePicture(path);
                ShowFeedback(message, success);
            }
            catch (System.Exception e)
            {
                ShowFeedback("Bir hata oluştu.", false);
                Debug.LogError($"Resim yükleme UI'da yakalanan hata: {e.Message}");
            }
            finally
            {
                if (loadingIndicator != null) loadingIndicator.SetActive(false);
                SetButtonsInteractable(true);
            }
        }, fileTypes);
    }

    public async void OnUpdateUsernameClicked()
    {
        Debug.Log("[PlayerProfileUI] Update username button clicked");
        
        if (PlayerProfileManager.instance == null || string.IsNullOrWhiteSpace(usernameInputField.text))
        {
            Debug.LogWarning("[PlayerProfileUI] Username update failed - invalid input");
            return;
        }

        if (usernameInputField.text == PlayerProfileManager.instance.PlayerUsername)
        {
            ShowFeedback("Mevcut isimle aynı.", false);
            return;
        }

        if (loadingIndicator != null) loadingIndicator.SetActive(true);
        SetButtonsInteractable(false);
        try
        {
            var (success, message) = await PlayerProfileManager.instance.ChangeUsername(usernameInputField.text);
            ShowFeedback(message, success);
            if (success)
            {
                usernameInputField.text = "";
            }
        }
        catch (System.Exception e)
        {
            ShowFeedback("Bir hata oluştu.", false);
            Debug.LogError($"İsim değiştirme UI'da yakalanan hata: {e.Message}");
        }
        finally
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            SetButtonsInteractable(true);
        }
    }

    public async void OnLinkWithGoogleClicked()
    {
        Debug.Log("[PlayerProfileUI] Link with Google button clicked");
        
        if (PlayerProfileManager.instance == null) return;

        SetButtonsInteractable(false);

        var (success, message) = await PlayerProfileManager.instance.LinkAccountWithGoogleAsync();

        ShowFeedback(message, success);
        SetButtonsInteractable(true);
    }

    private void ShowFeedback(string message, bool isSuccess)
    {
        if (UIManager.instance != null)
        {
            UIManager.instance.ShowNotification(message);
        }
        else
        {
            Debug.LogWarning($"[PlayerProfileUI] UIManager not found. Feedback: {message}");
        }
    }

    private void SetButtonsInteractable(bool state)
    {
        if (changePictureButton != null) changePictureButton.interactable = state;
        if (updateUsernameButton != null) updateUsernameButton.interactable = state;
        if (linkWithGoogleButton != null) linkWithGoogleButton.interactable = state;
    }

    private IEnumerator LoadImageFromUrl(string url, Image targetImage)
    {
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            yield return webRequest.SendWebRequest();

            if (this == null) yield break;

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                if (targetImage != null)
                {
                    targetImage.sprite = sprite;
                    targetImage.color = Color.white;
                }
            }
            else
            {
                Debug.LogError($"Resim yüklenemedi: {url} - Hata: {webRequest.error}");
            }
        }
    }
}
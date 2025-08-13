// PlayerProfileUI.cs - DÜZELTİLMİŞ VERSİYON

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
    public TextMeshProUGUI usernameText; 
    public TMP_InputField usernameInputField;
    public Button updateUsernameButton;
    public Button linkWithGoogleButton;

    private Coroutine imageLoadCoroutine;
    private bool isLoadingProfile = false;
    private bool isToggling = false;
    private bool isEventSubscribed = false; // EVENT ABONELİK DURUMU TAKIBI

    void Start()
    {
        if (profilePanel != null) profilePanel.SetActive(false);
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        
        Debug.Log("[PlayerProfileUI] Component initialized");
    }

    void OnEnable()
    {
        Debug.Log("[PlayerProfileUI] OnEnable called");
        SubscribeToEvents();
    }

    void OnDisable()
    {
        Debug.Log("[PlayerProfileUI] OnDisable called");
        UnsubscribeFromEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    // EVENT YÖNETİMİNİ AYRI METODLARA ÇEKTİK
    private void SubscribeToEvents()
    {
        if (!isEventSubscribed && PlayerProfileManager.instance != null)
        {
            PlayerProfileManager.instance.OnProfileUpdated += UpdateUI;
            isEventSubscribed = true;
            Debug.Log("[PlayerProfileUI] Subscribed to OnProfileUpdated event");
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (isEventSubscribed && PlayerProfileManager.instance != null)
        {
            PlayerProfileManager.instance.OnProfileUpdated -= UpdateUI;
            isEventSubscribed = false;
            Debug.Log("[PlayerProfileUI] Unsubscribed from OnProfileUpdated event");
        }
    }

    public async void TogglePanel()
    {
        Debug.Log($"[PlayerProfileUI] TogglePanel called. Panel active: {(profilePanel != null ? profilePanel.activeSelf : "null")}, isToggling: {isToggling}");
        
        if (isToggling)
        {
            Debug.LogWarning("[PlayerProfileUI] Toggle already in progress. Ignoring.");
            return;
        }

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
                Debug.Log("[PlayerProfileUI] Closing panel");
                ClosePanel();
            }
            else
            {
                Debug.Log("[PlayerProfileUI] Opening panel");
                await OpenPanel();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerProfileUI] Exception in TogglePanel: {e.Message}");
            Debug.LogError($"[PlayerProfileUI] Stack trace: {e.StackTrace}");
            
            // Hata durumunda paneli güvenli şekilde kapat
            if (profilePanel != null && profilePanel.activeSelf)
            {
                ClosePanel();
            }
        }
        finally
        {
            isToggling = false;
            Debug.Log("[PlayerProfileUI] Toggle operation completed");
        }
    }

    private void ClosePanel()
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(false);
            Debug.Log("[PlayerProfileUI] Panel closed");
        }
    }

    private async Task OpenPanel()
    {
        if (isLoadingProfile)
        {
            Debug.LogWarning("[PlayerProfileUI] Profile already loading, skipping...");
            return;
        }
        
        isLoadingProfile = true;
        
        try
        {
            // 1. ÖNCE PANELİ AÇ
            Debug.Log("[PlayerProfileUI] Setting panel active");
            profilePanel.SetActive(true);
            
            // 2. UI ELEMENT VALİDASYONU
            if (!ValidateUIElements())
            {
                Debug.LogError("[PlayerProfileUI] UI validation failed");
                ClosePanel();
                return;
            }
            
            // 3. LOADING GÖSTERGESİNİ AKTIVE ET
            if (loadingIndicator != null) 
            {
                loadingIndicator.SetActive(true);
                Debug.Log("[PlayerProfileUI] Loading indicator activated");
            }
            
            SetButtonsInteractable(false);

            // 4. MANAGER KONTROLÜ
            if (PlayerProfileManager.instance == null)
            {
                Debug.LogError("[PlayerProfileUI] PlayerProfileManager.instance is null!");
                ShowFeedback("Profil sistemi bulunamadı!", false);
                ClosePanel();
                return;
            }

            // 5. PROFİL VERİSİNİ ÇEK
            Debug.Log("[PlayerProfileUI] Fetching user profile...");
            
            // TASK TIMEOUT EKLEDİK
            var profileTask = PlayerProfileManager.instance.FetchUserProfile();
            var timeoutTask = Task.Delay(10000); // 10 saniye timeout
            
            var completedTask = await Task.WhenAny(profileTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                Debug.LogError("[PlayerProfileUI] Profile fetch timed out");
                ShowFeedback("Profil verisi yüklenemedi (timeout).", false);
                ClosePanel();
                return;
            }

            await profileTask; // Gerçek task'i bekle
            Debug.Log("[PlayerProfileUI] Profile data fetched successfully");
            
            // 6. SON KONTROLLER
            if (profilePanel != null && profilePanel.activeSelf)
            {
                Debug.Log("[PlayerProfileUI] Manually calling UpdateUI");
                UpdateUI();
            }
            else
            {
                Debug.LogWarning("[PlayerProfileUI] Panel was closed during loading");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerProfileUI] Error in OpenPanel: {e.Message}");
            ShowFeedback("Profil verisi yüklenemedi.", false);
            ClosePanel();
        }
        finally
        {
            if (loadingIndicator != null) 
            {
                loadingIndicator.SetActive(false);
            }
            
            SetButtonsInteractable(true);
            isLoadingProfile = false;
            Debug.Log("[PlayerProfileUI] Panel opening process completed");
        }
    }

    private bool ValidateUIElements()
    {
        if (profilePanel == null)
        {
            Debug.LogError("[PlayerProfileUI] profilePanel is null");
            return false;
        }
        
        if (usernameText == null)
        {
            Debug.LogError("[PlayerProfileUI] usernameText is null");
            return false;
        }
        
        // Diğer kritik UI elementlerini de kontrol et
        return true;
    }

    private void UpdateUI()
    {
        // EXTRA GÜVENLİK KONTROLLERI
        if (this == null || gameObject == null)
        {
            Debug.LogWarning("[PlayerProfileUI] GameObject is null in UpdateUI");
            return;
        }

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

        try
        {
            Debug.Log($"[PlayerProfileUI] UpdateUI - Username: {PlayerProfileManager.instance.PlayerUsername}");

            // Username'i güncelle
            if (usernameText != null)
            {
                string displayName = PlayerProfileManager.instance.PlayerUsername ?? "Misafir";
                usernameText.text = displayName;
                Debug.Log($"[PlayerProfileUI] Username set to: {displayName}");
            }

            // Profil resmini yükle
            LoadProfilePicture();

            // Link butonu kontrolü
            UpdateLinkButtonVisibility();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerProfileUI] Exception in UpdateUI: {e.Message}");
            // UI güncellemesi hatası panel kapanmasına sebep olmamalı
        }
    }

    private void LoadProfilePicture()
    {
        try
        {
            if (imageLoadCoroutine != null)
            {
                StopCoroutine(imageLoadCoroutine);
            }

            if (!string.IsNullOrEmpty(PlayerProfileManager.instance.ProfilePictureURL))
            {
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
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerProfileUI] Error loading profile picture: {e.Message}");
        }
    }

    private void UpdateLinkButtonVisibility()
    {
        try
        {
            if (linkWithGoogleButton != null)
            {
                bool shouldShowLinkButton = PlayerProfileManager.instance.IsGuestUser;
                linkWithGoogleButton.gameObject.SetActive(shouldShowLinkButton);
                Debug.Log($"[PlayerProfileUI] Link button visibility: {shouldShowLinkButton} (IsGuest: {PlayerProfileManager.instance.IsGuestUser})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerProfileUI] Error updating link button: {e.Message}");
        }
    }

    // Diğer metodlar aynı kalır...
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
    // PlayerProfileUI.cs içine ekleyin
    private void Update()
    {
        if (profilePanel != null && profilePanel.activeSelf != lastPanelState)
        {
            Debug.LogWarning($"[DEBUG] Panel state changed from {lastPanelState} to {profilePanel.activeSelf}");
            lastPanelState = profilePanel.activeSelf;
        
            // Stack trace ile hangi kod parçasının panel durumunu değiştirdiğini bulun
            Debug.LogWarning($"[DEBUG] Stack trace: {System.Environment.StackTrace}");
        }
    }

    private bool lastPanelState = false;
}
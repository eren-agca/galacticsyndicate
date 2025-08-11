// PlayerProfileUI.cs - YENİ VE GELİŞTİRİLMİŞ HALİ

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
    public TextMeshProUGUI feedbackText;
    
    // --- YENİDEN DÜZENLENMİŞ KULLANICI ADI ALANI ---
    [Header("Username Section")]
    [Tooltip("Mevcut kullanıcı adını gösteren metin alanı.")]
    public TextMeshProUGUI usernameText; 
    [Tooltip("Yeni kullanıcı adının girileceği alan.")]
    public TMP_InputField usernameInputField;
    [Tooltip("İsim değiştirme işlemini başlatan buton.")]
    public Button updateUsernameButton;
    [Tooltip("Misafir hesabını Google'a bağlayan buton.")]
    public Button linkWithGoogleButton;

    private Coroutine feedbackCoroutine;
    private Coroutine imageLoadCoroutine;

    void Start()
    {
        if (profilePanel != null) profilePanel.SetActive(false);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (PlayerProfileManager.instance != null)
        {
            PlayerProfileManager.instance.OnProfileUpdated += UpdateUI;
        }
        UpdateUI();
    }

    void OnDisable()
    {
        if (PlayerProfileManager.instance != null)
        {
            PlayerProfileManager.instance.OnProfileUpdated -= UpdateUI;
        }
    }

    public void TogglePanel()
    {
        if (profilePanel == null) return;
        bool isOpening = !profilePanel.activeSelf;
        profilePanel.SetActive(isOpening);
        if (isOpening)
        {
            UpdateUI();
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        }
    }

    private void UpdateUI()
    {
        if (PlayerProfileManager.instance == null) return;

        usernameText.text = PlayerProfileManager.instance.PlayerUsername;
        
        // Input alanını her güncellemede temizlemek, kullanıcı deneyimini iyileştirir.
        // usernameInputField.text = ""; 

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
            profilePicture.sprite = null;
            profilePicture.color = new Color(1, 1, 1, 0.2f);
        }

        // Sadece misafir kullanıcılar için "Google ile Kaydet" butonunu göster.
        if (linkWithGoogleButton != null)
            linkWithGoogleButton.gameObject.SetActive(PlayerProfileManager.instance.IsGuestUser);
    }

    public void OnChangePictureClicked()
    {
        if (NativeFilePicker.IsFilePickerBusy()) return;

        string[] fileTypes = { "image/png", "image/jpeg" };

        NativeFilePicker.PickFile(async (path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("Dosya seçimi iptal edildi.");
                return;
            }
            
            SetButtonsInteractable(false);
            var (success, message) = await PlayerProfileManager.instance.UploadProfilePicture(path);
            ShowFeedback(message, success);
            SetButtonsInteractable(true);
        }, fileTypes);
    }

    public async void OnUpdateUsernameClicked()
    {
        if (PlayerProfileManager.instance == null || string.IsNullOrWhiteSpace(usernameInputField.text))
        {
            return;
        }

        if (usernameInputField.text == PlayerProfileManager.instance.PlayerUsername)
        {
            ShowFeedback("Mevcut isimle aynı.", false);
            return;
        }

        SetButtonsInteractable(false);
        
        var (success, message) = await PlayerProfileManager.instance.ChangeUsername(usernameInputField.text);
        
        ShowFeedback(message, success);
        SetButtonsInteractable(true);

        if (success)
        {
            // Başarılı olunca input alanını temizle.
            usernameInputField.text = "";
        }
    }

    public async void OnLinkWithGoogleClicked()
    {
        if (PlayerProfileManager.instance == null) return;

        SetButtonsInteractable(false);

        var (success, message) = await PlayerProfileManager.instance.LinkAccountWithGoogleAsync();

        ShowFeedback(message, success);
        SetButtonsInteractable(true);
    }

    private void ShowFeedback(string message, bool isSuccess)
    {
        if (feedbackText == null) return;

        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }
        feedbackCoroutine = StartCoroutine(FeedbackCoroutine(message, isSuccess));
    }

    private IEnumerator FeedbackCoroutine(string message, bool isSuccess)
    {
        feedbackText.text = message;
        feedbackText.color = isSuccess ? Color.green : Color.red;
        feedbackText.gameObject.SetActive(true);

        yield return new WaitForSeconds(3f);

        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
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
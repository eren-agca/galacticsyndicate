// SyndicateUI.cs - GÜNCELLENDİ (Panel geçişlerine log eklendi)

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks; 
using System;
using UnityEngine.Networking; 
#if UNITY_ANDROID
// --- YENİ EKLENEN SATIR ---
// Android'deki çalışma zamanı izinlerini (runtime permissions) yönetmek için
// bu using direktifi gereklidir.
using UnityEngine.Android;
#endif

public class SyndicateUI : MonoBehaviour
{
    [Header("Ana Paneller")]
    public GameObject syndicateMainPanel;
    public GameObject syndicateHubPanel;
    public GameObject createSyndicatePanel;
    public GameObject mySyndicatePanel;
    public GameObject syndicateBrowserPanel; // YENİ
    public GameObject membersPanel;
    public GameObject loadingIndicator;
    
    [Tooltip("Liderin geliştirmeleri açacağı panel.")]
    public GameObject syndicateUpgradePanel; 

    [Header("Hub Butonları")]
    public Button createSyndicateHubButton;
    public Button mySyndicateHubButton;
    public Button findSyndicateHubButton; // YENİ

    [Header("Sendika Bilgi Ekranı")]
    public TextMeshProUGUI syndicateNameText;
    public TextMeshProUGUI syndicateTagText;
    public TextMeshProUGUI syndicateDescriptionText;
    public TextMeshProUGUI memberCountText;
    public TextMeshProUGUI syndicateTreasuryText;
    public Image syndicateEmblemImage;
    public Button leaveButton;
    public Button viewMembersButton;
    
    [Tooltip("Sadece liderin göreceği geliştirme butonu.")]
    public Button leaderUpgradesButton;
    [Tooltip("Sadece liderin göreceği amblem değiştirme butonu.")]
    public Button changeEmblemButton;

    [Header("Sendika Tarayıcısı")] // YENİ
    public GameObject browserContent;
    public GameObject browserSlotPrefab;
    public Button browserRefreshButton;
    [Header("Sendika Üye Listesi")] public Transform membersContent;
    public GameObject memberSlotPrefab;

    [Header("Sendika Oluşturma Formu")]
    public TMP_InputField createNameInput;
    public TMP_InputField createTagInput;
    public TMP_InputField createDescriptionInput;
    public Button createButton;

    void Start()
    {
        if (syndicateMainPanel != null)
        {
            syndicateMainPanel.SetActive(false);
        }
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        
        if (syndicateBrowserPanel != null) syndicateBrowserPanel.SetActive(false);
        if (membersPanel != null) membersPanel.SetActive(false);
        if (syndicateUpgradePanel != null) syndicateUpgradePanel.SetActive(false);
    }

    void OnEnable()
    {
        if (SyndicateManager.instance != null)
        {
            SyndicateManager.instance.OnSyndicateDataUpdated += UpdateAllSyndicatePanels;
        }
        UpdateAllSyndicatePanels();
    }

    void OnDisable()
    {
        if (SyndicateManager.instance != null)
        {
            SyndicateManager.instance.OnSyndicateDataUpdated -= UpdateAllSyndicatePanels;
        }
    }

    private void UpdateAllSyndicatePanels()
    {
        if (SyndicateManager.instance == null) return;
        UpdateHubButtons();
        UpdateMySyndicateView();
    }

    // --- YENİ TOGGLE METODU ---
    /// <summary>
    /// Sendika panelini açar veya kapatır (toggle).
    /// Açarken, verilerin güncel olduğundan emin olmak için sunucudan veri çeker.
    /// Bu, "çift tıklama" sorununu çözer.
    /// </summary>
    public async void ToggleSyndicatePanel()
    {
        if (syndicateMainPanel == null) return;

        bool isOpening = !syndicateMainPanel.activeSelf;

        if (isOpening)
        {
            syndicateMainPanel.SetActive(true);
            loadingIndicator.SetActive(true);
            // Diğer panellerin kapalı olduğundan emin ol, özellikle hub paneli.
            syndicateHubPanel.SetActive(false);

            try
            {
                // Eğer oyuncunun bir sendikası varsa, en güncel veriyi çek.
                if (!string.IsNullOrEmpty(SyndicateManager.instance.CurrentSyndicateId))
                {
                    await SyndicateManager.instance.RefreshCurrentSyndicateData();
                }

                // İşlem bittikten sonra (veya gerekmiyorsa hemen) ana hub'ı göster.
                ShowHubPanel();
            }
            catch (Exception e)
            {
                // Bir hata oluşursa, kullanıcıyı bilgilendir ve paneli güvenli bir şekilde kapat.
                Debug.LogError($"[SyndicateUI] Sendika paneli açılırken hata: {e.Message}");
                // UIManager.instance?.ShowNotification("Sendika verisi yüklenemedi.");
                CloseMainPanel(); // Hata durumunda paneli kapatmak, kilitlenmeyi önler.
            }
            finally
            {
                // Bu blok, işlem başarılı da olsa hata da olsa HER ZAMAN çalışır.
                // Yükleniyor göstergesinin ekranda takılı kalmasını engeller.
                if (loadingIndicator != null)
                {
                    loadingIndicator.SetActive(false);
                }
            }
        }
        else
        {
            // Panel zaten açıksa, sadece kapat.
            CloseMainPanel();
        }
    }

    /// <summary>
    /// Ana sendika panelini kapatır. Bu, 'X' butonu gibi bir kapatma işlevi için kullanılır.
    /// </summary>
    public void CloseMainPanel()
    {
        if (syndicateMainPanel != null)
        {
            Debug.Log("[SyndicateUI] Closing syndicate panel.");
            syndicateMainPanel.SetActive(false);
        }
    }

    public void ShowHubPanel()
    {
        // --- YENİ LOG ---
        Debug.Log("[SyndicateUI] ShowHubPanel çağrıldı.");
        syndicateHubPanel.SetActive(true);
        createSyndicatePanel.SetActive(false);
        mySyndicatePanel.SetActive(false);
        syndicateBrowserPanel.SetActive(false);
        membersPanel.SetActive(false);
        // ARTIK GEREKLİ DEĞİL: if (leaderboardPanel != null) leaderboardPanel.gameObject.SetActive(false);
        syndicateUpgradePanel.SetActive(false);
        loadingIndicator.SetActive(false);
        UpdateHubButtons();
    }

    public void ShowCreateSyndicatePanel()
    {
        syndicateHubPanel.SetActive(false);
        createSyndicatePanel.SetActive(true);
        loadingIndicator.SetActive(false);
    }

    public void ShowMySyndicatePanel()
    {
        syndicateHubPanel.SetActive(false);
        mySyndicatePanel.SetActive(true);
        if (membersPanel != null) membersPanel.SetActive(false);
        loadingIndicator.SetActive(false);
        UpdateMySyndicateView();
    }
    
    private void UpdateHubButtons()
    {
        if (SyndicateManager.instance == null) return;
        bool hasSyndicate = SyndicateManager.instance.CurrentSyndicate != null;
        createSyndicateHubButton.gameObject.SetActive(!hasSyndicate);
        mySyndicateHubButton.gameObject.SetActive(hasSyndicate);
        // Sendikası olmayana "Bul" butonunu göster.
        findSyndicateHubButton.gameObject.SetActive(!hasSyndicate);
    }

    public void ShowUpgradePanel()
    {
        // --- YENİ LOG ---
        Debug.Log("[SyndicateUI] ShowUpgradePanel çağrıldı.");
        if (syndicateUpgradePanel != null)
        {
            syndicateUpgradePanel.SetActive(true);
            mySyndicatePanel.SetActive(false); 
        }
    }

    private void UpdateMySyndicateView()
    {
        if (mySyndicatePanel.activeSelf == false || SyndicateManager.instance?.CurrentSyndicate == null)
        {
            if (viewMembersButton != null)
            {
                viewMembersButton.gameObject.SetActive(false);
            }
            if (leaderUpgradesButton != null) leaderUpgradesButton.gameObject.SetActive(false);
            return;
        }
        
        SyndicateData currentSyndicate = SyndicateManager.instance.CurrentSyndicate;
        syndicateNameText.text = currentSyndicate.SyndicateName;
        syndicateTagText.text = $"[{currentSyndicate.Tag}]";
        syndicateDescriptionText.text = currentSyndicate.Description;
        memberCountText.text = $"Üyeler: {currentSyndicate.MemberIDs.Count}";
        syndicateTreasuryText.text = $"Hazine: {currentSyndicate.Treasury:N0}c";
        
        if (syndicateEmblemImage != null)
        {
            if (!string.IsNullOrEmpty(currentSyndicate.EmblemURL))
            {
                StartCoroutine(LoadImageFromUrl(currentSyndicate.EmblemURL, syndicateEmblemImage));
            }
            else
            {
                syndicateEmblemImage.sprite = null;
                syndicateEmblemImage.color = new Color(1, 1, 1, 0.2f);
            }
        }

        if (viewMembersButton != null)
        {
            viewMembersButton.gameObject.SetActive(true);
        }

        bool isLeader = currentSyndicate.LeaderID == FirebaseManager.instance.UserID;
        if (changeEmblemButton != null)
        {
            changeEmblemButton.gameObject.SetActive(isLeader);
        }
        if (leaderUpgradesButton != null)
        {
            leaderUpgradesButton.gameObject.SetActive(isLeader);
        }
    }

    public async void OnCreateSyndicateClicked()
    {
        if (string.IsNullOrWhiteSpace(createNameInput.text) || string.IsNullOrWhiteSpace(createTagInput.text))
        {
            Debug.LogError("Sendika adı ve etiketi boş bırakılamaz!");
            return;
        }
        loadingIndicator.SetActive(true);
        bool success = await SyndicateManager.instance.CreateSyndicate(
            createNameInput.text, createTagInput.text, createDescriptionInput.text);
        loadingIndicator.SetActive(false);
        if (success)
        {
            ShowHubPanel();
        }
    }

    public async void OnLeaveSyndicateClicked()
    {
        loadingIndicator.SetActive(true);
        await SyndicateManager.instance.LeaveSyndicate();
        loadingIndicator.SetActive(false);
        ShowHubPanel();
    }

    public void ShowSyndicateBrowser()
    {
        syndicateHubPanel.SetActive(false);
        syndicateBrowserPanel.SetActive(true);
        _ = LoadAndDisplayPublicSyndicates();
    }

    public async void OnBrowserRefreshClicked()
    {
        await LoadAndDisplayPublicSyndicates();
    }

    private async Task LoadAndDisplayPublicSyndicates()
    {
        loadingIndicator.SetActive(true);

        var syndicates = await SyndicateManager.instance.FindPublicSyndicates();

        loadingIndicator.SetActive(false);

        if (this != null) // Obje yok edilmediyse devam et
        {
            StartCoroutine(RebuildListCoroutine(browserContent.transform, syndicates, browserSlotPrefab, (slotGO, info) =>
            {
                slotGO.GetComponent<SyndicateBrowserSlotUI>().Setup(info, async (id) =>
                {
                    if (await SyndicateManager.instance.JoinSyndicate(id)) ShowHubPanel();
                });
            }));
        }
    }

    public void ShowMembersPanel()
    {
        mySyndicatePanel.SetActive(false);
        membersPanel.SetActive(true);
        _ = LoadAndDisplayMembers();
    }

    public void HideMembersPanel()
    {
        membersPanel.SetActive(false);
        ShowMySyndicatePanel();
    }

    private async Task LoadAndDisplayMembers()
    {
        loadingIndicator.SetActive(true);

        var members = await SyndicateManager.instance.GetSyndicateMembers();

        loadingIndicator.SetActive(false);

        if (this != null) // Obje yok edilmediyse devam et
        {
            StartCoroutine(RebuildListCoroutine(membersContent, members, memberSlotPrefab, (slotGO, member) =>
            {
            slotGO.GetComponent<SyndicateMemberSlotUI>().Setup(member.Name, member.IsLeader, member.ProfilePictureUrl);
            }));
        }
    }

    public void OnChangeEmblemClicked()
    {
        // Eğer zaten bir dosya seçme işlemi devam ediyorsa, yenisini başlatmayı engelle.
        if (NativeFilePicker.IsFilePickerBusy())
        {
            Debug.LogWarning("Dosya seçici zaten meşgul.");
            return;
        }

        // --- YENİ GÜVENLİK ADIMI: Android İzin Kontrolü ---
        // Mobilde bu özelliğin çalışması için kullanıcının depolama izni vermesi gerekir.
        #if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            // İzin iste ve sonucu bekle. Kullanıcı izin verdiğinde butona tekrar basması gerekecek.
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
            UIManager.instance?.ShowNotification("Lütfen depolama izni verin.");
            return;
        }
        #endif

        // Sadece resim dosyalarını seçtirmek için MIME tiplerini belirtiyoruz.
        string[] fileTypes = { "image/png", "image/jpeg" };

        // Dosya seçme işlemini başlat. Sonuç, bir "callback" (geri arama) fonksiyonu ile dönecek.
        NativeFilePicker.PickFile((path) =>
        {
            // Kullanıcı bir dosya seçtiğinde veya işlemi iptal ettiğinde bu kod çalışır.
            if (path != null)
            {
                // Dosya başarıyla seçildi. Yükleme işlemini asenkron olarak başlat.
                Debug.Log("Seçilen dosya: " + path);
                _ = UploadEmblemAsync(path);
            }
            else
            {
                Debug.Log("Dosya seçimi iptal edildi.");
            }
        }, fileTypes);
    }

    private async Task UploadEmblemAsync(string path)
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(true);
        
        try
        {
            bool success = await SyndicateManager.instance.UploadSyndicateEmblem(path);
            if (success)
            {
                UIManager.instance?.ShowNotification("Amblem başarıyla güncellendi!");
            }
            else
            {
                UIManager.instance?.ShowNotification("Amblem yüklenemedi.");
                Debug.LogError("Amblem yükleme işlemi SyndicateManager tarafından başarısız olarak raporlandı.");
            }
        }
        catch (System.Exception e)
        {
            UIManager.instance?.ShowNotification("Bir hata oluştu.");
            Debug.LogError($"UploadEmblemAsync içinde beklenmedik hata: {e.Message}");
        }
        finally
        {
            // İşlem başarılı da olsa, başarısız da olsa yükleniyor göstergesini kapat.
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
        }
    }
    
    private IEnumerator LoadImageFromUrl(string url, Image targetImage)
    {
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                targetImage.sprite = sprite;
                targetImage.color = Color.white;
            }
        }
    }

    /// <summary>
    /// Bir listeyi, kaydırma pozisyonunu koruyarak yeniden oluşturur.
    /// </summary>
    /// <typeparam name="T">Liste elemanının veri tipi (örn: PublicSyndicateInfo)</typeparam>
    /// <param name="grid">Liste elemanlarının ekleneceği content objesinin Transform'u</param>
    /// <param name="items">Görüntülenecek veri listesi</param>
    /// <param name="slotPrefab">Her bir eleman için kullanılacak prefab</param>
    /// <param name="setupAction">Her bir prefab oluşturulduktan sonra onu veriyle dolduracak olan metot</param>
    private IEnumerator RebuildListCoroutine<T>(Transform grid, List<T> items, GameObject slotPrefab, Action<GameObject, T> setupAction)
    {
        var scrollRect = grid.GetComponentInParent<ScrollRect>();
        float savedVerticalPosition = 1f;

        // Sadece listeyi yeniliyorsak mevcut pozisyonu kaydet.
        if (scrollRect != null && grid.childCount > 0)
        {
            // Pozisyonun 0'dan küçük olmasını engelle (içerik kısaysa olabilir)
            savedVerticalPosition = Mathf.Max(0, scrollRect.verticalNormalizedPosition);
        }

        // Mevcut tüm elemanları temizle.
        foreach (Transform child in grid)
        {
            Destroy(child.gameObject);
        }

        // Yok etme işleminin tamamlanması için bir frame bekle.
        yield return null;

        // Listeyi yeni verilerle doldur.
        if (items != null)
        {
            foreach (var item in items)
            {
                GameObject slotGO = Instantiate(slotPrefab, grid);
                setupAction(slotGO, item);
            }
        }

        // Yeni elemanların layout'a eklenmesi için frame sonunu bekle ve pozisyonu geri yükle.
        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(grid.GetComponent<RectTransform>());
            scrollRect.verticalNormalizedPosition = savedVerticalPosition;
        }
    }
}
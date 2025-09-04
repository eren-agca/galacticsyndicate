// MainMenu.cs - GÜVENLİ ASENKRON KONTROLLERİ EKLENDİ

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System;

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    public Button primaryActionButton;
    public TextMeshProUGUI primaryActionButtonText;
    public Button newGameButton;
    public GameObject loadingIndicator;

    private static MainMenu instance;

    void Awake()
    {
        // Bu, sahne yeniden yüklendiğinde veya başka bir nedenle birden fazla
        // MainMenu oluştuğunda, sadece bir tanesinin kalmasını sağlar.
        // Ayrıca, bu nesnenin sahneler arasında taşınmamasını garanti eder.
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

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

    void Start()
    {
        primaryActionButton.interactable = false;
        newGameButton.gameObject.SetActive(false);
        loadingIndicator.SetActive(true);

        // İlk UI durumunu ayarla.
        UpdateMenuUI();
    }

    public void OnPrimaryActionButtonClicked()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnNewGameButtonClicked()
    {
        loadingIndicator.SetActive(true);
        primaryActionButton.interactable = false;
        newGameButton.interactable = false;

        // Yerel kayıt dosyasını sil ve sahneyi yeniden yükle.
        // GameInitializer yeni bir oyun başlatacak.
        SaveSystem.DeleteSave();
        SceneManager.LoadScene("GameScene");
    }

    /// <summary>
    /// Profil güncelleme olayı tetiklendiğinde çağrılır.
    /// </summary>
    private void OnProfileUpdatedHandler()
    {
        // UI güncelleme işlemini arka planda başlat.
        UpdateMenuUI();
    }

    /// <summary>
    /// Oyuncunun kayıt durumuna göre ana menü butonlarını günceller.
    /// </summary>
    private void UpdateMenuUI()
    {
        // Yerel kayıt dosyasının varlığını kontrol et.
        bool hasSaveData = SaveSystem.Load() != null;

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
using UnityEngine;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI creditsText;
    public TextMeshProUGUI speedText;

    private CanvasGroup canvasGroup;
    private Rigidbody2D playerRigidbody;

    void Awake()
    {
        // Bu panelin tıklamaları engellememesi için CanvasGroup bileşenini al veya ekle.
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) { canvasGroup = gameObject.AddComponent<CanvasGroup>(); }
    }

    void Start()
    {
        // Oyuncunun Rigidbody'sini bul ve sakla
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            playerRigidbody = player.GetComponent<Rigidbody2D>();
        }
        else
        {
            Debug.LogError("PlayerHUD could not find the PlayerController!", this.gameObject);
            // Oyuncu bulunamazsa bu paneli devre dışı bırak
            gameObject.SetActive(false);
            return;
        }

        // Panelin tıklamaları ve etkileşimi engellemediğinden emin ol.
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // PlayerStats'taki değişiklikleri dinlemek için event'e abone ol
        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.onStatsChanged.AddListener(UpdateStatsUI);
        }

        // Başlangıç değerlerini ayarla
        UpdateStatsUI();
    }

    void OnDestroy()
    {
        // Obje yok edildiğinde event aboneliğini iptal et (hafıza sızıntısı önler)
        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.onStatsChanged.RemoveListener(UpdateStatsUI);
        }
    }

    void Update()
    {
        // Hız göstergesini her frame güncelle
        if (playerRigidbody != null && speedText.gameObject.activeInHierarchy)
        {
            // Hızı hesapla (magnitude) ve tam sayıya yuvarla
            int currentSpeed = Mathf.RoundToInt(playerRigidbody.linearVelocity.magnitude);
            speedText.text = $"Hız: {currentSpeed} m/s";
        }
    }

    // Bu metot sadece istatistikler değiştiğinde (kredi, upgrade vb.) çağrılır.
    private void UpdateStatsUI()
    {
        if (PlayerStats.instance != null && creditsText.gameObject.activeInHierarchy)
        {
            creditsText.text = $"Kredi: {PlayerStats.instance.credits}c";
        }
    }
}
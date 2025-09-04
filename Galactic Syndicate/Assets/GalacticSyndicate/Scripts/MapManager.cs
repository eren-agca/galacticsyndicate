// MapManager.cs - NIHAI HALI

using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class MapManager : MonoBehaviour
{
    public static MapManager instance;

    [Header("UI References")]
    public GameObject mapPanel;
    public Transform mapIconContainer;
    public GameObject mapIconPrefab;
    public TextMeshProUGUI planetNameText;

    [Header("Player Tracking")]
    [Tooltip("Haritada oyuncuyu temsil eden UI ikonu.")]
    public RectTransform playerIcon;
    private Transform playerTransform;

    [Header("Map Settings")]
    public float mapScale = 0.1f;

    private List<MapIcon> mapIcons = new List<MapIcon>();

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        mapPanel.SetActive(false);
        if (QuestManager.instance != null)
        {
            QuestManager.instance.OnQuestLogUpdated += UpdateQuestMarkers;
        }

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("MapManager could not find the PlayerController in the scene!");
            if (playerIcon != null) playerIcon.gameObject.SetActive(false);
        }
    }

    void LateUpdate()
    {
        // Sadece harita açıksa ve referanslar geçerliyse çalıştır.
        if (mapPanel.activeSelf && playerTransform != null && playerIcon != null)
        {
            // Oyuncunun dünya pozisyonunu al, harita ölçeğiyle çarp ve UI ikonunun pozisyonuna ata.
            playerIcon.localPosition = playerTransform.position * mapScale;

            // --- YENİ EKLENEN SATIR ---
            // Oyuncunun dünya rotasyonunu doğrudan UI ikonunun rotasyonuna ata.
            playerIcon.rotation = playerTransform.rotation;
        }
    }

    public void PopulateMap(List<Planet> allPlanets)
    {
        // Bu metot değişmeden kalıyor...
        foreach (Planet planet in allPlanets)
        {
            GameObject iconGO = Instantiate(mapIconPrefab, mapIconContainer);
            iconGO.transform.localPosition = new Vector2(planet.transform.position.x, planet.transform.position.y) * mapScale;

            MapIcon mapIcon = iconGO.GetComponent<MapIcon>();
            mapIcon.Setup(planet);
            mapIcons.Add(mapIcon);
        }
        UpdateQuestMarkers();
    }

    public void UpdateQuestMarkers()
    {
        // Bu metot değişmeden kalıyor...
        foreach (var icon in mapIcons)
        {
            icon.SetQuestMarker(false);
        }

        List<Quest> activeQuests = QuestManager.instance.activeQuests;
        foreach (var quest in activeQuests)
        {
            MapIcon targetIcon = mapIcons.Find(icon => icon.linkedPlanet == quest.OriginPlanet);
            if (targetIcon != null)
            {
                targetIcon.SetQuestMarker(true);
            }
        }
    }

    public void ShowPlanetName(string name)
    {
        // Bu metot değişmeden kalıyor...
        if (planetNameText != null)
        {
            planetNameText.text = name;
        }
    }

    public void ToggleMap()
    {
        // Bu metot değişmeden kalıyor...
        bool isOpening = !mapPanel.activeSelf;
        mapPanel.SetActive(isOpening);

        if (isOpening && planetNameText != null)
        {
            planetNameText.text = "";
        }
    }

    private void OnDestroy()
    {
        // Bu metot değişmeden kalıyor...
        if (QuestManager.instance != null)
        {
            QuestManager.instance.OnQuestLogUpdated -= UpdateQuestMarkers;
        }
    }
    
    // --- YENİ EKLENEN METOT ---
    /// <summary>
    /// Haritadaki mevcut tüm gezegen ikonlarını temizler.
    /// </summary>
    public void ClearMapIcons()
    {
        foreach (var icon in mapIcons)
        {
            if (icon != null)
            {
                Destroy(icon.gameObject);
            }
        }
        mapIcons.Clear();
    }
}
// MapIcon.cs - NIHAI HALI (YENİ SCRİPT)

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MapIcon : MonoBehaviour
{
    public Planet linkedPlanet { get; private set; }

    [Tooltip("Bu ikonun bir görev hedefi olduğunu gösteren görsel (yıldız, parlama vb.)")]
    public GameObject questMarkerVisual;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnIconTapped);
    }

    public void Setup(Planet planet)
    {
        this.linkedPlanet = planet;
        // İkonun rengini veya sprite'ını gezegen türüne göre değiştirebiliriz.
        // GetComponent<Image>().sprite = planet.type.mapIconSprite;
    }

    public void SetQuestMarker(bool isActive)
    {
        if (questMarkerVisual != null)
        {
            questMarkerVisual.SetActive(isActive);
        }
    }

    private void OnIconTapped()
    {
        // Dokunma olayını merkezi yöneticiye bildir.
        if (MapManager.instance != null)
        {
            MapManager.instance.ShowPlanetName(linkedPlanet.name);
        }
    }
}
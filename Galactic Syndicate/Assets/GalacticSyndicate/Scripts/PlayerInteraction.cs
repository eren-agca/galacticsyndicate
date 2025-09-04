// PlayerInteraction.cs - NIHAI HALI

using UnityEngine;
using System.Threading.Tasks; // Task kullanmak için eklendi

public class PlayerInteraction : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Gezegene yanaşıldığında aktif olacak buton.")]
    public GameObject dockButton;
    
    [Tooltip("Yanaşma butonuna basıldığında yönetilecek olan panelin script'i.")]
    public DockingPanelUI dockingPanelUI; 

    private Planet currentPlanet;

    void Start()
    {
        if (dockButton != null) dockButton.SetActive(false);
        if (dockingPanelUI != null) dockingPanelUI.Hide();
    }

    // Oyuncu bir gezegenin etki alanına girdiğinde
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Eğer çarpıştığımız obje bir gezegense
        if (other.CompareTag("Planet"))
        {
            currentPlanet = other.GetComponent<Planet>();
            if (currentPlanet != null)
            {
                // Yanaşma butonunu göster
                if (dockButton != null) dockButton.SetActive(true);
            }
        }
    }

    // Oyuncu bir gezegenin etki alanından çıktığında
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Planet"))
        {
            // Eğer ayrıldığımız gezegen, şu anki hedefimiz olan gezegense
            if (other.GetComponent<Planet>() == currentPlanet)
            {
                // Yanaşma butonunu gizle ve referansı temizle
                if (dockButton != null) dockButton.SetActive(false);
                currentPlanet = null;
            }
        }
    }

    /// <summary>
    /// Bu metot, "Yanaş" butonunun OnClick event'ine atanmalıdır.
    /// </summary>
    public async void OnDockButtonPressed()
    {
        if (currentPlanet == null) return;

        // Önce görevleri tamamlamayı dene ve işlemin bitmesini bekle.
        if (QuestManager.instance != null)
        {
            // DEĞİŞİKLİK: QuestManager'daki yeni async metodu çağırıyoruz.
            await QuestManager.instance.TryCompleteQuests(currentPlanet);
        }

        // Görev tamamlama ve kaydetme bittikten sonra yanaşma panelini göster.
        if (dockingPanelUI != null)
        {
            dockingPanelUI.Show(currentPlanet);
        }
    }

    /// <summary>
    /// Returns the closest planet the player is currently interacting with.
    /// </summary>
    /// <returns>The closest Planet object, or null if none.</returns>
    public Planet GetClosestPlanet()
    {
        return currentPlanet;
    }
}
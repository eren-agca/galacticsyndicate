// UIManager.cs - YENİ SCRİPT

using UnityEngine;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("UI References")]
    [Tooltip("Ekranın ortasında belirecek bildirim metni objesi.")]
    public TextMeshProUGUI notificationText;
    
    [Tooltip("Bildirimin ekranda kalma süresi (saniye).")]
    public float notificationDuration = 2.5f;

    private Coroutine notificationCoroutine;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }
    }

    public void ShowNotification(string message)
    {
        if (notificationText == null) return;

        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }
        notificationCoroutine = StartCoroutine(NotificationCoroutine(message));
    }

    private IEnumerator NotificationCoroutine(string message)
    {
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        
        // Anlık olarak görünür yap
        notificationText.alpha = 1f;

        yield return new WaitForSeconds(notificationDuration);

        notificationText.gameObject.SetActive(false);
    }
}
// GhostPlayerController.cs (YENİ SCRİPT)
using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Sahnede diğer oyuncuları temsil eden tek bir "hayalet" gemiyi yönetir.
/// Hareketini yumuşatır ve bilgilerini günceller.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GhostPlayerController : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private TextMeshPro usernameText;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Hareket Ayarları")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float fadeDuration = 0.5f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    void Awake()
    {
        // Başlangıçta görünmez yap
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        var color = spriteRenderer.color;
        color.a = 0;
        spriteRenderer.color = color;
        usernameText.alpha = 0;
    }

    /// <summary>
    /// Hayalet gemiyi sunucudan gelen verilerle başlatır ve görünür hale getirir.
    /// </summary>
    public void Initialize(string username, Vector3 position, Quaternion rotation)
    {
        usernameText.text = username;
        transform.position = position; // Başlangıç pozisyonunu anında ayarla
        transform.rotation = rotation;
        UpdateData(position, rotation);
        StartCoroutine(Fade(true));
    }


    /// <summary>
    /// Hayalet geminin hedef konumunu ve rotasyonunu günceller.
    /// </summary>
    public void UpdateData(Vector3 position, Quaternion rotation)
    {
        targetPosition = position;
        targetRotation = rotation;
    }

    void Update()
    {
        // Hedefe doğru yumuşak hareket (Lerp)
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
        
        // Hedefe doğru yumuşak dönüş (Slerp)
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    /// <summary>
    /// Gemiyi yavaşça görünmez yapıp sahneden siler.
    /// </summary>
    public void FadeOutAndDestroy()
    {
        StartCoroutine(Fade(false, () => Destroy(gameObject)));
    }

    /// <summary>
    /// Görünme (fade-in) ve kaybolma (fade-out) efektini yöneten Coroutine.
    /// </summary>
    private IEnumerator Fade(bool fadeIn, System.Action onComplete = null)
    {
        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, time / fadeDuration);
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, alpha);
            usernameText.alpha = alpha;
            yield return null;
        }
        onComplete?.Invoke();
    }
}
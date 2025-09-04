// CameraFollow.cs - NIHAI HALI

using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Kameranın takip edeceği hedef (Oyuncu). Sahnede otomatik olarak bulunur.")]
    public Transform target;

    [Header("Settings")]
    [Tooltip("Kameranın takibi ne kadar yumuşak yapacağı. 0'a yaklaştıkça daha yavaş takip eder.")]
    [Range(0.01f, 1.0f)]
    public float smoothSpeed = 0.125f;

    [Tooltip("Kameranın oyuncuya göre duracağı mesafe. Genellikle Z ekseninde negatif bir değerdir (örn: -10).")]
    public Vector3 offset = new Vector3(0, 0, -10);

    void Start()
    {
        // Eğer hedef manuel olarak atanmamışsa, sahnede "PlayerController" olan objeyi bul.
        if (target == null)
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogError("CameraFollow, takip edilecek bir PlayerController bulamadı!", this);
            }
        }
    }

    // LateUpdate, tüm Update fonksiyonları çalıştıktan sonra çağrılır.
    // Bu, oyuncu hareketini tamamladıktan sonra kameranın pozisyonunu güncellemek için en iyi yerdir.
    void LateUpdate()
    {
        // Eğer takip edilecek bir hedef yoksa, hiçbir şey yapma.
        if (target == null) return;

        // 1. İstenen pozisyonu hesapla (hedefin güncel pozisyonu + bizim tanımladığımız sabit ofset).
        Vector3 desiredPosition = target.position + offset;

        // 2. Mevcut pozisyondan istenen pozisyona doğru yumuşak bir geçiş yap.
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // 3. Kameranın pozisyonunu güncelle.
        transform.position = smoothedPosition;
    }
}
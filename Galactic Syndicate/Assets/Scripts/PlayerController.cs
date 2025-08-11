// PlayerController.cs - NIHAI HALI

using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Ship Settings")]
    // public float moveSpeed = 50f; // BU SATIR SİLİNDİ! Değer artık PlayerStats'tan geliyor.
    public float rotationSpeed = 100f;
    public float brakeForce = 30f;

    private Rigidbody2D rb;
    private float moveInput;
    private float rotationInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        rb.AddTorque(rotationInput * rotationSpeed * Time.fixedDeltaTime);

        if (moveInput > 0)
        {
            // DEĞİŞİKLİK: moveSpeed yerine PlayerStats.instance.moveSpeed kullanılıyor.
            Vector2 thrustForce = transform.up * moveInput * PlayerStats.instance.moveSpeed * Time.fixedDeltaTime;
            rb.AddForce(thrustForce);
        }
        else if (moveInput < 0)
        {
            Vector2 brakeDirection = -rb.linearVelocity.normalized;
            rb.AddForce(brakeDirection * brakeForce * Time.fixedDeltaTime);
        }
    }

    public void StartThrusting() { moveInput = 1f; }
    public void StartBraking() { moveInput = -1f; }
    public void StopMoving() { moveInput = 0f; }
    public void StartRotatingLeft() { rotationInput = 1f; }
    public void StartRotatingRight() { rotationInput = -1f; }
    public void StopRotating() { rotationInput = 0f; }
}
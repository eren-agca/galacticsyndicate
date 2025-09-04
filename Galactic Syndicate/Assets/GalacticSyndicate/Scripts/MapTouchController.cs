// MapTouchController.cs - NIHAI HALI (YENİ SCRİPT)

using UnityEngine;

public class MapTouchController : MonoBehaviour
{
    [Tooltip("Kaydırılacak ve zoom yapılacak olan ikon konteyneri.")]
    public RectTransform mapContainer;
    
    [Header("Zoom Settings")]
    public float zoomSpeed = 0.01f;
    public float minZoom = 0.5f;
    public float maxZoom = 3f;

    private Vector2 lastPanPosition;

    void Update()
    {
        // Tek parmakla kaydırma (Pan)
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                lastPanPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                Vector2 panDelta = touch.position - lastPanPosition;
                mapContainer.anchoredPosition += panDelta;
                lastPanPosition = touch.position;
            }
        }

        // İki parmakla yakınlaştırma (Pinch-to-Zoom)
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;
            Zoom(difference * zoomSpeed);
        }
    }

    void Zoom(float increment)
    {
        float newScale = mapContainer.localScale.x + increment;
        newScale = Mathf.Clamp(newScale, minZoom, maxZoom);
        mapContainer.localScale = new Vector3(newScale, newScale, 1f);
    }
}
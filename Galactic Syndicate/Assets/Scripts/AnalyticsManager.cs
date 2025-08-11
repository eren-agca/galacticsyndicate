// Dosya: Assets/Scripts/AnalyticsManager.cs

using UnityEngine;
using Firebase.Analytics;
using System.Collections.Generic;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager instance;
    private bool isInitialized = false;

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

    /// <summary>
    /// Bu metot, FirebaseManager tamamen hazır olduğunda onun tarafından çağrılır.
    /// Bu, doğru çalışma sırasını ve zamanlamayı garanti eder.
    /// </summary>
    public void OnFirebaseInitialized()
    {
        isInitialized = true;
        Debug.LogWarning("AnalyticsManager: 'Firebase Hazır' sinyali alındı. Debug modu etkinleştiriliyor.");

        // Bu kod, artık kesinlikle doğru zamanda, yani Firebase tamamen hazır olduktan sonra çalışacak.
        #if UNITY_EDITOR
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        FirebaseAnalytics.SetUserProperty("debug", "true");
        Debug.LogWarning("AnalyticsManager: Debug Modu, Unity Editor için programatik olarak etkinleştirildi.");
        #endif
    }

    /// <summary>
    /// Firebase'e bir Analytics olayı gönderir.
    /// </summary>
    public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"AnalyticsManager henüz hazır değil. Olay '{eventName}' gönderilemedi.");
            return;
        }

        if (parameters == null)
        {
            FirebaseAnalytics.LogEvent(eventName);
        }
        else
        {
            Parameter[] firebaseParams = new Parameter[parameters.Count];
            int i = 0;
            foreach (var param in parameters)
            {
                // Parametre değerlerini doğru tiplere dönüştürerek gönderiyoruz.
                if (param.Value is string)
                    firebaseParams[i++] = new Parameter(param.Key, (string)param.Value);
                else if (param.Value is long || param.Value is int)
                    firebaseParams[i++] = new Parameter(param.Key, System.Convert.ToInt64(param.Value));
                else if (param.Value is double || param.Value is float)
                    firebaseParams[i++] = new Parameter(param.Key, System.Convert.ToDouble(param.Value));
            }
            FirebaseAnalytics.LogEvent(eventName, firebaseParams);
        }

        Debug.Log($"Analytics Event Logged: {eventName}");
    }
}
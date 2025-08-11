// Bu yeni bir dosyadır: Assets/Scripts/AnalyticsConnectionTester.cs

using UnityEngine;
using Firebase.Analytics;

public class AnalyticsConnectionTester : MonoBehaviour
{
    // Bu metot, FirebaseManager tamamen hazır olduğunda onun tarafından çağrılacak.
    public void OnFirebaseInitialized()
    {
        Debug.LogWarning("--- ANALYTICS CONNECTION TESTER: 'Firebase Hazır' sinyali alındı. ---");

#if UNITY_EDITOR
        Debug.LogWarning("--- ANALYTICS CONNECTION TESTER: Editor için Debug Modu etkinleştiriliyor. ---");
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        FirebaseAnalytics.SetUserProperty("debug", "true");
#endif

        Debug.LogWarning("--- ANALYTICS CONNECTION TESTER: Test olayı ŞİMDİ gönderiliyor. ---");
        
        // Mümkün olan en basit olayı gönderiyoruz.
        FirebaseAnalytics.LogEvent("connection_test_event");
        
        Debug.LogWarning("--- ANALYTICS CONNECTION TESTER: Test olayı gönderildi. Lütfen DebugView'ı kontrol et. ---");
    }
}
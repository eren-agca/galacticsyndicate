// Dosya: Assets/Scripts/FirebaseManager.cs

using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager instance;
    public bool IsInitialized { get; private set; } = false;

    public FirebaseApp app { get; private set; }
    public FirebaseAuth auth { get; private set; }
    public FirebaseFirestore db { get; private set; }
    public FirebaseUser user { get; private set; }
    public string UserID => user?.UserId ?? "N/A";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Firebase.FirebaseApp.LogLevel = Firebase.LogLevel.Verbose;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async Task InitializeFirebase()
    {
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            app = FirebaseApp.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;

            if (auth.CurrentUser != null)
            {
                user = auth.CurrentUser;
                Debug.Log($"Firebase: User already signed in with UserID: {user.UserId}");
            }
            else
            {
                AuthResult authResult = await auth.SignInAnonymouslyAsync();
                user = authResult.User;
                Debug.Log($"Firebase: Anonymously signed in with UserID: {user.UserId}");
            }
            
            IsInitialized = true;
            Debug.LogWarning("Firebase initialized. Waiting for 500ms for systems to settle...");
            await Task.Delay(500);

            // Hazır olduğumuzu diğer yöneticilere bildir.
            AnalyticsManager.instance?.OnFirebaseInitialized();
            // Gelecekte eklenebilecek diğer yöneticiler de buradan çağrılabilir.
        }
        else
        {
            Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
        }
    }
}
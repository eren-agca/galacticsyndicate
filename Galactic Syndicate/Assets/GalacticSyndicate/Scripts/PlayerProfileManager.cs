// PlayerProfileManager.cs - TEK OYUNCULU VERSİYON

using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.Networking;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class PlayerProfileManager : MonoBehaviour
{
    public static PlayerProfileManager instance;

    public string PlayerUsername { get; private set; }
    public string ProfilePictureURL { get; private set; }
    public bool IsGuestUser { get; private set; } = true; // Tek oyunculu modda herkes misafir sayılır.

    public event Action OnProfileUpdated;

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
    /// Kayıt dosyasından yüklenen verilerle profili ayarlar.
    /// </summary>
    public void LoadProfileFromData(string username, string pictureUrl)
    {
        PlayerUsername = string.IsNullOrEmpty(username) ? "Pilot" : username;
        ProfilePictureURL = pictureUrl;
        IsGuestUser = true; // Bu her zaman true olacak.
        OnProfileUpdated?.Invoke();
    }

    /// <summary>
    /// Kullanıcı adını değiştirir ve oyunu kaydeder.
    /// </summary>
    public async Task<(bool success, string message)> ChangeUsername(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return (false, "İsim boş olamaz.");
        }
        
        if (newName.Length > 12)
        {
             return (false, "İsim en fazla 12 karakter olabilir.");
        }

        PlayerUsername = newName;
        OnProfileUpdated?.Invoke();
        
        // Değişikliği hemen kaydet
        SaveManager.instance.SaveGame();
        
        await Task.Delay(100); // Simüle edilmiş ağ gecikmesi
        return (true, "İsim başarıyla değiştirildi!");
    }

    /// <summary>
    /// Profil resmini yükler ve oyunu kaydeder.
    /// </summary>
    public async Task<(bool success, string message)> UploadProfilePicture(string localFilePath)
    {
        // Bu metodun yerel versiyonu, dosya yolunu doğrudan kullanır.
        // Web'de bu bir URL olabilir, PC/mobilde ise yerel bir dosya yolu.
        ProfilePictureURL = "file://" + localFilePath;
        OnProfileUpdated?.Invoke();
        
        // Değişikliği hemen kaydet
        SaveManager.instance.SaveGame();

        await Task.Delay(100); // Simüle edilmiş ağ gecikmesi
        return (true, "Profil resmi güncellendi!");
    }
    
    // Bu metodlar artık online sistemlerle ilgili olmadığı için boş veya basitleştirilmiş halde.
    public Task FetchUserProfile()
    {
        // Yerel veriden yüklendiği için bu metodun içi boş olabilir.
        // Olayın tetiklenmesi, UI'ın güncellenmesini sağlar.
        OnProfileUpdated?.Invoke();
        return Task.CompletedTask;
    }

    public void SignOut()
    {
        // Tek oyunculu modda oturum kapatma işlemi bir anlam ifade etmez.
        Debug.Log("Tek oyunculu modda oturum kapatma işlemi yok.");
    }

    /// <summary>
    /// PlayerProfileUI'daki derleyici hatasını çözmek için eklenmiş boş metot.
    /// Tek oyunculu modda bir işlevi yoktur.
    /// </summary>
    public Task<(bool success, string message)> LinkAccountWithGoogleAsync()
    {
        Debug.Log("Tek oyunculu modda Google hesabı bağlama işlemi yok.");
        return Task.FromResult((false, "Bu özellik desteklenmiyor."));
    }
}
// SyndicateMemberSlotUI.cs - YENİ VE GELİŞTİRİLMİŞ HALİ

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

public class SyndicateMemberSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI memberNameText;
    public GameObject leaderIcon;
    public Image profilePicture;

    private Coroutine imageLoadCoroutine;

    public void Setup(string name, bool isLeader, string pictureUrl)
    {
        memberNameText.text = name;
        leaderIcon.SetActive(isLeader);

        // Varsayılan durumu ayarla (resim yoksa)
        profilePicture.sprite = null;
        profilePicture.color = new Color(1, 1, 1, 0.2f);

        if (!string.IsNullOrEmpty(pictureUrl))
        {
            // Eğer daha önce çalışan bir yükleme varsa durdur.
            if (imageLoadCoroutine != null)
            {
                StopCoroutine(imageLoadCoroutine);
            }
            imageLoadCoroutine = StartCoroutine(LoadImageFromUrl(pictureUrl, profilePicture));
        }
    }

    private IEnumerator LoadImageFromUrl(string url, Image targetImage)
    {
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            yield return webRequest.SendWebRequest();

            if (this == null) yield break; // Obje yok edildiyse coroutine'i durdur

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                if (targetImage != null)
                {
                    targetImage.sprite = sprite;
                    targetImage.color = Color.white;
                }
            }
            else
            {
                Debug.LogError($"Resim yüklenemedi: {url} - Hata: {webRequest.error}");
            }
        }
    }
}
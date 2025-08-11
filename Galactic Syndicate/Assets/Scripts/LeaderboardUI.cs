using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Threading.Tasks; // DÜZELTME: Task sınıfı için gerekli.

public class LeaderboardUI : MonoBehaviour
{
    [Header("Panels & Content")]
    public GameObject leaderboardPanel;
    public Transform playerContentGrid;
    public Transform syndicateContentGrid;
    public GameObject loadingIndicator;

    [Header("Prefabs & Buttons")]
    public GameObject leaderboardSlotPrefab;
    public Button playerBoardButton;
    public Button syndicateBoardButton;

    // ScrollRect referansları
    private ScrollRect playerScrollRect;
    private ScrollRect syndicateScrollRect;

    void Start()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }

        // ScrollRect referanslarını bir kere al ve sakla.
        if (playerContentGrid != null)
        {
            playerScrollRect = playerContentGrid.GetComponentInParent<ScrollRect>();
        }
        if (syndicateContentGrid != null)
        {
            syndicateScrollRect = syndicateContentGrid.GetComponentInParent<ScrollRect>();
        }
    }

    public void TogglePanel()
    {
        bool isOpening = !leaderboardPanel.activeSelf;
        leaderboardPanel.SetActive(isOpening);

        if (isOpening)
        {
            // Panel açıldığında varsayılan olarak oyuncu listesini göster
            playerBoardButton.interactable = false;
            syndicateBoardButton.interactable = true;
            SwitchBoard("players");
        }
    }

    public void OnPlayerBoardClicked()
    {
        SwitchBoard("players");
    }

    public void OnSyndicateBoardClicked()
    {
        SwitchBoard("syndicates");
    }

    private void SwitchBoard(string boardType)
    {
        bool isPlayerBoard = boardType == "players";
        playerBoardButton.interactable = !isPlayerBoard;
        syndicateBoardButton.interactable = isPlayerBoard;

        if (playerScrollRect != null) playerScrollRect.gameObject.SetActive(isPlayerBoard);
        if (syndicateScrollRect != null) syndicateScrollRect.gameObject.SetActive(!isPlayerBoard);

        Transform activeGrid = isPlayerBoard ? playerContentGrid : syndicateContentGrid;

        StopAllCoroutines();
        StartCoroutine(LoadAndRebuildList(boardType, activeGrid));
    }

    // Scroll konumu korunacak şekilde güncellenmiş sürüm
    private IEnumerator LoadAndRebuildList(string boardType, Transform grid)
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(true);

        // Aktif ScrollRect referansını al
        ScrollRect activeScrollRect = grid.GetComponentInParent<ScrollRect>();

        // Scroll pozisyonunu kaydet (kullanıcı nerede kaldıysa)
        float savedScrollPos = activeScrollRect != null ? activeScrollRect.verticalNormalizedPosition : 1f;

        // Önceki liste elemanlarını temizle
        foreach (Transform child in grid)
        {
            Destroy(child.gameObject);
        }

        // Unity'nin temizleme işlemini bitirmesi için bir frame bekle
        yield return null;

        // Veriyi çek
        Task<List<LeaderboardEntry>> dataTask = LeaderboardManager.instance.GetLeaderboardData(boardType);
        yield return new WaitUntil(() => dataTask.IsCompleted);

        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

        if (dataTask.IsFaulted)
        {
            Debug.LogError($"Leaderboard verisi çekilirken hata oluştu ('{boardType}'): {dataTask.Exception.Message}");
        }
        else
        {
            entries = dataTask.Result;
        }

        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (this == null || !gameObject.activeInHierarchy) yield break;

        // Listeyi oluştur
        if (entries != null && entries.Count > 0)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                GameObject slotGO = Instantiate(leaderboardSlotPrefab, grid);
                slotGO.GetComponent<LeaderboardSlotUI>().Setup(i + 1, entries[i].Name, entries[i].Value);
            }
        }
        else
        {
            GameObject emptySlot = Instantiate(leaderboardSlotPrefab, grid);
            emptySlot.GetComponent<LeaderboardSlotUI>().Setup(0, "Veri bulunamadı", 0);
        }

        // Scroll pozisyonunu geri yükle
        if (activeScrollRect != null)
        {
            // Layout sisteminin yeni eklenen elemanlara göre content'in boyutunu
            // hesaplaması için bir frame sonunu bekle. Bu, pozisyonu geri yüklemeden
            // önce yapılması gereken en güvenli adımdır.
            yield return new WaitForEndOfFrame();

            Canvas.ForceUpdateCanvases();
            activeScrollRect.verticalNormalizedPosition = savedScrollPos;
        }
    }
} 

// SyndicateUpgradeUI.cs - GÜNCELLENDİ (Güvenli veri ayrıştırma eklendi)

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using Firebase.Functions;
using System.Collections.Generic;
using System;
using System.Linq; // ToDictionary kullanmak için eklendi

public class SyndicateUpgradeUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI currentTreasuryText;
    public TextMeshProUGUI tradeBuffLevelText;
    public TextMeshProUGUI tradeBuffCostText;
    public Button upgradeTradeBuffButton;
    public GameObject loadingIndicator;
    public Button closeButton;

    private const int BASE_TRADE_BUFF_COST = 5000;
    private const float COST_MULTIPLIER = 2.5f;

    void Start()
    {
        upgradeTradeBuffButton.onClick.AddListener(OnUpgradeTradeBuffClicked);
        // CloseButton'un listener'ı Unity Editor'den ayarlanıyor.
    }

    async void OnEnable()
    {
        upgradeTradeBuffButton.interactable = false;
        if (loadingIndicator != null) loadingIndicator.SetActive(true);

        if (SyndicateManager.instance != null)
        {
            SyndicateManager.instance.OnSyndicateDataUpdated += UpdateUI;
            await SyndicateManager.instance.RefreshCurrentSyndicateData();
        }
        else
        {
            UpdateUI();
        }

        if (loadingIndicator != null) loadingIndicator.SetActive(false);
    }

    void OnDisable()
    {
        if (SyndicateManager.instance != null)
        {
            SyndicateManager.instance.OnSyndicateDataUpdated -= UpdateUI;
        }
    }

    public void UpdateUI()
    {
        SyndicateData syndicate = SyndicateManager.instance.CurrentSyndicate;
        if (syndicate == null) 
        {
            currentTreasuryText.text = "Hazine: -";
            tradeBuffLevelText.text = "Seviye: -";
            tradeBuffCostText.text = "Maliyet: -";
            upgradeTradeBuffButton.interactable = false;
            return;
        }

        currentTreasuryText.text = $"Mevcut Hazine: {syndicate.Treasury:N0}c";

        int currentLevel = syndicate.TradeBuffLevel;
        int upgradeCost = CalculateUpgradeCost(currentLevel);

        tradeBuffLevelText.text = $"Seviye: {currentLevel}";
        tradeBuffCostText.text = $"Maliyet: {upgradeCost:N0}c";
        
        upgradeTradeBuffButton.interactable = syndicate.Treasury >= upgradeCost;
    }

    private int CalculateUpgradeCost(int currentLevel)
    {
        return Mathf.RoundToInt(BASE_TRADE_BUFF_COST * Mathf.Pow(COST_MULTIPLIER, currentLevel));
    }

    public async void OnUpgradeTradeBuffClicked()
    {
        loadingIndicator.SetActive(true);
        upgradeTradeBuffButton.interactable = false;

        try
        {
            var functions = FirebaseFunctions.DefaultInstance;
            var function = functions.GetHttpsCallable("purchaseSyndicateUpgrade");
            
            var data = new Dictionary<string, object>
            {
                { "buffType", "trade" }
            };

            var result = await function.CallAsync(data);
            
            // --- HATA DÜZELTİLDİ: Güvenli ayrıştırma metodu kullanılıyor. ---
            var resultData = ParseFunctionResult(result.Data);

            if (resultData != null && resultData.ContainsKey("success") && (bool)resultData["success"])
            {
                Debug.Log("Sendika geliştirmesi başarıyla satın alındı!");
                await SyndicateManager.instance.RefreshCurrentSyndicateData();
            }
            else
            {
                string serverMessage = resultData?.ContainsKey("message") == true ? resultData["message"].ToString() : "Bilinmeyen bir sunucu hatası.";
                Debug.LogError($"Geliştirme satın alınamadı: {serverMessage}");
            }
        }
        catch (FunctionsException e)
        {
            Debug.LogError($"Geliştirme satın alınırken bir hata oluştu: {e.Message} (Kod: {e.ErrorCode})");
        }
        catch (Exception e)
        {
            Debug.LogError($"Geliştirme satın alınırken genel bir hata oluştu: {e.Message}");
        }
        finally
        {
            loadingIndicator.SetActive(false);
        }
    }

    // --- YENİ EKLENEN YARDIMCI METOT ---
    /// <summary>
    /// Firebase Cloud Function'dan dönen veriyi güvenli bir şekilde IDictionary<string, object> formatına dönüştürür.
    /// </summary>
    private IDictionary<string, object> ParseFunctionResult(object data)
    {
        if (data == null) return null;
        
        // Eğer zaten doğru türdeyse, doğrudan döndür.
        if (data is IDictionary<string, object> directDict)
        {
            return directDict;
        }
        
        // Eğer object, object türündeyse, onu string, object türüne dönüştür.
        if (data is IDictionary<object, object> objectDict)
        {
            return objectDict.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
        }

        // Diğer beklenmedik durumlar için null döndür.
        Debug.LogWarning($"Cloud function'dan beklenmedik veri türü alındı: {data.GetType()}");
        return null;
    }
}
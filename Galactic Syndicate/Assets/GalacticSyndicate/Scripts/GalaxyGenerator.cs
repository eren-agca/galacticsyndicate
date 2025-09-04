// GalaxyGenerator.cs - DÜZELTİLMİŞ VE SAĞLAMLAŞTIRILMIŞ HALİ

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GalaxyGenerator : MonoBehaviour
{
    [Header("Galaxy Settings")]
    [Tooltip("Oluşturulacak toplam gezegen sayısı.")]
    public int numberOfPlanets = 20;
    public Vector2 mapSize = new Vector2(500, 500);
    public float minDistanceBetweenPlanets = 50f;
    [Tooltip("Gezegenlerin oyuncunun başlangıç noktasına ne kadar yaklaşabileceği.")]
    public float playerSafeZoneRadius = 75f;

    [Header("Prefabs & Data")]
    public GameObject planetPrefab;
    [Tooltip("Oluşturulabilecek tüm farklı gezegen türleri.")]
    public PlanetType[] availablePlanetTypes;
    public Transform planetContainer;

    public int currentGalaxySeed { get; private set; }
    private List<Planet> allPlanets = new List<Planet>();
    private static System.Random rng;

    void Start()
    {
        // Bu scriptin Start'ta otomatik çalışmasını istemiyoruz.
        // GameInitializer veya SaveManager tarafından çağrılacak.
    }

    public void GenerateNewGalaxy()
    {
        currentGalaxySeed = Random.Range(0, 999999);
        GenerateGalaxyFromSeed(currentGalaxySeed);
    }

    public void GenerateGalaxyFromSeed(int seed)
    {
        currentGalaxySeed = seed;
        Random.InitState(seed);
        rng = new System.Random(seed);

        ClearExistingPlanets();

        if (MapManager.instance != null) MapManager.instance.ClearMapIcons();
        
        // --- YENİ, BASİT VE DOĞRU ÜRETİM MANTIĞI ---
        if (availablePlanetTypes.Length == 0)
        {
            Debug.LogError("Hiç gezegen türü (PlanetType) atanmamış!");
            return;
        }

        // 1. Boş bir liste ile başla.
        List<PlanetType> planetsToGenerate = new List<PlanetType>();

        // 2. Gezegen türlerinden oluşan bir havuz oluştur ve karıştır. Bu, ilk turda eklenecek gezegenlerin sırasını rastgele yapar.
        List<PlanetType> firstPassPool = new List<PlanetType>(availablePlanetTypes);
        Shuffle(firstPassPool);

        // 3. Kural 1: Her türden bir tane ekle, ama numberOfPlanets limitini aşma.
        foreach (var planetType in firstPassPool)
        {
            if (planetsToGenerate.Count >= numberOfPlanets) break; // Limite ulaşıldıysa döngüden çık.
            planetsToGenerate.Add(planetType);
        }

        // 4. Kural 2: Eğer hala yer varsa, ikinci kopyaları eklemeye başla.
        if (planetsToGenerate.Count < numberOfPlanets)
        {
            // İkinci tur için yeni bir karışık havuz oluştur.
            List<PlanetType> secondPassPool = new List<PlanetType>(availablePlanetTypes);
            Shuffle(secondPassPool);

            foreach (var planetType in secondPassPool)
            {
                if (planetsToGenerate.Count >= numberOfPlanets) break; // Limite ulaşıldıysa döngüden çık.
                planetsToGenerate.Add(planetType);
            }
        }
        
        // 5. Artık tam olarak istediğimiz sayıda gezegen içeren listemiz hazır. Gezegenleri oluştur.
        foreach (var planetType in planetsToGenerate)
        {
            Vector2? potentialPosition = FindValidPosition();
            if (potentialPosition.HasValue)
            {
                CreatePlanet(potentialPosition.Value, planetType);
            }
            else
            {
                Debug.LogWarning($"Gezegen '{planetType.name}' için haritada uygun yer bulunamadı. Atlanıyor.");
            }
        }
        // --- ÜRETİM MANTIĞI SONU ---

        if (MapManager.instance != null)
        {
            MapManager.instance.PopulateMap(allPlanets);
        }
    }

    private Vector2? FindValidPosition(int maxAttempts = 100)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 position = new Vector2(
                Random.Range(-mapSize.x / 2, mapSize.x / 2),
                Random.Range(-mapSize.y / 2, mapSize.y / 2)
            );

            if (IsPositionValid(position))
            {
                return position;
            }
        }
        return null;
    }

    private void ClearExistingPlanets()
    {
        foreach (Planet planet in allPlanets)
        {
            if (planet != null) Destroy(planet.gameObject);
        }
        allPlanets.Clear();
    }

    private bool IsPositionValid(Vector2 position)
    {
        if (Vector2.Distance(position, Vector2.zero) < playerSafeZoneRadius)
        {
            return false;
        }

        foreach (Planet planet in allPlanets)
        {
            if (Vector2.Distance(position, planet.transform.position) < minDistanceBetweenPlanets)
            {
                return false;
            }
        }
        return true;
    }

    private void CreatePlanet(Vector2 position, PlanetType typeToCreate)
    {
        GameObject planetGO = Instantiate(planetPrefab, position, Quaternion.identity, planetContainer);
        Planet planetComp = planetGO.GetComponent<Planet>();
        
        if (planetComp != null)
        {
            planetComp.Setup(typeToCreate);
            allPlanets.Add(planetComp);
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
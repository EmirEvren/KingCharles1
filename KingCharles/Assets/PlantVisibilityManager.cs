using System.Collections.Generic;
using UnityEngine;

public class PlantVisibilityManager : MonoBehaviour
{
    public static PlantVisibilityManager Instance;

    [Header("Animal Settings")]
    [SerializeField] private string animalTag = "Animal";
    private Transform[] animals = new Transform[0];

    [Header("Update Settings")]
    [SerializeField] private int batchSize = 50;
    [SerializeField] private float refreshAnimalsInterval = 2f;

    private readonly List<PlantVisibilityUnit> plants = new List<PlantVisibilityUnit>();
    private readonly HashSet<PlantVisibilityUnit> plantSet = new HashSet<PlantVisibilityUnit>();

    private int currentIndex = 0;
    private float refreshTimer;

    private void Awake()
    {
        Instance = this;
        RefreshAnimals();
        refreshTimer = refreshAnimalsInterval;
    }

    private void Start()
    {
        // ✅ SAHNE AÇILINCA TÜM BİTKİLERİ TOPLA (Instance hazırken)
        var allPlants = FindObjectsByType<PlantVisibilityUnit>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allPlants.Length; i++)
            Register(allPlants[i]);
    }

    private void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = refreshAnimalsInterval;
            RefreshAnimals();
        }

        if (plants.Count == 0 || animals.Length == 0) return;

        int processed = 0;
        while (processed < batchSize && plants.Count > 0)
        {
            if (currentIndex >= plants.Count) currentIndex = 0;

            var p = plants[currentIndex];
            if (p != null)
                p.UpdateVisibility(animals);

            currentIndex++;
            processed++;
        }
    }

    private void RefreshAnimals()
    {
        GameObject[] found = GameObject.FindGameObjectsWithTag(animalTag);
        animals = new Transform[found.Length];
        for (int i = 0; i < found.Length; i++)
            animals[i] = found[i].transform;
    }

    public void Register(PlantVisibilityUnit plant)
    {
        if (plant != null && plantSet.Add(plant))
            plants.Add(plant);
    }

    public void Unregister(PlantVisibilityUnit plant)
    {
        if (plant != null && plantSet.Remove(plant))
            plants.Remove(plant);
    }
}

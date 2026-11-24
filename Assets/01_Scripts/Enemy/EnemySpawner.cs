using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemySpawner : MonoBehaviour
{
    [Serializable]
    private class Route
    {
        public Transform spawnPoint;
        public Transform stopPoint;
        public bool faceRight;
    }

    [SerializeField] private EnemyMovement enemyPrefab;
    [SerializeField] private Route[] routes;
    [SerializeField] private NestHealth nest;
    [SerializeField] private DinoPlacementBoard placementBoard;
    [SerializeField] private Button startButton;
    [SerializeField] private Text startButtonLabel;
    [SerializeField, Min(1)] private int enemiesPerWave = 5;
    [SerializeField, Min(0.1f)] private float spawnInterval = 1.2f;

    public event Action<int> GoldEarned;
    public string WaveStatus { get; private set; } = "WAVE 1 / 10";

    private readonly List<EnemyMovement> spawnedEnemies = new();
    private int enemiesSpawned;
    private int previousRoute = -1;
    private int routeStreak;
    private double nextSpawnTime;
    private bool waveRunning;

    private void Start()
    {
        if (enemyPrefab == null || nest == null || startButton == null || startButtonLabel == null || routes == null || routes.Length == 0)
        {
            Debug.LogError("Set the wave prefab, nest, controls and routes.", this);
            return;
        }

        foreach (Route route in routes)
        {
            if (route == null || route.spawnPoint == null || route.stopPoint == null)
            {
                Debug.LogError("Every route needs a spawn point and a stop point.", this);
                return;
            }
        }

        startButton.onClick.AddListener(StartWave);
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartWave);
    }

    private void StartWave()
    {
        waveRunning = true;
        nextSpawnTime = Time.timeAsDouble;
        startButton.interactable = false;
        startButtonLabel.text = "WAVE 1";
    }

    private void Update()
    {
        if (!waveRunning)
            return;

        if (!nest.HasEggs)
        {
            FinishWave("DEFEAT");
            return;
        }

        if (enemiesSpawned < enemiesPerWave && Time.timeAsDouble >= nextSpawnTime)
        {
            SpawnEnemy();
            nextSpawnTime = Time.timeAsDouble + spawnInterval;
        }

        if (enemiesSpawned == enemiesPerWave && !HasLivingEnemies())
        {
            FinishWave("WAVE CLEAR");
            GoldEarned?.Invoke(50);
        }
    }

    private void SpawnEnemy()
    {
        int routeIndex = UnityEngine.Random.Range(0, routes.Length);

        // Prevent four consecutive spawns from the same direction.
        if (routes.Length > 1 && routeIndex == previousRoute && routeStreak >= 3)
            routeIndex = (routeIndex + UnityEngine.Random.Range(1, routes.Length)) % routes.Length;

        routeStreak = routeIndex == previousRoute ? routeStreak + 1 : 1;
        previousRoute = routeIndex;

        Route route = routes[routeIndex];
        EnemyMovement enemy = Instantiate(enemyPrefab, route.spawnPoint.position, route.spawnPoint.rotation);
        if (enemy.TryGetComponent(out SpriteRenderer sprite))
            sprite.flipX = route.faceRight;

        enemy.SetTarget(route.stopPoint, nest, placementBoard);
        if (enemy.TryGetComponent(out EnemyHealth health))
            health.Defeated += RewardEnemyKill;
        spawnedEnemies.Add(enemy);
        enemiesSpawned++;
    }

    private void RewardEnemyKill()
    {
        GoldEarned?.Invoke(10);
    }

    private bool HasLivingEnemies()
    {
        foreach (EnemyMovement enemy in spawnedEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }

    private void FinishWave(string message)
    {
        waveRunning = false;
        startButtonLabel.text = message;
        WaveStatus = message;
    }
}

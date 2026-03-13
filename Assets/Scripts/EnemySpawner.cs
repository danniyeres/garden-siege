using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Object enemyPrefab;
    [SerializeField] private Transform spawnPointsRoot;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Transform cropsRoot;
    [SerializeField] private Transform enemyParent;

    [Header("Spawn")]
    [SerializeField] private bool autoSpawn = true;
    [SerializeField] private bool spawnImmediately = true;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private bool logSpawns = true;

    [Header("Waves")]
    [SerializeField] private int wave1EnemyCount = 5;
    [SerializeField] private int wave2EnemyCount = 8;
    [SerializeField] private int wave3EnemyCount = 12;
    [SerializeField] private bool waitForWaveClear = true;
    [SerializeField] private float timeBetweenWaves = 1f;

    [Header("Enemy Speed Per Wave")]
    [SerializeField] private float baseEnemyMoveSpeed = 3f;
    [SerializeField] private float moveSpeedIncreasePerWave = 0.8f;

    [Header("Enemy Damage Per Wave")]
    [SerializeField] private float baseEnemyDamagePerSecond = 5f;
    [SerializeField] private float damageIncreasePerWave = 2f;

    [Header("Health")]
    [SerializeField] private float playerMaxHealth = 100f;
    [SerializeField] private float cropsMaxHealth = 300f;
    [SerializeField] private float enemyMaxHealth = 35f;

    private readonly List<Transform> spawnPoints = new List<Transform>();
    private readonly List<int> spawnPointBag = new List<int>();

    private float spawnTimer;
    private float waveDelayTimer;
    private int lastSpawnPointIndex = -1;

    private int currentWaveIndex;
    private int spawnedInCurrentWave;
    private bool waitingForNextWave;
    private bool allWavesComplete;

    public int TotalWaves => 3;
    public int CurrentWaveNumber => Mathf.Clamp(currentWaveIndex + 1, 1, TotalWaves);
    public int CurrentWaveEnemyCount => GetWaveEnemyCount(currentWaveIndex);
    public int SpawnedInCurrentWave => spawnedInCurrentWave;
    public bool IsCurrentWaveSpawnComplete =>
        CurrentWaveEnemyCount > 0 && spawnedInCurrentWave >= CurrentWaveEnemyCount;
    public bool IsAllWavesComplete => allWavesComplete;
    public float CurrentWaveEnemyMoveSpeed => GetEnemyMoveSpeedForWave(currentWaveIndex);
    public float CurrentWaveEnemyDamagePerSecond => GetEnemyDamagePerSecondForWave(currentWaveIndex);

    private void Awake()
    {
        if (spawnPointsRoot == null)
        {
            var root = GameObject.Find("EnemySpawnPoints");
            if (root != null)
            {
                spawnPointsRoot = root.transform;
            }
        }

        if (cropsRoot == null)
        {
            var crops = GameObject.Find("Crops");
            if (crops != null)
            {
                cropsRoot = crops.transform;
            }
        }

        if (playerRoot == null)
        {
            var player = GameObject.Find("JellyFishGirl");
            if (player != null)
            {
                playerRoot = player.transform;
            }
        }

        EnsureHealth(playerRoot, playerMaxHealth, false);
        EnsureHealth(cropsRoot, cropsMaxHealth, false);

        CacheSpawnPoints();
    }

    private void Start()
    {
        currentWaveIndex = 0;
        spawnedInCurrentWave = 0;
        waitingForNextWave = false;
        allWavesComplete = false;
        spawnTimer = 0f;
        waveDelayTimer = 0f;

        if (logSpawns)
        {
            Debug.Log(
                $"EnemySpawner ready. points={spawnPoints.Count}, prefab={(enemyPrefab != null ? enemyPrefab.name : "None")}, wave={CurrentWaveNumber}/{TotalWaves}, speed={CurrentWaveEnemyMoveSpeed:0.##}, damage={CurrentWaveEnemyDamagePerSecond:0.##}",
                this
            );
        }

        _ = GameAudioController.Instance;
        GameAudioController.Instance.PlayWaveStart(CurrentWaveNumber);

        if (spawnImmediately)
        {
            SpawnOne();
        }
    }

    private void Update()
    {
        if (!autoSpawn || enemyPrefab == null || spawnPoints.Count == 0 || allWavesComplete)
        {
            return;
        }

        if (IsCurrentWaveSpawnComplete)
        {
            if (waitForWaveClear && CountAliveEnemies() > 0)
            {
                return;
            }

            if (!waitingForNextWave)
            {
                waitingForNextWave = true;
                waveDelayTimer = 0f;

                if (logSpawns)
                {
                    Debug.Log($"Wave {CurrentWaveNumber} complete.", this);
                }
            }

            waveDelayTimer += Time.deltaTime;
            if (waveDelayTimer < Mathf.Max(0f, timeBetweenWaves))
            {
                return;
            }

            AdvanceWave();
            return;
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnOne();
        }
    }

    [ContextMenu("Spawn One Enemy")]
    public void SpawnOne()
    {
        if (allWavesComplete || enemyPrefab == null || spawnPoints.Count == 0)
        {
            return;
        }

        var waveEnemyCount = CurrentWaveEnemyCount;
        if (waveEnemyCount <= 0 || spawnedInCurrentWave >= waveEnemyCount)
        {
            return;
        }

        var point = GetNextSpawnPoint();
        if (point == null)
        {
            return;
        }

        var spawned = SpawnEnemyObject(point.position, point.rotation);
        if (spawned == null)
        {
            Debug.LogWarning("EnemySpawner: failed to spawn enemy.", this);
            return;
        }

        var spawnedGameObject = spawned as GameObject;
        if (spawnedGameObject == null && spawned is Component spawnedComponent)
        {
            spawnedGameObject = spawnedComponent.gameObject;
        }

        if (spawnedGameObject == null)
        {
            if (logSpawns)
            {
                Debug.LogWarning(
                    $"EnemySpawner spawned object of type {spawned.GetType().Name} at {point.position}, but it is not a GameObject.",
                    this
                );
            }
            return;
        }

        var mover = spawnedGameObject.GetComponent<EnemyMoveToCrops>();
        if (mover == null)
        {
            mover = spawnedGameObject.AddComponent<EnemyMoveToCrops>();
        }

        EnsureEnemyCharacterController(spawnedGameObject);
        EnsureHealth(spawnedGameObject.transform, enemyMaxHealth, true);

        var healthBar = spawnedGameObject.GetComponent<EnemyHealthBar>();
        if (healthBar == null)
        {
            spawnedGameObject.AddComponent<EnemyHealthBar>();
        }

        mover.SetCropsRoot(cropsRoot);
        mover.SetMoveSpeed(GetEnemyMoveSpeedForWave(currentWaveIndex));
        mover.SetDamagePerSecond(GetEnemyDamagePerSecondForWave(currentWaveIndex));

        spawnedInCurrentWave++;

        if (logSpawns)
        {
            Debug.Log(
                $"Wave {CurrentWaveNumber}: spawned {spawnedInCurrentWave}/{waveEnemyCount} at {point.name} (speed {GetEnemyMoveSpeedForWave(currentWaveIndex):0.##}, damage {GetEnemyDamagePerSecondForWave(currentWaveIndex):0.##})",
                this
            );
        }
    }

    private void AdvanceWave()
    {
        waitingForNextWave = false;
        waveDelayTimer = 0f;
        spawnTimer = 0f;

        if (currentWaveIndex >= TotalWaves - 1)
        {
            allWavesComplete = true;
            autoSpawn = false;

            if (logSpawns)
            {
                Debug.Log("All waves complete.", this);
            }
            return;
        }

        currentWaveIndex++;
        spawnedInCurrentWave = 0;
        GameAudioController.Instance.PlayWaveStart(CurrentWaveNumber);

        if (logSpawns)
        {
            Debug.Log(
                $"Wave {CurrentWaveNumber} started. enemies={CurrentWaveEnemyCount}, speed={CurrentWaveEnemyMoveSpeed:0.##}, damage={CurrentWaveEnemyDamagePerSecond:0.##}",
                this
            );
        }

        if (spawnImmediately)
        {
            SpawnOne();
        }
    }

    private int GetWaveEnemyCount(int waveIndex)
    {
        if (waveIndex == 0)
        {
            return Mathf.Max(0, wave1EnemyCount);
        }

        if (waveIndex == 1)
        {
            return Mathf.Max(0, wave2EnemyCount);
        }

        if (waveIndex == 2)
        {
            return Mathf.Max(0, wave3EnemyCount);
        }

        return 0;
    }

    private float GetEnemyMoveSpeedForWave(int waveIndex)
    {
        return Mathf.Max(0.1f, baseEnemyMoveSpeed + moveSpeedIncreasePerWave * Mathf.Max(0, waveIndex));
    }

    private float GetEnemyDamagePerSecondForWave(int waveIndex)
    {
        return Mathf.Max(0.1f, baseEnemyDamagePerSecond + damageIncreasePerWave * Mathf.Max(0, waveIndex));
    }

    private int CountAliveEnemies()
    {
        var enemies = FindObjectsByType<EnemyMoveToCrops>(FindObjectsSortMode.None);
        var aliveCount = 0;

        for (var i = 0; i < enemies.Length; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.isActiveAndEnabled)
            {
                continue;
            }

            var health = enemy.GetComponent<Health>();
            if (health == null || health.IsAlive)
            {
                aliveCount++;
            }
        }

        return aliveCount;
    }

    private Object SpawnEnemyObject(Vector3 position, Quaternion rotation)
    {
#if UNITY_EDITOR
        if (enemyPrefab != null)
        {
            var editorSpawned = PrefabUtility.InstantiatePrefab(enemyPrefab, gameObject.scene);
            if (editorSpawned is GameObject editorGo)
            {
                editorGo.transform.SetPositionAndRotation(position, rotation);
                if (enemyParent != null)
                {
                    editorGo.transform.SetParent(enemyParent, true);
                }

                return editorGo;
            }

            if (editorSpawned is Component editorComponent)
            {
                editorComponent.transform.SetPositionAndRotation(position, rotation);
                if (enemyParent != null)
                {
                    editorComponent.transform.SetParent(enemyParent, true);
                }

                return editorComponent;
            }
        }
#endif

        return Instantiate(enemyPrefab, position, rotation, enemyParent);
    }

    [ContextMenu("Refresh Spawn Points")]
    public void CacheSpawnPoints()
    {
        spawnPoints.Clear();
        spawnPointBag.Clear();
        lastSpawnPointIndex = -1;

        if (spawnPointsRoot == null)
        {
            return;
        }

        for (var i = 0; i < spawnPointsRoot.childCount; i++)
        {
            spawnPoints.Add(spawnPointsRoot.GetChild(i));
        }
    }

    private Transform GetNextSpawnPoint()
    {
        if (spawnPoints.Count == 0)
        {
            return null;
        }

        if (spawnPointBag.Count == 0)
        {
            RefillSpawnPointBag();
        }

        if (spawnPointBag.Count == 0)
        {
            return null;
        }

        var lastBagIndex = spawnPointBag.Count - 1;
        var pointIndex = spawnPointBag[lastBagIndex];
        spawnPointBag.RemoveAt(lastBagIndex);
        lastSpawnPointIndex = pointIndex;
        return spawnPoints[pointIndex];
    }

    private void RefillSpawnPointBag()
    {
        spawnPointBag.Clear();
        for (var i = 0; i < spawnPoints.Count; i++)
        {
            spawnPointBag.Add(i);
        }

        for (var i = spawnPointBag.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            var tmp = spawnPointBag[i];
            spawnPointBag[i] = spawnPointBag[j];
            spawnPointBag[j] = tmp;
        }

        if (spawnPointBag.Count > 1 && spawnPointBag[spawnPointBag.Count - 1] == lastSpawnPointIndex)
        {
            var swapIndex = Random.Range(0, spawnPointBag.Count - 1);
            var last = spawnPointBag.Count - 1;
            var tmp = spawnPointBag[last];
            spawnPointBag[last] = spawnPointBag[swapIndex];
            spawnPointBag[swapIndex] = tmp;
        }
    }

    private static void EnsureHealth(Transform targetTransform, float maxHealth, bool destroyOnDeath)
    {
        if (targetTransform == null)
        {
            return;
        }

        var health = targetTransform.GetComponent<Health>();
        if (health == null)
        {
            health = targetTransform.gameObject.AddComponent<Health>();
        }

        health.Configure(maxHealth, true, destroyOnDeath);
    }

    private static void EnsureEnemyCharacterController(GameObject enemyObject)
    {
        if (enemyObject == null)
        {
            return;
        }

        var controller = enemyObject.GetComponent<CharacterController>();
        if (controller != null)
        {
            return;
        }

        controller = enemyObject.AddComponent<CharacterController>();
        controller.height = 1f;
        controller.radius = 0.45f;
        controller.center = new Vector3(0f, 0.5f, 0f);
        controller.stepOffset = 0.25f;
        controller.slopeLimit = 45f;
        controller.skinWidth = 0.08f;
        controller.minMoveDistance = 0.001f;
    }
}

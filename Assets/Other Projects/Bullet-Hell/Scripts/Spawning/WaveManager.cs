using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the wave loop: 3 waves per room, then the room changes.
/// Boss on wave 3 of rooms 10, 20, 30… Mid-boss on wave 3 of rooms 5, 15, 25…
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    public GameObject enemyMeleePrefab;
    public GameObject enemyRangedPrefab;
    public GameObject enemyMidbossPrefab;
    public GameObject enemyBossPrefab;

    [Header("Spawn Locations")]
    public Transform[] spawnPoints;

    [Header("Room Structure")]
    public int wavesPerRoom = 3;

    [Header("Wave Timing")]
    public float timeBetweenWaves = 3f;
    public float delayBetweenSpawns = 0.35f;
    public bool autoStartFirstWave = false;

    [Header("Normal Wave Size")]
    public int baseMeleeCount = 3;
    public int baseRangedCount = 1;

    [Header("Scaling Per Room")]
    public int extraMeleePerRoom = 1;
    public int extraRangedEveryNRooms = 2;
    public float enemyHealthPerRoom = 0.07f;
    public float enemySpeedPerRoom = 0.025f;
    public float enemyDamagePerRoom = 0.04f;

    [Header("Spawn Mix")]
    [Range(0f, 0.5f)]
    public float maxRangedFraction = 0.33f;

    [Header("Mid-Boss Rooms (5, 15, 25… — wave 3 only)")]
    public int midbossRoomCycle = 10;
    public int midbossRoomOffset = 5;
    public float midbossWaveMobMultiplier = 0.5f;

    [Header("Boss Rooms (10, 20, 30… — wave 3 only)")]
    public int bossEveryNRooms = 10;
    public int bossEscortMeleeCount = 2;
    public int bossEscortRangedCount = 1;

    [Header("Runtime (read-only)")]
    [SerializeField] private int currentWave;
    [SerializeField] private int aliveEnemies;

    private bool waveInProgress;
    private bool waitingForNextWave;
    private bool waitingForRoomTransition;
    private float betweenWaveTimer;

    public int CurrentWave => currentWave;
    public int CurrentRoom => currentWave > 0 ? GetRoomNumber(currentWave) : 0;
    public int WaveInRoom => currentWave > 0 ? GetWaveInRoom(currentWave) : 0;

    public bool IsBossWave =>
        currentWave > 0 &&
        IsFinalWaveInRoom(currentWave) &&
        IsBossRoom(GetRoomNumber(currentWave));

    public bool IsMidbossWave =>
        currentWave > 0 &&
        IsFinalWaveInRoom(currentWave) &&
        IsMidbossRoom(GetRoomNumber(currentWave));

    public event System.Action<int> OnWaveCleared;
    public event System.Action<int> OnRoomCleared;

    private bool runStopped;

    public bool IsRunStopped => runStopped;

    public void StopWaves()
    {
        runStopped = true;
        waitingForNextWave = false;
        waitingForRoomTransition = false;
        waveInProgress = false;
    }

    public int GetRoomNumber(int wave) =>
        wave > 0 ? (wave - 1) / wavesPerRoom + 1 : 0;

    public int GetWaveInRoom(int wave) =>
        wave > 0 ? (wave - 1) % wavesPerRoom + 1 : 0;

    public bool IsFinalWaveInRoom(int wave) =>
        wave > 0 && wave % wavesPerRoom == 0;

    public bool IsBossRoom(int room) =>
        room > 0 && bossEveryNRooms > 0 && room % bossEveryNRooms == 0;

    public bool IsMidbossRoom(int room)
    {
        if (room <= 0 || IsBossRoom(room)) return false;
        return midbossRoomCycle > 0 && room % midbossRoomCycle == midbossRoomOffset;
    }

    private void Start()
    {
        if (autoStartFirstWave)
            BeginWaveCountdown(0f);
    }

    private void Update()
    {
        if (IsRunStopped || waitingForRoomTransition || waveInProgress || aliveEnemies > 0 || !waitingForNextWave)
            return;

        betweenWaveTimer -= Time.deltaTime;
        if (betweenWaveTimer <= 0f)
        {
            waitingForNextWave = false;
            StartNextWave();
        }
    }

    public void NotifyEnemyDefeated()
    {
        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
        TryScheduleNextWave();
    }

    public void SetSpawnPoints(Transform[] points)
    {
        spawnPoints = points;
    }

    public void CompleteRoomTransition()
    {
        waitingForRoomTransition = false;
        BeginWaveCountdown(0f);
    }

    private void TryScheduleNextWave()
    {
        if (waveInProgress || aliveEnemies > 0 || waitingForNextWave || waitingForRoomTransition) return;

        if (currentWave > 0)
            OnWaveCleared?.Invoke(currentWave);

        if (IsFinalWaveInRoom(currentWave))
        {
            waitingForRoomTransition = true;
            OnRoomCleared?.Invoke(GetRoomNumber(currentWave));

            if (!UsesRoomTransitions())
                CompleteRoomTransition();

            return;
        }

        BeginWaveCountdown(timeBetweenWaves);
    }

    private void BeginWaveCountdown(float delay)
    {
        waitingForNextWave = true;
        betweenWaveTimer = delay;
    }

    public void StartNextWave()
    {
        if (waveInProgress || IsRunStopped || waitingForRoomTransition) return;

        currentWave++;
        waveInProgress = true;
        waitingForNextWave = false;
        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        if (IsBossWave)
        {
            yield return SpawnBossWave();
        }
        else
        {
            if (IsMidbossWave)
            {
                SpawnEnemy(enemyMidbossPrefab);
                yield return new WaitForSeconds(delayBetweenSpawns);
            }

            int room = GetRoomNumber(currentWave);
            int waveInRoom = GetWaveInRoom(currentWave);
            int meleeCount = baseMeleeCount + (room - 1) * extraMeleePerRoom + (waveInRoom - 1);
            int rangedCount = CalculateRangedCount(meleeCount, room);

            if (IsMidbossWave)
            {
                meleeCount = Mathf.Max(1, Mathf.RoundToInt(meleeCount * midbossWaveMobMultiplier));
                rangedCount = Mathf.Max(0, Mathf.RoundToInt(rangedCount * midbossWaveMobMultiplier));
            }

            yield return SpawnMobWave(meleeCount, rangedCount);
        }

        waveInProgress = false;
        TryScheduleNextWave();
    }

    private IEnumerator SpawnBossWave()
    {
        SpawnEnemy(enemyBossPrefab);
        yield return new WaitForSeconds(delayBetweenSpawns * 2f);
        yield return SpawnMobWave(bossEscortMeleeCount, bossEscortRangedCount);
    }

    private IEnumerator SpawnMobWave(int meleeCount, int rangedCount)
    {
        for (int i = 0; i < meleeCount; i++)
        {
            SpawnEnemy(enemyMeleePrefab);
            yield return new WaitForSeconds(delayBetweenSpawns);
        }

        for (int i = 0; i < rangedCount; i++)
        {
            SpawnEnemy(enemyRangedPrefab);
            yield return new WaitForSeconds(delayBetweenSpawns);
        }
    }

    private int CalculateRangedCount(int meleeCount, int room)
    {
        int ranged = baseRangedCount;
        if (extraRangedEveryNRooms > 0 && room > 1)
            ranged += (room - 1) / extraRangedEveryNRooms;

        int totalWithoutCap = meleeCount + ranged;
        int maxRanged = Mathf.FloorToInt(totalWithoutCap * maxRangedFraction);
        return Mathf.Clamp(ranged, 0, maxRanged);
    }

    private void SpawnEnemy(GameObject prefab)
    {
        if (prefab == null) return;

        if (!EnsureSpawnPoints())
        {
            Debug.LogWarning("WaveManager: No spawn points — cannot spawn enemies.");
            return;
        }

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemy = Instantiate(prefab, point.position, Quaternion.identity);

        if (GameplayTuning.Instance != null)
            GameplayTuning.Instance.ApplyEnemyScale(enemy.transform);

        Health health = enemy.GetComponent<Health>();
        if (health == null) return;

        aliveEnemies++;
        WaveEnemyTracker tracker = enemy.GetComponent<WaveEnemyTracker>();
        if (tracker == null)
            tracker = enemy.AddComponent<WaveEnemyTracker>();
        tracker.Bind(this, health);

        int room = GetRoomNumber(currentWave);
        EnemyRoomScaler scaler = enemy.GetComponent<EnemyRoomScaler>();
        if (scaler == null)
            scaler = enemy.AddComponent<EnemyRoomScaler>();
        scaler.Apply(room, enemyHealthPerRoom, enemySpeedPerRoom, enemyDamagePerRoom);
    }

    private bool UsesRoomTransitions()
    {
        RoomController roomController = FindFirstObjectByType<RoomController>();
        return roomController != null && roomController.useProceduralRooms;
    }

    private bool EnsureSpawnPoints()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return true;

        RoomGenerator generator = FindFirstObjectByType<RoomGenerator>();
        if (generator != null && generator.SpawnPoints != null && generator.SpawnPoints.Length > 0)
        {
            spawnPoints = generator.SpawnPoints;
            return true;
        }

        SpawnPoint[] scenePoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        if (scenePoints.Length > 0)
        {
            spawnPoints = new Transform[scenePoints.Length];
            for (int i = 0; i < scenePoints.Length; i++)
                spawnPoints[i] = scenePoints[i].transform;
            return true;
        }

        return false;
    }
}

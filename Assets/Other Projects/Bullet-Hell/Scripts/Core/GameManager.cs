using System;
using UnityEngine;

/// <summary>
/// Top-level game state holder. Tracks the current room/wave, win/lose,
/// and provides a single access point for UI and future systems.
///
/// Setup:
///  - Add to the GameSystems object in Gameplay/Sandbox
///  - Assign Wave Manager (or leave blank — finds it automatically)
///  - Set Win Room Number low (e.g. 2–3) while playtesting victory
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public WaveManager waveManager;

    [Header("Win Condition")]
    [Tooltip("Clearing this room (all 3 waves) triggers victory. Lower for playtesting.")]
    public int winRoomNumber = 3;

    public int CurrentRoom => waveManager != null ? waveManager.CurrentRoom : 0;
    public int WinRoomNumber => winRoomNumber;
    public bool IsBossRoom => waveManager != null && waveManager.IsBossRoom(waveManager.CurrentRoom);
    public bool IsMidbossRoom => waveManager != null && waveManager.IsMidbossRoom(waveManager.CurrentRoom);
    public bool IsGameOver { get; private set; }
    public bool IsVictory { get; private set; }
    public bool IsRunEnded => IsGameOver || IsVictory;

    public event Action OnGameOver;
    public event Action OnVictory;

    private Health playerHealth;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerHealth = player.GetComponent<Health>();

        if (playerHealth != null)
            playerHealth.OnDeath += HandlePlayerDeath;

        if (waveManager != null)
            waveManager.OnRoomCleared += HandleRoomCleared;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnDeath -= HandlePlayerDeath;

        if (waveManager != null)
            waveManager.OnRoomCleared -= HandleRoomCleared;

        if (Instance == this)
            Instance = null;
    }

    private void HandlePlayerDeath()
    {
        if (IsRunEnded) return;

        IsGameOver = true;
        waveManager?.StopWaves();
        OnGameOver?.Invoke();
    }

    private void HandleRoomCleared(int clearedRoom)
    {
        if (IsRunEnded || winRoomNumber <= 0) return;
        if (clearedRoom < winRoomNumber) return;

        IsVictory = true;
        waveManager?.StopWaves();
        OnVictory?.Invoke();
    }
}

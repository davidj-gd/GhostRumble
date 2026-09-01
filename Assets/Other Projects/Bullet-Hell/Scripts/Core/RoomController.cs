using System.Collections;
using UnityEngine;

/// <summary>
/// Orchestrates procedural rooms: after every 3 cleared waves, fades to a new arena.
/// </summary>
[DefaultExecutionOrder(-100)]
public class RoomController : MonoBehaviour
{
    [Header("References")]
    public RoomGenerator roomGenerator;
    public RoomTransitionFade roomTransition;
    public WaveManager waveManager;
    public Transform player;

    [Header("Settings")]
    public bool useProceduralRooms = true;

    private bool transitioning;

    private void Awake()
    {
        if (!useProceduralRooms) return;

        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();

        if (waveManager != null)
            waveManager.autoStartFirstWave = false;
    }

    private void Start()
    {
        if (!useProceduralRooms) return;

        if (roomGenerator == null)
            roomGenerator = GetComponent<RoomGenerator>();
        if (roomTransition == null)
            roomTransition = GetComponent<RoomTransitionFade>();
        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (roomGenerator == null || waveManager == null)
        {
            Debug.LogError("RoomController: Missing RoomGenerator or WaveManager.");
            enabled = false;
            return;
        }

        waveManager.OnRoomCleared += HandleRoomCleared;

        BuildFirstRoom();
    }

    private void OnDestroy()
    {
        if (waveManager != null)
            waveManager.OnRoomCleared -= HandleRoomCleared;
    }

    private void BuildFirstRoom()
    {
        roomGenerator.GenerateRoom(1);
        waveManager.SetSpawnPoints(roomGenerator.SpawnPoints);
        RepositionPlayer();
        waveManager.StartNextWave();
    }

    private void HandleRoomCleared(int clearedRoom)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsRunEnded) return;
        if (!transitioning)
            StartCoroutine(TransitionToNextRoom(clearedRoom + 1));
    }

    private IEnumerator TransitionToNextRoom(int nextRoomNumber)
    {
        transitioning = true;

        if (roomTransition != null)
        {
            yield return roomTransition.PlayTransition(() =>
            {
                roomGenerator.GenerateRoom(nextRoomNumber);
                waveManager.SetSpawnPoints(roomGenerator.SpawnPoints);
                RepositionPlayer();
            });
        }
        else
        {
            roomGenerator.GenerateRoom(nextRoomNumber);
            waveManager.SetSpawnPoints(roomGenerator.SpawnPoints);
            RepositionPlayer();
        }

        waveManager.CompleteRoomTransition();
        transitioning = false;
    }

    private void RepositionPlayer()
    {
        if (player == null) return;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        Vector3 center = roomGenerator.GetRoomCenter();

        if (rb != null)
        {
            rb.position = center;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        else
        {
            player.position = center;
        }
    }
}

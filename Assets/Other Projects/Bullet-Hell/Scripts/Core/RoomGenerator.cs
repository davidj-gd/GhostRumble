using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural room: BH mesh floor scaled to the play area + spawn points around the edges.
/// Gameplay on XY at z=0; floor sits on -Z behind sprites.
/// </summary>
public class RoomGenerator : MonoBehaviour
{
    [Header("Grid Size (cells)")]
    public int minWidth = 44;
    public int maxWidth = 58;
    public int minHeight = 28;
    public int maxHeight = 40;
    public float cellSize = 1f;

    [Header("Floor")]
    [Tooltip("Default: BH.fbx prefab from Assets/Meshes/Smaller_Meshes/BH.fbx")]
    public GameObject floorPrefab;
    public RoomSurfaceSettings surfaceSettings;
    [Tooltip("Z position of floor mesh. Keep negative so sprites at z=0 draw in front.")]
    public float roomSurfaceZ = -1f;
    [Tooltip("Extra floor scale if no GameplayTuning is present.")]
    public Vector3 floorScaleOverride = Vector3.one;

    [Header("Spawn Points")]
    public int spawnPointCount = 8;
    public int spawnInsetCells = 3;

    [Header("Player Safe Zone")]
    public float playerClearRadius = 3f;

    [Header("Runtime (read-only)")]
    [SerializeField] private int roomNumber;
    [SerializeField] private int gridWidth;
    [SerializeField] private int gridHeight;

    public Transform[] SpawnPoints { get; private set; }
    public int RoomNumber => roomNumber;
    public float InnerWidth => (gridWidth - 2) * cellSize;
    public float InnerHeight => (gridHeight - 2) * cellSize;

    private Transform roomRoot;
    private bool[,] blocked;
    private Material runtimeFloorMaterial;

    private static readonly Quaternion FaceCameraRotation = Quaternion.Euler(0f, 180f, 0f);

    public void GenerateRoom(int number)
    {
        roomNumber = number;
        gridWidth = Random.Range(minWidth, maxWidth + 1);
        gridHeight = Random.Range(minHeight, maxHeight + 1);

        EnsureRuntimeMaterials();
        ClearRoom();
        BuildGrid();
        CreateFloor();
        CreateSpawnPoints();
    }

    public Vector3 GetRoomCenter() => Vector3.zero;

    public void ApplySurfaceMaterials(Material floorMaterial, Material wallMaterial)
    {
        RoomSurfacePlane[] planes = GetComponentsInChildren<RoomSurfacePlane>(true);
        foreach (RoomSurfacePlane plane in planes)
        {
            if (plane.surfaceType == RoomSurfacePlane.SurfaceType.Floor)
                plane.ApplyMaterial(floorMaterial);
        }
    }

    private void EnsureRuntimeMaterials()
    {
        if (surfaceSettings != null)
            runtimeFloorMaterial = surfaceSettings.floorMaterial;

        if (runtimeFloorMaterial == null)
            runtimeFloorMaterial = CreateFallbackMaterial(
                surfaceSettings != null ? surfaceSettings.floorColor : new Color(0.12f, 0.14f, 0.18f));
    }

    private static Material CreateFallbackMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;

        return mat;
    }

    private void ClearRoom()
    {
        if (roomRoot != null)
            Destroy(roomRoot.gameObject);

        GameObject rootGo = new GameObject($"Room_{roomNumber}");
        rootGo.transform.SetParent(transform, false);
        roomRoot = rootGo.transform;
    }

    private void BuildGrid()
    {
        blocked = new bool[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (x == 0 || y == 0 || x == gridWidth - 1 || y == gridHeight - 1)
                    blocked[x, y] = true;
            }
        }
    }

    private void CreateFloor()
    {
        Vector3 scale = GameplayTuning.Instance != null
            ? GameplayTuning.Instance.GetFloorScale(InnerWidth, InnerHeight)
            : new Vector3(InnerWidth * floorScaleOverride.x, InnerHeight * floorScaleOverride.y, floorScaleOverride.z);

        if (floorPrefab != null)
        {
            GameObject floor = Instantiate(floorPrefab, roomRoot);
            floor.name = "BH_Floor";
            floor.transform.localPosition = new Vector3(0f, 0f, roomSurfaceZ);
            floor.transform.localRotation = FaceCameraRotation;
            floor.transform.localScale = scale;

            ApplyMaterialToRenderers(floor, runtimeFloorMaterial);

            RoomSurfacePlane surface = floor.GetComponent<RoomSurfacePlane>();
            if (surface == null)
                surface = floor.AddComponent<RoomSurfacePlane>();
            surface.surfaceType = RoomSurfacePlane.SurfaceType.Floor;
            return;
        }

        CreateFallbackFloorQuad(scale);
    }

    private void CreateFallbackFloorQuad(Vector3 scale)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Floor_Fallback";
        go.transform.SetParent(roomRoot, false);
        go.transform.localPosition = new Vector3(0f, 0f, roomSurfaceZ);
        go.transform.localRotation = FaceCameraRotation;
        go.transform.localScale = scale;

        Collider builtInCollider = go.GetComponent<Collider>();
        if (builtInCollider != null)
            Destroy(builtInCollider);

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null && runtimeFloorMaterial != null)
            renderer.sharedMaterial = runtimeFloorMaterial;

        RoomSurfacePlane surface = go.AddComponent<RoomSurfacePlane>();
        surface.surfaceType = RoomSurfacePlane.SurfaceType.Floor;
    }

    private static void ApplyMaterialToRenderers(GameObject root, Material material)
    {
        if (material == null) return;

        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            renderer.sharedMaterial = material;
    }

    private void CreateSpawnPoints()
    {
        List<Vector3> candidates = CollectSpawnCandidates();
        var points = new List<Transform>();

        Shuffle(candidates);
        int count = Mathf.Min(spawnPointCount, candidates.Count);

        if (count == 0)
            Debug.LogWarning("RoomGenerator: No spawn candidates — check grid size and spawn inset.");

        for (int i = 0; i < count; i++)
        {
            GameObject sp = new GameObject($"SpawnPoint_{i + 1}");
            sp.transform.SetParent(roomRoot, false);
            sp.transform.position = candidates[i];
            sp.AddComponent<SpawnPoint>();
            points.Add(sp.transform);
        }

        SpawnPoints = points.ToArray();
    }

    private List<Vector3> CollectSpawnCandidates()
    {
        var list = new List<Vector3>();

        for (int x = 1; x < gridWidth - 1; x++)
        {
            for (int y = 1; y < gridHeight - 1; y++)
            {
                if (blocked[x, y]) continue;
                if (!IsNearWall(x, y, spawnInsetCells)) continue;
                if (Vector2.Distance(CellToWorld(x, y), Vector3.zero) < playerClearRadius) continue;
                list.Add(CellToWorld(x, y));
            }
        }

        return list;
    }

    private bool IsNearWall(int x, int y, int inset)
    {
        for (int dx = -inset; dx <= inset; dx++)
        {
            for (int dy = -inset; dy <= inset; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= gridWidth || ny >= gridHeight) continue;
                if (blocked[nx, ny]) return true;
            }
        }
        return false;
    }

    private Vector3 CellToWorld(int x, int y)
    {
        float worldX = (x - gridWidth * 0.5f + 0.5f) * cellSize;
        float worldY = (y - gridHeight * 0.5f + 0.5f) * cellSize;
        return new Vector3(worldX, worldY, 0f);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

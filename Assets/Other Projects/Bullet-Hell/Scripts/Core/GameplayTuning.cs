using UnityEngine;

/// <summary>
/// Central inspector knobs for floor, unit, and idle pulse scaling.
/// Add to GameManager (or any scene object) and tune in Play Mode.
/// </summary>
public class GameplayTuning : MonoBehaviour
{
    public static GameplayTuning Instance { get; private set; }

    [Header("Unit Scale")]
    public float playerScale = 1f;
    public float enemyScale = 1f;

    [Header("Floor (BH mesh)")]
    [Tooltip("Multiplied with generated room inner width/height.")]
    public Vector3 floorScale = Vector3.one;

    [Header("Idle Scale Pulse")]
    public bool enableIdleScalePulse = true;
    public float idleScaleMin = 1f;
    public float idleScaleMax = 1.5f;
    public float idleScaleSpeed = 2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ApplyPlayerScale(Transform playerRoot)
    {
        if (playerRoot == null) return;
        playerRoot.localScale = Vector3.one * playerScale;
    }

    public void ApplyEnemyScale(Transform enemyRoot)
    {
        if (enemyRoot == null) return;
        enemyRoot.localScale = Vector3.one * enemyScale;
    }

    public void ApplyToUnitVisuals(UnitVisuals visuals)
    {
        if (visuals == null) return;

        visuals.enableIdleScalePulse = enableIdleScalePulse;
        visuals.idleScaleMin = idleScaleMin;
        visuals.idleScaleMax = idleScaleMax;
        visuals.idleScaleSpeed = idleScaleSpeed;
        visuals.RefreshBaseScale();
    }

    public Vector3 GetFloorScale(float innerWidth, float innerHeight)
    {
        return new Vector3(innerWidth * floorScale.x, innerHeight * floorScale.y, floorScale.z);
    }
}

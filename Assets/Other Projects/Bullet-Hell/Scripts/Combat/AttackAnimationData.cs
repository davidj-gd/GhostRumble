using UnityEngine;

/// <summary>
/// Idle sprite + one-shot attack frames for a unit. Assign in Inspector or via .asset file.
/// </summary>
[CreateAssetMenu(fileName = "AttackAnimation", menuName = "Bullet Hell/Attack Animation")]
public class AttackAnimationData : ScriptableObject
{
    public Sprite idleSprite;
    public Sprite[] attackFrames;
    [Min(1f)] public float frameRate = 12f;
}

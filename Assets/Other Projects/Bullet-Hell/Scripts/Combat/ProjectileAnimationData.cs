using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileAnimation", menuName = "Bullet Hell/Projectile Animation")]
public class ProjectileAnimationData : ScriptableObject
{
    public Sprite[] travelFrames;
    [Min(1f)] public float frameRate = 14f;
}

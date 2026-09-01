using UnityEngine;

/// <summary>
/// Loops sprite frames on a SpriteRenderer (used by projectiles while traveling).
/// </summary>
[DisallowMultipleComponent]
public class SpriteFramePlayer : MonoBehaviour
{
    public Sprite[] frames;
    [Min(1f)] public float frameRate = 14f;

    private SpriteRenderer spriteRenderer;
    private int frameIndex;
    private float frameTimer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Play(Sprite[] spriteFrames, float fps)
    {
        frames = spriteFrames;
        frameRate = fps;
        frameIndex = 0;
        frameTimer = 0f;

        if (spriteRenderer != null && frames != null && frames.Length > 0)
            spriteRenderer.sprite = frames[0];
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0 || spriteRenderer == null)
            return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / frameRate;

        if (frameTimer < frameDuration)
            return;

        frameTimer -= frameDuration;
        frameIndex = (frameIndex + 1) % frames.Length;
        spriteRenderer.sprite = frames[frameIndex];
    }
}

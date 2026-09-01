using System;
using UnityEngine;

/// <summary>
/// Idle sprite with scale pulse, one-shot attack sheet playback on a visual child.
/// </summary>
[DisallowMultipleComponent]
public class UnitVisuals : MonoBehaviour
{
    [Header("Sprites")]
    public AttackAnimationData animationData;

    [Header("Idle Scale Pulse")]
    public bool enableIdleScalePulse = true;
    public float idleScaleMin = 1f;
    public float idleScaleMax = 1.5f;
    public float idleScaleSpeed = 2f;

    private SpriteRenderer spriteRenderer;
    private Vector3 baseLocalScale;
    private float idlePhase;
    private bool playingAttack;
    private int frameIndex;
    private float frameTimer;
    private Action attackCompleteCallback;

    public bool IsPlayingAttack => playingAttack;
    public SpriteRenderer SpriteRenderer => spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseLocalScale = transform.localScale;
        SetIdle();
    }

    private void Update()
    {
        if (enableIdleScalePulse && !playingAttack)
            ApplyIdleScalePulse();

        if (playingAttack)
            TickAttackAnimation();
    }

    public void Configure(AttackAnimationData data)
    {
        animationData = data;
        SetIdle();
    }

    public void RefreshBaseScale()
    {
        baseLocalScale = transform.localScale;
    }

    public void SetIdle()
    {
        playingAttack = false;
        attackCompleteCallback = null;
        transform.localScale = baseLocalScale;

        if (animationData != null && animationData.idleSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = animationData.idleSprite;
    }

    public void PlayAttack(Action onComplete)
    {
        PlayAttack(animationData, onComplete);
    }

    public void PlayAttack(AttackAnimationData data, Action onComplete)
    {
        if (data != null)
            animationData = data;

        if (animationData == null || animationData.attackFrames == null || animationData.attackFrames.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        playingAttack = true;
        frameIndex = 0;
        frameTimer = 0f;
        attackCompleteCallback = onComplete;
        transform.localScale = baseLocalScale;

        if (spriteRenderer != null)
            spriteRenderer.sprite = animationData.attackFrames[0];
    }

    private void TickAttackAnimation()
    {
        float frameDuration = 1f / animationData.frameRate;
        frameTimer += Time.deltaTime;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex++;

            if (frameIndex >= animationData.attackFrames.Length)
            {
                playingAttack = false;
                Action callback = attackCompleteCallback;
                attackCompleteCallback = null;
                SetIdle();
                callback?.Invoke();
                return;
            }

            if (spriteRenderer != null)
                spriteRenderer.sprite = animationData.attackFrames[frameIndex];
        }
    }

    private void ApplyIdleScalePulse()
    {
        idlePhase += Time.deltaTime * idleScaleSpeed;
        float t = (Mathf.Sin(idlePhase) + 1f) * 0.5f;
        float scale = Mathf.Lerp(idleScaleMin, idleScaleMax, t);
        transform.localScale = baseLocalScale * scale;
    }
}

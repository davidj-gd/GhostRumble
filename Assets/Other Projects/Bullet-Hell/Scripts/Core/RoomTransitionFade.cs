using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen fade used when transitioning between procedural rooms.
/// Builds its own overlay Canvas at runtime.
///
/// Setup: add to RoomSystems alongside RoomController.
/// </summary>
public class RoomTransitionFade : MonoBehaviour
{
    [Header("Timing")]
    public float fadeOutDuration = 0.4f;
    public float holdBlackDuration = 0.08f;
    public float fadeInDuration = 0.4f;

    private Image fadeImage;
    private bool isTransitioning;

    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        BuildOverlay();
        SetAlpha(0f);
    }

    public IEnumerator PlayTransition(Action onMidpoint)
    {
        if (isTransitioning) yield break;

        isTransitioning = true;
        yield return FadeTo(1f, fadeOutDuration);

        if (holdBlackDuration > 0f)
            yield return new WaitForSeconds(holdBlackDuration);

        onMidpoint?.Invoke();

        yield return FadeTo(0f, fadeInDuration);
        isTransitioning = false;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeImage == null)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float startAlpha = fadeImage.color.a;
        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage == null) return;
        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }

    private void BuildOverlay()
    {
        GameObject canvasGo = new GameObject("RoomFadeCanvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject imageGo = new GameObject("FadeImage");
        imageGo.transform.SetParent(canvasGo.transform, false);

        RectTransform rect = imageGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeImage = imageGo.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;
    }
}

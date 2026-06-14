using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// GhostHUD — Vertical liquid HP bar + vertical dash pips.
///
/// Layout (both sides mirror each other):
///
///   P1 side (left)          P2 side (right)
///   ┌──────────────┐        ┌──────────────┐
///   │ [HP] [DASH]  │        │ [DASH] [HP]  │
///   │              │        │              │
///   │  tall liquid │        │  tall liquid │
///   │  bar + 3     │        │  bar + 3     │
///   │  dash pips   │        │  dash pips   │
///   │              │        │              │
///   │    P1        │        │           P2 │
///   └──────────────┘        └──────────────┘
///
/// SETUP: Create empty GameObject → attach GhostHUD → assign 4 fields → Play.
/// </summary>
public class GhostHUD : MonoBehaviour
{
    [Header("Players")]
    public PlayerHealth  player1Health;
    public GhostMovement player1Movement;
    public PlayerHealth  player2Health;
    public GhostMovement player2Movement;

    // ── HP Bar ─────────────────────────────────────────────────────────────
    [Header("HP Bar")]
    public Color p1BarColor       = new Color(0.15f, 0.85f, 0.95f, 1f);
    public Color p2BarColor       = new Color(0.95f, 0.20f, 0.50f, 1f);
    public Color barTrackColor    = new Color(0.04f, 0.04f, 0.08f, 0.92f);
    public Color panelBgColor     = new Color(0.02f, 0.02f, 0.06f, 0.78f);
    public Color damageFlashColor = new Color(1f, 0.1f, 0.1f, 0.7f);
    public float flashDuration    = 0.20f;

    [Header("HP Bar Size")]
    public float barWidth         = 36f;
    public float barHeight        = 200f;
    public float barInnerPad      = 3f;    // gap between track edge and fill

    // ── Dash Pips ──────────────────────────────────────────────────────────
    [Header("Dash Pips")]
    public Color dashFullColor      = new Color(0.98f, 0.88f, 0.25f, 1f);
    public Color dashEmptyColor     = new Color(0.12f, 0.12f, 0.18f, 1f);
    public Color dashRechargeColor  = new Color(0.70f, 0.55f, 0.10f, 1f);
    public float pipWidth           = 14f;
    public float pipHeightFraction  = 0.28f; // each pip is this fraction of barHeight
    public float pipGap             = 5f;

    // ── Panel Layout ───────────────────────────────────────────────────────
    [Header("Panel Layout")]
    public float panelPad    = 10f;
    public float elementGap  = 6f;    // gap between HP bar and dash column
    public float screenEdge  = 20f;
    public int   labelSize   = 14;

    // ── Liquid animation ───────────────────────────────────────────────────
    [Header("Liquid Feel")]
    [Tooltip("Speed the fill lerps toward target HP. Higher = snappier.")]
    public float liquidSpeed = 6f;

    // ── private ────────────────────────────────────────────────────────────
    Canvas canvas;

    // Per-player state
    struct PlayerHUD
    {
        public Image   panel;
        public Image   hpFill;        // the liquid fill rect
        public Image   hpTrack;
        public Image   dashFill;      // single gauge fill
        public Image   dashBg;        // gauge background
        public float   targetFill;    // 0-1
        public float   currentFill;
    }

    PlayerHUD p1, p2;
    int p1MaxHP, p2MaxHP, p1DashMax, p2DashMax;

    // ── Start ──────────────────────────────────────────────────────────────
    void Start()
    {
        BuildCanvas();

        p1MaxHP   = player1Health != null ? player1Health.maxHP : 25;
        p2MaxHP   = player2Health != null ? player2Health.maxHP : 25;
        p1DashMax = 1; // unused but kept to avoid removing field
        p2DashMax = 1;

        BuildPanel(true,  ref p1, p1MaxHP, p1BarColor);
        BuildPanel(false, ref p2, p2MaxHP, p2BarColor);

        p1.targetFill = p1.currentFill = 1f;
        p2.targetFill = p2.currentFill = 1f;

        if (player1Health != null)
            player1Health.onHealthChanged.AddListener((hp, max) =>
            {
                p1.targetFill = (float)hp / max;
                StartCoroutine(FlashPanel(p1.panel));
            });

        if (player2Health != null)
            player2Health.onHealthChanged.AddListener((hp, max) =>
            {
                p2.targetFill = (float)hp / max;
                StartCoroutine(FlashPanel(p2.panel));
            });
    }

    void Update()
    {
        AnimateLiquid(ref p1);
        AnimateLiquid(ref p2);
        UpdateDashGauge(ref p1, player1Movement);
        UpdateDashGauge(ref p2, player2Movement);
    }

    // ── Liquid lerp ────────────────────────────────────────────────────────
    void AnimateLiquid(ref PlayerHUD h)
    {
        h.currentFill = Mathf.Lerp(h.currentFill, h.targetFill, Time.deltaTime * liquidSpeed);

        // The fill image is anchored at the bottom; we scale its anchorMax.y
        RectTransform rt = h.hpFill.rectTransform;
        rt.anchorMax = new Vector2(1f, h.currentFill);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ── Dash gauge ─────────────────────────────────────────────────────────
    void UpdateDashGauge(ref PlayerHUD h, GhostMovement mv)
    {
        if (mv == null) return;

        float gauge     = mv.DashGauge;   // 0-1
        bool  dashing   = mv.IsDashing;

        h.dashFill.fillAmount = gauge;
        h.dashFill.color      = dashing ? dashFullColor : dashRechargeColor;
        h.dashBg.color        = dashEmptyColor;
    }

    // ── Panel builder ──────────────────────────────────────────────────────
    void BuildPanel(bool isLeft, ref PlayerHUD h, int maxHP, Color barColor)
    {
        // Panel size: HP bar + gap + single dash gauge bar, plus padding
        float panelW = panelPad * 2 + barWidth + elementGap + pipWidth;
        float panelH = panelPad * 2 + barHeight + labelSize + 8f;

        // ── Panel background ───────────────────────────────────────────────
        GameObject panelGO = NewImage("Panel", canvas.transform, panelBgColor);
        h.panel = panelGO.GetComponent<Image>();
        RectTransform pr = panelGO.GetComponent<RectTransform>();
        pr.sizeDelta = new Vector2(panelW, panelH);
        // Anchor to top-left or top-right
        float ax = isLeft ? 0f : 1f;
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(ax, 1f);
        pr.anchoredPosition = new Vector2(isLeft ? screenEdge : -screenEdge, -screenEdge);

        // ── Label ──────────────────────────────────────────────────────────
        // Sits at bottom of panel
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(panelGO.transform, false);
        Text lbl = labelGO.AddComponent<Text>();
        lbl.text      = isLeft ? "P1" : "P2";
        lbl.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lbl.fontSize  = labelSize;
        lbl.fontStyle = FontStyle.Bold;
        lbl.color     = barColor;
        lbl.alignment = isLeft ? TextAnchor.LowerLeft : TextAnchor.LowerRight;
        RectTransform lr = labelGO.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0f, 0f);
        lr.anchorMax = new Vector2(1f, 0f);
        lr.pivot     = new Vector2(0.5f, 0f);
        lr.offsetMin = new Vector2(panelPad, panelPad - 2f);
        lr.offsetMax = new Vector2(-panelPad, panelPad + labelSize + 2f);

        // ── HP track ───────────────────────────────────────────────────────
        // Anchored: left side of panel for P1, right side for P2
        // X position of HP bar inside panel
        float hpX = isLeft
            ? panelPad
            : -(panelPad + barWidth);   // measured from right edge for P2

        GameObject trackGO = NewImage("HP_Track", panelGO.transform, barTrackColor);
        h.hpTrack = trackGO.GetComponent<Image>();
        RectTransform tr = trackGO.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(isLeft ? 0f : 1f, 1f);
        tr.pivot     = new Vector2(isLeft ? 0f : 1f, 1f);
        tr.sizeDelta = new Vector2(barWidth, barHeight);
        tr.anchoredPosition = new Vector2(
            isLeft ? panelPad : -panelPad,
            -(panelPad + labelSize + 6f));

        // ── HP fill (liquid — anchored bottom, we scale anchorMax.y) ───────
        GameObject fillGO = NewImage("HP_Fill", trackGO.transform, barColor);
        h.hpFill = fillGO.GetComponent<Image>();
        RectTransform fr = fillGO.GetComponent<RectTransform>();
        fr.anchorMin = new Vector2(0f, 0f);
        fr.anchorMax = new Vector2(1f, 1f);
        fr.offsetMin = new Vector2(barInnerPad, barInnerPad);
        fr.offsetMax = new Vector2(-barInnerPad, -barInnerPad);

        // (shine overlay removed — caused visual artefacts on both bars)

        // ── Dash gauge (single vertical bar beside HP) ─────────────────────
        // Same X logic as before: right of HP for P1, left of HP for P2
        float dashColX = isLeft
            ? panelPad + barWidth + elementGap
            : panelPad;
        float dashTopY = -(panelPad + labelSize + 6f);

        // Background
        GameObject dashBgGO = NewImage("Dash_BG", panelGO.transform, dashEmptyColor);
        h.dashBg = dashBgGO.GetComponent<Image>();
        RectTransform dbr = dashBgGO.GetComponent<RectTransform>();
        dbr.anchorMin = dbr.anchorMax = new Vector2(0f, 1f);
        dbr.pivot     = new Vector2(0f, 1f);
        dbr.sizeDelta = new Vector2(pipWidth, barHeight);
        dbr.anchoredPosition = new Vector2(dashColX, dashTopY);

        // Fill — vertical, bottom to top
        GameObject dashFillGO = NewImage("Dash_Fill", dashBgGO.transform, dashFullColor);
        RectTransform dfr = dashFillGO.GetComponent<RectTransform>();
        dfr.anchorMin = new Vector2(0f, 0f);
        dfr.anchorMax = new Vector2(1f, 1f);
        dfr.offsetMin = new Vector2(2f, 2f);
        dfr.offsetMax = new Vector2(-2f, -2f);
        Image dashFillImg = dashFillGO.GetComponent<Image>();
        dashFillImg.type        = Image.Type.Filled;
        dashFillImg.fillMethod  = Image.FillMethod.Vertical;
        dashFillImg.fillOrigin  = (int)Image.OriginVertical.Bottom;
        dashFillImg.fillAmount  = 1f;
        h.dashFill = dashFillImg;
    }

    // ── Flash coroutine ────────────────────────────────────────────────────
    IEnumerator FlashPanel(Image panel)
    {
        Color orig = panel.color;
        panel.color = damageFlashColor;
        yield return new WaitForSeconds(flashDuration);
        panel.color = orig;
    }

    // ── Canvas ─────────────────────────────────────────────────────────────
    void BuildCanvas()
    {
        GameObject go = new GameObject("GhostHUD_Canvas");
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();
    }

    GameObject NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return go;
    }
}

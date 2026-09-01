using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple HUD showing player level and XP progress.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("References")]
    public PlayerStats playerStats;
    public WaveManager waveManager;

    private Health playerHealth;
    private Text levelText;
    private Text xpText;
    private Text waveText;
    private Text hpText;
    private Image xpFill;
    private Image hpFill;

    private void Start()
    {
        if (playerStats == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerStats = player.GetComponent<PlayerStats>();
        }

        if (playerStats == null)
        {
            Debug.LogError("GameHUD: No PlayerStats found. Assign the Player or tag it 'Player'.");
            enabled = false;
            return;
        }

        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();

        playerHealth = playerStats.GetComponent<Health>();

        BuildUI();
        playerStats.OnXPChanged += RefreshXP;
        playerStats.OnLevelUp += RefreshLevel;
        if (playerHealth != null)
            playerHealth.OnHealthChanged += RefreshHealth;
        RefreshLevel(playerStats.level);
        RefreshXP(playerStats.currentXP, playerStats.xpToNextLevel);
        RefreshHealth(playerHealth != null ? playerHealth.CurrentHealth : 0f,
            playerHealth != null ? playerHealth.MaxHealth : 1f);
        RefreshWave();
    }

    private void Update()
    {
        RefreshWave();
    }

    private void RefreshWave()
    {
        if (waveText == null || waveManager == null) return;

        int room = waveManager.CurrentRoom;
        int waveInRoom = waveManager.WaveInRoom;
        if (room <= 0)
        {
            waveText.text = "Room —";
            return;
        }

        string label = $"Room {room}  Wave {waveInRoom}/3";

        if (waveManager.IsBossWave)
            waveText.text = $"{label}  BOSS";
        else if (waveManager.IsMidbossWave)
            waveText.text = $"{label}  Mid-Boss";
        else
            waveText.text = label;
    }

    private void OnDestroy()
    {
        if (playerStats == null) return;
        playerStats.OnXPChanged -= RefreshXP;
        playerStats.OnLevelUp -= RefreshLevel;
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= RefreshHealth;
    }

    private void RefreshLevel(int level)
    {
        if (levelText != null)
            levelText.text = $"Level {level}";
    }

    private void RefreshXP(float current, float toNext)
    {
        if (xpFill != null)
            xpFill.fillAmount = toNext > 0f ? current / toNext : 0f;
        if (xpText != null)
            xpText.text = $"XP {Mathf.FloorToInt(current)} / {Mathf.FloorToInt(toNext)}";
    }

    private void RefreshHealth(float current, float max)
    {
        if (hpFill != null)
            hpFill.fillAmount = max > 0f ? current / max : 0f;
        if (hpText != null)
            hpText.text = $"HP {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("HUDCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGo.AddComponent<GraphicRaycaster>();

        Font font = UIHelper.GetFont();

        GameObject levelGo = new GameObject("LevelText");
        levelGo.transform.SetParent(canvasGo.transform, false);
        RectTransform levelRect = levelGo.AddComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0f, 1f);
        levelRect.anchorMax = new Vector2(0f, 1f);
        levelRect.pivot = new Vector2(0f, 1f);
        levelRect.anchoredPosition = new Vector2(24f, -24f);
        levelRect.sizeDelta = new Vector2(260f, 40f);

        levelText = levelGo.AddComponent<Text>();
        levelText.font = font;
        levelText.fontSize = 28;
        levelText.alignment = TextAnchor.MiddleLeft;
        levelText.color = Color.white;

        GameObject hpBgGo = new GameObject("HPBarBackground");
        hpBgGo.transform.SetParent(canvasGo.transform, false);
        RectTransform hpBgRect = hpBgGo.AddComponent<RectTransform>();
        hpBgRect.anchorMin = new Vector2(1f, 1f);
        hpBgRect.anchorMax = new Vector2(1f, 1f);
        hpBgRect.pivot = new Vector2(1f, 1f);
        hpBgRect.anchoredPosition = new Vector2(-24f, -24f);
        hpBgRect.sizeDelta = new Vector2(280f, 18f);

        Image hpBg = hpBgGo.AddComponent<Image>();
        hpBg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        GameObject hpFillGo = new GameObject("HPBarFill");
        hpFillGo.transform.SetParent(hpBgGo.transform, false);
        RectTransform hpFillRect = hpFillGo.AddComponent<RectTransform>();
        hpFillRect.anchorMin = Vector2.zero;
        hpFillRect.anchorMax = Vector2.one;
        hpFillRect.offsetMin = new Vector2(2f, 2f);
        hpFillRect.offsetMax = new Vector2(-2f, -2f);

        hpFill = hpFillGo.AddComponent<Image>();
        hpFill.color = new Color(0.85f, 0.25f, 0.25f, 1f);
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        hpFill.fillAmount = 1f;

        GameObject hpTextGo = new GameObject("HPText");
        hpTextGo.transform.SetParent(canvasGo.transform, false);
        RectTransform hpTextRect = hpTextGo.AddComponent<RectTransform>();
        hpTextRect.anchorMin = new Vector2(1f, 1f);
        hpTextRect.anchorMax = new Vector2(1f, 1f);
        hpTextRect.pivot = new Vector2(1f, 1f);
        hpTextRect.anchoredPosition = new Vector2(-24f, -50f);
        hpTextRect.sizeDelta = new Vector2(280f, 28f);

        hpText = hpTextGo.AddComponent<Text>();
        hpText.font = font;
        hpText.fontSize = 20;
        hpText.alignment = TextAnchor.MiddleRight;
        hpText.color = new Color(0.95f, 0.85f, 0.85f, 1f);

        GameObject barBgGo = new GameObject("XPBarBackground");
        barBgGo.transform.SetParent(canvasGo.transform, false);
        RectTransform barBgRect = barBgGo.AddComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0f, 1f);
        barBgRect.anchorMax = new Vector2(0f, 1f);
        barBgRect.pivot = new Vector2(0f, 1f);
        barBgRect.anchoredPosition = new Vector2(24f, -70f);
        barBgRect.sizeDelta = new Vector2(320f, 18f);

        Image barBg = barBgGo.AddComponent<Image>();
        barBg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        GameObject barFillGo = new GameObject("XPBarFill");
        barFillGo.transform.SetParent(barBgGo.transform, false);
        RectTransform barFillRect = barFillGo.AddComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = Vector2.one;
        barFillRect.offsetMin = new Vector2(2f, 2f);
        barFillRect.offsetMax = new Vector2(-2f, -2f);

        xpFill = barFillGo.AddComponent<Image>();
        xpFill.color = new Color(0.2f, 0.85f, 0.35f, 1f);
        xpFill.type = Image.Type.Filled;
        xpFill.fillMethod = Image.FillMethod.Horizontal;
        xpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        xpFill.fillAmount = 0f;

        GameObject xpTextGo = new GameObject("XPText");
        xpTextGo.transform.SetParent(canvasGo.transform, false);
        RectTransform xpTextRect = xpTextGo.AddComponent<RectTransform>();
        xpTextRect.anchorMin = new Vector2(0f, 1f);
        xpTextRect.anchorMax = new Vector2(0f, 1f);
        xpTextRect.pivot = new Vector2(0f, 1f);
        xpTextRect.anchoredPosition = new Vector2(24f, -96f);
        xpTextRect.sizeDelta = new Vector2(320f, 28f);

        xpText = xpTextGo.AddComponent<Text>();
        xpText.font = font;
        xpText.fontSize = 20;
        xpText.alignment = TextAnchor.MiddleLeft;
        xpText.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        GameObject waveTextGo = new GameObject("WaveText");
        waveTextGo.transform.SetParent(canvasGo.transform, false);
        RectTransform waveTextRect = waveTextGo.AddComponent<RectTransform>();
        waveTextRect.anchorMin = new Vector2(0.5f, 1f);
        waveTextRect.anchorMax = new Vector2(0.5f, 1f);
        waveTextRect.pivot = new Vector2(0.5f, 1f);
        waveTextRect.anchoredPosition = new Vector2(0f, -24f);
        waveTextRect.sizeDelta = new Vector2(400f, 40f);

        waveText = waveTextGo.AddComponent<Text>();
        waveText.font = font;
        waveText.fontSize = 26;
        waveText.alignment = TextAnchor.MiddleCenter;
        waveText.color = Color.white;
    }
}

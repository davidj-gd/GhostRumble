using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Shows Game Over / Victory overlay and pauses the run.
/// Builds its own Canvas at runtime.
///
/// Setup: add to GameSystems alongside GameManager.
/// </summary>
public class EndGameUI : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;

    private Canvas canvas;
    private GameObject panel;
    private Text titleText;
    private Text subtitleText;
    private Button restartButton;

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager == null)
        {
            Debug.LogError("EndGameUI: No GameManager found.");
            enabled = false;
            return;
        }

        BuildUI();
        HidePanel();
        gameManager.OnGameOver += ShowGameOver;
        gameManager.OnVictory += ShowVictory;
    }

    private void OnDestroy()
    {
        if (gameManager == null) return;
        gameManager.OnGameOver -= ShowGameOver;
        gameManager.OnVictory -= ShowVictory;

        if (Time.timeScale == 0f)
            Time.timeScale = 1f;
    }

    private void ShowGameOver()
    {
        ShowPanel("Game Over", "You were defeated.", new Color(0.85f, 0.2f, 0.2f, 1f));
    }

    private void ShowVictory()
    {
        int room = gameManager != null ? gameManager.WinRoomNumber : 0;
        ShowPanel("Victory!", $"You cleared room {room}.", new Color(0.25f, 0.85f, 0.45f, 1f));
    }

    private void ShowPanel(string title, string subtitle, Color titleColor)
    {
        panel.SetActive(true);
        titleText.text = title;
        titleText.color = titleColor;
        subtitleText.text = subtitle;
        Time.timeScale = 0f;
    }

    private void HidePanel()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void RestartRun()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("EndGameCanvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGo.AddComponent<GraphicRaycaster>();

        panel = new GameObject("EndGamePanel");
        panel.transform.SetParent(canvasGo.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.72f);

        Font font = UIHelper.GetFont();

        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        RectTransform titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 80f);
        titleRect.sizeDelta = new Vector2(700f, 90f);

        titleText = titleGo.AddComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 64;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;

        GameObject subtitleGo = new GameObject("Subtitle");
        subtitleGo.transform.SetParent(panel.transform, false);
        RectTransform subtitleRect = subtitleGo.AddComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
        subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
        subtitleRect.pivot = new Vector2(0.5f, 0.5f);
        subtitleRect.anchoredPosition = new Vector2(0f, 10f);
        subtitleRect.sizeDelta = new Vector2(700f, 50f);

        subtitleText = subtitleGo.AddComponent<Text>();
        subtitleText.font = font;
        subtitleText.fontSize = 28;
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        GameObject buttonGo = new GameObject("RestartButton");
        buttonGo.transform.SetParent(panel.transform, false);
        RectTransform buttonRect = buttonGo.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -90f);
        buttonRect.sizeDelta = new Vector2(280f, 56f);

        Image buttonBg = buttonGo.AddComponent<Image>();
        buttonBg.color = new Color(0.2f, 0.45f, 0.85f, 1f);

        restartButton = buttonGo.AddComponent<Button>();
        restartButton.targetGraphic = buttonBg;
        restartButton.onClick.AddListener(RestartRun);

        GameObject buttonLabelGo = new GameObject("Label");
        buttonLabelGo.transform.SetParent(buttonGo.transform, false);
        RectTransform labelRect = buttonLabelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text buttonLabel = buttonLabelGo.AddComponent<Text>();
        buttonLabel.font = font;
        buttonLabel.fontSize = 28;
        buttonLabel.alignment = TextAnchor.MiddleCenter;
        buttonLabel.color = Color.white;
        buttonLabel.text = "Restart";
    }
}

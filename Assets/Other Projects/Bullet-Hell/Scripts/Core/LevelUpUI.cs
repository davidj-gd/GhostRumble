using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Shows a perk pick screen when the player levels up.
/// Builds its own Canvas at runtime — no manual UI setup required.
///
/// Setup:
///  - Add to any scene object (e.g. empty "GameSystems" in Gameplay/Sandbox)
///  - Assign Player (or leave blank — finds tag "Player" automatically)
///  - On level-up: pauses the game, shows 3 perk buttons, resumes after pick
/// </summary>
public class LevelUpUI : MonoBehaviour
{
    [Header("References")]
    public PlayerStats playerStats;

    [Header("Settings")]
    public int choicesPerLevel = 3;
    public float perkBoost = 0.1f;

    private Canvas canvas;
    private GameObject panel;
    private Text titleText;
    private Button[] choiceButtons;
    private Text[] choiceLabels;

    private readonly Queue<int> pendingLevels = new Queue<int>();
    private bool isOpen;
    private PerkDefinition[] currentChoices;

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
            Debug.LogError("LevelUpUI: No PlayerStats found. Assign the Player or tag it 'Player'.");
            enabled = false;
            return;
        }

        BuildUI();
        playerStats.OnLevelUp += HandleLevelUp;
        HidePanel();
    }

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnLevelUp -= HandleLevelUp;

        if (isOpen)
            Time.timeScale = 1f;
    }

    private void HandleLevelUp(int newLevel)
    {
        pendingLevels.Enqueue(newLevel);
        if (!isOpen)
            ShowNextLevelUp();
    }

    private void ShowNextLevelUp()
    {
        if (pendingLevels.Count == 0) return;

        int level = pendingLevels.Dequeue();
        isOpen = true;
        Time.timeScale = 0f;

        titleText.text = $"Level {level}!  Choose a perk";
        currentChoices = RollPerkChoices();
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            bool active = i < currentChoices.Length;
            choiceButtons[i].gameObject.SetActive(active);
            if (active)
                choiceLabels[i].text = currentChoices[i].DisplayText;
        }

        panel.SetActive(true);
    }

    private PerkDefinition[] RollPerkChoices()
    {
        List<PerkDefinition> pool = new List<PerkDefinition>(PerkDefinition.All);
        int count = Mathf.Min(choicesPerLevel, pool.Count);
        PerkDefinition[] rolled = new PerkDefinition[count];

        for (int i = 0; i < count; i++)
        {
            int pick = UnityEngine.Random.Range(0, pool.Count);
            rolled[i] = pool[pick];
            pool.RemoveAt(pick);
        }

        return rolled;
    }

    private void PickPerk(int index)
    {
        if (!isOpen || currentChoices == null || index >= currentChoices.Length) return;

        currentChoices[index].Apply(playerStats, perkBoost);
        CloseAndContinue();
    }

    private void CloseAndContinue()
    {
        HidePanel();
        isOpen = false;

        if (pendingLevels.Count > 0)
            ShowNextLevelUp();
        else
            Time.timeScale = 1f;
    }

    private void HidePanel()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void BuildUI()
    {
        EnsureEventSystem();

        GameObject canvasGo = new GameObject("LevelUpCanvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGo.AddComponent<GraphicRaycaster>();

        panel = CreatePanel(canvasGo.transform);
        titleText = CreateTitle(panel.transform);
        choiceButtons = new Button[choicesPerLevel];
        choiceLabels = new Text[choicesPerLevel];

        for (int i = 0; i < choicesPerLevel; i++)
            CreateChoiceButton(panel.transform, i);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();
    }

    private GameObject CreatePanel(Transform parent)
    {
        GameObject panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(parent, false);

        RectTransform rect = panelGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bg = panelGo.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        return panelGo;
    }

    private Text CreateTitle(Transform parent)
    {
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(parent, false);

        RectTransform rect = titleGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.72f);
        rect.anchorMax = new Vector2(0.5f, 0.72f);
        rect.sizeDelta = new Vector2(900f, 80f);

        Text text = titleGo.AddComponent<Text>();
        text.font = UIHelper.GetFont();
        text.fontSize = 42;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = "Level Up!";

        return text;
    }

    private void CreateChoiceButton(Transform parent, int index)
    {
        GameObject buttonGo = new GameObject($"Choice{index + 1}");
        buttonGo.transform.SetParent(parent, false);

        RectTransform rect = buttonGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 70f);
        rect.anchoredPosition = new Vector2(0f, 80f - index * 90f);

        Image image = buttonGo.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

        Button button = buttonGo.AddComponent<Button>();
        int captured = index;
        button.onClick.AddListener(() => PickPerk(captured));

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.3f, 0.45f, 0.7f, 1f);
        colors.pressedColor = new Color(0.2f, 0.35f, 0.55f, 1f);
        button.colors = colors;

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(buttonGo.transform, false);

        RectTransform labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 6f);
        labelRect.offsetMax = new Vector2(-12f, -6f);

        Text label = labelGo.AddComponent<Text>();
        label.font = UIHelper.GetFont();
        label.fontSize = 24;
        label.supportRichText = false;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;

        choiceButtons[index] = button;
        choiceLabels[index] = label;
    }

    private readonly struct PerkDefinition
    {
        public static readonly PerkDefinition[] All =
        {
            new PerkDefinition("Swift Feet", "+10% Move Speed", ApplyMoveSpeed),
            new PerkDefinition("Phantom Dash", "+10% Dash & Slide Speed", ApplyDashGlide),
            new PerkDefinition("Heavy Hitter", "+10% Damage", ApplyDamage),
            new PerkDefinition("Quick Trigger", "+10% Attack Speed", ApplyAttackSpeed),
            new PerkDefinition("Thick Skin", "+10% Max Health", ApplyMaxHealth),
            new PerkDefinition("Fast Bullets", "+10% Projectile Speed", ApplyProjectileSpeed),
        };

        private readonly string title;
        private readonly string description;
        private readonly Action<PlayerStats, float> apply;

        public string DisplayText => $"{title} — {description}";

        public PerkDefinition(string title, string description, Action<PlayerStats, float> apply)
        {
            this.title = title;
            this.description = description;
            this.apply = apply;
        }

        public void Apply(PlayerStats stats, float amount) => apply(stats, amount);

        private static void ApplyMoveSpeed(PlayerStats stats, float amount) => stats.AddMoveSpeedMult(amount);
        private static void ApplyDashGlide(PlayerStats stats, float amount) => stats.AddDashGlideSpeedMult(amount);
        private static void ApplyDamage(PlayerStats stats, float amount) => stats.AddDamageMult(amount);
        private static void ApplyAttackSpeed(PlayerStats stats, float amount) => stats.AddAttackSpeedMult(amount);
        private static void ApplyMaxHealth(PlayerStats stats, float amount) => stats.AddMaxHealthMult(amount);
        private static void ApplyProjectileSpeed(PlayerStats stats, float amount) => stats.AddProjectileSpeedMult(amount);
    }
}

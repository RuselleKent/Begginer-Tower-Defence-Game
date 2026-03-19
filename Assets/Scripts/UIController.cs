using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text resourcesText;
    [SerializeField] private TMP_Text warningText;

    [SerializeField] private GameObject towerPanel;
    [SerializeField] private GameObject towerCardPrefab;
    [SerializeField] private Transform cardsContainer;
    [SerializeField] private TowerData[] towers;
    private List<GameObject> activeCards = new List<GameObject>();

    private Platform _currentPlatform;

    [Header("Tower Actions Panel")]
    [SerializeField] private GameObject towerActionsPanel;
    [SerializeField] private Button refundButton;
    [SerializeField] private Button closeTowerActionsButton;
    [SerializeField] private TMP_Text refundValueText;
    [SerializeField] private TMP_Text towerInfoText;
    private TowerManager _selectedTower;

    [Header("Not Enough Resources")]
    [SerializeField] private TMP_Text notEnoughResourcesText;
    private const float NotEnoughResourcesDisplayDuration = 2f;
    private Coroutine _notEnoughResourcesCoroutine;

    [Header("Countdown")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TMP_Text countdownText;

    [Header("Wave Timer")]
    [SerializeField] private TMP_Text waveTimerText;

    [Header("Boss Warning")]
    [SerializeField] private GameObject bossWarningPanel;
    [SerializeField] private float bossWarningDuration = 2.5f;

    [Header("Floating Text")]
    [SerializeField] private GameObject floatingTextPrefab;
    private readonly List<FloatingText> _floatingTextPool = new List<FloatingText>();

    [SerializeField] private Button speed1Button;
    [SerializeField] private Button speed2Button;
    [SerializeField] private Button speed3Button;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button nextLevelButton;

    [SerializeField] private Color normalButtonColor = Color.white;
    [SerializeField] private Color selectedButtonColor = Color.blue;
    [SerializeField] private Color normalTextColor = Color.black;
    [SerializeField] private Color selectedTextColor = Color.white;

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject missionCompletePanel;
    [SerializeField] private TMP_Text objectiveText;
    private bool _isGamePaused;

    public static bool IsCountdownActive { get; private set; }

    private const float SlowSpeed = 0.5f;
    private const float NormalSpeed = 1f;
    private const float FastSpeed = 2f;

    private Coroutine _bossWarningCoroutine;
    private RangeIndicator _rangeIndicator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateRangeIndicator();
        }
    }

    private void OnEnable()
    {
        Spawner.OnWaveChanged += UpdateWaveText;
        Spawner.OnCountdownTick += ShowCountdown;
        Spawner.OnCountdownComplete += HideCountdown;
        Spawner.OnMissionComplete += ShowMissionComplete;
        Spawner.OnBossWarning += ShowBossWarning;
        Spawner.OnNextWaveIn += ShowNextWaveTimer;
        GameManager.OnLivesChanged += UpdateLivesText;
        GameManager.OnResourcesChanged += UpdateResourcesText;
        GameManager.OnResourcesEarned += SpawnFloatingText;
        Platform.OnPlatformClicked += HandlePlatformClicked;
        TowerCard.OnTowerSelected += HandleTowerSelected;
        TowerManager.OnTowerClicked += HandleTowerClicked;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TutorialManager.OnTutorialComplete += HandleTutorialComplete;
    }

    private void OnDisable()
    {
        Spawner.OnWaveChanged -= UpdateWaveText;
        Spawner.OnCountdownTick -= ShowCountdown;
        Spawner.OnCountdownComplete -= HideCountdown;
        Spawner.OnMissionComplete -= ShowMissionComplete;
        Spawner.OnBossWarning -= ShowBossWarning;
        Spawner.OnNextWaveIn -= ShowNextWaveTimer;
        GameManager.OnLivesChanged -= UpdateLivesText;
        GameManager.OnResourcesChanged -= UpdateResourcesText;
        GameManager.OnResourcesEarned -= SpawnFloatingText;
        Platform.OnPlatformClicked -= HandlePlatformClicked;
        TowerCard.OnTowerSelected -= HandleTowerSelected;
        TowerManager.OnTowerClicked -= HandleTowerClicked;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        TutorialManager.OnTutorialComplete -= HandleTutorialComplete;
    }

    private void Start()
    {
        if (speed1Button != null)
            speed1Button.onClick.AddListener(() => SetGameSpeed(SlowSpeed));
        if (speed2Button != null)
            speed2Button.onClick.AddListener(() => SetGameSpeed(NormalSpeed));
        if (speed3Button != null)
            speed3Button.onClick.AddListener(() => SetGameSpeed(FastSpeed));

        if (refundButton != null)
            refundButton.onClick.AddListener(RefundTower);
        if (closeTowerActionsButton != null)
            closeTowerActionsButton.onClick.AddListener(HideTowerActionsPanel);

        if (GameManager.Instance != null)
            HighlightSelectedSpeedButton(GameManager.Instance.GameSpeed);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    // ─── Range Indicator ─────────────────────────────────────────────────────

    private void CreateRangeIndicator()
    {
        GameObject obj = new GameObject("RangeIndicator");
        obj.transform.SetParent(transform);

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 1f, 0f, 0.5f);
        lr.endColor = new Color(1f, 1f, 0f, 0.5f);
        lr.startWidth = 0.07f;
        lr.endWidth = 0.07f;
        lr.sortingLayerName = "UI";
        lr.sortingOrder = 50;

        _rangeIndicator = obj.AddComponent<RangeIndicator>();
        obj.SetActive(false);
    }

    // ─── Floating Text (pooled) ───────────────────────────────────────────────

    private void SpawnFloatingText(int amount, Vector3 worldPosition)
    {
        if (floatingTextPrefab == null)
            return;

        FloatingText ft = GetPooledFloatingText();
        ft.transform.position = worldPosition;
        ft.Initialize($"+{amount}", Color.yellow);
        ft.gameObject.SetActive(true);
    }

    private FloatingText GetPooledFloatingText()
    {
        foreach (FloatingText ft in _floatingTextPool)
        {
            if (ft != null && !ft.gameObject.activeSelf)
                return ft;
        }

        GameObject obj = Instantiate(floatingTextPrefab);
        FloatingText newFt = obj.GetComponent<FloatingText>();
        _floatingTextPool.Add(newFt);
        return newFt;
    }

    // ─── Wave Timer ───────────────────────────────────────────────────────────

    private void ShowNextWaveTimer(int seconds)
    {
        if (waveTimerText == null)
            return;

        if (bossWarningPanel != null && bossWarningPanel.activeSelf)
            return;

        waveTimerText.gameObject.SetActive(true);
        waveTimerText.text = seconds > 0 ? $"Next wave in {seconds}s..." : "Incoming!";
    }

    private void HideNextWaveTimer()
    {
        if (waveTimerText != null)
            waveTimerText.gameObject.SetActive(false);
    }

    // ─── HUD Text ─────────────────────────────────────────────────────────────

    private void UpdateWaveText(int currentWave)
    {
        if (waveText != null)
            waveText.text = $"Wave: {currentWave + 1}";

        HideNextWaveTimer();
    }

    private void UpdateLivesText(int currentLives)
    {
        if (livesText != null)
            livesText.text = $"Lives: {currentLives}";

        if (currentLives <= 0)
            ShowGameOver();
    }

    private void UpdateResourcesText(int currentResources)
    {
        if (resourcesText != null)
            resourcesText.text = $"Resources: {currentResources}";
    }

    // ─── Countdown ────────────────────────────────────────────────────────────

    private void ShowCountdown(int seconds)
    {
        IsCountdownActive = true;

        if (countdownPanel != null)
        {
            countdownPanel.SetActive(true);
            if (countdownText != null)
                countdownText.text = seconds.ToString();
        }
    }

    private void HideCountdown()
    {
        IsCountdownActive = false;

        if (countdownPanel != null)
            countdownPanel.SetActive(false);
    }

    // ─── Boss Warning ─────────────────────────────────────────────────────────

    private void ShowBossWarning()
    {
        if (bossWarningPanel == null)
            return;

        if (_bossWarningCoroutine != null)
            StopCoroutine(_bossWarningCoroutine);

        HideNextWaveTimer();
        _bossWarningCoroutine = StartCoroutine(BossWarningCoroutine());
    }

    private IEnumerator BossWarningCoroutine()
    {
        bossWarningPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(bossWarningDuration);
        bossWarningPanel.SetActive(false);
        _bossWarningCoroutine = null;
    }

    // ─── Tower Panel ──────────────────────────────────────────────────────────

    private void HandlePlatformClicked(Platform platform)
    {
        if (IsCountdownActive || TutorialManager.IsActive || platform.HasTower)
            return;

        _currentPlatform = platform;
        _selectedTower = null;
        ShowTowerPanel();
    }

    private void HandleTowerClicked(TowerManager tower)
    {
        if (IsCountdownActive || TutorialManager.IsActive)
            return;

        _selectedTower = tower;
        _currentPlatform = tower.Platform;
        ShowTowerActionsPanel();
    }

    private void ShowTowerPanel()
    {
        if (towerPanel == null)
            return;

        if (towerActionsPanel != null && towerActionsPanel.activeSelf)
            HideTowerActionsPanel();

        towerPanel.SetActive(true);
        Platform.towerPanelOpen = true;

        if (GameManager.Instance != null)
            GameManager.Instance.SetTimeScale(0f);

        PopulateTowerCards();
    }

    public void HideTowerPanel()
    {
        if (towerPanel != null)
            towerPanel.SetActive(false);

        Platform.towerPanelOpen = false;
        _rangeIndicator?.Hide();

        if (GameManager.Instance != null)
            GameManager.Instance.SetTimeScale(GameManager.Instance.GameSpeed);

        _currentPlatform = null;
    }

    private void PopulateTowerCards()
    {
        if (cardsContainer == null || towerCardPrefab == null)
            return;

        foreach (var card in activeCards)
        {
            if (card != null)
                Destroy(card);
        }
        activeCards.Clear();

        foreach (var data in towers)
        {
            if (data == null)
                continue;

            GameObject cardObject = Instantiate(towerCardPrefab, cardsContainer);
            TowerCard card = cardObject.GetComponent<TowerCard>();
            if (card != null)
            {
                card.Initialize(data);
                activeCards.Add(cardObject);
            }
        }
    }

    private void HandleTowerSelected(TowerData towerData)
    {
        if (_currentPlatform == null || _currentPlatform.transform.childCount > 0)
        {
            HideTowerPanel();
            StartCoroutine(ShowWarningMessage("This platform already has a tower!"));
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.Resources >= towerData.cost)
        {
            GameManager.Instance.SpendResources(towerData.cost);
            _currentPlatform.PlaceTower(towerData);
            HideTowerPanel();
        }
        else
        {
            // Keep tower panel open so the player can pick a cheaper tower
            ShowNotEnoughResourcesPanel();
        }
    }

    // ─── Not Enough Resources ─────────────────────────────────────────────────

    private void ShowNotEnoughResourcesPanel()
    {
        if (notEnoughResourcesText == null)
            return;

        if (_notEnoughResourcesCoroutine != null)
            StopCoroutine(_notEnoughResourcesCoroutine);

        notEnoughResourcesText.gameObject.SetActive(true);
        _notEnoughResourcesCoroutine = StartCoroutine(AutoHideNotEnoughResources());
    }

    private IEnumerator AutoHideNotEnoughResources()
    {
        yield return new WaitForSecondsRealtime(NotEnoughResourcesDisplayDuration);
        HideNotEnoughResourcesPanel();
    }

    /// <summary>Immediately hides the not enough resources text.</summary>
    public void HideNotEnoughResourcesPanel()
    {
        if (_notEnoughResourcesCoroutine != null)
        {
            StopCoroutine(_notEnoughResourcesCoroutine);
            _notEnoughResourcesCoroutine = null;
        }

        if (notEnoughResourcesText != null)
            notEnoughResourcesText.gameObject.SetActive(false);
    }

    // ─── Tower Actions Panel ──────────────────────────────────────────────────

    private void ShowTowerActionsPanel()
    {
        if (towerActionsPanel == null || _selectedTower == null)
            return;

        if (towerPanel != null && towerPanel.activeSelf)
            HideTowerPanel();

        towerActionsPanel.SetActive(true);
        Platform.towerPanelOpen = true;

        if (GameManager.Instance != null)
            GameManager.Instance.SetTimeScale(0f);

        TowerData d = _selectedTower.CurrentData;

        if (towerInfoText != null && d != null)
        {
            string displayName = !string.IsNullOrEmpty(d.displayName) ? d.displayName : d.name;
            float fireRate = d.shootInterval > 0f ? 1f / d.shootInterval : 0f;
            towerInfoText.text = $"<b>{displayName}</b>\n" +
                                 $"DMG: {d.damage}\n" +
                                 $"RNG: {d.range}\n" +
                                 $"SPD: {fireRate:F1}/s";
        }

        if (refundValueText != null)
            refundValueText.text = $"Refund: {_selectedTower.RefundValue}g";

        if (_rangeIndicator != null && d != null)
            _rangeIndicator.Show(_selectedTower.transform.position, d.range);
    }

    public void HideTowerActionsPanel()
    {
        if (towerActionsPanel != null)
            towerActionsPanel.SetActive(false);

        Platform.towerPanelOpen = false;
        _rangeIndicator?.Hide();

        if (GameManager.Instance != null)
            GameManager.Instance.SetTimeScale(GameManager.Instance.GameSpeed);

        _selectedTower = null;
        _currentPlatform = null;
    }

    public void RefundTower()
    {
        if (_selectedTower != null)
        {
            _selectedTower.Refund();
            HideTowerActionsPanel();
        }
    }

    private IEnumerator ShowWarningMessage(string message)
    {
        if (warningText != null)
        {
            warningText.text = message;
            warningText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(3f);
            warningText.gameObject.SetActive(false);
        }
    }

    // ─── Speed Buttons ────────────────────────────────────────────────────────

    private void SetGameSpeed(float timeScale)
    {
        HighlightSelectedSpeedButton(timeScale);

        if (GameManager.Instance == null)
            return;

        GameManager.Instance.StoreGameSpeed(timeScale);

        if (!_isGamePaused)
            GameManager.Instance.SetTimeScale(timeScale);
    }

    private void UpdateButtonVisual(Button button, bool isSelected)
    {
        if (button == null)
            return;

        if (button.image != null)
            button.image.color = isSelected ? selectedButtonColor : normalButtonColor;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.color = isSelected ? selectedTextColor : normalTextColor;
    }

    private void HighlightSelectedSpeedButton(float selectedSpeed)
    {
        UpdateButtonVisual(speed1Button, Mathf.Approximately(selectedSpeed, SlowSpeed));
        UpdateButtonVisual(speed2Button, Mathf.Approximately(selectedSpeed, NormalSpeed));
        UpdateButtonVisual(speed3Button, Mathf.Approximately(selectedSpeed, FastSpeed));
    }

    // ─── Pause ────────────────────────────────────────────────────────────────

    public void TogglePause()
    {
        if (IsCountdownActive || TutorialManager.IsActive)
            return;

        if ((towerPanel != null && towerPanel.activeSelf) ||
            (towerActionsPanel != null && towerActionsPanel.activeSelf))
            return;

        _isGamePaused = !_isGamePaused;

        if (pausePanel != null)
            pausePanel.SetActive(_isGamePaused);

        if (GameManager.Instance != null)
            GameManager.Instance.SetTimeScale(_isGamePaused ? 0f : GameManager.Instance.GameSpeed);
    }

    // ─── Scene / Navigation ───────────────────────────────────────────────────

    public void RestartLevel()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevel != null)
            LevelManager.Instance.LoadLevel(LevelManager.Instance.CurrentLevel);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void GoToMainMenu()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetTimeScale(1f);

        SceneManager.LoadScene(GameConstants.SCENE_MAIN_MENU);
    }

    private void ShowGameOver()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetTimeScale(0f);

        HideNextWaveTimer();
        HideNotEnoughResourcesPanel();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && Camera.main != null)
            canvas.worldCamera = Camera.main;

        HidePanels();

        if (scene.name == GameConstants.SCENE_MAIN_MENU)
            HideUI();
        else
        {
            ShowUI();
            if (GameManager.Instance != null)
                HighlightSelectedSpeedButton(GameManager.Instance.GameSpeed);
            StartCoroutine(ShowObjectiveAndStartCountdown());
        }
    }

    private IEnumerator ShowObjectiveAndStartCountdown()
    {
        LevelData level = LevelManager.Instance?.CurrentLevel;
        bool hasTutorial = level != null
                           && level.tutorialSteps != null
                           && level.tutorialSteps.Length > 0
                           && TutorialManager.Instance != null;

        if (hasTutorial)
        {
            TutorialManager.Instance.StartTutorial(level.tutorialSteps);
        }
        else
        {
            if (Spawner.Instance != null)
                Spawner.Instance.StartGameWithCountdown(3);
        }

        yield break;
    }

    /// <summary>Called by TutorialManager.OnTutorialComplete — closes any open panels then begins the pre-wave countdown.</summary>
    private void HandleTutorialComplete()
    {
        HideTowerPanel();
        HideTowerActionsPanel();

        if (Spawner.Instance != null)
            Spawner.Instance.StartGameWithCountdown(3);
    }

    private void ShowMissionComplete()
    {
        UpdateNextLevelButton();
        HideNextWaveTimer();
        HideNotEnoughResourcesPanel();

        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(true);

        if (GameManager.Instance != null)
            GameManager.Instance.SetTimeScale(0f);
    }

    public void EnterEndlessMode()
    {
        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.SetTimeScale(GameManager.Instance.GameSpeed);

        if (Spawner.Instance != null)
            Spawner.Instance.EnableEndlessMode();
    }

    private void HideUI()
    {
        if (waveText != null) waveText.gameObject.SetActive(false);
        if (livesText != null) livesText.gameObject.SetActive(false);
        if (resourcesText != null) resourcesText.gameObject.SetActive(false);
        if (warningText != null) warningText.gameObject.SetActive(false);
        if (waveTimerText != null) waveTimerText.gameObject.SetActive(false);
        if (speed1Button != null) speed1Button.gameObject.SetActive(false);
        if (speed2Button != null) speed2Button.gameObject.SetActive(false);
        if (speed3Button != null) speed3Button.gameObject.SetActive(false);
        if (pauseButton != null) pauseButton.gameObject.SetActive(false);
        if (objectiveText != null) objectiveText.gameObject.SetActive(false);
    }

    private void ShowUI()
    {
        if (waveText != null) waveText.gameObject.SetActive(true);
        if (livesText != null) livesText.gameObject.SetActive(true);
        if (resourcesText != null) resourcesText.gameObject.SetActive(true);
        if (speed1Button != null) speed1Button.gameObject.SetActive(true);
        if (speed2Button != null) speed2Button.gameObject.SetActive(true);
        if (speed3Button != null) speed3Button.gameObject.SetActive(true);
        if (pauseButton != null) pauseButton.gameObject.SetActive(true);
    }

    private void HidePanels()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (missionCompletePanel != null) missionCompletePanel.SetActive(false);
        if (towerActionsPanel != null) towerActionsPanel.SetActive(false);
        if (countdownPanel != null) countdownPanel.SetActive(false);
        if (bossWarningPanel != null) bossWarningPanel.SetActive(false);
        HideNotEnoughResourcesPanel();

        _rangeIndicator?.Hide();

        if (_bossWarningCoroutine != null)
        {
            StopCoroutine(_bossWarningCoroutine);
            _bossWarningCoroutine = null;
        }

        IsCountdownActive = false;
        _isGamePaused = false;
    }

    public void LoadNextLevel()
    {
        if (LevelManager.Instance == null)
            return;

        var levelManager = LevelManager.Instance;
        if (levelManager.CurrentLevel == null || levelManager.allLevels == null)
            return;

        int currentIndex = Array.IndexOf(levelManager.allLevels, levelManager.CurrentLevel);
        int nextIndex = currentIndex + 1;

        if (nextIndex < levelManager.allLevels.Length)
        {
            if (missionCompletePanel != null)
                missionCompletePanel.SetActive(false);

            levelManager.LoadLevel(levelManager.allLevels[nextIndex]);
        }
    }

    private void UpdateNextLevelButton()
    {
        if (nextLevelButton == null || LevelManager.Instance == null)
            return;

        var levelManager = LevelManager.Instance;
        if (levelManager.CurrentLevel == null || levelManager.allLevels == null)
            return;

        int currentIndex = Array.IndexOf(levelManager.allLevels, levelManager.CurrentLevel);
        nextLevelButton.interactable = currentIndex + 1 < levelManager.allLevels.Length;
    }
}

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
    public static UIController Instance { get; private set; } // singleton instance para isa lang ang UI controller

    [SerializeField] private TMP_Text waveText; // text para sa wave number
    [SerializeField] private TMP_Text livesText; // text para sa lives ng player
    [SerializeField] private TMP_Text resourcesText; // text para sa resources (gold)
    [SerializeField] private TMP_Text warningText; // text para sa warning messages

    [SerializeField] private GameObject towerPanel; // panel para sa pagpili ng tower
    [SerializeField] private GameObject towerCardPrefab; // prefab ng tower card
    [SerializeField] private Transform cardsContainer; // container ng mga tower cards
    [SerializeField] private TowerData[] towers; // listahan ng mga tower na pwedeng bilhin
    private List<GameObject> activeCards = new List<GameObject>(); // listahan ng mga active tower cards

    private Platform _currentPlatform; // yung platform na kasalukuyang pinili

    [Header("Tower Actions Panel")]
    [SerializeField] private GameObject towerActionsPanel; // panel para sa actions ng tower (refund, info)
    [SerializeField] private Button refundButton; // button para ibenta yung tower
    [SerializeField] private Button closeTowerActionsButton; // button para isara yung actions panel
    [SerializeField] private TMP_Text refundValueText; // text para sa refund value
    [SerializeField] private TMP_Text towerInfoText; // text para sa tower info (damage, range, speed)
    private TowerManager _selectedTower; // yung tower na kasalukuyang napili

    [Header("Not Enough Resources")]
    [SerializeField] private TMP_Text notEnoughResourcesText; // text na nagpapakita ng "not enough resources"
    private const float NotEnoughResourcesDisplayDuration = 2f; // ilang seconds ipapakita yung warning
    private Coroutine _notEnoughResourcesCoroutine; // coroutine para sa auto-hide

    [Header("Countdown")]
    [SerializeField] private GameObject countdownPanel; // panel para sa countdown bago mag-start
    [SerializeField] private TMP_Text countdownText; // text na nagpapakita ng countdown number

    [Header("Wave Timer")]
    [SerializeField] private TMP_Text waveTimerText; // text para sa timer ng next wave

    [Header("Boss Warning")]
    [SerializeField] private GameObject bossWarningPanel; // panel para sa boss warning
    [SerializeField] private float bossWarningDuration = 2.5f; // ilang seconds ipapakita yung boss warning

    [Header("Floating Text")]
    [SerializeField] private GameObject floatingTextPrefab; // prefab para sa floating text (ex. "+25g")
    private readonly List<FloatingText> _floatingTextPool = new List<FloatingText>(); // pool ng floating text objects

    [SerializeField] private Button speed1Button; // button para sa slow speed (0.5x)
    [SerializeField] private Button speed2Button; // button para sa normal speed (1x)
    [SerializeField] private Button speed3Button; // button para sa fast speed (2x)
    [SerializeField] private Button pauseButton; // button para i-pause yung game
    [SerializeField] private Button nextLevelButton; // button para sa next level
    [SerializeField] private Button quitButton; // button para mag-quit

    [SerializeField] private Color normalButtonColor = Color.white; // kulay ng button kapag hindi selected
    [SerializeField] private Color selectedButtonColor = Color.blue; // kulay ng button kapag selected
    [SerializeField] private Color normalTextColor = Color.black; // kulay ng text kapag hindi selected
    [SerializeField] private Color selectedTextColor = Color.white; // kulay ng text kapag selected

    [SerializeField] private GameObject pausePanel; // panel na lumalabas kapag naka-pause
    [SerializeField] private GameObject gameOverPanel; // panel na lumalabas kapag game over
    [SerializeField] private GameObject missionCompletePanel; // panel na lumalabas kapag completed na yung mission
    [SerializeField] private TMP_Text objectiveText; // text para sa objective (waves to win)
    private bool _isGamePaused; // flag kung naka-pause
    private bool _isGameOver; // flag kung game over na

    public static bool IsCountdownActive { get; private set; } // flag kung may active na countdown

    private const float SlowSpeed = 0.5f; // slow game speed
    private const float NormalSpeed = 1f; // normal game speed
    private const float FastSpeed = 2f; // fast game speed
    private const string ItchIoUrl = "https://play.unity.com/en/games/79da6443-ef6a-499c-9b56-681cc1022f9d/towerdefenders"; // URL para sa WebGL redirect

    private Coroutine _bossWarningCoroutine; // coroutine para sa boss warning
    private Coroutine _warningCoroutine; // coroutine para sa warning messages
    private RangeIndicator _rangeIndicator; // range indicator (yung bilog na nagpapakita ng range ng tower)

    private void Awake()
    {
        if (Instance != null && Instance != this) // kung may existing instance
        {
            Destroy(gameObject); // sirain yung duplicate
        }
        else // kung wala pang instance
        {
            Instance = this; // i-set yung instance
            DontDestroyOnLoad(gameObject); // huwag sirain pag nag-load ng bagong scene
            CreateRangeIndicator(); // gawin yung range indicator object
        }
    }

    private void OnEnable()
    {
        Spawner.OnWaveChanged += UpdateWaveText; // subscribe sa wave change event
        Spawner.OnCountdownTick += ShowCountdown; // subscribe sa countdown tick event
        Spawner.OnCountdownComplete += HideCountdown; // subscribe sa countdown complete event
        Spawner.OnMissionComplete += ShowMissionComplete; // subscribe sa mission complete event
        Spawner.OnBossWarning += ShowBossWarning; // subscribe sa boss warning event
        Spawner.OnNextWaveIn += ShowNextWaveTimer; // subscribe sa next wave timer event
        GameManager.OnLivesChanged += UpdateLivesText; // subscribe sa lives change event
        GameManager.OnResourcesChanged += UpdateResourcesText; // subscribe sa resources change event
        GameManager.OnResourcesEarned += SpawnFloatingText; // subscribe sa resources earned event
        Platform.OnPlatformClicked += HandlePlatformClicked; // subscribe sa platform click event
        TowerCard.OnTowerSelected += HandleTowerSelected; // subscribe sa tower selected event
        TowerManager.OnTowerClicked += HandleTowerClicked; // subscribe sa tower click event
        SceneManager.sceneLoaded += OnSceneLoaded; // subscribe sa scene loaded event
        TutorialManager.OnTutorialComplete += HandleTutorialComplete; // subscribe sa tutorial complete event
    }

    private void OnDisable()
    {
        Spawner.OnWaveChanged -= UpdateWaveText; // unsubscribe
        Spawner.OnCountdownTick -= ShowCountdown; // unsubscribe
        Spawner.OnCountdownComplete -= HideCountdown; // unsubscribe
        Spawner.OnMissionComplete -= ShowMissionComplete; // unsubscribe
        Spawner.OnBossWarning -= ShowBossWarning; // unsubscribe
        Spawner.OnNextWaveIn -= ShowNextWaveTimer; // unsubscribe
        GameManager.OnLivesChanged -= UpdateLivesText; // unsubscribe
        GameManager.OnResourcesChanged -= UpdateResourcesText; // unsubscribe
        GameManager.OnResourcesEarned -= SpawnFloatingText; // unsubscribe
        Platform.OnPlatformClicked -= HandlePlatformClicked; // unsubscribe
        TowerCard.OnTowerSelected -= HandleTowerSelected; // unsubscribe
        TowerManager.OnTowerClicked -= HandleTowerClicked; // unsubscribe
        SceneManager.sceneLoaded -= OnSceneLoaded; // unsubscribe
        TutorialManager.OnTutorialComplete -= HandleTutorialComplete; // unsubscribe
    }

    private void Start()
    {
        if (speed1Button != null) // kung may speed1 button
            speed1Button.onClick.AddListener(() => SetGameSpeed(SlowSpeed)); // i-add yung listener para sa slow speed
        if (speed2Button != null) // kung may speed2 button
            speed2Button.onClick.AddListener(() => SetGameSpeed(NormalSpeed)); // i-add yung listener para sa normal speed
        if (speed3Button != null) // kung may speed3 button
            speed3Button.onClick.AddListener(() => SetGameSpeed(FastSpeed)); // i-add yung listener para sa fast speed

        if (refundButton != null) // kung may refund button
            refundButton.onClick.AddListener(RefundTower); // i-add yung listener para sa refund
        if (closeTowerActionsButton != null) // kung may close button
            closeTowerActionsButton.onClick.AddListener(HideTowerActionsPanel); // i-add yung listener para isara yung actions panel

        if (quitButton != null) // kung may quit button
            quitButton.onClick.AddListener(QuitGame); // i-add yung listener para mag-quit

        if (GameManager.Instance != null) // kung may GameManager
            HighlightSelectedSpeedButton(GameManager.Instance.GameSpeed); // i-highlight yung speed button base sa current game speed
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) // kung na-press yung escape key
            TogglePause(); // i-toggle yung pause
    }

    // ─── Range Indicator ─────────────────────────────────────────────────────

    private void CreateRangeIndicator()
    {
        GameObject obj = new GameObject("RangeIndicator"); // gumawa ng bagong GameObject para sa range indicator
        obj.transform.SetParent(transform); // gawing anak ng UIController

        LineRenderer lr = obj.AddComponent<LineRenderer>(); // lagyan ng LineRenderer component
        lr.material = new Material(Shader.Find("Sprites/Default")); // i-set yung material
        lr.startColor = new Color(1f, 1f, 0f, 0.5f); // kulay dilaw na semi-transparent
        lr.endColor = new Color(1f, 1f, 0f, 0.5f); // same na kulay
        lr.startWidth = 0.07f; // lapad ng linya
        lr.endWidth = 0.07f; // lapad ng linya
        lr.sortingLayerName = "UI"; // i-set yung sorting layer sa UI
        lr.sortingOrder = 50; // i-set yung sorting order

        _rangeIndicator = obj.AddComponent<RangeIndicator>(); // lagyan ng RangeIndicator component
        obj.SetActive(false); // i-disable muna (mag-aactivate lang pag kailangan)
    }

    // ─── Floating Text (pooled) ───────────────────────────────────────────────

    private void SpawnFloatingText(int amount, Vector3 worldPosition)
    {
        if (floatingTextPrefab == null) // kung walang prefab
            return; // wag mag-spawn

        FloatingText ft = GetPooledFloatingText(); // kumuha ng floating text sa pool
        ft.transform.position = worldPosition; // i-position sa world position
        ft.Initialize($"+{amount}", Color.yellow); // i-initialize yung text at kulay
        ft.gameObject.SetActive(true); // i-activate (magpapakita at magfa-fade)
    }

    private FloatingText GetPooledFloatingText()
    {
        foreach (FloatingText ft in _floatingTextPool) // dumaan sa bawat floating text sa pool
        {
            if (ft != null && !ft.gameObject.activeSelf) // kung may existing at hindi active
                return ft; // ibalik yun (reuse)
        }

        GameObject obj = Instantiate(floatingTextPrefab); // gumawa ng bagong floating text
        FloatingText newFt = obj.GetComponent<FloatingText>(); // kunin yung FloatingText component
        _floatingTextPool.Add(newFt); // idagdag sa pool
        return newFt; // ibalik yung bagong gawa
    }

    // ─── Wave Timer ───────────────────────────────────────────────────────────

    /// <summary>Returns true if any blocking panel is currently visible.</summary>
    private bool IsAnyPanelShowing()
    {
        return TutorialManager.IsActive || // kung active ang tutorial
               (pausePanel != null && pausePanel.activeSelf) || // kung naka-pause
               (gameOverPanel != null && gameOverPanel.activeSelf) || // kung game over
               (missionCompletePanel != null && missionCompletePanel.activeSelf) || // kung mission complete
               (towerPanel != null && towerPanel.activeSelf) || // kung may open na tower panel
               (towerActionsPanel != null && towerActionsPanel.activeSelf) || // kung may open na tower actions panel
               (bossWarningPanel != null && bossWarningPanel.activeSelf); // kung may open na boss warning panel
    }

    private void ShowNextWaveTimer(int seconds)
    {
        if (waveTimerText == null) // kung walang wave timer text
            return; // wag magpakita

        if (IsAnyPanelShowing()) // kung may panel na naka-open
        {
            HideNextWaveTimer(); // itago yung wave timer
            return; // tapos na
        }

        waveTimerText.gameObject.SetActive(true); // ipakita yung wave timer
        waveTimerText.text = seconds > 0 ? $"Next wave in {seconds}s..." : "Incoming!"; // i-set yung text (kung positive seconds, ipakita countdown, kung zero, "Incoming!")
    }

    private void HideNextWaveTimer()
    {
        if (waveTimerText != null) // kung may wave timer text
            waveTimerText.gameObject.SetActive(false); // itago
    }

    // ─── HUD Text ─────────────────────────────────────────────────────────────

    private void UpdateWaveText(int currentWave)
    {
        if (waveText != null) // kung may wave text
            waveText.text = $"Wave: {currentWave + 1}"; // i-set yung text (currentWave + 1 kasi zero-based)

        HideNextWaveTimer(); // itago yung wave timer (mag-aappear ulit pag next wave na)
    }

    private void UpdateLivesText(int currentLives)
    {
        if (livesText != null) // kung may lives text
            livesText.text = $"Lives: {currentLives}"; // i-set yung text

        // Guard: only trigger game-over once, even if multiple enemies reach the
        // end in the same frame and fire multiple OnLivesChanged events.
        if (currentLives <= 0 && !_isGameOver) // kung zero lives na at hindi pa game over
        {
            _isGameOver = true; // markahan na game over na
            ShowGameOver(); // ipakita yung game over panel
        }
    }

    private void UpdateResourcesText(int currentResources)
    {
        if (resourcesText != null) // kung may resources text
            resourcesText.text = $"Resources: {currentResources}"; // i-set yung text
    }

    // ─── Countdown ────────────────────────────────────────────────────────────

    private void ShowCountdown(int seconds)
    {
        IsCountdownActive = true; // markahan na active ang countdown

        if (countdownPanel != null) // kung may countdown panel
        {
            countdownPanel.SetActive(true); // ipakita yung panel
            if (countdownText != null) // kung may countdown text
                countdownText.text = seconds.ToString(); // i-set yung text sa current second
        }
    }

    private void HideCountdown()
    {
        IsCountdownActive = false; // markahan na hindi na active ang countdown

        if (countdownPanel != null) // kung may countdown panel
            countdownPanel.SetActive(false); // itago
    }

    // ─── Boss Warning ─────────────────────────────────────────────────────────

    private void ShowBossWarning()
    {
        if (bossWarningPanel == null) // kung walang boss warning panel
            return; // wag magpakita

        if (_bossWarningCoroutine != null) // kung may existing coroutine
            StopCoroutine(_bossWarningCoroutine); // i-stop muna

        HideNextWaveTimer(); // itago yung wave timer
        _bossWarningCoroutine = StartCoroutine(BossWarningCoroutine()); // simulan yung boss warning coroutine
    }

    private IEnumerator BossWarningCoroutine()
    {
        bossWarningPanel.SetActive(true); // ipakita yung boss warning panel
        yield return new WaitForSecondsRealtime(bossWarningDuration); // maghintay ng ilang seconds (real time, kahit naka-pause)
        bossWarningPanel.SetActive(false); // itago yung panel
        _bossWarningCoroutine = null; // i-reset yung coroutine reference
    }

    // ─── Tower Panel ──────────────────────────────────────────────────────────

    private void HandlePlatformClicked(Platform platform)
    {
        if (IsCountdownActive || TutorialManager.IsActive || platform.HasTower) // kung may countdown, tutorial, o may tower na
            return; // wag magpakita ng tower panel

        _currentPlatform = platform; // i-save yung platform
        _selectedTower = null; // walang selected tower
        ShowTowerPanel(); // ipakita yung tower selection panel
    }

    private void HandleTowerClicked(TowerManager tower)
    {
        if (IsCountdownActive || TutorialManager.IsActive) // kung may countdown o tutorial
            return; // wag magpakita

        _selectedTower = tower; // i-save yung selected tower
        _currentPlatform = tower.Platform; // i-save yung platform
        ShowTowerActionsPanel(); // ipakita yung tower actions panel
    }

    private void ShowTowerPanel()
    {
        if (towerPanel == null) // kung walang tower panel
            return; // wag magpakita

        if (towerActionsPanel != null && towerActionsPanel.activeSelf) // kung may open na tower actions panel
            HideTowerActionsPanel(); // isara muna

        HideNextWaveTimer(); // itago yung wave timer

        towerPanel.SetActive(true); // ipakita yung tower panel
        Platform.towerPanelOpen = true; // markahan na may open na tower panel (para hindi makapag-click sa iba)

        if (GameManager.Instance != null) // kung may GameManager
            GameManager.Instance.SetTimeScale(0f); // i-pause yung game

        PopulateTowerCards(); // i-populate yung tower cards
    }

    /// <summary>Hides the tower selection panel and resumes the game.</summary>
    public void HideTowerPanel()
    {
        if (towerPanel != null) // kung may tower panel
            towerPanel.SetActive(false); // itago

        Platform.towerPanelOpen = false; // markahan na walang open na tower panel
        _rangeIndicator?.Hide(); // itago yung range indicator (kung meron)
        HideNotEnoughResourcesPanel(); // itago yung "not enough resources" text

        if (GameManager.Instance != null) // kung may GameManager
            GameManager.Instance.SetTimeScale(GameManager.Instance.GameSpeed); // i-resume yung game (ibalik sa dating speed)

        _currentPlatform = null; // i-clear yung current platform
    }

    private void PopulateTowerCards()
    {
        if (cardsContainer == null || towerCardPrefab == null) // kung walang container o walang prefab
            return; // wag mag-populate

        foreach (var card in activeCards) // dumaan sa bawat active card
        {
            if (card != null) // kung may card
                Destroy(card); // sirain (para ma-clear)
        }
        activeCards.Clear(); // i-clear yung listahan

        foreach (var data in towers) // dumaan sa bawat tower data
        {
            if (data == null) // kung walang data
                continue; // skip

            GameObject cardObject = Instantiate(towerCardPrefab, cardsContainer); // gumawa ng bagong card
            TowerCard card = cardObject.GetComponent<TowerCard>(); // kunin yung TowerCard component
            if (card != null) // kung may card
            {
                card.Initialize(data); // i-initialize yung card gamit yung tower data
                activeCards.Add(cardObject); // idagdag sa listahan
            }
        }
    }

    private void HandleTowerSelected(TowerData towerData)
    {
        if (_currentPlatform == null || _currentPlatform.transform.childCount > 0) // kung walang platform o may tower na
        {
            HideTowerPanel(); // itago yung tower panel
            if (_warningCoroutine != null) StopCoroutine(_warningCoroutine); // i-stop yung existing warning coroutine
            _warningCoroutine = StartCoroutine(ShowWarningMessage("This platform already has a tower!")); // magpakita ng warning
            return; // tapos na
        }

        if (GameManager.Instance != null && GameManager.Instance.Resources >= towerData.cost) // kung may sapat na resources
        {
            GameManager.Instance.SpendResources(towerData.cost); // bawas yung resources
            _currentPlatform.PlaceTower(towerData); // i-place yung tower sa platform
            HideTowerPanel(); // itago yung tower panel
        }
        else // kung hindi sapat ang resources
        {
            ShowNotEnoughResourcesPanel(); // magpakita ng "not enough resources" message
        }
    }

    // ─── Not Enough Resources ─────────────────────────────────────────────────

    private void ShowNotEnoughResourcesPanel()
    {
        if (notEnoughResourcesText == null) // kung walang text
            return; // wag magpakita

        if (_notEnoughResourcesCoroutine != null) // kung may existing coroutine
            StopCoroutine(_notEnoughResourcesCoroutine); // i-stop

        notEnoughResourcesText.gameObject.SetActive(true); // ipakita yung text
        _notEnoughResourcesCoroutine = StartCoroutine(AutoHideNotEnoughResources()); // simulan yung auto-hide coroutine
    }

    private IEnumerator AutoHideNotEnoughResources()
    {
        yield return new WaitForSecondsRealtime(NotEnoughResourcesDisplayDuration); // maghintay ng ilang seconds (real time)
        HideNotEnoughResourcesPanel(); // itago yung panel
    }

    /// <summary>Immediately hides the not enough resources text.</summary>
    public void HideNotEnoughResourcesPanel()
    {
        if (_notEnoughResourcesCoroutine != null) // kung may active coroutine
        {
            StopCoroutine(_notEnoughResourcesCoroutine); // i-stop
            _notEnoughResourcesCoroutine = null; // i-reset
        }

        if (notEnoughResourcesText != null) // kung may text
            notEnoughResourcesText.gameObject.SetActive(false); // itago
    }

    // ─── Tower Actions Panel ──────────────────────────────────────────────────

    private void ShowTowerActionsPanel()
    {
        if (towerActionsPanel == null || _selectedTower == null) // kung walang panel o walang selected tower
            return; // wag magpakita

        if (towerPanel != null && towerPanel.activeSelf) // kung may open na tower panel
            HideTowerPanel(); // isara muna

        HideNextWaveTimer(); // itago yung wave timer

        towerActionsPanel.SetActive(true); // ipakita yung tower actions panel
        Platform.towerPanelOpen = true; // markahan na may open na panel

        if (GameManager.Instance != null) // kung may GameManager
            GameManager.Instance.SetTimeScale(0f); // i-pause yung game

        TowerData d = _selectedTower.CurrentData; // kunin yung tower data

        if (towerInfoText != null && d != null) // kung may info text at may data
        {
            string displayName = !string.IsNullOrEmpty(d.displayName) ? d.displayName : d.name; // kunin yung display name (kung wala, gamitin yung name)
            float fireRate = d.shootInterval > 0f ? 1f / d.shootInterval : 0f; // compute yung fire rate (shots per second)
            towerInfoText.text = $"<b>{displayName}</b>\n" + // display name (bold)
                                 $"DMG: {d.damage}\n" + // damage
                                 $"RNG: {d.range}\n" + // range
                                 $"SPD: {fireRate:F1}/s"; // speed (fire rate)
        }

        if (refundValueText != null) // kung may refund value text
            refundValueText.text = $"Refund: {_selectedTower.RefundValue}g"; // i-set yung refund value

        if (_rangeIndicator != null && d != null) // kung may range indicator at may data
            _rangeIndicator.Show(_selectedTower.transform.position, d.range); // ipakita yung range indicator
    }

    /// <summary>Hides the tower actions panel and resumes the game.</summary>
    public void HideTowerActionsPanel()
    {
        if (towerActionsPanel != null) // kung may panel
            towerActionsPanel.SetActive(false); // itago

        Platform.towerPanelOpen = false; // markahan na walang open na panel
        _rangeIndicator?.Hide(); // itago yung range indicator

        if (GameManager.Instance != null) // kung may GameManager
            GameManager.Instance.SetTimeScale(GameManager.Instance.GameSpeed); // i-resume yung game

        _selectedTower = null; // i-clear yung selected tower
        _currentPlatform = null; // i-clear yung current platform
    }

    /// <summary>Refunds the selected tower and closes the actions panel.</summary>
    public void RefundTower()
    {
        if (_selectedTower != null) // kung may selected tower
        {
            _selectedTower.Refund(); // ibenta yung tower
            HideTowerActionsPanel(); // itago yung actions panel
        }
    }

    private IEnumerator ShowWarningMessage(string message)
    {
        if (warningText != null) // kung may warning text
        {
            warningText.text = message; // i-set yung message
            warningText.gameObject.SetActive(true); // ipakita
            yield return new WaitForSecondsRealtime(3f); // maghintay ng 3 seconds (real time)
            warningText.gameObject.SetActive(false); // itago
        }
        _warningCoroutine = null; // i-reset yung coroutine reference
    }

    // ─── Speed Buttons ────────────────────────────────────────────────────────

    private void SetGameSpeed(float timeScale)
    {
        HighlightSelectedSpeedButton(timeScale); // i-highlight yung napiling speed button

        if (GameManager.Instance == null) // kung walang GameManager
            return; // wag mag-set

        GameManager.Instance.StoreGameSpeed(timeScale); // i-store yung game speed

        if (!_isGamePaused) // kung hindi naka-pause
            GameManager.Instance.SetTimeScale(timeScale); // i-apply yung time scale
    }

    private void UpdateButtonVisual(Button button, bool isSelected)
    {
        if (button == null) // kung walang button
            return; // wag mag-update

        if (button.image != null) // kung may image
            button.image.color = isSelected ? selectedButtonColor : normalButtonColor; // i-set yung kulay (selected o normal)

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(); // kunin yung text ng button
        if (text != null) // kung may text
            text.color = isSelected ? selectedTextColor : normalTextColor; // i-set yung kulay ng text
    }

    private void HighlightSelectedSpeedButton(float selectedSpeed)
    {
        UpdateButtonVisual(speed1Button, Mathf.Approximately(selectedSpeed, SlowSpeed)); // i-highlight kung slow speed
        UpdateButtonVisual(speed2Button, Mathf.Approximately(selectedSpeed, NormalSpeed)); // i-highlight kung normal speed
        UpdateButtonVisual(speed3Button, Mathf.Approximately(selectedSpeed, FastSpeed)); // i-highlight kung fast speed
    }

    // ─── Pause ────────────────────────────────────────────────────────────────

    /// <summary>Toggles the pause state. Blocked during countdown, tutorial, and open panels.</summary>
    public void TogglePause()
    {
        if (IsCountdownActive || TutorialManager.IsActive) // kung may countdown o tutorial
            return; // hindi pwedeng mag-pause

        if ((towerPanel != null && towerPanel.activeSelf) || // kung may open na tower panel
            (towerActionsPanel != null && towerActionsPanel.activeSelf)) // o may open na tower actions panel
            return; // hindi pwedeng mag-pause

        _isGamePaused = !_isGamePaused; // i-toggle yung pause state

        if (_isGamePaused) // kung naka-pause
        {
            HideNextWaveTimer(); // itago yung wave timer
        }
        else // kung nag-resume
        {
            Spawner.Instance?.RefreshWaveTimer(); // i-refresh yung wave timer (para magpakita ulit)
        }

        if (pausePanel != null) // kung may pause panel
            pausePanel.SetActive(_isGamePaused); // ipakita o itago depende sa pause state

        if (GameManager.Instance != null) // kung may GameManager
            GameManager.Instance.SetTimeScale(_isGamePaused ? 0f : GameManager.Instance.GameSpeed); // i-set yung time scale (0 pag naka-pause, game speed pag hindi)
    }

    // ─── Scene / Navigation ───────────────────────────────────────────────────

    /// <summary>Restarts the current level.</summary>
    public void RestartLevel()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevel != null) // kung may LevelManager at current level
            LevelManager.Instance.LoadLevel(LevelManager.Instance.CurrentLevel); // i-reload yung current level
    }

    /// <summary>
    /// Quits the application.
    /// Editor: exits play mode.
    /// WebGL: attempts to close the current tab, redirects to game page if unsuccessful.
    /// Standalone: calls Application.Quit().
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // kung nasa editor, i-stop yung play mode
#elif UNITY_WEBGL
        Application.ExternalEval(@"
            window.close(); // subukang isara yung browser tab
            setTimeout(function() {
                if (!window.closed) { // kung hindi nagsara
                    window.location.href = '" + ItchIoUrl + @"'; // i-redirect sa game page
                }
            }, 150);
        ");
#else
        Application.Quit(); // kung standalone, i-quit yung application
#endif
    }

    /// <summary>Returns to the main menu and resets time scale.</summary>
    public void GoToMainMenu()
    {
        if (GameManager.Instance != null) // kung may GameManager
            GameManager.Instance.SetTimeScale(1f); // i-reset yung time scale sa normal

        SceneManager.LoadScene(GameConstants.SCENE_MAIN_MENU); // i-load yung main menu scene
    }

    private void ShowGameOver()
    {
        if (GameManager.Instance != null) // kung may GameManager
            GameManager.Instance.SetTimeScale(0f); // i-pause yung game

        HideNextWaveTimer(); // itago yung wave timer
        HideNotEnoughResourcesPanel(); // itago yung not enough resources text

        if (_warningCoroutine != null) // kung may active warning coroutine
        {
            StopCoroutine(_warningCoroutine); // i-stop
            _warningCoroutine = null; // i-reset
        }
        if (warningText != null) // kung may warning text
            warningText.gameObject.SetActive(false); // itago

        if (gameOverPanel != null) // kung may game over panel
            gameOverPanel.SetActive(true); // ipakita
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Canvas canvas = GetComponent<Canvas>(); // kunin yung Canvas component
        if (canvas != null && Camera.main != null) // kung may canvas at main camera
            canvas.worldCamera = Camera.main; // i-set yung camera ng canvas sa main camera

        HidePanels(); // itago lahat ng panels

        if (scene.name == GameConstants.SCENE_MAIN_MENU) // kung nasa main menu
            HideUI(); // itago yung UI elements
        else // kung nasa game scene
        {
            ShowUI(); // ipakita yung UI elements
            if (GameManager.Instance != null) // kung may GameManager
                HighlightSelectedSpeedButton(GameManager.Instance.GameSpeed); // i-highlight yung speed button
            StartCoroutine(ShowObjectiveAndStartCountdown()); // simulan yung objective at countdown
        }
    }

    private IEnumerator ShowObjectiveAndStartCountdown()
    {
        // Wait one frame so all Awake() methods in the newly loaded scene
        // (including Spawner) have run and Instance references are valid.
        yield return null; // maghintay ng isang frame para mag-awake lahat ng components

        LevelData level = LevelManager.Instance?.CurrentLevel; // kunin yung current level
        bool hasTutorial = level != null // kung may level
                           && level.tutorialSteps != null // at may tutorial steps
                           && level.tutorialSteps.Length > 0 // at hindi empty
                           && TutorialManager.Instance != null; // at may TutorialManager

        if (hasTutorial) // kung may tutorial
        {
            TutorialManager.Instance.StartTutorial(level.tutorialSteps); // simulan yung tutorial
        }
        else // kung walang tutorial
        {
            if (Spawner.Instance != null) // kung may Spawner
                Spawner.Instance.StartGameWithCountdown(3); // magsimula ng countdown (3 seconds)
        }
    }

    /// <summary>Called by TutorialManager.OnTutorialComplete — closes any open panels then begins the pre-wave countdown.</summary>
    private void HandleTutorialComplete()
    {
        HideTowerPanel(); // itago yung tower panel
        HideTowerActionsPanel(); // itago yung tower actions panel

        if (Spawner.Instance != null) // kung may Spawner
            Spawner.Instance.StartGameWithCountdown(3); // magsimula ng countdown (3 seconds)
    }

    private void ShowMissionComplete()
    {
        UpdateNextLevelButton(); // i-update yung next level button (ipakita kung may next level)
        HideNextWaveTimer(); // itago yung wave timer
        HideNotEnoughResourcesPanel(); // itago yung not enough resources text

        if (missionCompletePanel != null) // kung may mission complete panel
            missionCompletePanel.SetActive(true); // ipakita

        if (GameManager.Instance != null) // kung may GameManager
            GameManager.Instance.SetTimeScale(0f); // i-pause yung game
    }

    private void HideUI()
    {
        if (waveText != null) waveText.gameObject.SetActive(false); // itago yung wave text
        if (livesText != null) livesText.gameObject.SetActive(false); // itago yung lives text
        if (resourcesText != null) resourcesText.gameObject.SetActive(false); // itago yung resources text
        if (warningText != null) warningText.gameObject.SetActive(false); // itago yung warning text
        if (waveTimerText != null) waveTimerText.gameObject.SetActive(false); // itago yung wave timer
        if (speed1Button != null) speed1Button.gameObject.SetActive(false); // itago yung speed1 button
        if (speed2Button != null) speed2Button.gameObject.SetActive(false); // itago yung speed2 button
        if (speed3Button != null) speed3Button.gameObject.SetActive(false); // itago yung speed3 button
        if (pauseButton != null) pauseButton.gameObject.SetActive(false); // itago yung pause button
        if (objectiveText != null) objectiveText.gameObject.SetActive(false); // itago yung objective text
    }

    private void ShowUI()
    {
        if (waveText != null) waveText.gameObject.SetActive(true); // ipakita yung wave text
        if (livesText != null) livesText.gameObject.SetActive(true); // ipakita yung lives text
        if (resourcesText != null) resourcesText.gameObject.SetActive(true); // ipakita yung resources text
        if (speed1Button != null) speed1Button.gameObject.SetActive(true); // ipakita yung speed1 button
        if (speed2Button != null) speed2Button.gameObject.SetActive(true); // ipakita yung speed2 button
        if (speed3Button != null) speed3Button.gameObject.SetActive(true); // ipakita yung speed3 button
        if (pauseButton != null) pauseButton.gameObject.SetActive(true); // ipakita yung pause button
    }

    private void HidePanels()
    {
        if (pausePanel != null) pausePanel.SetActive(false); // itago yung pause panel
        if (gameOverPanel != null) gameOverPanel.SetActive(false); // itago yung game over panel
        if (missionCompletePanel != null) missionCompletePanel.SetActive(false); // itago yung mission complete panel
        if (towerActionsPanel != null) towerActionsPanel.SetActive(false); // itago yung tower actions panel
        if (countdownPanel != null) countdownPanel.SetActive(false); // itago yung countdown panel
        if (bossWarningPanel != null) bossWarningPanel.SetActive(false); // itago yung boss warning panel
        HideNextWaveTimer(); // itago yung wave timer
        HideNotEnoughResourcesPanel(); // itago yung not enough resources text

        if (_warningCoroutine != null) // kung may active warning coroutine
        {
            StopCoroutine(_warningCoroutine); // i-stop
            _warningCoroutine = null; // i-reset
        }
        if (warningText != null) // kung may warning text
            warningText.gameObject.SetActive(false); // itago

        _rangeIndicator?.Hide(); // itago yung range indicator

        if (_bossWarningCoroutine != null) // kung may active boss warning coroutine
        {
            StopCoroutine(_bossWarningCoroutine); // i-stop
            _bossWarningCoroutine = null; // i-reset
        }

        // Clear stale card references from previous level load.
        foreach (var card in activeCards) // dumaan sa bawat active card
        {
            if (card != null) // kung may card
                Destroy(card); // sirain
        }
        activeCards.Clear(); // i-clear yung listahan

        IsCountdownActive = false; // i-reset yung countdown flag
        _isGamePaused = false; // i-reset yung pause flag

        // Reset game-over guard so it can fire again on the next run.
        _isGameOver = false; // i-reset yung game over flag
    }

    private void UpdateNextLevelButton()
    {
        if (nextLevelButton == null || LevelManager.Instance == null) return; // kung walang button o walang LevelManager, wag mag-update

        int currentIndex = Array.IndexOf(LevelManager.Instance.allLevels, LevelManager.Instance.CurrentLevel); // hanapin yung index ng current level
        nextLevelButton.gameObject.SetActive(currentIndex < LevelManager.Instance.allLevels.Length - 1); // ipakita yung next level button kung hindi ito yung last level
    }

    /// <summary>Loads the next level in the level list.</summary>
    public void LoadNextLevel()
    {
        if (LevelManager.Instance == null) return; // kung walang LevelManager, wag mag-load

        int currentIndex = Array.IndexOf(LevelManager.Instance.allLevels, LevelManager.Instance.CurrentLevel); // hanapin yung index ng current level
        if (currentIndex < LevelManager.Instance.allLevels.Length - 1) // kung hindi ito yung last level
            LevelManager.Instance.LoadLevel(LevelManager.Instance.allLevels[currentIndex + 1]); // i-load yung next level
    }
}
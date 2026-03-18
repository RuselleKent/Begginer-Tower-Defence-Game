using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }
    public static bool IsActive { get; private set; }

    public static event Action OnTutorialComplete;

    [Header("Tutorial Panel")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image illustrationImage;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private TMP_Text stepCounterText;

    private TutorialStep[] _steps;
    private int _currentStepIndex;
    private Coroutine _sequenceCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (nextButton != null)
            nextButton.onClick.AddListener(AdvanceStep);

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipTutorial);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    /// <summary>Begins the tutorial sequence with the provided steps.</summary>
    public void StartTutorial(TutorialStep[] steps)
    {
        if (steps == null || steps.Length == 0)
        {
            CompleteTutorial();
            return;
        }

        _steps = steps;
        _currentStepIndex = 0;

        if (_sequenceCoroutine != null)
            StopCoroutine(_sequenceCoroutine);

        _sequenceCoroutine = StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        IsActive = true;
        GameManager.Instance?.SetTimeScale(0f);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        ShowStep(_currentStepIndex);

        yield break;
    }

    private void ShowStep(int index)
    {
        if (_steps == null || index >= _steps.Length)
        {
            CompleteTutorial();
            return;
        }

        TutorialStep step = _steps[index];

        if (messageText != null)
            messageText.text = step.message;

        if (illustrationImage != null)
        {
            illustrationImage.sprite = step.illustration;
            illustrationImage.gameObject.SetActive(step.illustration != null);
        }

        if (stepCounterText != null)
            stepCounterText.text = $"{index + 1} / {_steps.Length}";

        bool isLastStep = index >= _steps.Length - 1;
        if (nextButton != null)
        {
            TMP_Text label = nextButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = isLastStep ? "Got it!" : "Next";
        }
    }

    private void AdvanceStep()
    {
        _currentStepIndex++;

        if (_currentStepIndex >= _steps.Length)
            CompleteTutorial();
        else
            ShowStep(_currentStepIndex);
    }

    private void SkipTutorial()
    {
        CompleteTutorial();
    }

    private void CompleteTutorial()
    {
        IsActive = false;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.SetTimeScale(GameManager.Instance.GameSpeed);

        OnTutorialComplete?.Invoke();
    }
}

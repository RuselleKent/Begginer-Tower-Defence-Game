using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; } // singleton instance
    public static bool IsActive { get; private set; } // flag kung active ang tutorial (para i-disable yung ibang UI interactions)

    public static event Action OnTutorialComplete; // event pag tapos na ang tutorial

    [Header("Tutorial Panel")]
    [SerializeField] private GameObject tutorialPanel; // yung panel na nagpapakita ng tutorial
    [SerializeField] private TMP_Text messageText; // text na nagpapakita ng instruction
    [SerializeField] private Image illustrationImage; // image na nagpapakita ng illustration (kung meron)
    [SerializeField] private Button nextButton; // button para mag-next step
    [SerializeField] private Button skipButton; // button para i-skip ang tutorial
    [SerializeField] private TMP_Text stepCounterText; // text na nagpapakita kung ilang steps (ex. "1 / 5")

    private TutorialStep[] _steps; // array ng mga tutorial steps (message, illustration)
    private int _currentStepIndex; // kung anong step na
    private Coroutine _sequenceCoroutine; // coroutine reference (para ma-stop kung kailangan)

    private void Awake()
    {
        if (Instance != null && Instance != this) // kung may existing instance
        {
            Destroy(gameObject); // sirain yung duplicate
            return; // tapos na
        }

        Instance = this; // i-set yung instance

        if (nextButton != null) // kung may next button
            nextButton.onClick.AddListener(AdvanceStep); // i-add yung listener (tatawagin pag na-click)

        if (skipButton != null) // kung may skip button
            skipButton.onClick.AddListener(SkipTutorial); // i-add yung listener

        if (tutorialPanel != null) // kung may tutorial panel
            tutorialPanel.SetActive(false); // itago muna (magpapakita lang pag nag-start)
    }

    /// <summary>Begins the tutorial sequence with the provided steps.</summary>
    public void StartTutorial(TutorialStep[] steps)
    {
        if (steps == null || steps.Length == 0) // kung walang steps
        {
            CompleteTutorial(); // tapusin agad (walang tutorial)
            return; // tapos na
        }

        _steps = steps; // i-save yung steps
        _currentStepIndex = 0; // simula sa step 0

        if (_sequenceCoroutine != null) // kung may existing coroutine
            StopCoroutine(_sequenceCoroutine); // i-stop muna

        _sequenceCoroutine = StartCoroutine(RunSequence()); // simulan yung sequence
    }

    private IEnumerator RunSequence()
    {
        IsActive = true; // i-set na active ang tutorial (para ma-disable ang ibang input)
        GameManager.Instance?.SetTimeScale(0f); // i-pause yung game (time scale = 0)

        if (tutorialPanel != null) // kung may panel
            tutorialPanel.SetActive(true); // ipakita yung tutorial panel

        ShowStep(_currentStepIndex); // ipakita yung unang step

        yield break; // tapos na (hindi na kailangan mag-yield)
    }

    private void ShowStep(int index)
    {
        if (_steps == null || index >= _steps.Length) // kung walang steps o lagpas na sa dami
        {
            CompleteTutorial(); // tapusin yung tutorial
            return; // tapos na
        }

        TutorialStep step = _steps[index]; // kunin yung step

        if (messageText != null) // kung may message text
            messageText.text = step.message; // i-set yung message

        if (illustrationImage != null) // kung may illustration image
        {
            illustrationImage.sprite = step.illustration; // i-set yung sprite
            illustrationImage.gameObject.SetActive(step.illustration != null); // ipakita lang kung may illustration
        }

        if (stepCounterText != null) // kung may step counter text
            stepCounterText.text = $"{index + 1} / {_steps.Length}"; // i-set yung text (ex. "1 / 5")

        bool isLastStep = index >= _steps.Length - 1; // tseke kung last step na
        if (nextButton != null) // kung may next button
        {
            TMP_Text label = nextButton.GetComponentInChildren<TMP_Text>(); // kunin yung text ng button
            if (label != null) // kung may label
                label.text = isLastStep ? "Got it!" : "Next"; // kung last step, "Got it!", kung hindi, "Next"
        }
    }

    private void AdvanceStep()
    {
        // Guard against clicks before the tutorial has been started.
        if (_steps == null) // kung walang steps (hindi pa nag-start)
            return; // wag mag-process

        _currentStepIndex++; // dagdagan yung step index

        if (_currentStepIndex >= _steps.Length) // kung tapos na lahat ng steps
            CompleteTutorial(); // tapusin yung tutorial
        else // kung may steps pa
            ShowStep(_currentStepIndex); // ipakita yung next step
    }

    private void SkipTutorial()
    {
        CompleteTutorial(); // tapusin agad yung tutorial (skip)
    }

    private void CompleteTutorial()
    {
        IsActive = false; // hindi na active ang tutorial

        if (tutorialPanel != null) // kung may tutorial panel
            tutorialPanel.SetActive(false); // itago yung panel

        if (GameManager.Instance != null) // kung may GameManager
            GameManager.Instance.SetTimeScale(GameManager.Instance.GameSpeed); // i-resume yung game (ibalik sa dating game speed)

        OnTutorialComplete?.Invoke(); // i-trigger yung event na tapos na ang tutorial
    }
} 

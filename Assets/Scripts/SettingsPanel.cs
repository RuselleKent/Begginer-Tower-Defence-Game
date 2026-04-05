using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SettingsPanel : MonoBehaviour
{
    public static SettingsPanel Instance { get; private set; }

    /// <summary>Fired only when the user explicitly closes the panel via the close button.</summary>
    public static event Action OnClosed;

    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Music")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Button musicMuteButton;
    [SerializeField] private TMP_Text musicMuteLabel;

    [Header("SFX")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button sfxMuteButton;
    [SerializeField] private TMP_Text sfxMuteLabel;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private float _lastMusicVolume = 1f;
    private float _lastSfxVolume   = 1f;
    private bool  _musicMuted;
    private bool  _sfxMuted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (panel != null)
            panel.SetActive(false);

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        if (musicMuteButton != null)
            musicMuteButton.onClick.AddListener(ToggleMusicMute);
        if (sfxMuteButton != null)
            sfxMuteButton.onClick.AddListener(ToggleSfxMute);
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanelByUser); // user-initiated only
    }

    /// <summary>Opens the settings panel and syncs sliders to current AudioManager values.</summary>
    public void OpenPanel()
    {
        if (AudioManager.Instance != null)
        {
            if (musicSlider != null)
                musicSlider.value = AudioManager.Instance.MusicVolume;
            if (sfxSlider != null)
                sfxSlider.value = AudioManager.Instance.SfxVolume;
        }

        if (panel != null)
            panel.SetActive(true);
    }

    /// <summary>Hides the panel silently. Called by HidePanels() on scene transitions —
    /// does NOT play a sound and does NOT fire OnClosed.</summary>
    public void ClosePanel()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    /// <summary>Hides the panel, plays the button sound, and fires OnClosed.
    /// Only called when the user taps the close button.</summary>
    public void ClosePanelByUser()
    {
        AudioManager.Instance?.PlayButtonClick();
        ClosePanel();
        OnClosed?.Invoke();
    }

    /// <summary>Returns true when the settings panel is visible.</summary>
    public bool IsOpen => panel != null && panel.activeSelf;

    private void OnMusicSliderChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
        _musicMuted = Mathf.Approximately(value, 0f);
        UpdateMuteLabel(musicMuteLabel, _musicMuted);
    }

    private void OnSfxSliderChanged(float value)
    {
        AudioManager.Instance?.SetSfxVolume(value);
        _sfxMuted = Mathf.Approximately(value, 0f);
        UpdateMuteLabel(sfxMuteLabel, _sfxMuted);
    }

    private void ToggleMusicMute()
    {
        AudioManager.Instance?.PlayButtonClick();
        _musicMuted = !_musicMuted;

        if (_musicMuted)
        {
            _lastMusicVolume = AudioManager.Instance?.MusicVolume ?? 1f;
            AudioManager.Instance?.SetMusicVolume(0f);
            if (musicSlider != null) musicSlider.value = 0f;
        }
        else
        {
            float restore = _lastMusicVolume > 0f ? _lastMusicVolume : 1f;
            AudioManager.Instance?.SetMusicVolume(restore);
            if (musicSlider != null) musicSlider.value = restore;
        }

        UpdateMuteLabel(musicMuteLabel, _musicMuted);
    }

    private void ToggleSfxMute()
    {
        AudioManager.Instance?.PlayButtonClick();
        _sfxMuted = !_sfxMuted;

        if (_sfxMuted)
        {
            _lastSfxVolume = AudioManager.Instance?.SfxVolume ?? 1f;
            AudioManager.Instance?.SetSfxVolume(0f);
            if (sfxSlider != null) sfxSlider.value = 0f;
        }
        else
        {
            float restore = _lastSfxVolume > 0f ? _lastSfxVolume : 1f;
            AudioManager.Instance?.SetSfxVolume(restore);
            if (sfxSlider != null) sfxSlider.value = restore;
        }

        UpdateMuteLabel(sfxMuteLabel, _sfxMuted);
    }

    private void UpdateMuteLabel(TMP_Text label, bool muted)
    {
        if (label != null)
            label.text = muted ? "Unmute" : "Mute";
    }
}

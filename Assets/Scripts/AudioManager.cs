using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips — Menus")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip buttonClickClip;

    [Header("Clips — Game Events")]
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip youWinClip;

    [Header("Clips — Towers")]
    [SerializeField] private AudioClip burstTowerClip;
    [SerializeField] private AudioClip fastTowerClip;
    [SerializeField] private AudioClip strongTowerClip;

    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SfxVolume";
    private const float DefaultVolume = 1f;

    public float MusicVolume => musicSource != null ? musicSource.volume : 1f;
    public float SfxVolume => sfxSource != null ? sfxSource.volume : 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumeSettings();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Spawner.OnMissionComplete += PlayYouWin;
        GameManager.OnLivesChanged += HandleLivesChanged;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Spawner.OnMissionComplete -= PlayYouWin;
        GameManager.OnLivesChanged -= HandleLivesChanged;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == GameConstants.SCENE_MAIN_MENU)
        {
            PlayMusic(mainMenuMusic);
            return;
        }

        // For game levels, play the background music assigned to the LevelData asset.
        AudioClip levelMusic = LevelManager.Instance?.CurrentLevel?.backgroundMusic;
        if (levelMusic != null)
            PlayMusic(levelMusic);
        else
            StopMusic();
    }

    // ─── Music ────────────────────────────────────────────────────────────────

    /// <summary>Plays a background music clip, looping. Stops current clip first.</summary>
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    /// <summary>Stops background music immediately.</summary>
    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    // ─── SFX ─────────────────────────────────────────────────────────────────

    /// <summary>Plays the button click sound effect.</summary>
    public void PlayButtonClick()
    {
        PlaySfx(buttonClickClip);
    }

    /// <summary>Plays the shoot sound for a given tower display name.</summary>
    public void PlayTowerShoot(string towerDisplayName)
    {
        if (string.IsNullOrEmpty(towerDisplayName))
            return;

        string lower = towerDisplayName.ToLower();

        if (lower.Contains("burst"))
            PlaySfx(burstTowerClip);
        else if (lower.Contains("fast"))
            PlaySfx(fastTowerClip);
        else if (lower.Contains("strong"))
            PlaySfx(strongTowerClip);
    }

    private void PlayYouWin()
    {
        StopMusic();
        PlaySfx(youWinClip);
    }

    private void HandleLivesChanged(int lives)
    {
        if (lives <= 0)
        {
            StopMusic();
            PlaySfx(gameOverClip);
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    // ─── Volume ───────────────────────────────────────────────────────────────

    /// <summary>Sets music volume (0–1) and saves to PlayerPrefs.</summary>
    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (musicSource != null)
            musicSource.volume = volume;
        PlayerPrefs.SetFloat(MusicVolumeKey, volume);
        PlayerPrefs.Save();
    }

    /// <summary>Sets SFX volume (0–1) and saves to PlayerPrefs.</summary>
    public void SetSfxVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (sfxSource != null)
            sfxSource.volume = volume;
        PlayerPrefs.SetFloat(SfxVolumeKey, volume);
        PlayerPrefs.Save();
    }

    private void LoadVolumeSettings()
    {
        float music = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume);
        float sfx = PlayerPrefs.GetFloat(SfxVolumeKey, DefaultVolume);

        if (musicSource != null)
            musicSource.volume = music;
        if (sfxSource != null)
            sfxSource.volume = sfx;
    }
}

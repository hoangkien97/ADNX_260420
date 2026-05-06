using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private AudioClip reloadClip;
    [SerializeField] private AudioClip itemClip;
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioSource backgroundAudioSource;
    [SerializeField] private AudioClip backgroundAudioClip;
    private string targetScene = "SampleScene";
    private string currentScene;
    
    private FileDataHandler<AudioSettingsData> settingsHandler;
    private AudioSettingsData currentSettings;

    private void Start()
    {
        currentScene = SceneManager.GetActiveScene().name;
        settingsHandler = new FileDataHandler<AudioSettingsData>(Application.persistentDataPath, "settings.json");
        
        LoadSettingsIfNeeded();
        
        if (backgroundAudioSource != null) backgroundAudioSource.volume = currentSettings.musicVolume;
        if (audioSource != null) 
        {
            audioSource.volume = currentSettings.musicVolume; 
            audioSource.mute = !currentSettings.isSfxEnabled;
        }

        if (currentScene == targetScene)
            PlayBackgroundMusic();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void LoadSettingsIfNeeded()
    {
        if (currentSettings == null)
        {
            currentSettings = settingsHandler.Load() ?? new AudioSettingsData();
        }
    }

    public void SetMusicVolume(float volume)
    {
        if (backgroundAudioSource != null) backgroundAudioSource.volume = volume;
        if (audioSource != null) audioSource.volume = volume;
        
        LoadSettingsIfNeeded();
        currentSettings.musicVolume = volume;
        settingsHandler.Save(currentSettings);
    }

    public float GetMusicVolume()
    {
        LoadSettingsIfNeeded();
        return currentSettings.musicVolume;
    }

    public void SetSfxEnabled(bool isEnabled)
    {
        if (audioSource != null) audioSource.mute = !isEnabled;
        
        LoadSettingsIfNeeded();
        currentSettings.isSfxEnabled = isEnabled;
        settingsHandler.Save(currentSettings);
    }

    public bool GetSfxEnabled()
    {
        LoadSettingsIfNeeded();
        return currentSettings.isSfxEnabled;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentScene = scene.name;

        if (currentScene == targetScene)
            PlayBackgroundMusic();
        else
            StopBackgroundMusic();
    }
    public void PlayShootSound()
    {
        audioSource.PlayOneShot(shootClip);
    }
    public void PlayReloadSound()
    {
        audioSource.PlayOneShot(reloadClip);
    }
    public void PlayItemSound()
    {
        audioSource.PlayOneShot(itemClip);
    }
    public void PlayCoinSound()
    {
        audioSource.PlayOneShot(coinClip);
    }

    public void PlayBackgroundMusic()
    {
        if (backgroundAudioSource == null || backgroundAudioClip == null) return;
        backgroundAudioSource.clip = backgroundAudioClip;
        backgroundAudioSource.loop = true;
        backgroundAudioSource.Play();
    }

    public void StopBackgroundMusic()
    {
        if (backgroundAudioSource == null) return;
        backgroundAudioSource.Stop();
    }
}

[System.Serializable]
public class AudioSettingsData
{
    public float musicVolume = 1f;
    public bool isSfxEnabled = true;
}

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

    private void Start()
    {
        currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == targetScene)
            PlayBackgroundMusic();

        SceneManager.sceneLoaded += OnSceneLoaded;
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

using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private AudioClip reloadClip;
    [SerializeField] private AudioClip itemClip;
    [SerializeField] private AudioClip coinClip;
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
}

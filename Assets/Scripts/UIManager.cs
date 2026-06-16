using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Trách nhiệm duy nhất: Điều khiển Bật/Tắt các Panel và cập nhật chữ trên màn hình (Coin, Score).
/// </summary>
public class UIManager : MonoBehaviour
{
    [SerializeField] private Text txtCoin;
    [SerializeField] private Text txtScore;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Toggle sfxToggle;

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        SetupAudioUI();
    }

    private void SetupAudioUI()
    {
        AudioManager am = FindAnyObjectByType<AudioManager>();
        if (am == null) return;

        if (musicSlider != null)
        {
            try { musicSlider.value = am.GetMusicVolume(); } catch { }
            musicSlider.onValueChanged.AddListener(am.SetMusicVolume);
        }
        if (sfxToggle != null)
        {
            try { sfxToggle.isOn = am.GetSfxEnabled(); } catch { }
            sfxToggle.onValueChanged.AddListener(am.SetSfxEnabled);
        }
    }

    public void UpdateCoinText(int coin)
    {
        if (txtCoin != null) txtCoin.text = coin.ToString();
    }

    public void UpdateScoreText(int score)
    {
        if (txtScore != null) txtScore.text = score.ToString();
    }

    public void SetPausePanelActive(bool active)
    {
        if (pausePanel != null) pausePanel.SetActive(active);
    }
    
    public void TogglePausePanelLocal()
    {
        if (pausePanel != null) pausePanel.SetActive(!pausePanel.activeSelf);
    }

    public void SetShopPanelActive(bool active)
    {
        if (shopPanel != null) shopPanel.SetActive(active);
    }
}

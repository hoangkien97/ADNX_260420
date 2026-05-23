using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Quản lý chế độ Spectate khi player chết.
/// - Khi local player chết → camera chuyển sang theo dõi player khác
/// - Tab để đổi player đang spectate
/// - Khi tất cả player chết → load GameOver scene
///
/// Tối ưu:
/// - Cache Cinemachine component 1 lần ở Start (không FindObjectsOfType mỗi frame)
/// - RefreshAlivePlayers() chỉ chạy theo timer (0.5s/lần) thay vì mỗi frame
/// </summary>
public class SpectateManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject spectatePanel;
    [SerializeField] private TMP_Text spectateNameText;
    [SerializeField] private TMP_Text spectateHintText;

    [Header("Camera")]
    [SerializeField] private Camera spectateCamera;

    private Player _deadPlayer;
    private List<Player> _alivePlayers = new List<Player>();
    private int _currentSpectateIndex = 0;
    private bool _isSpectating;

    // Cache Cinemachine – chỉ tìm 1 lần ở Start
    private Component _cinemachineVCam;

    // Timer refresh danh sách player còn sống
    private float _refreshInterval = 0.5f;
    private float _nextRefreshTime = 0f;

    // ─────────────────── UNITY LIFECYCLE ─────────────────────

    private void Start()
    {
        CacheCinemachine();
    }

    /// <summary>
    /// Tìm Cinemachine Virtual Camera 1 lần duy nhất thay vì mỗi frame.
    /// </summary>
    private void CacheCinemachine()
    {
        Component[] all = FindObjectsOfType<Component>(true);
        foreach (var comp in all)
        {
            string n = comp.GetType().Name;
            if (n == "CinemachineVirtualCamera" || n == "CinemachineCamera")
            {
                _cinemachineVCam = comp;
                break;
            }
        }
    }

    private void Update()
    {
        if (!_isSpectating) return;

        // Tab để đổi target
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ForceRefreshAndCycle();
        }

        // Refresh danh sách theo timer (không gọi FindObjectsOfType mỗi frame)
        if (Time.time >= _nextRefreshTime)
        {
            RefreshAlivePlayers();
            _nextRefreshTime = Time.time + _refreshInterval;

            if (_alivePlayers.Count == 0)
            {
                TriggerGameOver();
                return;
            }

            // Clamp index phòng trường hợp player đang xem vừa chết
            if (_currentSpectateIndex >= _alivePlayers.Count)
                _currentSpectateIndex = 0;

            UpdateSpectateNameUI();
        }

        // Follow target (chỉ cập nhật transform camera, rất rẻ)
        UpdateSpectateCamera();
    }

    // ─────────────────── PUBLIC API ──────────────────────────

    /// <summary>
    /// Gọi khi local player chết. Bắt đầu spectate.
    /// </summary>
    public void StartSpectating(Player deadPlayer)
    {
        _deadPlayer = deadPlayer;
        _isSpectating = true;

        // Refresh ngay lập tức khi bắt đầu spectate
        RefreshAlivePlayers();
        _nextRefreshTime = Time.time + _refreshInterval;

        if (spectatePanel != null)
            spectatePanel.SetActive(true);

        if (spectateHintText != null)
            spectateHintText.text = "[Tab] để đổi người xem";

        if (_alivePlayers.Count == 0)
        {
            TriggerGameOver();
            return;
        }

        _currentSpectateIndex = 0;
        UpdateSpectateNameUI();
    }

    public void TriggerGameOver()
    {
        _isSpectating = false;

        if (spectatePanel != null)
            spectatePanel.SetActive(false);

        Time.timeScale = 1f;

        if (PurrNet.NetworkManager.main != null)
        {
            if (PurrNet.NetworkManager.main.isServer)
                PurrNet.NetworkManager.main.sceneModule.LoadSceneAsync("GameOver", LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene("GameOver");
        }
    }

    // ─────────────────── PRIVATE ─────────────────────────────

    private void RefreshAlivePlayers()
    {
        _alivePlayers.Clear();
        Player[] allPlayers = FindObjectsOfType<Player>();
        foreach (Player p in allPlayers)
        {
            if (p != _deadPlayer && !p.IsDead && p.gameObject.activeInHierarchy)
                _alivePlayers.Add(p);
        }
    }

    private void ForceRefreshAndCycle()
    {
        RefreshAlivePlayers();
        _nextRefreshTime = Time.time + _refreshInterval;

        if (_alivePlayers.Count == 0)
        {
            TriggerGameOver();
            return;
        }

        _currentSpectateIndex = (_currentSpectateIndex + 1) % _alivePlayers.Count;
        UpdateSpectateNameUI();
    }

    private void UpdateSpectateCamera()
    {
        if (_alivePlayers.Count == 0) return;

        if (_currentSpectateIndex >= _alivePlayers.Count)
            _currentSpectateIndex = 0;

        Player target = _alivePlayers[_currentSpectateIndex];
        if (target == null) return;

        // Dùng Cinemachine đã cache (không FindObjectsOfType mỗi frame)
        if (_cinemachineVCam != null)
        {
            var followProp = _cinemachineVCam.GetType().GetProperty("Follow");
            if (followProp != null) followProp.SetValue(_cinemachineVCam, target.transform);

            var lookAtProp = _cinemachineVCam.GetType().GetProperty("LookAt");
            if (lookAtProp != null) lookAtProp.SetValue(_cinemachineVCam, target.transform);
        }
        else
        {
            // Fallback camera thường
            Camera cam = spectateCamera != null ? spectateCamera : Camera.main;
            if (cam != null)
            {
                Vector3 pos = target.transform.position;
                cam.transform.position = new Vector3(pos.x, pos.y, cam.transform.position.z);
            }
        }
    }

    private void UpdateSpectateNameUI()
    {
        if (_alivePlayers.Count == 0 || spectateNameText == null) return;

        if (_currentSpectateIndex >= _alivePlayers.Count)
            _currentSpectateIndex = 0;

        Player target = _alivePlayers[_currentSpectateIndex];
        if (target != null)
            spectateNameText.text = $"Đang xem: {target.PlayerDisplayName}";
    }
}

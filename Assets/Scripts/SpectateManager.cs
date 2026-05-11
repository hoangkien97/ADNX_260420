using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Quản lý chế độ Spectate khi player chết.
/// - Khi local player chết → camera chuyển sang theo dõi player khác
/// - Tab để đổi player đang spectate
/// - Khi tất cả player chết → load GameOver scene
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

    private void Update()
    {
        if (!_isSpectating) return;

        // Tab để đổi target
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CycleSpectateTarget();
        }

        // Follow target
        UpdateSpectateCamera();
    }

    /// <summary>
    /// Gọi khi local player chết. Bắt đầu spectate.
    /// </summary>
    public void StartSpectating(Player deadPlayer)
    {
        _deadPlayer = deadPlayer;
        _isSpectating = true;

        // Refresh danh sách player còn sống
        RefreshAlivePlayers();

        if (spectatePanel != null)
            spectatePanel.SetActive(true);

        if (spectateHintText != null)
            spectateHintText.text = "[Tab] để đổi người xem";

        if (_alivePlayers.Count == 0)
        {
            // Tất cả đã chết → Game Over
            TriggerGameOver();
            return;
        }

        _currentSpectateIndex = 0;
        UpdateSpectateNameUI();
    }

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

    private void CycleSpectateTarget()
    {
        RefreshAlivePlayers();

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
        RefreshAlivePlayers();

        if (_alivePlayers.Count == 0)
        {
            TriggerGameOver();
            return;
        }

        // Clamp index
        if (_currentSpectateIndex >= _alivePlayers.Count)
            _currentSpectateIndex = 0;

        Player target = _alivePlayers[_currentSpectateIndex];

        // 1. Thử hỗ trợ Cinemachine trước
        bool cinemachineUpdated = false;
        Component[] allComponents = FindObjectsOfType<Component>(true);
        foreach (var comp in allComponents)
        {
            string compName = comp.GetType().Name;
            if (compName == "CinemachineVirtualCamera" || compName == "CinemachineCamera")
            {
                var followProp = comp.GetType().GetProperty("Follow");
                if (followProp != null) 
                {
                    followProp.SetValue(comp, target.transform);
                    cinemachineUpdated = true;
                }
                
                var lookAtProp = comp.GetType().GetProperty("LookAt");
                if (lookAtProp != null) lookAtProp.SetValue(comp, target.transform);
            }
        }

        // 2. Fallback camera thường nếu không có Cinemachine
        if (!cinemachineUpdated)
        {
            Camera cam = spectateCamera != null ? spectateCamera : Camera.main;
            if (cam != null && target != null)
            {
                Vector3 targetPos = target.transform.position;
                cam.transform.position = new Vector3(targetPos.x, targetPos.y, cam.transform.position.z);
            }
        }
    }

    private void UpdateSpectateNameUI()
    {
        if (_alivePlayers.Count == 0) return;

        Player target = _alivePlayers[_currentSpectateIndex];
        if (spectateNameText != null && target != null)
            spectateNameText.text = $"Đang xem: {target.PlayerDisplayName}";
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
                PurrNet.NetworkManager.main.sceneModule.LoadSceneAsync("GameOver", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene("GameOver");
        }
    }
}

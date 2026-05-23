using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance { get; private set; }

    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private TextMeshProUGUI chatDisplay;
    [SerializeField] private ScrollRect scrollRect;
    
    [Header("Settings")]
    [SerializeField] private int maxMessages = 20;

    private List<string> messageList = new List<string>();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (chatInput != null)
        {
            chatInput.onSubmit.AddListener(OnSubmitChat);
        }
    }

    private void Update()
    {
        // Bấm Enter để bắt đầu chat (nếu chưa focus vào ô nhập)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (chatInput != null && !chatInput.isFocused)
            {
                chatInput.ActivateInputField();
            }
        }
    }

    private void OnSubmitChat(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // Tìm player local để mượn đường gửi RPC lên mạng
        Player localPlayer = GetLocalPlayer();
        if (localPlayer != null)
        {
            if (localPlayer.isSpawned)
            {
                // Gửi qua PurrNet
                localPlayer.CmdSendChat(text.Trim());
            }
            else
            {
                // Offline fallback
                AddMessage(localPlayer.PlayerDisplayName, text.Trim());
            }
        }

        // Xóa trắng input (không giữ focus nữa để người chơi tiếp tục dùng phím WASD di chuyển)
        if (chatInput != null)
        {
            chatInput.text = "";
            chatInput.DeactivateInputField();
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void AddMessage(string sender, string message)
    {
        if (messageList.Count >= maxMessages)
        {
            messageList.RemoveAt(0);
        }

        messageList.Add($"<b>{sender}</b>: {message}");
        UpdateChatDisplay();
    }

    private void UpdateChatDisplay()
    {
        if (chatDisplay != null)
        {
            chatDisplay.text = string.Join("\n", messageList);
        }

        // Tự động cuộn xuống cuối
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private Player GetLocalPlayer()
    {
        Player[] players = FindObjectsByType<Player>(FindObjectsInactive.Exclude);
        foreach (var p in players)
        {
            if (!p.isSpawned || p.isOwner) return p;
        }
        return null;
    }
}

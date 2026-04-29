using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoginSceneManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    
    private bool busy;

    private void Start()
    {
        ApiManager.EnsureInstance().Logout();
        
        if (loginButton != null)
            loginButton.onClick.AddListener(SubmitLogin);
            
        if (registerButton != null)
            registerButton.onClick.AddListener(SubmitRegister);
    }

    private void SubmitLogin()
    {
        SubmitAuth(false);
    }

    private void SubmitRegister()
    {
        SubmitAuth(true);
    }

    private void SubmitAuth(bool register)
    {
        if (busy)
        {
            return;
        }

        string username = usernameInput != null ? usernameInput.text.Trim() : "";
        string password = passwordInput != null ? passwordInput.text : "";
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            SetMessage("Username and Password must not be left blank.");
            return;
        }

        busy = true;
        SetInteractable(false);
        SetMessage(register ? "Registering..." : "Signing in...");

        ApiManager api = ApiManager.EnsureInstance();
        System.Action<bool, string> callback = (success, message) =>
        {
            busy = false;
            SetInteractable(true);

            if (!success)
            {
                SetMessage(message);
                return;
            }

            if (register)
            {
                api.Logout();
                if (passwordInput != null)
                {
                    passwordInput.text = "";
                }

                SetMessage(message + ". Please log in to begin.");
                return;
            }

            SceneManager.LoadScene("GameStart");
        };

        if (register)
        {
            api.Register(username, password, callback);
        }
        else
        {
            api.Login(username, password, callback);
        }
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message ?? "";
        }
    }

    private void SetInteractable(bool interactable)
    {
        if (usernameInput != null) usernameInput.interactable = interactable;
        if (passwordInput != null) passwordInput.interactable = interactable;
        if (loginButton != null) loginButton.interactable = interactable;
        if (registerButton != null) registerButton.interactable = interactable;
    }
}

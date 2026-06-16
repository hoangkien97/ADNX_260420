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
    [SerializeField] private Button guestButton;
    
    private bool busy;

    private void Start()
    {
        //IAuthService auth = ServiceLocator.Get<IAuthService>();
        //if (auth.IsLoggedIn)
        //{
        //    SceneManager.LoadScene("GameStart");
        //    return;
        //}

        //auth.Logout();
        
        if (loginButton != null)
            loginButton.onClick.AddListener(SubmitLogin);
            
        if (registerButton != null)
            registerButton.onClick.AddListener(SubmitRegister);

        if (guestButton != null)
            guestButton.onClick.AddListener(LoginGuest);
    }

    private void SubmitLogin()
    {
        SubmitAuth(false);
    }

    private void SubmitRegister()
    {
        SubmitAuth(true);
    }

    public void LoginGuest()
    {
        if (busy) return;
       
        ServiceLocator.Get<IAuthService>().Logout();
        SceneManager.LoadScene("GameStart");
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

        IAuthService auth = ServiceLocator.Get<IAuthService>();
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
                auth.Logout();
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
            auth.Register(username, password, callback);
        }
        else
        {
            auth.Login(username, password, callback);
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
        if (guestButton != null) guestButton.interactable = interactable;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginSceneManager : MonoBehaviour
{
    private InputField usernameInput;
    private InputField passwordInput;
    private Text messageText;
    private Button loginButton;
    private Button registerButton;
    private bool busy;

    private void Start()
    {
        ApiManager.EnsureInstance().Logout();
        BuildUi();
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
            SetMessage("Username và Password không được để trống");
            return;
        }

        busy = true;
        SetInteractable(false);
        SetMessage(register ? "Đang đăng ký..." : "Đang đăng nhập...");

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

                SetMessage(message + ". Hãy đăng nhập để bắt đầu");
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

    private void BuildUi()
    {
        EnsureEventSystem();

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        ClearCanvas(canvas.transform);
        Font font = GetDefaultFont();

        GameObject panel = new GameObject("LoginPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(440f, 360f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.82f);

        CreateText(panel.transform, "Title", "-- Account --", font, 30, new Vector2(0f, 130f), new Vector2(360f, 46f));
        usernameInput = CreateInput(panel.transform, "UsernameInput", "Username", font, new Vector2(0f, 62f));
        passwordInput = CreateInput(panel.transform, "PasswordInput", "Password", font, new Vector2(0f, 4f));
        passwordInput.contentType = InputField.ContentType.Password;
        passwordInput.ForceLabelUpdate();

        loginButton = CreateButton(panel.transform, "LoginButton", "Đăng nhập", font, new Vector2(-96f, -62f));
        loginButton.onClick.AddListener(SubmitLogin);

        registerButton = CreateButton(panel.transform, "RegisterButton", "Đăng ký", font, new Vector2(96f, -62f));
        registerButton.onClick.AddListener(SubmitRegister);

        messageText = CreateText(panel.transform, "Message", "Đăng nhập hoặc đăng ký để bắt đầu", font, 18, new Vector2(0f, -124f), new Vector2(380f, 54f));
        messageText.color = new Color(1f, 0.9f, 0.55f, 1f);
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void ClearCanvas(Transform canvasTransform)
    {
        for (int i = canvasTransform.childCount - 1; i >= 0; i--)
        {
            Destroy(canvasTransform.GetChild(i).gameObject);
        }
    }

    private InputField CreateInput(Transform parent, string name, string placeholder, Font font, Vector2 position)
    {
        GameObject inputObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
        inputObject.transform.SetParent(parent, false);
        RectTransform rect = inputObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(310f, 42f);

        Image image = inputObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.92f);

        Text text = CreateText(inputObject.transform, "Text", "", font, 18, new Vector2(8f, 0f), new Vector2(278f, 34f), TextAnchor.MiddleLeft);
        text.color = Color.black;

        Text placeholderText = CreateText(inputObject.transform, "Placeholder", placeholder, font, 18, new Vector2(8f, 0f), new Vector2(278f, 34f), TextAnchor.MiddleLeft);
        placeholderText.color = new Color(0.35f, 0.35f, 0.35f, 0.75f);

        InputField input = inputObject.GetComponent<InputField>();
        input.textComponent = text;
        input.placeholder = placeholderText;
        input.targetGraphic = image;
        return input;
    }

    private Button CreateButton(Transform parent, string name, string label, Font font, Vector2 position)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(150f, 44f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.95f, 0.82f, 0.25f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        Text text = CreateText(buttonObject.transform, "Text", label, font, 18, Vector2.zero, new Vector2(140f, 36f));
        text.color = Color.black;
        return button;
    }

    private Text CreateText(Transform parent, string name, string text, Font font, int fontSize, Vector2 position, Vector2 size, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text uiText = textObject.GetComponent<Text>();
        uiText.font = font;
        uiText.text = text;
        uiText.fontSize = fontSize;
        uiText.alignment = alignment;
        uiText.color = Color.white;
        uiText.resizeTextForBestFit = true;
        uiText.resizeTextMinSize = 12;
        uiText.resizeTextMaxSize = fontSize;
        return uiText;
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

    private Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return font;
    }
}

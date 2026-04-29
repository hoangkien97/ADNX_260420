using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiManager : MonoBehaviour
{
    public static ApiManager Instance;

    private const string PLAYER_ID_KEY = "Api_PlayerId";
    private const string USERNAME_KEY = "Api_Username";

    [SerializeField] private string baseUrl = "https://localhost:7277/api";

    public static int CurrentPlayerId { get; private set; }
    public static string CurrentUsername { get; private set; }
    public static bool IsLoggedIn => CurrentPlayerId > 0;

    public static ApiManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        ApiManager existing = FindAnyObjectByType<ApiManager>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("ApiManager");
        return go.AddComponent<ApiManager>();
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ClearStoredSession();
    }

    public void Login(string username, string password,
                      System.Action<bool, string> callback)
    {
        username = (username ?? "").Trim();
        if (password == null) password = "";
        StartCoroutine(PostRequest("/auth/login",
            JsonUtility.ToJson(new LoginRequest { username = username, password = password }),
            (ok, json) =>
            {
                LoginResponse res = ParseLoginResponse(json, ok ? "Log in successfully" : "Login error");
                if (ok && res.success)
                {
                    SetSession(res.playerId, username);
                }
                callback?.Invoke(ok && res.success, res.message);
            }));
    }

    public void Register(string username, string password,
                         System.Action<bool, string> callback)
    {
        username = (username ?? "").Trim();
        if (password == null) password = "";
        StartCoroutine(PostRequest("/auth/register",
            JsonUtility.ToJson(new LoginRequest { username = username, password = password }),
            (ok, json) =>
            {
                LoginResponse res = ParseLoginResponse(json, ok ? "Registered successfully" : "Registration error");
                callback?.Invoke(ok && res.success, res.message);
            }));
    }

    public void PostScore(int score, int wave,
                          System.Action<bool> callback = null)
    {
        if (!IsLoggedIn)
        {
            callback?.Invoke(false);
            return;
        }

        var body = JsonUtility.ToJson(new PostScoreRequest
        {
            playerId = CurrentPlayerId,
            score = score,
            wave = wave
        });

        StartCoroutine(PostRequest("/score",
            body, (ok, _) => callback?.Invoke(ok)));
    }

    public void GetLeaderboard(System.Action<LeaderboardEntry[]> callback)
    {
        StartCoroutine(GetRequest("/score/leaderboard", (ok, json) =>
        {
            if (!ok)
            {
                Debug.LogError("Get leaderboard failed: " + json);
                callback(null);
                return;
            }

            try
            {
                LeaderboardWrapper wrapper = JsonUtility.FromJson<LeaderboardWrapper>(json);
                callback(wrapper.data ?? new LeaderboardEntry[0]);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Parse leaderboard failed: " + ex.Message + "\nResponse: " + json);
                callback(null);
            }
        }));
    }

    public void Logout()
    {
        ClearStoredSession();
    }

    IEnumerator PostRequest(string endpoint, string json,
                            System.Action<bool, string> callback)
    {
        using (UnityWebRequest req = new UnityWebRequest(BuildUrl(endpoint), "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            AttachCertificateHandler(req);
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            bool ok = req.result == UnityWebRequest.Result.Success;
            callback(ok, ResponseText(req));
        }
    }

    IEnumerator GetRequest(string endpoint,
                           System.Action<bool, string> callback)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(BuildUrl(endpoint)))
        {
            AttachCertificateHandler(req);
            yield return req.SendWebRequest();

            bool ok = req.result == UnityWebRequest.Result.Success;
            callback(ok, ResponseText(req));
        }
    }

    private static LoginResponse ParseLoginResponse(string json, string fallbackMessage)
    {
        try
        {
            LoginResponse response = JsonUtility.FromJson<LoginResponse>(json);
            if (response != null && !string.IsNullOrWhiteSpace(response.message))
            {
                return response;
            }
        }
        catch
        {
        }

        return new LoginResponse { success = false, playerId = 0, message = fallbackMessage };
    }

    private void SetSession(int playerId, string username)
    {
        CurrentPlayerId = playerId;
        CurrentUsername = username;
    }

    private static void ClearStoredSession()
    {
        CurrentPlayerId = 0;
        CurrentUsername = "";
        PlayerPrefs.DeleteKey(PLAYER_ID_KEY);
        PlayerPrefs.DeleteKey(USERNAME_KEY);
        PlayerPrefs.Save();
    }

    private string BuildUrl(string endpoint)
    {
        return baseUrl.TrimEnd('/') + "/" + endpoint.TrimStart('/');
    }

    private static string ResponseText(UnityWebRequest req)
    {
        string text = "";
        if (req.downloadHandler != null && !string.IsNullOrWhiteSpace(req.downloadHandler.text))
        {
            text = req.downloadHandler.text;
        }
        else if (!string.IsNullOrWhiteSpace(req.error))
        {
            text = req.error;
        }

        return text;
    }

    private static void AttachCertificateHandler(UnityWebRequest req)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        req.certificateHandler = new LocalhostCertificateHandler();
#endif
    }
}

[System.Serializable] public class LoginRequest { public string username, password; }
[System.Serializable] public class LoginResponse { public bool success; public int playerId; public string message; }
[System.Serializable] public class PostScoreRequest { public int playerId, score, wave; }
[System.Serializable]
public class LeaderboardEntry
{
    public string username;
    public int bestScore, bestWave;

    public string GetUsername() => string.IsNullOrWhiteSpace(username) ? "Unknown" : username;
    public int GetBestScore() => bestScore;
    public int GetBestWave() => bestWave;
}
[System.Serializable] public class LeaderboardWrapper { public LeaderboardEntry[] data; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
public class LocalhostCertificateHandler : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}
#endif


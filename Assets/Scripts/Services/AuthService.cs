using System;
using UnityEngine;

public class AuthService : IAuthService
{
    public int CurrentPlayerId { get; private set; }
    public string CurrentUsername { get; private set; }
    public bool IsLoggedIn => CurrentPlayerId > 0;

    public void Login(string username, string password, Action<bool, string> callback)
    {
        username = (username ?? "").Trim();
        if (password == null) password = "";

        string json = JsonUtility.ToJson(new LoginRequest { username = username, password = password });
        ServiceLocator.Get<INetworkService>().PostRequest("/auth/login", json, (ok, responseJson) =>
        {
            LoginResponse res = ParseLoginResponse(responseJson, ok ? "Log in successfully" : "Login error");
            if (ok && res.success)
            {
                CurrentPlayerId = res.playerId;
                CurrentUsername = username;
            }
            callback?.Invoke(ok && res.success, res.message);
        });
    }

    public void Register(string username, string password, Action<bool, string> callback)
    {
        username = (username ?? "").Trim();
        if (password == null) password = "";

        string json = JsonUtility.ToJson(new LoginRequest { username = username, password = password });
        ServiceLocator.Get<INetworkService>().PostRequest("/auth/register", json, (ok, responseJson) =>
        {
            LoginResponse res = ParseLoginResponse(responseJson, ok ? "Registered successfully" : "Registration error");
            callback?.Invoke(ok && res.success, res.message);
        });
    }

    public void Logout()
    {
        CurrentPlayerId = 0;
        CurrentUsername = "";
    }

    private LoginResponse ParseLoginResponse(string json, string fallbackMessage)
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
}

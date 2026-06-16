using System;

public interface IAuthService
{
    int CurrentPlayerId { get; }
    string CurrentUsername { get; }
    bool IsLoggedIn { get; }
    
    void Login(string username, string password, Action<bool, string> callback);
    void Register(string username, string password, Action<bool, string> callback);
    void Logout();
}

[System.Serializable] 
public class LoginRequest 
{ 
    public string username, password; 
}

[System.Serializable] 
public class LoginResponse 
{ 
    public bool success; 
    public int playerId; 
    public string message; 
}

[System.Serializable] 
public class PostScoreRequest 
{ 
    public int playerId, score, wave; 
}

[System.Serializable]
public class LeaderboardEntry
{
    public string username;
    public int bestScore, bestWave;

    public string GetUsername() => string.IsNullOrWhiteSpace(username) ? "Unknown" : username;
    public int GetBestScore() => bestScore;
    public int GetBestWave() => bestWave;
}

[System.Serializable] 
public class LeaderboardWrapper 
{ 
    public LeaderboardEntry[] data; 
}

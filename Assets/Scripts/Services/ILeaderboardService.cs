using System;

public interface ILeaderboardService
{
    void PostScore(int score, int wave, Action<bool> callback = null);
    void GetLeaderboard(Action<LeaderboardEntry[]> callback);
}

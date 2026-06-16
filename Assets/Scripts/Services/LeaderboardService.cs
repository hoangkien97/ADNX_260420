using System;
using UnityEngine;

public class LeaderboardService : ILeaderboardService
{
    public void PostScore(int score, int wave, Action<bool> callback = null)
    {
        var auth = ServiceLocator.Get<IAuthService>();
        if (!auth.IsLoggedIn)
        {
            callback?.Invoke(false);
            return;
        }

        var body = JsonUtility.ToJson(new PostScoreRequest
        {
            playerId = auth.CurrentPlayerId,
            score = score,
            wave = wave
        });

        ServiceLocator.Get<INetworkService>().PostRequest("/score", body, (ok, _) => callback?.Invoke(ok));
    }

    public void GetLeaderboard(Action<LeaderboardEntry[]> callback)
    {
        ServiceLocator.Get<INetworkService>().GetRequest("/score/leaderboard", (ok, json) =>
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
            catch (Exception ex)
            {
                Debug.LogError("Parse leaderboard failed: " + ex.Message + "\nResponse: " + json);
                callback(null);
            }
        });
    }
}

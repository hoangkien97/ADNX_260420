using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RankingManager : MonoBehaviour
{
    private const int MAX_ENTRIES = 5;
    private const string KEY_PREFIX = "Ranking_Score_";
    private const string KEY_COUNT = "Ranking_Count";
    private bool _isNewRecord = false; 
    [ContextMenu("Clear Ranking")]
    public void ClearRankingEditor()
    {
        ClearRanking();
        RefreshUI();
        Debug.Log("Ranking đã được xóa!");
    }

    [Header("UI – Ranking Entries (top 1 → top 5)")]
    [SerializeField] private Text[] rankingTexts;      

    [Header("UI – Score vừa đạt được")]
    [SerializeField] private Text currentScoreText;

    [Header("UI – Thông báo kỷ lục mới")]
    [SerializeField] private GameObject newRecordPanel; 
    public static int SaveScore(int newScore, out bool isNewRecord)
    {
        List<int> scores = LoadAllScores();
        int previousBest = scores.Count > 0 ? scores[0] : -1;

        scores.Add(newScore);
        scores.Sort((a, b) => b.CompareTo(a));

        if (scores.Count > MAX_ENTRIES)
            scores.RemoveRange(MAX_ENTRIES, scores.Count - MAX_ENTRIES);

        PlayerPrefs.SetInt(KEY_COUNT, scores.Count);
        for (int i = 0; i < scores.Count; i++)
            PlayerPrefs.SetInt(KEY_PREFIX + i, scores[i]);
        PlayerPrefs.Save();
        isNewRecord = previousBest >= 0 && newScore > previousBest;

        return scores.IndexOf(newScore) + 1;
    }
    public static void ClearRanking()
    {
        int count = PlayerPrefs.GetInt(KEY_COUNT, 0);
        for (int i = 0; i < count; i++)
            PlayerPrefs.DeleteKey(KEY_PREFIX + i);
        PlayerPrefs.DeleteKey(KEY_COUNT);
        PlayerPrefs.Save();
    }

    public static List<int> LoadAllScores()
    {
        int count = PlayerPrefs.GetInt(KEY_COUNT, 0);
        List<int> scores = new List<int>(count);
        for (int i = 0; i < count; i++)
            scores.Add(PlayerPrefs.GetInt(KEY_PREFIX + i, 0));
        return scores;
    }


    private void OnEnable() => RefreshUI();


    public void RefreshUI(bool isNewRecord = false)
    {
        _isNewRecord = isNewRecord;
        List<int> scores = LoadAllScores();

        if (currentScoreText != null)
            currentScoreText.text = "Score: " + GameManager.Score.ToString();

        if (rankingTexts != null)
        {
            for (int i = 0; i < rankingTexts.Length; i++)
            {
                if (rankingTexts[i] == null) continue;
                rankingTexts[i].text = i < scores.Count
                    ? string.Format("#{0}   {1}", i + 1, scores[i])
                    : string.Format("#{0}   ---", i + 1);
            }
        }
        if (newRecordPanel != null)
            newRecordPanel.SetActive(_isNewRecord);
    }
}

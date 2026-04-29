using UnityEngine;
using UnityEngine.UI;

public class RankingManager : MonoBehaviour
{
    private static readonly Color CurrentRunColor = Color.red;

    [Header("UI - Ranking Entries (top 1 -> top 5)")]
    [SerializeField] private Text[] rankingTexts;

    [Header("UI - Score vua dat duoc")]
    [SerializeField] private Text currentScoreText;

    [Header("UI - Thong bao ky luc moi")]
    [SerializeField] private GameObject newRecordPanel;

    private Color[] rankingDefaultColors;

    private void Awake()
    {
        CaptureDefaultColors();
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI(bool isNewRecord = false)
    {
        EnsureDefaultColors();

        if (currentScoreText != null)
        {
            currentScoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
            currentScoreText.verticalOverflow = VerticalWrapMode.Overflow;
            currentScoreText.text = "Score: " + GameManager.Score + "   Wave: " + GameManager.Wave;
        }

        if (newRecordPanel != null)
        {
            newRecordPanel.SetActive(isNewRecord);
        }

        SetLoadingRows();
        ApiManager.EnsureInstance().GetLeaderboard(UpdateLeaderboardFromApi);
    }

    private void SetLoadingRows()
    {
        if (rankingTexts == null)
        {
            return;
        }

        for (int i = 0; i < rankingTexts.Length; i++)
        {
            if (rankingTexts[i] == null)
            {
                continue;
            }

            SetRankingColor(i, GetDefaultColor(i));
            rankingTexts[i].text = i < 5
                ? string.Format("#{0}   Loading...", i + 1)
                : "";
        }
    }

    private void UpdateLeaderboardFromApi(LeaderboardEntry[] entries)
    {
        if (this == null || rankingTexts == null)
        {
            return;
        }

        if (entries == null)
        {
            SetErrorRows();
            return;
        }

        int maxRows = Mathf.Min(5, rankingTexts.Length);
        for (int i = 0; i < maxRows; i++)
        {
            if (rankingTexts[i] == null)
            {
                continue;
            }

            if (i < entries.Length)
            {
                LeaderboardEntry entry = entries[i];
                SetRankingColor(i, IsCurrentRunEntry(entry) ? CurrentRunColor : GetDefaultColor(i));
                rankingTexts[i].text = string.Format("#{0}   {1}   {2}   W{3}",
                    i + 1,
                    entry.GetUsername(),
                    entry.GetBestScore(),
                    entry.GetBestWave());
            }
            else
            {
                SetRankingColor(i, GetDefaultColor(i));
                rankingTexts[i].text = string.Format("#{0}   ---", i + 1);
            }
        }

        for (int i = maxRows; i < rankingTexts.Length; i++)
        {
            if (rankingTexts[i] != null)
            {
                SetRankingColor(i, GetDefaultColor(i));
                rankingTexts[i].text = "";
            }
        }
    }

    private void SetErrorRows()
    {
        if (rankingTexts == null)
        {
            return;
        }

        for (int i = 0; i < rankingTexts.Length; i++)
        {
            if (rankingTexts[i] == null)
            {
                continue;
            }

            SetRankingColor(i, GetDefaultColor(i));
            rankingTexts[i].text = i == 0 ? "Khong tai duoc leaderboard tu Database" : "";
        }
    }

    private bool IsCurrentRunEntry(LeaderboardEntry entry)
    {
        if (entry == null || entry.GetBestScore() != GameManager.Score)
        {
            return false;
        }

        int entryWave = entry.GetBestWave();
        if (entryWave > 0 && entryWave != GameManager.Wave)
        {
            return false;
        }

        string currentUsername = ApiManager.CurrentUsername;
        return string.IsNullOrWhiteSpace(currentUsername)
            || string.Equals(entry.GetUsername(), currentUsername, System.StringComparison.OrdinalIgnoreCase);
    }

    private void CaptureDefaultColors()
    {
        if (rankingTexts == null)
        {
            rankingDefaultColors = null;
            return;
        }

        rankingDefaultColors = new Color[rankingTexts.Length];
        for (int i = 0; i < rankingTexts.Length; i++)
        {
            rankingDefaultColors[i] = rankingTexts[i] != null ? rankingTexts[i].color : Color.white;
        }
    }

    private void EnsureDefaultColors()
    {
        if (rankingTexts == null)
        {
            return;
        }

        if (rankingDefaultColors == null || rankingDefaultColors.Length != rankingTexts.Length)
        {
            CaptureDefaultColors();
        }
    }

    private Color GetDefaultColor(int index)
    {
        EnsureDefaultColors();
        if (rankingDefaultColors == null || index < 0 || index >= rankingDefaultColors.Length)
        {
            return Color.white;
        }

        return rankingDefaultColors[index];
    }

    private void SetRankingColor(int index, Color color)
    {
        if (rankingTexts != null && index >= 0 && index < rankingTexts.Length && rankingTexts[index] != null)
        {
            rankingTexts[index].color = color;
        }
    }
}

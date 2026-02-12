using UnityEngine;
using TMPro;

public class LeaderboardEntryUI : MonoBehaviour
{
    public TMP_Text rankText;
    public TMP_Text nameText;
    public TMP_Text scoreText;

    public void Set(int rank, string name, int score)
    {
        rankText.text = "#" + rank.ToString();
        nameText.text = name;
        scoreText.text = score.ToString();
    }
}

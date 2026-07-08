using TMPro;
using UnityEngine;
using Unity.Netcode;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI bestScoreText;

    private NetworkVariable<int> score =
        new NetworkVariable<int>(0);

    private int bestScore;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        bestScore = PlayerPrefs.GetInt("BestScore", 0);

        score.OnValueChanged += ScoreChanged;

        AtualizarUI();
    }

    void ScoreChanged(int antigo, int novo)
    {
        AtualizarUI();
    }

    public int GetScore()
    {
        return score.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddScoreServerRpc(int value)
    {
        score.Value += value;

        if (score.Value > bestScore)
        {
            bestScore = score.Value;

            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetScoreServerRpc()
    {
        score.Value = 0;
    }

    void AtualizarUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score.Value;

        if (bestScoreText != null)
            bestScoreText.text = "Recorde: " + bestScore;
    }
}
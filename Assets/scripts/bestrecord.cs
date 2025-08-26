using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI currentScoreText;
    [SerializeField] private TextMeshProUGUI bestScoreText;
    [SerializeField] private TextMeshProUGUI gameOverBestScoreText;
    [SerializeField] private GameObject newRecordEffect;

    [Header("Settings")]
    [SerializeField] private string scorePrefix = "Maçãs: ";
    [SerializeField] private string bestScorePrefix = "Recorde: ";

    private int currentScore = 0;
    private int bestScore = 0;
    private bool newRecordAchieved = false;

    private const string BEST_SCORE_KEY = "BestSnakeScore";

    private void Start()
    {
        LoadBestScore();
        UpdateScoreUI();
    }

    public void AddScore(int points)
    {
        currentScore += points;
        CheckForNewRecord();
        UpdateScoreUI();
    }

    public void ResetScore()
    {
        currentScore = 0;
        newRecordAchieved = false;
        UpdateScoreUI();
    }

    private void CheckForNewRecord()
    {
        if (currentScore > bestScore)
        {
            bestScore = currentScore;
            newRecordAchieved = true;
            SaveBestScore();

            // Efeito visual de novo recorde
            if (newRecordEffect != null)
            {
                Instantiate(newRecordEffect, transform.position, Quaternion.identity);
            }

            // Toque um som de recorde aqui se desejar
        }
    }

    private void UpdateScoreUI()
    {
        if (currentScoreText != null)
        {
            currentScoreText.text = scorePrefix + currentScore.ToString();
        }

        if (bestScoreText != null)
        {
            bestScoreText.text = bestScorePrefix + bestScore.ToString();
        }

        if (gameOverBestScoreText != null)
        {
            string recordText = newRecordAchieved ? "NOVO RECORDE!" : bestScorePrefix + bestScore.ToString();
            gameOverBestScoreText.text = recordText;
        }
    }

    private void LoadBestScore()
    {
        bestScore = PlayerPrefs.GetInt(BEST_SCORE_KEY, 0);
    }

    private void SaveBestScore()
    {
        PlayerPrefs.SetInt(BEST_SCORE_KEY, bestScore);
        PlayerPrefs.Save();
    }

    // Método para resetar o recorde (opcional, para debug)
    public void ResetBestScore()
    {
        PlayerPrefs.DeleteKey(BEST_SCORE_KEY);
        bestScore = 0;
        newRecordAchieved = false;
        UpdateScoreUI();
    }
}
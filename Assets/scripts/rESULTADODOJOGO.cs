using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultadoJogoUI : MonoBehaviour
{
    public static ResultadoJogoUI Instance;

    [Header("Telas")]
    public GameObject gameOverPanel;
    public GameObject youWinPanel;

    private void Awake()
    {
        Instance = this;

        // Começam escondidas
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (youWinPanel != null)
            youWinPanel.SetActive(false);
    }

    // =========================================
    // MOSTRAR GAME OVER
    // =========================================

    public void MostrarGameOver()
    {
        Debug.Log("MOSTRANDO GAME OVER");

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (youWinPanel != null)
            youWinPanel.SetActive(false);
    }

    // =========================================
    // MOSTRAR YOU WIN
    // =========================================

    public void MostrarYouWin()
    {
        Debug.Log("MOSTRANDO YOU WIN");

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (youWinPanel != null)
            youWinPanel.SetActive(true);
    }

    // =========================================
    // BOTÃO VOLTAR AO MENU
    // =========================================

    public void VoltarAoMenu()
    {
        Debug.Log("Voltando ao menu...");

        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}
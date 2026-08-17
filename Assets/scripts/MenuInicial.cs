using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuInicial : MonoBehaviour
{
    [Header("SanakeGame")]
    public string nomeCenaJogo = "SnakeGame";

    public void Jogar()
    {
        SceneManager.LoadScene(nomeCenaJogo);
    }

    public void Sair()
    {
        Application.Quit();
    }
}
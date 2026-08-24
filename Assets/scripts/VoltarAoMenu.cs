using UnityEngine;
using UnityEngine.SceneManagement;

public class VoltarAoMenu : MonoBehaviour
{
    public static VoltarAoMenu Instance;

    [Header("Nome da cena do Menu")]
    public string nomeCenaMenu = "Menu";

    private void Awake()
    {
        Instance = this;
    }

    public void VoltarMenu()
    {
        Debug.Log("Voltando para o Menu...");

        SceneManager.LoadScene(nomeCenaMenu);
    }
}
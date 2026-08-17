using UnityEngine;
using UnityEngine.SceneManagement;

public class VoltarAoMenu : MonoBehaviour
{
    public static VoltarAoMenu Instance;

    [Header("Cena inicial")]
    public string nomeCenaMenu = "Menu";

    private bool voltando = false;

    private void Awake()
    {
        Instance = this;
    }

    public void VoltarMenu()
    {
        if (voltando)
            return;

        voltando = true;

        SceneManager.LoadScene(nomeCenaMenu);
    }
}
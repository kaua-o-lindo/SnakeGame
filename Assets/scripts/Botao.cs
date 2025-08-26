using UnityEngine;
using UnityEngine.SceneManagement;

public class BotaoRestart : MonoBehaviour
{
    [Header("Configurações de Áudio")]
    public AudioClip somClique; // Arraste seu arquivo de áudio para cá

    private AudioSource audioSource;

    void Start()
    {
        // Obtém ou adiciona um AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && somClique != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void ReiniciarJogo()
    {
        // Toca o som se configurado
        if (somClique != null && audioSource != null)
        {
            audioSource.PlayOneShot(somClique);
        }

        // Recarrega a cena
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
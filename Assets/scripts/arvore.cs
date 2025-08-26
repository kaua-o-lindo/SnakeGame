using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class TreeKill_GameOver : MonoBehaviour
{
    [Header("Detecção")]
    [Tooltip("Tag do objeto que morre ao encostar.")]
    public string playerTag = "Player";

    [Header("Feedback")]
    [Tooltip("Prefab de explosão/partículas ao matar o jogador (opcional).")]
    public GameObject explosionPrefab;
    [Tooltip("Volume do som de explosão (opcional).")]
    public AudioClip explosionSound;
    [Range(0f, 1f)] public float explosionSoundVolume = 1f;

    [Header("Game Over UI")]
    [Tooltip("Painel de Game Over (UI) a ser ativado quando o jogador morrer.")]
    public GameObject gameOverPanel; // arrastar Canvas panel
    [Tooltip("Botão de reiniciar (opcional). Se deixado, ligue o botão ao método RestartScene ou chame programaticamente).")]
    public Button restartButton;
    [Tooltip("Texto do painel (opcional) — exemplo: 'Game Over' ou instruções.")]
    public Text gameOverText;

    [Header("Reinício")]
    [Tooltip("Se true, reinicia automaticamente depois de restartDelay segundos.")]
    public bool autoRestart = true;
    [Tooltip("Delay antes do reinício automático (segundos).")]
    public float restartDelay = 2.5f;

    [Tooltip("Se true, destrói o GameObject do player. Se false, só desativa componentes/colisor para manter o objeto (útil para respawn).")]
    public bool destroyPlayer = true;

    bool gameOverShown = false;

    private void Reset()
    {
        // Ajuda no editor: desliga o painel por padrão
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartScene);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (gameOverShown) return;
        if (other.CompareTag(playerTag))
        {
            HandlePlayerDeath(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (gameOverShown) return;
        if (collision.collider.CompareTag(playerTag))
        {
            HandlePlayerDeath(collision.gameObject);
        }
    }

    void HandlePlayerDeath(GameObject player)
    {
        gameOverShown = true;

        // spawn explosão no local do player
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, player.transform.position, Quaternion.identity);
        }

        // som de explosão
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, player.transform.position, explosionSoundVolume);
        }

        // destruir ou desativar movimento/comportamentos do player
        if (destroyPlayer)
        {
            Destroy(player);
        }
        else
        {
            // tenta desabilitar componentes comuns para "paralisar" o player
            // desliga todos os MonoBehaviours no player (exceto este, caso esteja nele)
            var mbs = player.GetComponents<MonoBehaviour>();
            foreach (var mb in mbs)
            {
                // evita desativar scripts que sejam essenciais ao Game Over UI (se houver)
                // aqui suprimimos tudo - ajuste se necessário
                mb.enabled = false;
            }

            // desativa coliders e physics para evitar empurrões
            var colliders = player.GetComponentsInChildren<Collider>();
            foreach (var c in colliders) c.enabled = false;

            var rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }

        // mostra painel de Game Over
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // texto opcional
        if (gameOverText != null)
            gameOverText.text = "Game Over";

        // reinicia automaticamente se configurado
        if (autoRestart)
        {
            StartCoroutine(RestartAfterDelay(restartDelay));
        }
    }

    IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RestartScene();
    }

    public void RestartScene()
    {
        // recarrega a cena atual
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // opcional: chamada para menu principal
    public void QuitToMainMenu(string menuSceneName)
    {
        SceneManager.LoadScene(menuSceneName);
    }
}

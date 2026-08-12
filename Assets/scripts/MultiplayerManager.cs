using System.Collections;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class MultiplayerManager : NetworkBehaviour
{
    public static MultiplayerManager Instance;

    [Header("UI")]
    public GameObject waitingPanel;
    public TextMeshProUGUI playersText;
    public TextMeshProUGUI countdownText;

    [Header("Spawn Points")]
    public Transform player1Spawn;
    public Transform player2Spawn;

    [Header("Lobby Camera")]
    public Camera mainCamera;

    public NetworkVariable<bool> GameStarted =
        new NetworkVariable<bool>(false);

    public NetworkVariable<int> Players =
        new NetworkVariable<int>(0);

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Players.OnValueChanged += OnPlayersChanged;
        GameStarted.OnValueChanged += OnGameStarted;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback +=
                OnClientConnected;

            NetworkManager.Singleton.OnClientDisconnectCallback +=
                OnClientDisconnected;

            Players.Value =
                NetworkManager.Singleton.ConnectedClients.Count;
        }

        AtualizarUI();

        // A câmera do lobby começa ligada
        if (mainCamera != null)
        {
            mainCamera.enabled = true;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer)
            return;

        Players.Value =
            NetworkManager.Singleton.ConnectedClients.Count;

        if (Players.Value >= 2 && !GameStarted.Value)
        {
            StartCoroutine(IniciarPartida());
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer)
            return;

        Players.Value =
            NetworkManager.Singleton.ConnectedClients.Count;

        GameStarted.Value = false;

        // Se alguém sair antes da partida,
        // a UI volta a aparecer.
        MostrarUILobbyClientRpc();
    }

    private IEnumerator IniciarPartida()
    {
        // =========================
        // 3
        // =========================

        CountdownClientRpc("3");

        yield return new WaitForSeconds(1f);


        // =========================
        // 2
        // =========================

        CountdownClientRpc("2");

        yield return new WaitForSeconds(1f);


        // =========================
        // 1
        // =========================

        CountdownClientRpc("1");

        yield return new WaitForSeconds(1f);


        // =========================
        // GO!
        // =========================

        CountdownClientRpc("GO!");

        yield return new WaitForSeconds(1f);


        // =========================
        // COMEÇA A PARTIDA
        // =========================

        GameStarted.Value = true;

        // Esconde a UI e desliga a câmera do lobby
        EsconderLobbyClientRpc();
    }

    // =========================================================
    // CONTAGEM
    // =========================================================

    [ClientRpc]
    private void CountdownClientRpc(string texto)
    {
        if (countdownText != null)
        {
            countdownText.text = texto;
        }
    }


    // =========================================================
    // ESCONDER LOBBY
    // =========================================================

    [ClientRpc]
    private void EsconderLobbyClientRpc()
    {
        // Desliga o painel inteiro
        // Aqui ficam Create / Join / código / jogadores etc.
        if (waitingPanel != null)
        {
            waitingPanel.SetActive(false);
        }

        // Desliga a câmera que mostra o cenário do lobby
        if (mainCamera != null)
        {
            mainCamera.enabled = false;
        }

        // Limpa o texto do contador
        if (countdownText != null)
        {
            countdownText.text = "";
        }

        Debug.Log("LOBBY ESCONDIDO - PARTIDA COMEÇOU!");
    }


    // =========================================================
    // MOSTRAR LOBBY NOVAMENTE
    // =========================================================

    [ClientRpc]
    private void MostrarUILobbyClientRpc()
    {
        if (waitingPanel != null)
        {
            waitingPanel.SetActive(true);
        }

        if (mainCamera != null)
        {
            mainCamera.enabled = true;
        }

        if (countdownText != null)
        {
            countdownText.text = "";
        }
    }


    // =========================================================
    // JOGADORES MUDARAM
    // =========================================================

    private void OnPlayersChanged(int antigo, int novo)
    {
        AtualizarUI();
    }


    // =========================================================
    // ESTADO DA PARTIDA MUDOU
    // =========================================================

    private void OnGameStarted(bool antigo, bool novo)
    {
        AtualizarUI();
    }


    // =========================================================
    // ATUALIZAR UI
    // =========================================================

    private void AtualizarUI()
    {
        if (playersText != null)
        {
            playersText.text =
                "Jogadores: " +
                Players.Value +
                "/2";
        }

        if (waitingPanel != null)
        {
            waitingPanel.SetActive(!GameStarted.Value);
        }
    }


    // =========================================================
    // VERIFICAR SE PODE JOGAR
    // =========================================================

    public bool PodeJogar()
    {
        return GameStarted.Value;
    }


    // =========================================================
    // LIMPEZA
    // =========================================================

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null && IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -=
                OnClientConnected;

            NetworkManager.Singleton.OnClientDisconnectCallback -=
                OnClientDisconnected;
        }

        Players.OnValueChanged -= OnPlayersChanged;
        GameStarted.OnValueChanged -= OnGameStarted;
    }
}
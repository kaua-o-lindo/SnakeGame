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

            // Coloca o servidor no Spawn 1
            StartCoroutine(PosicionarJogadorDepoisDoSpawn(
                NetworkManager.Singleton.LocalClientId
            ));
        }

        AtualizarUI();

        if (mainCamera != null)
            mainCamera.enabled = true;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer)
            return;

        Players.Value =
            NetworkManager.Singleton.ConnectedClients.Count;

        StartCoroutine(PosicionarJogadorDepoisDoSpawn(clientId));

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
    }

    private IEnumerator PosicionarJogadorDepoisDoSpawn(ulong clientId)
    {
        // Espera o PlayerObject realmente existir
        yield return new WaitForSeconds(0.2f);

        if (!NetworkManager.Singleton.ConnectedClients
            .TryGetValue(clientId, out NetworkClient client))
        {
            yield break;
        }

        if (client.PlayerObject == null)
        {
            yield break;
        }

        Transform jogador = client.PlayerObject.transform;

        Transform spawn;

        if (clientId == NetworkManager.ServerClientId)
        {
            spawn = player1Spawn;
            Debug.Log("PLAYER 1 -> SPAWN 1");
        }
        else
        {
            spawn = player2Spawn;
            Debug.Log("PLAYER 2 -> SPAWN 2");
        }

        if (spawn == null)
        {
            Debug.LogError("Spawn Point não foi configurado!");
            yield break;
        }

        // O servidor define a posição
        jogador.SetPositionAndRotation(
            spawn.position,
            spawn.rotation
        );

        Debug.Log(
            "Jogador " + clientId +
            " colocado em " +
            jogador.position
        );
    }

    private IEnumerator IniciarPartida()
    {
        CountdownClientRpc("3");
        yield return new WaitForSeconds(1f);

        CountdownClientRpc("2");
        yield return new WaitForSeconds(1f);

        CountdownClientRpc("1");
        yield return new WaitForSeconds(1f);

        CountdownClientRpc("GO!");
        yield return new WaitForSeconds(1f);

        GameStarted.Value = true;

        DesativarMainCameraClientRpc();
    }

    [ClientRpc]
    private void DesativarMainCameraClientRpc()
    {
        if (mainCamera != null)
        {
            mainCamera.enabled = false;
        }
    }

    [ClientRpc]
    private void CountdownClientRpc(string texto)
    {
        if (countdownText != null)
            countdownText.text = texto;
    }

    private void OnPlayersChanged(int antigo, int novo)
    {
        AtualizarUI();
    }

    private void OnGameStarted(bool antigo, bool novo)
    {
        AtualizarUI();
    }

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
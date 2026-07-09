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

    public NetworkVariable<bool> partidaComecou = new NetworkVariable<bool>(false);
    public NetworkVariable<int> jogadores = new NetworkVariable<int>(0);

    private bool iniciando = false;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            jogadores.Value = NetworkManager.Singleton.ConnectedClientsList.Count;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        jogadores.OnValueChanged += AtualizarUI;
        partidaComecou.OnValueChanged += PartidaIniciou;

        AtualizarUI(0, jogadores.Value);

        if (waitingPanel != null)
            waitingPanel.SetActive(true);
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    void OnClientConnected(ulong clientId)
    {
        jogadores.Value = NetworkManager.Singleton.ConnectedClientsList.Count;

        PosicionarJogadores();

        if (jogadores.Value >= 2 && !iniciando)
        {
            StartCoroutine(IniciarPartida());
        }
    }

    void OnClientDisconnected(ulong clientId)
    {
        jogadores.Value = NetworkManager.Singleton.ConnectedClientsList.Count;
    }

    void PosicionarJogadores()
    {
        int index = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
                continue;

            if (index == 0)
            {
                client.PlayerObject.transform.position = player1Spawn.position;
                client.PlayerObject.transform.rotation = player1Spawn.rotation;
            }
            else if (index == 1)
            {
                client.PlayerObject.transform.position = player2Spawn.position;
                client.PlayerObject.transform.rotation = player2Spawn.rotation;
            }

            index++;
        }
    }

    IEnumerator IniciarPartida()
    {
        iniciando = true;

        CountdownClientRpc("3");
        yield return new WaitForSeconds(1);

        CountdownClientRpc("2");
        yield return new WaitForSeconds(1);

        CountdownClientRpc("1");
        yield return new WaitForSeconds(1);

        CountdownClientRpc("GO!");
        yield return new WaitForSeconds(1);

        partidaComecou.Value = true;
    }

    void AtualizarUI(int oldValue, int newValue)
    {
        if (playersText != null)
            playersText.text = $"Jogadores: {newValue}/2";
    }

    void PartidaIniciou(bool oldValue, bool newValue)
    {
        if (waitingPanel != null)
            waitingPanel.SetActive(!newValue);

        if (countdownText != null && newValue)
            countdownText.text = "";
    }

    [ClientRpc]
    void CountdownClientRpc(string texto)
    {
        if (countdownText != null)
            countdownText.text = texto;
    }

    public bool PodeJogar()
    {
        return partidaComecou.Value;
    }
}
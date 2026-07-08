using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;

public class MultiplayerManager : NetworkBehaviour
{
    public static MultiplayerManager Instance;

    [Header("UI")]
    public GameObject waitingPanel;
    public TextMeshProUGUI playersText;
    public TextMeshProUGUI countdownText;

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
        Players.OnValueChanged += OnPlayersChanged;
        GameStarted.OnValueChanged += OnGameStarted;

        if (IsServer)
        {
            Players.Value = NetworkManager.Singleton.ConnectedClients.Count;

            NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
        }

        waitingPanel.SetActive(true);
    }

    void ClientConnected(ulong id)
    {
        Players.Value = NetworkManager.Singleton.ConnectedClients.Count;

        if (Players.Value >= 2 && !GameStarted.Value)
        {
            StartCoroutine(StartGame());
        }
    }

    void ClientDisconnected(ulong id)
    {
        Players.Value = NetworkManager.Singleton.ConnectedClients.Count;
    }

    IEnumerator StartGame()
    {
        CountdownClientRpc("3");
        yield return new WaitForSeconds(1);

        CountdownClientRpc("2");
        yield return new WaitForSeconds(1);

        CountdownClientRpc("1");
        yield return new WaitForSeconds(1);

        CountdownClientRpc("GO!");
        yield return new WaitForSeconds(1);

        GameStarted.Value = true;
    }

    void OnPlayersChanged(int oldValue, int newValue)
    {
        playersText.text = $"Jogadores: {newValue}/2";
    }

    void OnGameStarted(bool oldValue, bool newValue)
    {
        waitingPanel.SetActive(!newValue);
    }

    [ClientRpc]
    void CountdownClientRpc(string text)
    {
        countdownText.text = text;
    }
}
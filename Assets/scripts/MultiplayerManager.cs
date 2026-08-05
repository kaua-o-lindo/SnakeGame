using System.Collections;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class MultiplayerManager : NetworkBehaviour
{
    public static MultiplayerManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject waitingPanel;
    [SerializeField] private TextMeshProUGUI playersText;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("SPAWN DOS JOGADORES")]
    [SerializeField] private Transform player1Spawn;
    [SerializeField] private Transform player2Spawn;

    [Header("Configuração")]
    [SerializeField] private int quantidadeJogadores = 2;

    public NetworkVariable<bool> partidaComecou =
        new NetworkVariable<bool>(false);

    public NetworkVariable<int> jogadores =
        new NetworkVariable<int>(0);

    private bool iniciandoPartida = false;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        jogadores.OnValueChanged += AtualizarTextoJogadores;
        partidaComecou.OnValueChanged += PartidaComecou;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback +=
                JogadorEntrou;

            NetworkManager.Singleton.OnClientDisconnectCallback +=
                JogadorSaiu;

            jogadores.Value =
                NetworkManager.Singleton.ConnectedClientsList.Count;
        }

        if (waitingPanel != null)
        {
            waitingPanel.SetActive(true);
        }

        AtualizarTextoJogadores(
            0,
            jogadores.Value
        );
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback -=
            JogadorEntrou;

        NetworkManager.Singleton.OnClientDisconnectCallback -=
            JogadorSaiu;
    }

    // =====================================================
    // JOGADOR ENTROU
    // =====================================================

    private void JogadorEntrou(ulong clientId)
    {
        if (!IsServer)
            return;

        jogadores.Value =
            NetworkManager.Singleton.ConnectedClientsList.Count;

        Debug.Log(
            "Jogador entrou: " + clientId
        );

        // Coloca cada jogador no seu Spawn
        PosicionarJogadores();

        // Se tiver 2 jogadores, começa a partida
        if (jogadores.Value >= quantidadeJogadores &&
            !iniciandoPartida &&
            !partidaComecou.Value)
        {
            StartCoroutine(IniciarPartida());
        }
    }

    // =====================================================
    // JOGADOR SAIU
    // =====================================================

    private void JogadorSaiu(ulong clientId)
    {
        if (!IsServer)
            return;

        jogadores.Value =
            NetworkManager.Singleton.ConnectedClientsList.Count;

        Debug.Log(
            "Jogador saiu: " + clientId
        );
    }

    // =====================================================
    // POSICIONAR JOGADORES
    // =====================================================

    private void PosicionarJogadores()
    {
        if (!IsServer)
            return;

        int index = 0;

        foreach (var client in
                 NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
                continue;

            Transform jogador =
                client.PlayerObject.transform;

            // PLAYER 1
            if (index == 0)
            {
                if (player1Spawn != null)
                {
                    jogador.position =
                        player1Spawn.position;

                    jogador.rotation =
                        player1Spawn.rotation;

                    Debug.Log(
                        "Player 1 colocado no Spawn 1"
                    );
                }
            }

            // PLAYER 2
            else if (index == 1)
            {
                if (player2Spawn != null)
                {
                    jogador.position =
                        player2Spawn.position;

                    jogador.rotation =
                        player2Spawn.rotation;

                    Debug.Log(
                        "Player 2 colocado no Spawn 2"
                    );
                }
            }

            index++;
        }
    }

    // =====================================================
    // INICIAR PARTIDA
    // =====================================================

    private IEnumerator IniciarPartida()
    {
        iniciandoPartida = true;

        CountdownClientRpc("3");

        yield return new WaitForSeconds(1f);

        CountdownClientRpc("2");

        yield return new WaitForSeconds(1f);

        CountdownClientRpc("1");

        yield return new WaitForSeconds(1f);

        CountdownClientRpc("GO!");

        yield return new WaitForSeconds(1f);

        partidaComecou.Value = true;

        iniciandoPartida = false;
    }

    // =====================================================
    // ATUALIZAR TEXTO
    // =====================================================

    private void AtualizarTextoJogadores(
        int valorAntigo,
        int valorNovo)
    {
        if (playersText != null)
        {
            playersText.text =
                "Jogadores: " +
                valorNovo +
                "/" +
                quantidadeJogadores;
        }
    }

    // =====================================================
    // PARTIDA COMEÇOU
    // =====================================================

    private void PartidaComecou(
        bool valorAntigo,
        bool valorNovo)
    {
        if (waitingPanel != null)
        {
            waitingPanel.SetActive(!valorNovo);
        }

        if (valorNovo &&
            countdownText != null)
        {
            countdownText.text = "";
        }
    }

    // =====================================================
    // CONTAGEM REGRESSIVA
    // =====================================================

    [ClientRpc]
    private void CountdownClientRpc(
        string texto)
    {
        if (countdownText != null)
        {
            countdownText.text = texto;
        }
    }

    // =====================================================
    // VERIFICAR SE PODE JOGAR
    // =====================================================

    public bool PodeJogar()
    {
        return partidaComecou.Value;
    }
}
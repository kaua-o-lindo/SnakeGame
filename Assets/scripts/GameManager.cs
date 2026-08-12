using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public GameObject waitingPanel;
    public TextMeshProUGUI waitingText;
    public TextMeshProUGUI countdownText;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    public NetworkVariable<bool> partidaComecou =
        new NetworkVariable<bool>(false);

    public NetworkVariable<int> jogadores =
        new NetworkVariable<int>(0);

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        jogadores.OnValueChanged += AtualizarTela;

        if (IsServer)
        {
            jogadores.Value =
                NetworkManager.Singleton.ConnectedClients.Count;

            NetworkManager.Singleton.OnClientConnectedCallback +=
                ClienteEntrou;

            NetworkManager.Singleton.OnClientDisconnectCallback +=
                ClienteSaiu;

            // Coloca os jogadores que já existem nos spawns
            StartCoroutine(PosicionarJogadores());
        }

        AtualizarTela(0, jogadores.Value);
    }

    // =========================================================
    // QUANDO UM JOGADOR ENTRA
    // =========================================================

    private void ClienteEntrou(ulong clientId)
    {
        if (!IsServer)
            return;

        jogadores.Value =
            NetworkManager.Singleton.ConnectedClients.Count;

        Debug.Log(
            "[GameManager] Jogador entrou: " + clientId
        );

        // Coloca o jogador no spawn correto
        StartCoroutine(
            PosicionarJogador(clientId)
        );

        // Quando tiver 2 jogadores, começa
        if (jogadores.Value >= 2 &&
            !partidaComecou.Value)
        {
            StartCoroutine(
                IniciarPartida()
            );
        }
    }

    // =========================================================
    // QUANDO UM JOGADOR SAI
    // =========================================================

    private void ClienteSaiu(ulong id)
    {
        if (!IsServer)
            return;

        jogadores.Value =
            NetworkManager.Singleton.ConnectedClients.Count;

        Debug.Log(
            "[GameManager] Jogador saiu: " + id
        );
    }

    // =========================================================
    // POSICIONAR TODOS
    // =========================================================

    private IEnumerator PosicionarJogadores()
    {
        yield return new WaitForSeconds(0.5f);

        int indice = 0;

        foreach (var cliente in
                 NetworkManager.Singleton.ConnectedClientsList)
        {
            PosicionarJogadorDireto(
                cliente.ClientId,
                indice
            );

            indice++;
        }
    }

    // =========================================================
    // POSICIONAR UM JOGADOR
    // =========================================================

    private IEnumerator PosicionarJogador(
        ulong clientId)
    {
        // Espera o PlayerObject existir
        yield return new WaitForSeconds(0.2f);

        if (!NetworkManager.Singleton.ConnectedClients
            .TryGetValue(
                clientId,
                out NetworkClient cliente))
        {
            Debug.LogWarning(
                "[GameManager] Cliente não encontrado: "
                + clientId
            );

            yield break;
        }

        if (cliente.PlayerObject == null)
        {
            Debug.LogWarning(
                "[GameManager] PlayerObject ainda não existe."
            );

            yield break;
        }

        // Descobre o índice do jogador
        int indice = 0;

        foreach (var c in
                 NetworkManager.Singleton.ConnectedClientsList)
        {
            if (c.ClientId == clientId)
                break;

            indice++;
        }

        PosicionarJogadorDireto(
            clientId,
            indice
        );
    }

    // =========================================================
    // PLACE ON SPAWN
    // ESTILO DO SCRIPT DO PROFESSOR
    // =========================================================

    private void PosicionarJogadorDireto(
        ulong clientId,
        int indice)
    {
        if (!IsServer)
            return;

        if (spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            Debug.LogError(
                "[GameManager] Nenhum Spawn Point configurado!"
            );

            return;
        }

        if (!NetworkManager.Singleton.ConnectedClients
            .TryGetValue(
                clientId,
                out NetworkClient cliente))
        {
            return;
        }

        if (cliente.PlayerObject == null)
        {
            Debug.LogWarning(
                "[GameManager] PlayerObject não encontrado."
            );

            return;
        }

        // Igual ao estilo do professor:
        // pega o spawn pelo índice.
        Transform sp =
            spawnPoints[
                indice % spawnPoints.Length
            ];

        if (sp == null)
        {
            Debug.LogWarning(
                "[GameManager] Spawn Point " +
                indice +
                " está vazio!"
            );

            return;
        }

        Transform jogador =
            cliente.PlayerObject.transform;

        // IMPORTANTE:
        // mantém exatamente a altura do Spawn Point.
        Vector3 novaPosicao = sp.position;

        Quaternion novaRotacao =
            sp.rotation;

        jogador.SetPositionAndRotation(
            novaPosicao,
            novaRotacao
        );

        // Se tiver NetworkTransform,
        // tenta sincronizar imediatamente.
        NetworkTransform networkTransform =
            cliente.PlayerObject
                .GetComponent<NetworkTransform>();

        if (networkTransform != null)
        {
            networkTransform.Teleport(
                novaPosicao,
                novaRotacao,
                jogador.localScale
            );
        }

        Debug.Log(
            "[GameManager] Jogador " +
            clientId +
            " -> Spawn " +
            indice +
            " | Posição: " +
            novaPosicao
        );
    }

    // =========================================================
    // CONTAGEM 3 2 1 GO
    // =========================================================

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

        partidaComecou.Value = true;

        EsconderTelaClientRpc();

        Debug.Log(
            "[GameManager] PARTIDA COMEÇOU!"
        );
    }

    // =========================================================
    // MOSTRAR CONTAGEM
    // =========================================================

    [ClientRpc]
    private void CountdownClientRpc(
        string texto)
    {
        if (countdownText != null)
        {
            countdownText.text = texto;
        }
    }

    // =========================================================
    // ESCONDER WAITING PANEL
    // =========================================================

    [ClientRpc]
    private void EsconderTelaClientRpc()
    {
        if (waitingPanel != null)
        {
            waitingPanel.SetActive(false);
        }
    }

    // =========================================================
    // ATUALIZAR UI
    // =========================================================

    private void AtualizarTela(
        int antigo,
        int atual)
    {
        if (waitingText != null)
        {
            waitingText.text =
                "Esperando jogadores...\n" +
                atual +
                "/2";
        }

        if (waitingPanel != null &&
            !partidaComecou.Value)
        {
            waitingPanel.SetActive(true);
        }
    }

    // =========================================================
    // PODE JOGAR?
    // =========================================================

    public bool PodeJogar()
    {
        return partidaComecou.Value;
    }

    // =========================================================
    // LIMPEZA
    // =========================================================

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null &&
            IsServer)
        {
            NetworkManager.Singleton
                .OnClientConnectedCallback -=
                ClienteEntrou;

            NetworkManager.Singleton
                .OnClientDisconnectCallback -=
                ClienteSaiu;
        }

        jogadores.OnValueChanged -=
            AtualizarTela;
    }
}
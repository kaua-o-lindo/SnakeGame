using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public GameObject waitingPanel;
    public TextMeshProUGUI waitingText;
    public TextMeshProUGUI countdownText;

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
            jogadores.Value = NetworkManager.Singleton.ConnectedClients.Count;

            NetworkManager.Singleton.OnClientConnectedCallback += ClienteEntrou;
            NetworkManager.Singleton.OnClientDisconnectCallback += ClienteSaiu;
        }

        AtualizarTela(0, jogadores.Value);
    }

    private void ClienteEntrou(ulong id)
    {
        jogadores.Value = NetworkManager.Singleton.ConnectedClients.Count;

        if (jogadores.Value >= 2 && !partidaComecou.Value)
        {
            StartCoroutine(IniciarPartida());
        }
    }

    private void ClienteSaiu(ulong id)
    {
        jogadores.Value = NetworkManager.Singleton.ConnectedClients.Count;
    }

    IEnumerator IniciarPartida()
    {
        CountdownClientRpc("3");
        yield return new WaitForSeconds(1);

        CountdownClientRpc("2");
        yield return new WaitForSeconds(1);

        CountdownClientRpc("1");
        yield return new WaitForSeconds(1);

        CountdownClientRpc("GO!");
        yield return new WaitForSeconds(1);

        partidaComecou.Value = true;

        EsconderTelaClientRpc();
    }

    [ClientRpc]
    void CountdownClientRpc(string texto)
    {
        if (countdownText != null)
            countdownText.text = texto;
    }

    [ClientRpc]
    void EsconderTelaClientRpc()
    {
        if (waitingPanel != null)
            waitingPanel.SetActive(false);
    }

    void AtualizarTela(int antigo, int atual)
    {
        if (waitingText != null)
        {
            waitingText.text = $"Esperando jogadores...\n{atual}/2";
        }

        if (waitingPanel != null && !partidaComecou.Value)
        {
            waitingPanel.SetActive(true);
        }
    }

    public bool PodeJogar()
    {
        return partidaComecou.Value;
    }
}
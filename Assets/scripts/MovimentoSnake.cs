using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class MovimentoSnake : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 180f;

    [Header("Body")]
    public float bodyDistance = 1.2f;
    public float bodySmooth = 10f;
    public GameObject bodyPrefab;

    [Header("References")]
    public AppleSpawner appleSpawner;

    [Header("Score")]
    public TMP_Text scoreText;

    [Header("Gravity")]
    public float alturaMinima = -5f;

    [Header("Game Over")]
    public float tempoAntesDeVoltar = 1.5f;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public GameObject youWinPanel;

    // =========================================================
    // CORPO
    // =========================================================

    private readonly List<Transform> bodyParts =
        new List<Transform>();

    // =========================================================
    // NETWORK VARIABLES
    // =========================================================

    private NetworkVariable<bool> alive =
        new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private NetworkVariable<int> appleCount =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    // =========================================================
    // VARIÁVEIS
    // =========================================================

    private bool initialized = false;

    private Rigidbody rb;

    private bool voltandoAoMenu = false;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = true;
        rb.isKinematic = false;

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;
    }


    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        appleCount.OnValueChanged +=
            OnAppleCountChanged;

        UpdateScoreText(appleCount.Value);

        // Esconde as telas inicialmente
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (youWinPanel != null)
            youWinPanel.SetActive(false);

        if (!IsOwner)
            return;

        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Initialize();
    }


    // =========================================================
    // INITIALIZE
    // =========================================================

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;

        bodyParts.Clear();

        // A própria Snake é a cabeça
        bodyParts.Add(transform);

        // Cria duas partes iniciais
        CreateInitialBodyServerRpc(2);
    }


    // =========================================================
    // CRIAR CORPO INICIAL
    // =========================================================

    [ServerRpc]
    private void CreateInitialBodyServerRpc(
        int amount,
        ServerRpcParams rpcParams = default)
    {
        if (bodyPrefab == null)
        {
            Debug.LogError(
                "Body Prefab não foi configurado!"
            );

            return;
        }

        ulong ownerId =
            rpcParams.Receive.SenderClientId;

        if (!NetworkManager.Singleton
            .ConnectedClients
            .TryGetValue(
                ownerId,
                out NetworkClient client))
        {
            return;
        }

        if (client.PlayerObject == null)
            return;

        MovimentoSnake snake =
            client.PlayerObject
            .GetComponent<MovimentoSnake>();

        if (snake == null)
            return;

        List<NetworkObjectReference> references =
            new List<NetworkObjectReference>();

        Transform lastPart =
            snake.transform;

        for (int i = 0; i < amount; i++)
        {
            Vector3 position =
                lastPart.position -
                lastPart.forward *
                snake.bodyDistance;

            GameObject newPart =
                Instantiate(
                    snake.bodyPrefab,
                    position,
                    lastPart.rotation
                );

            newPart.tag = "SnakeBody";

            NetworkObject netObj =
                newPart.GetComponent<NetworkObject>();

            if (netObj == null)
            {
                Debug.LogError(
                    "O BodyPrefab precisa ter NetworkObject!"
                );

                Destroy(newPart);

                continue;
            }

            netObj.SpawnWithOwnership(ownerId);

            snake.AddBodyPartServer(
                newPart.transform
            );

            references.Add(
                new NetworkObjectReference(
                    netObj
                )
            );

            lastPart =
                newPart.transform;
        }

        snake.AddInitialBodyOwnerClientRpc(
            references.ToArray()
        );
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!IsOwner)
            return;

        if (!alive.Value)
            return;

        // Espera o GO
        if (MultiplayerManager.Instance != null)
        {
            if (!MultiplayerManager.Instance
                .GameStarted.Value)
            {
                return;
            }
        }

        Move();

        VerificarQueda();
    }


    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;

        if (!alive.Value)
            return;

        UpdateBody();
    }


    // =========================================================
    // MOVIMENTO
    // =========================================================

    private void Move()
    {
        float horizontal =
            Input.GetAxis("Horizontal");

        transform.Rotate(
            Vector3.up,
            horizontal *
            rotationSpeed *
            Time.deltaTime
        );

        Vector3 movimento =
            transform.forward *
            moveSpeed *
            Time.deltaTime;

        // A gravidade controla o Y
        movimento.y = 0f;

        transform.position += movimento;
    }


    // =========================================================
    // MOVIMENTO DO CORPO
    // =========================================================

    private void UpdateBody()
    {
        if (!IsOwner)
            return;

        if (bodyParts.Count <= 1)
            return;

        for (int i = 1;
             i < bodyParts.Count;
             i++)
        {
            Transform current =
                bodyParts[i];

            if (current == null)
                continue;

            NetworkObject networkObject =
                current.GetComponent<NetworkObject>();

            if (networkObject == null)
                continue;

            if (!networkObject.IsOwner)
                continue;

            Transform target =
                bodyParts[i - 1];

            if (target == null)
                continue;

            Vector3 targetPosition =
                target.position -
                target.forward *
                bodyDistance;

            current.position =
                Vector3.Lerp(
                    current.position,
                    targetPosition,
                    bodySmooth *
                    Time.fixedDeltaTime
                );

            current.rotation =
                Quaternion.Slerp(
                    current.rotation,
                    target.rotation,
                    bodySmooth *
                    Time.fixedDeltaTime
                );
        }
    }


    // =========================================================
    // VERIFICAR QUEDA
    // =========================================================

    private void VerificarQueda()
    {
        if (transform.position.y <= alturaMinima)
        {
            Debug.Log(
                "Snake " +
                OwnerClientId +
                " caiu da plataforma!"
            );

            DieServerRpc();
        }
    }


    // =========================================================
    // CRESCER COBRA
    // =========================================================

    private void GrowLocal()
    {
        if (!IsOwner)
            return;

        if (bodyPrefab == null)
        {
            Debug.LogError(
                "Body Prefab não foi configurado!"
            );

            return;
        }

        if (bodyParts.Count == 0)
        {
            Debug.LogError(
                "bodyParts está vazio!"
            );

            return;
        }

        Transform lastPart =
            bodyParts[
                bodyParts.Count - 1
            ];

        Vector3 position =
            lastPart.position -
            lastPart.forward *
            bodyDistance;

        GrowServerRpc(
            position,
            lastPart.rotation
        );
    }


    // =========================================================
    // CRESCER NO SERVIDOR
    // =========================================================

    [ServerRpc]
    private void GrowServerRpc(
        Vector3 position,
        Quaternion rotation,
        ServerRpcParams rpcParams = default)
    {
        if (bodyPrefab == null)
        {
            Debug.LogError(
                "Body Prefab não foi configurado!"
            );

            return;
        }

        ulong ownerId =
            rpcParams.Receive.SenderClientId;

        if (!NetworkManager.Singleton
            .ConnectedClients
            .TryGetValue(
                ownerId,
                out NetworkClient client))
        {
            return;
        }

        if (client.PlayerObject == null)
            return;

        MovimentoSnake snake =
            client.PlayerObject
            .GetComponent<MovimentoSnake>();

        if (snake == null)
            return;

        GameObject newPart =
            Instantiate(
                bodyPrefab,
                position,
                rotation
            );

        newPart.tag = "SnakeBody";

        NetworkObject netObj =
            newPart.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError(
                "O BodyPrefab precisa ter NetworkObject!"
            );

            Destroy(newPart);

            return;
        }

        netObj.SpawnWithOwnership(
            ownerId
        );

        snake.AddBodyPartServer(
            newPart.transform
        );

        snake.AddBodyPartOwnerClientRpc(
            new NetworkObjectReference(
                netObj
            )
        );
    }


    // =========================================================
    // ADICIONAR BODY NO SERVIDOR
    // =========================================================

    public void AddBodyPartServer(
        Transform part)
    {
        if (!IsServer)
            return;

        if (part == null)
            return;

        if (!bodyParts.Contains(part))
        {
            bodyParts.Add(part);
        }
    }


    // =========================================================
    // ADICIONAR BODY SOMENTE NO DONO
    // =========================================================

    [ClientRpc]
    private void AddBodyPartOwnerClientRpc(
        NetworkObjectReference reference)
    {
        if (!IsOwner)
            return;

        if (!reference.TryGet(
            out NetworkObject networkObject))
        {
            return;
        }

        Transform part =
            networkObject.transform;

        if (!bodyParts.Contains(part))
        {
            bodyParts.Add(part);
        }
    }


    // =========================================================
    // ADICIONAR CORPO INICIAL
    // =========================================================

    [ClientRpc]
    private void AddInitialBodyOwnerClientRpc(
        NetworkObjectReference[] references)
    {
        if (!IsOwner)
            return;

        if (references == null)
            return;

        foreach (
            NetworkObjectReference reference
            in references)
        {
            if (!reference.TryGet(
                out NetworkObject networkObject))
            {
                continue;
            }

            Transform part =
                networkObject.transform;

            if (!bodyParts.Contains(part))
            {
                bodyParts.Add(part);
            }
        }
    }


    // =========================================================
    // ADICIONAR BODY
    // =========================================================

    private void AddBody()
    {
        GrowLocal();
    }


    // =========================================================
    // COLISÕES
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner)
            return;

        if (!alive.Value)
            return;


        // PAREDE
        if (other.CompareTag("Wall"))
        {
            DieServerRpc();
            return;
        }


        // OUTRA COBRA
        if (other.CompareTag("Snake") ||
            other.CompareTag("SnakeBody"))
        {
            DieServerRpc();
            return;
        }


        // MAÇÃ
        if (other.CompareTag("Apple"))
        {
            NetworkObject apple =
                other.GetComponent<NetworkObject>();

            if (apple != null)
            {
                EatAppleServerRpc(
                    apple.NetworkObjectId
                );
            }
        }
    }


    // =========================================================
    // COMER MAÇÃ
    // =========================================================

    [ServerRpc]
    private void EatAppleServerRpc(
        ulong appleId)
    {
        if (!NetworkManager.Singleton
            .SpawnManager
            .SpawnedObjects
            .TryGetValue(
                appleId,
                out NetworkObject apple))
        {
            return;
        }

        apple.Despawn(true);

        appleCount.Value++;

        AddBodyClientRpc();

        if (appleSpawner != null)
        {
            appleSpawner.SpawnApple();
        }
    }


    // =========================================================
    // CRESCIMENTO NO DONO
    // =========================================================

    [ClientRpc]
    private void AddBodyClientRpc()
    {
        if (!IsOwner)
            return;

        AddBody();
    }


    // =========================================================
    // SCORE
    // =========================================================

    private void OnAppleCountChanged(
        int oldValue,
        int newValue)
    {
        if (!IsOwner)
            return;

        UpdateScoreText(newValue);
    }


    private void UpdateScoreText(int value)
    {
        if (scoreText != null)
        {
            scoreText.text =
                "Maçãs: " +
                value;
        }
    }


    // =========================================================
    // MORRER POR OBSTÁCULO
    // =========================================================

    public void MorrerPorObstaculo()
    {
        if (!IsOwner)
            return;

        if (!alive.Value)
            return;

        Debug.Log(
            "Snake " +
            OwnerClientId +
            " morreu ao tocar em um obstáculo!"
        );

        DieServerRpc();
    }


    // =========================================================
    // MORRER
    // =========================================================

    [ServerRpc]
    private void DieServerRpc(
        ServerRpcParams rpcParams = default)
    {
        if (!alive.Value)
            return;

        alive.Value = false;

        // Quem morreu
        ulong jogadorMorto =
            rpcParams.Receive.SenderClientId;

        Debug.Log(
            "JOGADOR MORTO: " +
            jogadorMorto
        );

        // =====================================================
        // MOSTRA GAME OVER PARA QUEM MORREU
        // =====================================================

        ClientRpcParams mortoParams =
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds =
                        new ulong[]
                        {
                            jogadorMorto
                        }
                }
            };

        MostrarGameOverClientRpc(
            mortoParams
        );


        // =====================================================
        // PROCURA O OUTRO JOGADOR
        // =====================================================

        ulong jogadorVencedor =
            ulong.MaxValue;

        foreach (
            ulong clientId
            in NetworkManager.Singleton
                .ConnectedClientsIds)
        {
            if (clientId != jogadorMorto)
            {
                jogadorVencedor = clientId;
                break;
            }
        }


        // =====================================================
        // MOSTRA YOU WIN PARA O VENCEDOR
        // =====================================================

        if (jogadorVencedor != ulong.MaxValue)
        {
            ClientRpcParams vencedorParams =
                new ClientRpcParams
                {
                    Send =
                        new ClientRpcSendParams
                        {
                            TargetClientIds =
                                new ulong[]
                                {
                                    jogadorVencedor
                                }
                        }
                };

            MostrarYouWinClientRpc(
                vencedorParams
            );
        }


        // =====================================================
        // DESATIVA O JOGO
        // =====================================================

        DieClientRpc();

        // Voltar ao menu depois do tempo
        Invoke(
            nameof(VoltarTodosAoMenu),
            tempoAntesDeVoltar
        );
    }


    // =========================================================
    // GAME OVER - SOMENTE QUEM MORREU
    // =========================================================

    [ClientRpc]
    private void MostrarGameOverClientRpc(
        ClientRpcParams clientRpcParams = default)
    {
        Debug.Log(
            "GAME OVER - ESTE JOGADOR MORREU"
        );

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "Game Over Panel não foi configurado!"
            );
        }
    }


    // =========================================================
    // YOU WIN - SOMENTE QUEM SOBREVIVEU
    // =========================================================

    [ClientRpc]
    private void MostrarYouWinClientRpc(
        ClientRpcParams clientRpcParams = default)
    {
        Debug.Log(
            "YOU WIN - ESTE JOGADOR SOBREVIVEU"
        );

        if (youWinPanel != null)
        {
            youWinPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "You Win Panel não foi configurado!"
            );
        }
    }


    // =========================================================
    // MORTE NOS CLIENTES
    // =========================================================

    [ClientRpc]
    private void DieClientRpc()
    {
        Debug.Log(
            "Snake morreu: " +
            OwnerClientId
        );

        if (rb != null)
        {
            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;
        }

        enabled = false;
    }


    // =========================================================
    // VOLTAR AO MENU
    // =========================================================

    private void VoltarTodosAoMenu()
    {
        if (!IsServer)
            return;

        if (voltandoAoMenu)
            return;

        voltandoAoMenu = true;

        VoltarMenuClientRpc();
    }


    // =========================================================
    // VOLTAR AO MENU NOS CLIENTES
    // =========================================================

    [ClientRpc]
    private void VoltarMenuClientRpc()
    {
        if (VoltarAoMenu.Instance != null)
        {
            VoltarAoMenu.Instance.VoltarMenu();
        }
        else
        {
            Debug.LogError(
                "VoltarAoMenu não foi encontrado na cena!"
            );
        }
    }


    // =========================================================
    // NETWORK DESPAWN
    // =========================================================

    public override void OnNetworkDespawn()
    {
        appleCount.OnValueChanged -=
            OnAppleCountChanged;

        base.OnNetworkDespawn();
    }
}
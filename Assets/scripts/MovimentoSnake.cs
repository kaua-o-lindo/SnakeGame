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

    private List<Transform> bodyParts =
        new List<Transform>();

    private NetworkVariable<bool> alive =
        new NetworkVariable<bool>(true);

    private NetworkVariable<int> appleCount =
        new NetworkVariable<int>(0);

    private bool initialized = false;

    private Rigidbody rb;


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

        // Ativa a gravidade
        rb.useGravity = true;

        // A física será controlada pelo Rigidbody
        rb.isKinematic = false;

        // A cobra não pode tombar
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

        // Quando a quantidade de maçãs mudar,
        // atualiza o texto.
        appleCount.OnValueChanged += OnAppleCountChanged;

        // Somente o dono controla sua própria cobra
        if (!IsOwner)
            return;

        // Garante que a gravidade esteja ativada
        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Initialize();

        // Atualiza o texto inicialmente
        UpdateScoreText(appleCount.Value);
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
        for (int i = 0; i < 2; i++)
        {
            GrowLocal();
        }
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

        // Espera o GO para começar a andar
        if (MultiplayerManager.Instance != null)
        {
            if (!MultiplayerManager.Instance.GameStarted.Value)
                return;
        }

        Move();

        // Verifica se caiu da plataforma
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

        // Rotação somente no eixo Y
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

        // Não aplicamos movimento vertical manualmente.
        // O Rigidbody cuida da gravidade.
        movimento.y = 0f;

        transform.position += movimento;
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
    // CORPO DA COBRA
    // =========================================================

    private void UpdateBody()
    {
        for (int i = 1; i < bodyParts.Count; i++)
        {
            Transform current =
                bodyParts[i];

            Transform target =
                bodyParts[i - 1];

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
    // CRESCER COBRA
    // =========================================================

    private void GrowLocal()
    {
        if (bodyPrefab == null)
        {
            Debug.LogError(
                "Body Prefab não foi configurado!"
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

        GameObject newPart =
            Instantiate(
                bodyPrefab,
                position,
                lastPart.rotation
            );

        newPart.tag = "SnakeBody";

        bodyParts.Add(
            newPart.transform
        );
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


        // -----------------------------------------
        // PAREDE
        // -----------------------------------------

        if (other.CompareTag("Wall"))
        {
            DieServerRpc();
            return;
        }


        // -----------------------------------------
        // OUTRA COBRA
        // -----------------------------------------

        if (other.CompareTag("Snake") ||
            other.CompareTag("SnakeBody"))
        {
            DieServerRpc();
            return;
        }


        // -----------------------------------------
        // MAÇÃ
        // -----------------------------------------

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

        // Remove a maçã
        apple.Despawn(true);

        // Aumenta a contagem
        appleCount.Value++;

        // Avisa o dono para adicionar o Body
        AddBodyClientRpc();

        // Cria outra maçã
        if (appleSpawner != null)
        {
            appleSpawner.SpawnApple();
        }
    }


    // =========================================================
    // ADICIONAR BODY NO CLIENTE
    // =========================================================

    [ClientRpc]
    private void AddBodyClientRpc()
    {
        if (!IsOwner)
            return;

        AddBody();
    }


    // =========================================================
    // CONTAGEM DE MAÇÃS
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
                "Maçãs: " + value;
        }
    }


    // =========================================================
    // MORRER
    // =========================================================

    [ServerRpc]
    private void DieServerRpc()
    {
        if (!alive.Value)
            return;

        alive.Value = false;

        DieClientRpc();
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
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        enabled = false;
    }


    // =========================================================
    // NETWORK DESPAWN
    // =========================================================

    public override void OnNetworkDespawn()
    {
        appleCount.OnValueChanged -= OnAppleCountChanged;

        base.OnNetworkDespawn();
    }
}


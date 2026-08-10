using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

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

    private List<Transform> bodyParts =
        new List<Transform>();

    private NetworkVariable<bool> alive =
        new NetworkVariable<bool>(true);

    private bool initialized = false;

    private float fixedY;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
            return;

        // Guarda a altura recebida do Spawn Point
        fixedY = transform.position.y;

        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;

        bodyParts.Clear();

        // O próprio Snake é a cabeça
        bodyParts.Add(transform);

        for (int i = 0; i < 2; i++)
        {
            GrowLocal();
        }
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (!alive.Value)
            return;

        if (MultiplayerManager.Instance != null)
        {
            if (!MultiplayerManager.Instance.GameStarted.Value)
                return;
        }

        Move();
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;

        if (!alive.Value)
            return;

        UpdateBody();

        // Mantém a cabeça exatamente na altura do Spawn
        Vector3 pos = transform.position;

        pos.y = fixedY;

        transform.position = pos;
    }

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

        // Movimento SOMENTE no X/Z
        movimento.y = 0f;

        transform.position += movimento;
    }

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
            bodyParts[bodyParts.Count - 1];

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

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner)
            return;

        if (!alive.Value)
            return;

        if (other.CompareTag("Wall"))
        {
            DieServerRpc();
            return;
        }

        if (other.CompareTag("Snake") ||
            other.CompareTag("SnakeBody"))
        {
            DieServerRpc();
            return;
        }

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

        if (appleSpawner != null)
        {
            appleSpawner.SpawnApple();
        }
    }

    [ServerRpc]
    private void DieServerRpc()
    {
        if (!alive.Value)
            return;

        alive.Value = false;

        DieClientRpc();
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        Debug.Log(
            "Snake morreu: " +
            OwnerClientId
        );

        enabled = false;
    }
}
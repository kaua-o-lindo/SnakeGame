using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MovimentoSnake : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 180f;
    public float bodyDistance = 1.2f;
    public float bodySmooth = 10f;

    [Header("References")]
    public GameObject bodyPrefab;
    public AppleSpawner appleSpawner;

    private List<Transform> bodyParts = new List<Transform>();

    private NetworkVariable<int> score =
        new NetworkVariable<int>(0);

    private NetworkVariable<bool> alive =
        new NetworkVariable<bool>(true);

    private bool initialized;

    void Start()
    {
        if (!IsOwner)
            return;

        Initialize();
    }

    void Initialize()
    {
        if (initialized)
            return;

        initialized = true;

        bodyParts.Clear();

        for (int i = 0; i < 2; i++)
        {
          
        }
    }

    void Update()
    {
        if (!IsOwner)
            return;

        if (!alive.Value)
            return;

        if (!GameManager.Instance.PodeJogar())
            return;

        Move();
    }

    void FixedUpdate()
    {
        if (!IsOwner)
            return;

        if (!alive.Value)
            return;

        UpdateBody();
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");

        transform.Rotate(
            Vector3.up,
            h * rotationSpeed * Time.deltaTime);

        transform.position +=
            transform.forward *
            moveSpeed *
            Time.deltaTime;
    }

    void UpdateBody()
    {
        for (int i = 1; i < bodyParts.Count; i++)
        {
            Transform current = bodyParts[i];
            Transform target = bodyParts[i - 1];

            Vector3 targetPos =
                target.position -
                target.forward * bodyDistance;

            current.position = Vector3.Lerp(
                current.position,
                targetPos,
                bodySmooth * Time.deltaTime);

            current.rotation = Quaternion.Slerp(
                current.rotation,
                target.rotation,
                bodySmooth * Time.deltaTime);
        }
        
        }
    [ClientRpc]
    void SpawnBodyClientRpc(ulong objectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(objectId))
            return;

        NetworkObject obj =
            NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId];

        Transform newPart = obj.transform;

        if (bodyParts.Count == 0)
        {
            newPart.position =
                transform.position -
                transform.forward * bodyDistance;
        }
        else
        {
            Transform last = bodyParts[bodyParts.Count - 1];

            newPart.position =
                last.position -
                last.forward * bodyDistance;

            newPart.rotation = last.rotation;
        }

        bodyParts.Add(newPart);
    }
    [ServerRpc]
        void Grow_ServerRpc(ServerRpcParams rpcParams = default)
        {
            GameObject part = Instantiate(bodyPrefab);

            NetworkObject netObj = part.GetComponent<NetworkObject>();

            if (netObj != null)
                netObj.Spawn(true);

            SpawnBodyClientRpc(netObj.NetworkObjectId);
        }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner)
            return;

        if (!alive.Value)
            return;

        if (other.CompareTag("Apple"))
        {
            EatAppleServerRpc(other.GetComponent<NetworkObject>().NetworkObjectId);
        }

        if (other.CompareTag("Wall"))
        {
            DieServerRpc();
        }

        if (other.CompareTag("SnakeBody"))
        {
            DieServerRpc();
        }

        if (other.CompareTag("Snake"))
        {
            DieServerRpc();
        }
    }

    [ServerRpc]
    void EatAppleServerRpc(ulong appleId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(appleId))
            return;

        NetworkObject apple =
            NetworkManager.Singleton.SpawnManager.SpawnedObjects[appleId];

        apple.Despawn(true);

        score.Value++;

        

        appleSpawner.SpawnApple();
    }

    [ServerRpc]
    void DieServerRpc()
    {
        alive.Value = false;

        DieClientRpc();
    }

    [ClientRpc]
    void DieClientRpc()
    {
        foreach (Transform part in bodyParts)
        {
            Rigidbody rb = part.GetComponent<Rigidbody>();

            if (rb == null)
                rb = part.gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = false;

            rb.AddExplosionForce(
                200f,
                transform.position,
                5f);
        }

        Rigidbody headRb = GetComponent<Rigidbody>();

        if (headRb != null)
        {
            headRb.isKinematic = false;

            headRb.AddExplosionForce(
                200f,
                transform.position,
                5f);
        }

        Destroy(gameObject, 2f);
    }
}
    
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class SnakeController : NetworkBehaviour
{
    [Header("Movimento")]
    public float velocidade = 5f;
    public float rotacao = 180f;

    [Header("Corpo")]
    public GameObject bodyPrefab;
    public float distancia = 1.2f;
    public float suavidade = 10f;

    [Header("UI")]
    public TextMeshProUGUI scoreText;

    private List<Transform> body = new List<Transform>();

    private NetworkVariable<int> score = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        score.OnValueChanged += AtualizarScore;

        if (!IsOwner)
            return;

        AtualizarScore(0, score.Value);
    }

    void Update()
    {
        if (!IsOwner)
            return;

        Mover();
    }

    void FixedUpdate()
    {
        if (!IsOwner)
            return;

        AtualizarCorpo();
    }

    void Mover()
    {
        float h = Input.GetAxis("Horizontal");

        transform.Rotate(Vector3.up * h * rotacao * Time.deltaTime);

        transform.position += transform.forward * velocidade * Time.deltaTime;
    }

    void AtualizarCorpo()
    {
        if (body.Count == 0)
            return;

        for (int i = body.Count - 1; i > 0; i--)
        {
            body[i].position = Vector3.Lerp(
                body[i].position,
                body[i - 1].position,
                suavidade * Time.deltaTime);

            body[i].rotation = Quaternion.Lerp(
                body[i].rotation,
                body[i - 1].rotation,
                suavidade * Time.deltaTime);
        }

        Vector3 alvo =
            transform.position -
            transform.forward * distancia;

        body[0].position = Vector3.Lerp(
            body[0].position,
            alvo,
            suavidade * Time.deltaTime);

        body[0].rotation = Quaternion.Lerp(
            body[0].rotation,
            transform.rotation,
            suavidade * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner)
            return;

        if (other.CompareTag("Apple"))
        {
            ComerMacaServerRpc(
                other.GetComponent<NetworkObject>().NetworkObjectId);
        }

        if (other.CompareTag("Wall") ||
            other.CompareTag("Snake") ||
            other.CompareTag("SnakeBody"))
        {
            MorrerServerRpc();
        }
    }

    [ServerRpc]
    void ComerMacaServerRpc(ulong appleId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(appleId))
            return;

        NetworkObject apple =
            NetworkManager.Singleton.SpawnManager.SpawnedObjects[appleId];

        apple.Despawn(true);

        score.Value++;

        CriarParte();

        FindFirstObjectByType<AppleSpawner>().SpawnApple();
    }

    void CriarParte()
    {
        GameObject parte = Instantiate(bodyPrefab);

        NetworkObject net = parte.GetComponent<NetworkObject>();

        net.Spawn();

        Transform t = parte.transform;

        if (body.Count == 0)
        {
            t.position =
                transform.position -
                transform.forward * distancia;
        }
        else
        {
            Transform ultima = body[body.Count - 1];

            t.position =
                ultima.position -
                ultima.forward * distancia;

            t.rotation = ultima.rotation;
        }

        body.Add(t);
    }

    [ServerRpc]
    void MorrerServerRpc()
    {
        MorrerClientRpc();
    }

    [ClientRpc]
    void MorrerClientRpc()
    {
        Destroy(gameObject);
    }

    void AtualizarScore(int antigo, int novo)
    {
        if (!IsOwner)
            return;

        if (scoreText != null)
            scoreText.text = "Score: " + novo;
    }
}
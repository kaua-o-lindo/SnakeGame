using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;

public class AppleSpawner : NetworkBehaviour
{
    [Header("🍎 Apple")]
    public GameObject applePrefab;

    [Header("🐍 Body")]
    public GameObject bodyPrefab;

    [Header("📦 Área de Spawn")]
    public Transform areaMin;
    public Transform areaMax;

    [Header("⏱️ Configurações")]
    public float spawnInterval = 10f;
    public float spawnHeight = 0.5f;

    [Header("🔢 Pontuação")]
    public TMP_Text scoreText;

    public static AppleSpawner Instance;

    private Coroutine spawnCoroutine;

    // =========================================================
    // PONTUAÇÃO
    // =========================================================

    private NetworkVariable<int> score =
        new NetworkVariable<int>(0);

    private void Awake()
    {
        Instance = this;
    }


    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Escuta mudanças na pontuação
        score.OnValueChanged += OnScoreChanged;

        // Atualiza o texto inicialmente
        UpdateScoreText(score.Value);

        // Somente o servidor cria as maçãs
        if (IsServer)
        {
            SpawnApple();

            spawnCoroutine =
                StartCoroutine(
                    SpawnAppleRoutine()
                );
        }
    }


    // =========================================================
    // SPAWN AUTOMÁTICO
    // =========================================================

    private IEnumerator SpawnAppleRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(
                spawnInterval
            );

            SpawnApple();
        }
    }


    // =========================================================
    // SPAWN DA MAÇÃ
    // =========================================================

    public void SpawnApple()
    {
        if (!IsServer)
            return;

        if (applePrefab == null)
        {
            Debug.LogError(
                "AppleSpawner: Apple Prefab não foi configurado!"
            );

            return;
        }

        if (areaMin == null ||
            areaMax == null)
        {
            Debug.LogError(
                "AppleSpawner: Area Min ou Area Max não foi configurado!"
            );

            return;
        }

        float minX =
            Mathf.Min(
                areaMin.position.x,
                areaMax.position.x
            );

        float maxX =
            Mathf.Max(
                areaMin.position.x,
                areaMax.position.x
            );

        float minZ =
            Mathf.Min(
                areaMin.position.z,
                areaMax.position.z
            );

        float maxZ =
            Mathf.Max(
                areaMin.position.z,
                areaMax.position.z
            );

        Vector3 spawnPosition =
            new Vector3(
                Random.Range(minX, maxX),
                spawnHeight,
                Random.Range(minZ, maxZ)
            );

        GameObject apple =
            Instantiate(
                applePrefab,
                spawnPosition,
                Quaternion.identity
            );

        NetworkObject networkObject =
            apple.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            networkObject.Spawn(true);
        }
        else
        {
            Debug.LogError(
                "Apple Prefab precisa ter um NetworkObject!"
            );

            Destroy(apple);
        }
    }


    // =========================================================
    // 🍎 MAÇÃ FOI COMIDA
    // =========================================================

    public void AppleEaten()
    {
        // Somente o servidor altera a pontuação
        if (!IsServer)
            return;

        // +1 na contagem
        score.Value++;

        Debug.Log(
            "Maçã comida! Total: " +
            score.Value
        );
    }


    // =========================================================
    // SERVER RPC
    // =========================================================

    [ServerRpc(RequireOwnership = false)]
    public void AppleEatenServerRpc()
    {
        AppleEaten();
    }


    // =========================================================
    // SERVER RPC - SPAWN
    // =========================================================

    [ServerRpc(RequireOwnership = false)]
    public void SpawnAppleServerRpc()
    {
        SpawnApple();
    }


    // =========================================================
    // SCORE
    // =========================================================

    private void OnScoreChanged(
        int oldValue,
        int newValue)
    {
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
    // GIZMO DA ÁREA
    // =========================================================

    private void OnDrawGizmos()
    {
        if (areaMin == null ||
            areaMax == null)
            return;

        float minX =
            Mathf.Min(
                areaMin.position.x,
                areaMax.position.x
            );

        float maxX =
            Mathf.Max(
                areaMin.position.x,
                areaMax.position.x
            );

        float minZ =
            Mathf.Min(
                areaMin.position.z,
                areaMax.position.z
            );

        float maxZ =
            Mathf.Max(
                areaMin.position.z,
                areaMax.position.z
            );

        Vector3 center =
            new Vector3(
                (minX + maxX) / 2f,
                spawnHeight,
                (minZ + maxZ) / 2f
            );

        Vector3 size =
            new Vector3(
                maxX - minX,
                0.1f,
                maxZ - minZ
            );

        Gizmos.color = Color.green;

        Gizmos.DrawWireCube(
            center,
            size
        );
    }


    // =========================================================
    // NETWORK DESPAWN
    // =========================================================

    public override void OnNetworkDespawn()
    {
        score.OnValueChanged -= OnScoreChanged;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);

            spawnCoroutine = null;
        }

        base.OnNetworkDespawn();
    }
}


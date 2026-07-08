using UnityEngine;
using Unity.Netcode;

public class AppleSpawner : NetworkBehaviour
{
    [Header("Apple")]
    public GameObject applePrefab;

    [Header("Spawn Area")]
    public Vector2 spawnX = new Vector2(-8, 8);
    public Vector2 spawnZ = new Vector2(-8, 8);

    public static AppleSpawner Instance;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnApple();
        }
    }

    public void SpawnApple()
    {
        if (!IsServer)
            return;

        Vector3 pos = new Vector3(
            Random.Range(spawnX.x, spawnX.y),
            0.5f,
            Random.Range(spawnZ.x, spawnZ.y)
        );

        GameObject apple = Instantiate(applePrefab, pos, Quaternion.identity);

        NetworkObject net = apple.GetComponent<NetworkObject>();

        if (net != null)
        {
            net.Spawn(true);
        }
    }

    [ServerRpc(RequireOwnership= false)]
    public void SpawnAppleServerRpc()
    {
        SpawnApple();
    }
}
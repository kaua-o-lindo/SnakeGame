using UnityEngine;
using Unity.Netcode;    
public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance;

    public Transform player1Spawn;
    public Transform player2Spawn;

    private void Awake()
    {
        Instance = this;
    }

    public Vector3 GetSpawnPosition(int index)
    {
        if (index == 0)
            return player1Spawn.position;

        return player2Spawn.position;
    }

    public Quaternion GetSpawnRotation(int index)
    {
        if (index == 0)
            return player1Spawn.rotation;

        return player2Spawn.rotation;
    }
}
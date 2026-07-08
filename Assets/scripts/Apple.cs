using UnityEngine;
using Unity.Netcode;

public class Apple : NetworkBehaviour
{
    private void Start()
    {
        gameObject.tag = "Apple";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        SnakeController snake = other.GetComponent<SnakeController>();

        if (snake == null)
            return;

        NetworkObject.Despawn(true);

        AppleSpawner.Instance.SpawnApple();
    }
}
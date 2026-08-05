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

       Camera snake = other.GetComponent<Camera>();

        if (snake == null)
            return;

        NetworkObject.Despawn(true);

        AppleSpawner.Instance.SpawnApple();
    }
}
using UnityEngine;
using Unity.Netcode;

public class PlayerCamera : NetworkBehaviour
{
    private UnityEngine.Camera playerCamera;
    private AudioListener audioListener;

    [Header("Câmera")]
    public float altura = 8f;
    public float distancia = 12f;
    public float suavidade = 5f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        playerCamera = GetComponentInChildren<UnityEngine.Camera>(true);
        audioListener = GetComponentInChildren<AudioListener>(true);

        // Desativa câmeras de jogadores que não são o dono
        if (!IsOwner)
        {
            if (playerCamera != null)
                playerCamera.enabled = false;

            if (audioListener != null)
                audioListener.enabled = false;

            return;
        }

        // Ativa a câmera do jogador local
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            playerCamera.tag = "MainCamera";
        }

        if (audioListener != null)
            audioListener.enabled = true;

        Debug.Log("Câmera ativada para: " + OwnerClientId);
    }

    private void LateUpdate()
    {
        if (!IsOwner)
            return;

        if (playerCamera == null)
            return;

        Vector3 posicaoDesejada =
            transform.position
            - transform.forward * distancia
            + Vector3.up * altura;

        playerCamera.transform.position = Vector3.Lerp(
            playerCamera.transform.position,
            posicaoDesejada,
            suavidade * Time.deltaTime
        );

        playerCamera.transform.LookAt(transform.position);
    }
}
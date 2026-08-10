using Unity.Netcode;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    [Header("Configuração")]
    public float altura = 8f;
    public float distancia = 12f;
    public float suavidade = 5f;

    private Camera minhaCamera;
    private AudioListener meuAudioListener;

    private bool cameraAtivada = false;

    private void Awake()
    {
        minhaCamera = GetComponent<Camera>();
        meuAudioListener = GetComponent<AudioListener>();

        // Começa desligada.
        if (minhaCamera != null)
            minhaCamera.enabled = false;

        if (meuAudioListener != null)
            meuAudioListener.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Cada computador controla somente sua própria câmera.
        if (!IsOwner)
        {
            DesativarCamera();
            return;
        }

        DesativarCamera();
    }

    public void AtivarCamera()
    {
        if (!IsOwner)
            return;

        cameraAtivada = true;

        if (minhaCamera != null)
            minhaCamera.enabled = true;

        if (meuAudioListener != null)
            meuAudioListener.enabled = true;

        Debug.Log(
            "Câmera ativada para jogador: " + OwnerClientId
        );
    }

    public void DesativarCamera()
    {
        cameraAtivada = false;

        if (minhaCamera != null)
            minhaCamera.enabled = false;

        if (meuAudioListener != null)
            meuAudioListener.enabled = false;
    }

    private void LateUpdate()
    {
        if (!IsOwner)
            return;

        if (!cameraAtivada)
            return;

        if (minhaCamera == null)
            return;

        Vector3 alvo = transform.parent != null
            ? transform.parent.position
            : transform.position;

        Vector3 posicaoDesejada =
            alvo + Vector3.up * altura;

        posicaoDesejada -=
            transform.parent != null
            ? transform.parent.forward * distancia
            : Vector3.forward * distancia;

        transform.position = Vector3.Lerp(
            transform.position,
            posicaoDesejada,
            suavidade * Time.deltaTime
        );

        transform.LookAt(alvo);
    }
}
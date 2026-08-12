using UnityEngine;
using Unity.Netcode;

public class PlayerCamera : NetworkBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float altura = 8f;
    [SerializeField] private float distancia = 12f;
    [SerializeField] private float suavidade = 5f;

    private Camera minhaCamera;
    private AudioListener meuAudioListener;

    private bool cameraAtivada = false;

    private void Awake()
    {
        minhaCamera = GetComponent<Camera>();
        meuAudioListener = GetComponent<AudioListener>();

        // Toda câmera começa desligada.
        // Depois o jogador dono dela será ativado.
        if (minhaCamera != null)
            minhaCamera.enabled = false;

        if (meuAudioListener != null)
            meuAudioListener.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Esta câmera NÃO pertence a este jogador.
        // Portanto permanece desligada.
        if (!IsOwner)
        {
            DesativarCamera();
            return;
        }

        // Esta é a câmera do jogador local.
        AtivarCamera();

        Debug.Log(
            "Câmera ativada para o jogador: " +
            OwnerClientId
        );
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
        // Só o dono controla essa câmera.
        if (!IsOwner)
            return;

        if (!cameraAtivada)
            return;

        if (minhaCamera == null)
            return;

        Transform jogador = transform.parent;

        if (jogador == null)
            jogador = transform.root;

        if (jogador == null)
            return;

        // Posição da cobra
        Vector3 alvo = jogador.position;

        // Mantém a câmera acima e atrás da cobra.
        Vector3 posicaoDesejada =
            alvo
            + Vector3.up * altura
            - jogador.forward * distancia;

        // Mantém a câmera no mesmo nível vertical configurado.
        posicaoDesejada.y = alvo.y + altura;

        transform.position = Vector3.Lerp(
            transform.position,
            posicaoDesejada,
            suavidade * Time.deltaTime
        );

        // Olha para a cobra.
        Vector3 direcao =
            alvo - transform.position;

        if (direcao.sqrMagnitude > 0.001f)
        {
            Quaternion rotacaoDesejada =
                Quaternion.LookRotation(direcao);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotacaoDesejada,
                suavidade * Time.deltaTime
            );
        }
    }
}
using System.Collections;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Camera))]
public class Camera : MonoBehaviour
{
    [Header("Configuração da Câmera")]
    [SerializeField] private float altura = 8f;
    [SerializeField] private float distancia = 12f;
    [SerializeField] private float angulo = 30f;

    [Header("Suavidade")]
    [SerializeField] private float suavidadePosicao = 0.25f;
    [SerializeField] private float suavidadeRotacao = 5f;

    private UnityEngine.Camera minhaCamera;
    private Transform jogadorLocal;

    private Vector3 velocidadeSuave;

    private IEnumerator Start()
    {
        minhaCamera = GetComponent<UnityEngine.Camera>();

        // Começa desligada
        minhaCamera.enabled = false;

        // Espera o NetworkManager existir
        while (NetworkManager.Singleton == null)
        {
            yield return null;
        }

        // Espera o jogador local aparecer
        while (
            NetworkManager.Singleton.LocalClient == null ||
            NetworkManager.Singleton.LocalClient.PlayerObject == null
        )
        {
            yield return null;
        }

        // Pega APENAS o jogador deste computador
        NetworkObject jogador =
            NetworkManager.Singleton.LocalClient.PlayerObject;

        jogadorLocal = jogador.transform;

        Debug.Log(
            "Câmera encontrou o jogador local: " +
            jogadorLocal.name
        );

        // Ativa a câmera deste jogador
        minhaCamera.enabled = true;

        // Procura a Main Camera da cena
        UnityEngine.Camera[] cameras =
            FindObjectsByType<UnityEngine.Camera>(
                FindObjectsSortMode.None
            );

        foreach (UnityEngine.Camera cam in cameras)
        {
            if (cam == minhaCamera)
                continue;

            // Desativa outras câmeras
            cam.enabled = false;
        }

        // Posiciona a câmera imediatamente
        Vector3 offset = CalcularOffset();

        transform.position =
            jogadorLocal.position + offset;

        transform.LookAt(
            jogadorLocal.position
        );
    }

    private void LateUpdate()
    {
        // Ainda não encontrou o jogador
        if (jogadorLocal == null)
            return;

        // Segue apenas o jogador local
        SeguirJogador();
    }

    private void SeguirJogador()
    {
        Vector3 offset =
            CalcularOffset();

        Vector3 posicaoDesejada =
            jogadorLocal.position + offset;

        transform.position =
            Vector3.SmoothDamp(
                transform.position,
                posicaoDesejada,
                ref velocidadeSuave,
                suavidadePosicao
            );

        Vector3 direcao =
            jogadorLocal.position -
            transform.position;

        if (direcao.sqrMagnitude > 0.01f)
        {
            Quaternion rotacaoDesejada =
                Quaternion.LookRotation(direcao);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    rotacaoDesejada,
                    suavidadeRotacao *
                    Time.deltaTime
                );
        }
    }

    private Vector3 CalcularOffset()
    {
        Vector3 offset =
            new Vector3(
                0f,
                altura,
                -distancia
            );

        return Quaternion.Euler(
            angulo,
            0f,
            0f
        ) * offset;
    }
}
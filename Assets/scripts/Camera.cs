using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class SnakeCamera : MonoBehaviour
{
    public float altura = 10f;
    public float distancia = 8f;
    public float suavidade = 8f;

    private Transform alvo;

    IEnumerator Start()
    {
        while (NetworkManager.Singleton == null)
            yield return null;

        while (NetworkManager.Singleton.LocalClient == null)
            yield return null;

        while (NetworkManager.Singleton.LocalClient.PlayerObject == null)
            yield return null;

        alvo = NetworkManager.Singleton.LocalClient.PlayerObject.transform;
    }

    void LateUpdate()
    {
        if (alvo == null)
            return;

        Vector3 posicaoDesejada =
            alvo.position
            - alvo.forward * distancia
            + Vector3.up * altura;

        transform.position = Vector3.Lerp(
            transform.position,
            posicaoDesejada,
            suavidade * Time.deltaTime);

        transform.LookAt(alvo.position + Vector3.up * 1.5f);
    }
}
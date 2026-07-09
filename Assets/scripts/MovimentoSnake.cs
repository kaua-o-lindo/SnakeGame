using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class MovimentoSnake : NetworkBehaviour
{
    [Header("Movimento")]
    public float velocidade = 5f;
    public float rotacao = 180f;

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (!IsOwner)
            enabled = false;
    }

    void Update()
    {
        if (!IsOwner)
            return;

        float h = Input.GetAxis("Horizontal");

        transform.Rotate(Vector3.up * h * rotacao * Time.deltaTime);

        Vector3 move = transform.forward * velocidade;

        controller.Move(move * Time.deltaTime);
    }
}
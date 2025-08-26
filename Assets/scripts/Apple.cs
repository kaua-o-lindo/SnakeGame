using UnityEngine;

public class Apple : MonoBehaviour
{
    void Start()
    {
        // Garante que a maçã tenha a tag correta
        gameObject.tag = "Apple";
    }

    // Este método pode ser expandido para incluir comportamentos adicionais
    void OnTriggerEnter(Collider other)
    {
        // Por enquanto, não é necessário implementar nada aqui,
        // pois a lógica de colisão está no script da cobrinha.
    }
}

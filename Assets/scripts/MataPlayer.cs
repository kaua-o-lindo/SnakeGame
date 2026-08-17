using UnityEngine;

public class MataPlayer : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            MovimentoSnake snake =
                other.GetComponent<MovimentoSnake>();

            if (snake != null)
            {
                snake.MorrerPorObstaculo();
            }
        }
    }
}
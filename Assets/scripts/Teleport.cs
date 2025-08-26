using UnityEngine;

public class TeleportTree : MonoBehaviour
{
    // Define as cores possíveis para a árvore
    public enum TreeColor
    {
        Red,    // Teleporta a cobra
        Green   // Mata a cobra
    }

    // Propriedade que define a cor da árvore (pode ser ajustada no Inspector)
    public TreeColor treeColor = TreeColor.Red;
}
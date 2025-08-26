using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class SnakeController : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    public float velocidadeMovimento = 5f;
    public float velocidadeRotacao = 180f;
    public float distanciaEntrePartes = 1.5f;
    public float suavidadeMovimento = 0.2f;

    [Header("Referências")]
    public GameObject parteDoCorpoPrefab;
    public AppleSpawner spawnerDeMacas;
    public TextMeshProUGUI textoPontuacao;
    public GameObject painelGameOver;
    public TextMeshProUGUI textoPontuacaoFinal;
    public TextMeshProUGUI textoHighScore;

    private List<Transform> partesDoCorpo = new List<Transform>();
    private bool estaVivo = true;
    private int pontuacao = 0;
    private int recorde = 0;
    private bool podeCrescer = true;
    private bool jogoIniciado = false;

    void Start()
    {
        IniciarJogo();
    }

    void IniciarJogo()
    {
        if (jogoIniciado) return;
        jogoIniciado = true;

        gameObject.tag = "Snake";
        recorde = PlayerPrefs.GetInt("HighScore", 0);
        AtualizarUI();

        // Limpa partes existentes
        foreach (Transform parte in partesDoCorpo)
        {
            if (parte != null) Destroy(parte.gameObject);
        }
        partesDoCorpo.Clear();

        // Cria partes iniciais
        for (int i = 0; i < 2; i++)
        {
            Crescer();
        }

        painelGameOver.SetActive(false);
    }

    void Update()
    {
        if (!estaVivo || !jogoIniciado) return;

        MoverCobra();
        AtualizarPartesDoCorpo();
        VerificarLimitesDoMapa();

        if (pontuacao > recorde)
        {
            recorde = pontuacao;
            PlayerPrefs.SetInt("HighScore", recorde);
            AtualizarUI();
        }
    }

    void MoverCobra()
    {
        transform.Translate(Vector3.forward * velocidadeMovimento * Time.deltaTime);
        float girar = Input.GetAxis("Horizontal");
        transform.Rotate(0, girar * velocidadeRotacao * Time.deltaTime, 0);
    }

    void AtualizarPartesDoCorpo()
    {
        for (int i = 0; i < partesDoCorpo.Count; i++)
        {
            Transform parteAtual = partesDoCorpo[i];
            Transform parteDaFrente = (i == 0) ? transform : partesDoCorpo[i - 1];

            Vector3 posicaoAlvo = parteDaFrente.position - parteDaFrente.forward * distanciaEntrePartes;
            parteAtual.position = Vector3.Lerp(parteAtual.position, posicaoAlvo, suavidadeMovimento);

            Vector3 direcaoOlhar = parteDaFrente.position - parteAtual.position;
            if (direcaoOlhar != Vector3.zero)
            {
                parteAtual.rotation = Quaternion.Slerp(
                    parteAtual.rotation,
                    Quaternion.LookRotation(direcaoOlhar),
                    suavidadeMovimento
                );
            }
        }
    }

    void Crescer()
    {
        Vector3 posicaoNascimento = partesDoCorpo.Count > 0 ?
            partesDoCorpo[partesDoCorpo.Count - 1].position - partesDoCorpo[partesDoCorpo.Count - 1].forward * distanciaEntrePartes :
            transform.position - transform.forward * distanciaEntrePartes;

        GameObject novaParte = Instantiate(parteDoCorpoPrefab, posicaoNascimento, Quaternion.identity);
        novaParte.tag = "SnakeBody";
        novaParte.transform.SetParent(transform.parent);
        partesDoCorpo.Add(novaParte.transform);
    }

    void OnTriggerEnter(Collider outro)
    {
        if (!estaVivo || !jogoIniciado) return;

        if (outro.CompareTag("Apple"))
        {
            Destroy(outro.gameObject);

            if (podeCrescer)
            {
                Crescer();
                podeCrescer = false;
                Invoke("LiberarCrescimento", 0.3f);
            }

            spawnerDeMacas.SpawnApple();
            pontuacao++;
            AtualizarUI();
        }
        else if (outro.CompareTag("Snake") || outro.CompareTag("SnakeBody") || outro.CompareTag("Wall"))
        {
            Morrer();
        }
    }

    void LiberarCrescimento()
    {
        podeCrescer = true;
    }

    void VerificarLimitesDoMapa()
    {
        if (transform.position.y < -10f)
        {
            Morrer();
        }
    }

    void Morrer()
    {
        if (!estaVivo) return;

        estaVivo = false;
        painelGameOver.SetActive(true);
        textoPontuacaoFinal.text = $"Pontuação Final: {pontuacao}";

        // Efeito de explosão
        foreach (Transform parte in partesDoCorpo)
        {
            if (parte != null)
            {
                Rigidbody rb = parte.GetComponent<Rigidbody>() ?? parte.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.AddExplosionForce(100f, transform.position, 5f);
                Destroy(parte.gameObject, 2f);
            }
        }

        Destroy(gameObject, 2f);
    }

    void AtualizarUI()
    {
        if (textoPontuacao != null)
            textoPontuacao.text = $"Score: {pontuacao}";

        if (textoHighScore != null)
            textoHighScore.text = $"High Score: {recorde}";
    }
}
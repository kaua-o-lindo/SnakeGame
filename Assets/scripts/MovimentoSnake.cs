using System.Collections.Generic;
using UnityEngine;

public class SnakeMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float rotationSpeed = 300f;
    public float bodySpacing = 0.5f;
    [Tooltip("How smoothly body parts follow each other")]
    public float followSmoothness = 10f;

    [Header("References")]
    public GameObject bodyPrefab;
    public ParticleSystem deathParticles;
    public AudioClip growSound;
    public AudioClip deathSound;

    [Header("Game Rules")]
    public float deathYThreshold = -5f;
    public bool canCollideWithSelf = false;

    private List<Transform> bodyParts = new List<Transform>();
    private bool isAlive = true;
    private AudioSource audioSource;

    public int BodyCount => bodyParts.Count - 1; // Excluding head
    public bool IsAlive => isAlive;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();

        // Initialize with head
        bodyParts.Add(transform);

        // Add initial body parts if needed
        for (int i = 0; i < 2; i++)
        {
            Grow();
        }
    }

    void Update()
    {
        if (!isAlive) return;

        HandleInput();
        CheckDeathConditions();
    }

    void FixedUpdate()
    {
        if (isAlive)
        {
            Move();
            UpdateBodyParts();
        }
    }

    void HandleInput()
    {
        // Support for both keyboard and mobile tilt controls
        float horizontal = Input.GetAxis("Horizontal");
#if UNITY_ANDROID || UNITY_IOS
        horizontal += Input.acceleration.x;
#endif

        transform.Rotate(Vector3.up * horizontal * rotationSpeed * Time.deltaTime);
    }

    void Move()
    {
        transform.position += transform.forward * speed * Time.fixedDeltaTime;
    }

    void UpdateBodyParts()
    {
        for (int i = 1; i < bodyParts.Count; i++)
        {
            Transform currentPart = bodyParts[i];
            Transform targetPart = bodyParts[i - 1];

            Vector3 targetPosition = targetPart.position - targetPart.forward * bodySpacing;

            // Smoother movement with interpolation
            currentPart.position = Vector3.Lerp(
                currentPart.position,
                targetPosition,
                followSmoothness * Time.fixedDeltaTime);

            // Only rotate if moving significantly
            if ((targetPart.position - currentPart.position).sqrMagnitude > 0.001f)
            {
                currentPart.rotation = Quaternion.Slerp(
                    currentPart.rotation,
                    targetPart.rotation,
                    followSmoothness * Time.fixedDeltaTime);
            }
        }
    }

    public void Grow()
    {
        if (bodyPrefab == null)
        {
            Debug.LogError("Body prefab not assigned!");
            return;
        }

        GameObject newPart = Instantiate(bodyPrefab);
        Transform newPartTransform = newPart.transform;

        // Position new part behind the last one
        Transform lastPart = bodyParts[bodyParts.Count - 1];
        newPartTransform.position = lastPart.position - lastPart.forward * bodySpacing;
        newPartTransform.rotation = lastPart.rotation;

        bodyParts.Add(newPartTransform);

        // Play sound effect
        if (growSound != null)
        {
            audioSource.PlayOneShot(growSound);
        }
    }

    void CheckDeathConditions()
    {
        // Fell below threshold
        if (transform.position.y < deathYThreshold)
        {
            Die();
            return;
        }

        // Check for self-collision if enabled
        if (!canCollideWithSelf && bodyParts.Count > 3)
        {
            for (int i = 3; i < bodyParts.Count; i++)
            {
                if (Vector3.Distance(transform.position, bodyParts[i].position) < 0.5f)
                {
                    Die();
                    return;
                }
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isAlive) return;

        if (collision.gameObject.CompareTag("Wall") ||
            (collision.gameObject.CompareTag("Body") && !canCollideWithSelf))
        {
            Die();
        }
    }

    public void Die()
    {
        if (!isAlive) return;

        isAlive = false;

        // Visual and audio effects
        if (deathParticles != null)
        {
            Instantiate(deathParticles, transform.position, Quaternion.identity);
        }

        if (deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // Disable physics on all parts
        foreach (Transform part in bodyParts)
        {
            var rb = part.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddExplosionForce(100f, transform.position, 5f);
            }

            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        Debug.Log("Game Over! Snake length: " + BodyCount);
        // Here you would typically trigger game over UI
        // GameManager.Instance.GameOver(BodyCount);
    }

 
}
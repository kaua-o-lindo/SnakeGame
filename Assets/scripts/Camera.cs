using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SnakeCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private float height = 8f;
    [SerializeField] private float distance = 12f;
    [SerializeField] private float angle = 30f;

    [Header("Follow Settings")]
    [SerializeField] private float positionSmoothTime = 0.3f;
    [SerializeField] private float rotationSmoothTime = 0.2f;
    [SerializeField] private bool dynamicDistance = true;
    [SerializeField] private float minDistance = 8f;
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private float distanceSpeedFactor = 0.5f;

    private Vector3 positionVelocity;
    private float rotationVelocity;
    private float currentDistance;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("Camera target not assigned!");
            enabled = false;
            return;
        }

        currentDistance = distance;
        InitializeCameraPosition();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        UpdateCameraDistance();
        FollowTarget();
    }

    private void InitializeCameraPosition()
    {
        // Posição inicial baseada nas configurações
        Vector3 offset = CalculateOffset();
        transform.position = target.position + offset;
        transform.LookAt(target.position);
    }

    private void UpdateCameraDistance()
    {
        if (!dynamicDistance) return;

        // Calcula distância dinâmica baseada na velocidade da cobra
        float speedFactor = 1f;
        if (target.TryGetComponent<Rigidbody>(out var rb))
        {
            speedFactor = Mathf.Clamp(rb.velocity.magnitude * distanceSpeedFactor, 0.8f, 1.5f);
        }

        currentDistance = Mathf.Lerp(
            currentDistance,
            Mathf.Clamp(distance * speedFactor, minDistance, maxDistance),
            positionSmoothTime * Time.deltaTime
        );
    }

    private void FollowTarget()
    {
        // Calcula offset com a distância atual
        Vector3 offset = CalculateOffset();

        // Suaviza movimento da posição
        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref positionVelocity,
            positionSmoothTime
        );

        // Suaviza rotação para olhar para o alvo
        Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
        float delta = rotationSmoothTime * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, delta);
    }

    private Vector3 CalculateOffset()
    {
        // Calcula offset baseado em ângulo, altura e distância
        Vector3 offset = new Vector3(0, height, -currentDistance);
        offset = Quaternion.Euler(angle, 0, 0) * offset;
        return offset;
    }

    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 offset = CalculateOffset();
            Gizmos.DrawLine(target.position, target.position + offset);
            Gizmos.DrawWireSphere(target.position + offset, 0.5f);
        }
    }
}
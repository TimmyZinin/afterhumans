using UnityEngine;

namespace Afterhumans.Kafka
{
    /// <summary>
    /// Kafka companion follow behavior. Walks toward player, faces player,
    /// uses embedded Animator for walk animation from Tripo3D FBX.
    /// </summary>
    public class KafkaFollowSimple : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Header("Behavior")]
        [SerializeField] private float stopDistance = 1.6f;
        [SerializeField] private float maxSpeed = 2.8f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float catchUpThreshold = 5f;
        [SerializeField] private float catchUpSpeed = 4.2f;

        [Header("Body orientation")]
        [SerializeField] private float turnSpeed = 6f;

        public static KafkaFollowSimple Instance { get; private set; }

        private float _currentSpeed;
        private Animator _animator; // cached — not searched every frame

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _animator = GetComponentInChildren<Animator>();
            if (_animator != null)
                Debug.Log($"[Kafka] Animator found: {_animator.runtimeAnimatorController?.name ?? "NO CONTROLLER"}");
            else
                Debug.LogWarning("[Kafka] No Animator found in children!");
        }

        private void Update()
        {
            if (target == null)
            {
                var playerGO = GameObject.FindWithTag("Player");
                if (playerGO != null) target = playerGO.transform;
                else return;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            // Speed calculation
            float targetSpeed;
            if (dist < stopDistance)
                targetSpeed = 0f;
            else if (dist > catchUpThreshold)
                targetSpeed = catchUpSpeed;
            else
                targetSpeed = maxSpeed;

            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * Time.deltaTime);

            // Movement
            if (dist > 0.05f && _currentSpeed > 0.01f)
            {
                Vector3 dir = toTarget.normalized;
                transform.position += dir * _currentSpeed * Time.deltaTime;
            }

            // ALWAYS face toward player (not movement direction)
            if (dist > 0.1f)
            {
                Vector3 lookDir = toTarget.normalized;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                }
            }

            // Animator: play walk when moving, slow/stop when idle
            if (_animator != null)
            {
                _animator.speed = _currentSpeed > 0.1f ? 1f : 0f;
            }
        }
    }
}

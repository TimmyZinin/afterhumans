using UnityEngine;

namespace Afterhumans.Kafka
{
    /// <summary>
    /// Kafka companion follow behavior. Walks to an offset point near the player
    /// (not directly behind), with reaction delay so the player sees her from
    /// different angles. Uses Animator Idle ↔ Walk states.
    /// </summary>
    public class KafkaFollowSimple : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Header("Behavior")]
        [SerializeField] private float stopDistance = 1.4f;
        [SerializeField] private float maxSpeed = 2.5f;
        [SerializeField] private float acceleration = 6f;
        [SerializeField] private float catchUpThreshold = 5f;
        [SerializeField] private float catchUpSpeed = 4.5f;

        [Header("Companion offset")]
        [SerializeField] private float sideOffset = 1.0f;       // how far to the right of player
        [SerializeField] private float behindOffset = 0.5f;      // how far behind player
        [SerializeField] private float reactionDelay = 0.6f;     // seconds before Kafka reacts to player movement
        [SerializeField] private float goalUpdateInterval = 0.8f; // how often goal point recalculates

        [Header("Body orientation")]
        [SerializeField] private float turnSpeed = 5f;

        public static KafkaFollowSimple Instance { get; private set; }

        private float _currentSpeed;
        private Animator _animator;
        private Vector3 _goalPoint;
        private float _goalTimer;
        private float _reactionTimer;
        private Vector3 _lastPlayerPos;
        private bool _playerMoving;

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

            _goalPoint = transform.position;
        }

        private void Update()
        {
            if (target == null)
            {
                var playerGO = GameObject.FindWithTag("Player");
                if (playerGO != null) target = playerGO.transform;
                else return;
            }

            float dt = Time.deltaTime;

            // Detect if player is moving
            Vector3 playerDelta = target.position - _lastPlayerPos;
            playerDelta.y = 0f;
            _playerMoving = playerDelta.magnitude > 0.01f;
            _lastPlayerPos = target.position;

            // Reaction delay: Kafka doesn't immediately chase
            if (_playerMoving)
                _reactionTimer = reactionDelay;

            if (_reactionTimer > 0f)
                _reactionTimer -= dt;

            // Update goal point periodically (offset to the right-behind of player)
            _goalTimer -= dt;
            if (_goalTimer <= 0f)
            {
                _goalTimer = goalUpdateInterval;
                UpdateGoalPoint();
            }

            // Distance to goal
            Vector3 toGoal = _goalPoint - transform.position;
            toGoal.y = 0f;
            float distToGoal = toGoal.magnitude;

            // Distance to player (for catch-up)
            Vector3 toPlayer = target.position - transform.position;
            toPlayer.y = 0f;
            float distToPlayer = toPlayer.magnitude;

            // Speed: stop near goal, normal speed when moving, catch up when far
            float targetSpeed;
            if (distToGoal < stopDistance && _reactionTimer <= 0f)
                targetSpeed = 0f;
            else if (distToPlayer > catchUpThreshold)
                targetSpeed = catchUpSpeed; // run directly to player when too far
            else if (_reactionTimer > 0f && distToGoal < stopDistance * 2f)
                targetSpeed = 0f; // wait during reaction delay
            else
                targetSpeed = maxSpeed;

            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * dt);

            // Movement toward goal (or directly to player if catching up)
            if (_currentSpeed > 0.01f)
            {
                Vector3 moveTarget = distToPlayer > catchUpThreshold ? toPlayer : toGoal;
                if (moveTarget.magnitude > 0.05f)
                {
                    Vector3 dir = moveTarget.normalized;
                    transform.position += dir * _currentSpeed * dt;
                }
            }

            // Face movement direction when walking, face player when idle
            Vector3 lookDir;
            if (_currentSpeed > 0.1f && toGoal.magnitude > 0.1f)
                lookDir = (distToPlayer > catchUpThreshold ? toPlayer : toGoal).normalized;
            else
                lookDir = toPlayer.normalized;

            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * dt);
            }

            // Animator
            if (_animator != null)
                _animator.SetBool("IsWalking", _currentSpeed > 0.1f);
        }

        private void UpdateGoalPoint()
        {
            if (target == null) return;

            // Place goal to the right and slightly behind the player
            Vector3 playerRight = target.right;
            Vector3 playerBack = -target.forward;

            _goalPoint = target.position
                + playerRight * sideOffset
                + playerBack * behindOffset;
            _goalPoint.y = transform.position.y; // stay on ground
        }
    }
}

using UnityEngine;

namespace Afterhumans.Kafka
{
    /// <summary>
    /// Lightweight companion follow behaviour for Kafka (the Welsh Corgi Cardigan).
    /// Walks toward the player with a side offset and stops within stopDistance —
    /// no NavMeshAgent required, so the walking skeleton works without baked NavMesh.
    ///
    /// Kafka is persistent across scenes via DontDestroyOnLoad. She appears in
    /// Botanika, City and Desert, and triggers Anna's memory moment in the City.
    /// Replace the placeholder cube with a CC0 corgi mesh once acquired — this
    /// script cares only about Transform movement, not visuals.
    /// </summary>
    public class KafkaFollowSimple : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Assigned at runtime from GameObject with tag=Player")]
        public Transform target;

        [Header("Behavior")]
        [SerializeField] private float followDistance = 2.2f;
        [SerializeField] private float stopDistance = 1.6f;
        [SerializeField] private float maxSpeed = 2.8f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float catchUpThreshold = 5f;
        [SerializeField] private float catchUpSpeed = 4.2f;

        [Header("Body orientation")]
        [SerializeField] private float turnSpeed = 6f;

        public static KafkaFollowSimple Instance { get; private set; }

        private float _currentSpeed;
        private Vector3 _velocity;

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

            float targetSpeed;
            if (dist < stopDistance)
            {
                targetSpeed = 0f;
            }
            else if (dist > catchUpThreshold)
            {
                targetSpeed = catchUpSpeed;
            }
            else
            {
                targetSpeed = maxSpeed;
            }

            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * Time.deltaTime);

            if (dist > 0.05f && _currentSpeed > 0.01f)
            {
                Vector3 dir = toTarget.normalized;
                transform.position += dir * _currentSpeed * Time.deltaTime;

                // Face movement direction
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }
        }
    }
}

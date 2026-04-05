using UnityEngine;
using UnityEngine.AI;

namespace Afterhumans.Kafka
{
    /// <summary>
    /// Makes Kafka (the black-and-white Welsh Corgi Cardigan companion) follow the player.
    /// Uses NavMeshAgent for natural pathfinding, with randomized offset from player position
    /// so she doesn't clip through player body. Occasionally pauses to "look around".
    ///
    /// Kafka is persistent across scenes via DontDestroyOnLoad, part of the core loop:
    /// she's present in Botanika, City, and Desert, and plays a key role in triggering
    /// Anna's memory moment in the City scene.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class KafkaFollow : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Usually the Player transform, assigned at runtime by KafkaSpawner")]
        public Transform target;

        [Header("Behavior")]
        [SerializeField] private float followDistance = 2.5f;
        [SerializeField] private float catchUpDistance = 6f;
        [SerializeField] private float stopDistance = 1.8f;
        [SerializeField] private float updateInterval = 0.3f;
        [SerializeField] private float sideOffsetRange = 1.2f;

        [Header("Speed")]
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float runSpeed = 3.5f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string barkTrigger = "Bark";

        private NavMeshAgent _agent;
        private float _nextUpdate;
        private Vector3 _sideOffset;
        private float _nextOffsetChange;

        public static KafkaFollow Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _agent = GetComponent<NavMeshAgent>();
            _agent.stoppingDistance = stopDistance;
            _agent.speed = walkSpeed;
            RandomizeOffset();
        }

        private void Update()
        {
            if (target == null)
            {
                // Try to find player on scene load
                var playerGO = GameObject.FindWithTag("Player");
                if (playerGO != null) target = playerGO.transform;
                else return;
            }

            if (Time.time >= _nextUpdate)
            {
                _nextUpdate = Time.time + updateInterval;
                UpdateDestination();
            }

            if (Time.time >= _nextOffsetChange)
            {
                _nextOffsetChange = Time.time + Random.Range(4f, 9f);
                RandomizeOffset();
            }

            if (animator != null)
            {
                animator.SetFloat(speedParam, _agent.velocity.magnitude);
            }
        }

        private void UpdateDestination()
        {
            if (_agent == null || !_agent.isOnNavMesh) return;

            float distance = Vector3.Distance(transform.position, target.position);

            // If player runs ahead, speed up
            _agent.speed = distance > catchUpDistance ? runSpeed : walkSpeed;

            // Position slightly offset from player to avoid clipping
            Vector3 destination = target.position + target.right * _sideOffset.x - target.forward * followDistance;
            _agent.SetDestination(destination);
        }

        private void RandomizeOffset()
        {
            _sideOffset = new Vector3(Random.Range(-sideOffsetRange, sideOffsetRange), 0, 0);
        }

        /// <summary>
        /// Trigger bark animation + sound. Called from special events (near server in desert).
        /// </summary>
        public void Bark()
        {
            if (animator != null)
            {
                animator.SetTrigger(barkTrigger);
            }
        }
    }
}

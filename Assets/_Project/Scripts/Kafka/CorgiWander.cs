using UnityEngine;
using UnityEngine.AI;

namespace Afterhumans.Kafka
{
    /// <summary>
    /// Future-movement foundation for the hero corgi in the Botanika scene.
    /// When no follow-target is active, the corgi wanders the baked NavMesh between
    /// random reachable points with natural idle pauses, staying off obstacles
    /// (column, furniture) via NavMeshAgent avoidance. Pair with a baked NavMesh
    /// (see Editor/BotanikaNavSetup) and, optionally, KafkaFollowSimple for the
    /// player-follow behaviour — this script yields while a target is set.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class CorgiWander : MonoBehaviour
    {
        [Header("Wander")]
        public float wanderRadius = 6f;      // how far a new goal can be from the home anchor
        public float arriveDistance = 0.4f;
        public Vector2 idlePauseRange = new Vector2(2.5f, 6f);
        public float repathTimeout = 12f;    // give up on an unreachable goal

        [Header("Optional follow override")]
        public Transform followTarget;       // if set, wander yields and the corgi follows

        private NavMeshAgent _agent;
        private Vector3 _home;
        private float _stateTimer;
        private float _goalTimer;
        private bool _idling;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _home = transform.position;
        }

        private void OnEnable()
        {
            // snap onto the navmesh if placed slightly off it
            if (_agent != null && !_agent.isOnNavMesh &&
                NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
            }
            PickNewGoal();
        }

        private void Update()
        {
            if (_agent == null || !_agent.isOnNavMesh) return;

            if (followTarget != null)
            {
                _agent.SetDestination(followTarget.position);
                return;
            }

            _goalTimer += Time.deltaTime;

            if (_idling)
            {
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f) { _idling = false; PickNewGoal(); }
                return;
            }

            bool arrived = !_agent.pathPending &&
                           _agent.remainingDistance <= Mathf.Max(arriveDistance, _agent.stoppingDistance);
            if (arrived || _goalTimer > repathTimeout)
            {
                _idling = true;
                _stateTimer = Random.Range(idlePauseRange.x, idlePauseRange.y);
            }
        }

        private void PickNewGoal()
        {
            _goalTimer = 0f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 probe = _home + new Vector3(
                    Random.Range(-wanderRadius, wanderRadius), 0f,
                    Random.Range(-wanderRadius, wanderRadius));
                if (NavMesh.SamplePosition(probe, out var hit, 2f, NavMesh.AllAreas))
                {
                    _agent.SetDestination(hit.position);
                    return;
                }
            }
        }
    }
}

using UnityEngine;
using UnityEngine.AI;

namespace ChaosDraftParty.UI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class WanderAI : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] float wanderRadius = 5f;
        [SerializeField] float minIdleSeconds = 1.5f;
        [SerializeField] float maxIdleSeconds = 5f;
        [SerializeField] float arriveDistance = 0.35f;
        [SerializeField] float specialActionChance = 0.25f;
        [SerializeField] string speedParameter = "Speed";
        [SerializeField] string[] specialTriggers = { "Wave", "Cheer", "Trip" };

        NavMeshAgent agent;
        Vector3 origin;
        float nextMoveAt;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            origin = transform.position;
        }

        void Start()
        {
            PickNewDestination();
        }

        void Update()
        {
            if (agent == null) return;

            float speed = agent.velocity.magnitude;
            if (animator != null && !string.IsNullOrEmpty(speedParameter))
            {
                animator.SetFloat(speedParameter, speed);
            }

            if (agent.pathPending) return;
            if (agent.remainingDistance > arriveDistance) return;
            if (Time.time < nextMoveAt) return;

            if (animator != null && specialTriggers.Length > 0 && Random.value < specialActionChance)
            {
                var trig = specialTriggers[Random.Range(0, specialTriggers.Length)];
                animator.SetTrigger(trig);
                nextMoveAt = Time.time + Random.Range(minIdleSeconds + 0.5f, maxIdleSeconds + 1f);
                return;
            }

            PickNewDestination();
        }

        void PickNewDestination()
        {
            for (int attempts = 0; attempts < 5; attempts++)
            {
                var target = origin + Random.insideUnitSphere * wanderRadius;
                target.y = origin.y;
                if (NavMesh.SamplePosition(target, out var hit, wanderRadius, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                    nextMoveAt = Time.time + Random.Range(minIdleSeconds, maxIdleSeconds);
                    return;
                }
            }
            nextMoveAt = Time.time + minIdleSeconds;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.75f, 0.3f, 0.4f);
            Gizmos.DrawWireSphere(Application.isPlaying ? origin : transform.position, wanderRadius);
        }
    }
}

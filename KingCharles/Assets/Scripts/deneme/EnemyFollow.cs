using UnityEngine;
using UnityEngine.AI;

public class EnemyFollow : MonoBehaviour
{
    public Transform target;   // Oyuncu
    public float stoppingDistance = 1.5f;

    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }

        if (agent != null)
            agent.stoppingDistance = stoppingDistance;
    }

    private void Update()
    {
        if (agent == null || target == null) return;

        agent.SetDestination(target.position);
    }
}

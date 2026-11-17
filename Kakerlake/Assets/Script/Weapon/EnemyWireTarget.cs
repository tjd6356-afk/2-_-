using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyWireTarget : MonoBehaviour
{
    [SerializeField]
    private EnemyAI enemyAI;

    [SerializeField]
    private NavMeshAgent agent;

    [SerializeField]
    private float throwDuration = 0.7f;


    private bool controlled;


    private void Awake()
    {
        if (enemyAI == null)
        {
            enemyAI =
                GetComponent<EnemyAI>();
        }


        if (agent == null)
        {
            agent =
                GetComponent<NavMeshAgent>();
        }
    }


    public void BeginGrab()
    {
        controlled = true;


        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }


        if (agent != null &&
            agent.enabled)
        {
            agent.enabled = false;
        }
    }


    public void PullTowards(
        Vector3 target,
        float speed
    )
    {
        if (!controlled)
            return;


        transform.position =
            Vector3.MoveTowards(
                transform.position,
                target,
                speed *
                Time.deltaTime
            );
    }


    public void ReleaseGrab()
    {
        controlled = false;

        RestoreAI();
    }


    public void Throw(
        Vector3 direction,
        float speed
    )
    {
        StopAllCoroutines();

        StartCoroutine(
            ThrowRoutine(
                direction,
                speed
            )
        );
    }


    private IEnumerator ThrowRoutine(
        Vector3 direction,
        float speed
    )
    {
        controlled = true;


        Vector3 velocity =
            direction.normalized *
            speed;


        float timer = 0f;


        while (timer <
               throwDuration)
        {
            velocity +=
                Physics.gravity *
                Time.deltaTime;


            transform.position +=
                velocity *
                Time.deltaTime;


            timer +=
                Time.deltaTime;


            yield return null;
        }


        controlled = false;

        RestoreAI();
    }


    private void RestoreAI()
    {
        if (agent != null)
        {
            if (NavMesh.SamplePosition(
                    transform.position,
                    out NavMeshHit hit,
                    4f,
                    NavMesh.AllAreas))
            {
                transform.position =
                    hit.position;


                agent.enabled =
                    true;


                agent.Warp(
                    hit.position
                );
            }
        }


        if (enemyAI != null)
        {
            enemyAI.enabled = true;
        }
    }
}
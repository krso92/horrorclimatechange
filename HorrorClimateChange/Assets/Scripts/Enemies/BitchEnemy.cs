using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BitchEnemy : MonoBehaviour, IEnemy
{
    [SerializeField]
    private EnemyState currentState;

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private NavMeshAgent agent;

    [SerializeField]
    private Transform walkPointsPool;    

    private Vector3 chosenPoint;

    private bool followPlayer;

    private Vector3 GetRandomPoint
    {
        get
        {
            int i = Random.Range(0, walkPointsPool.childCount);
            return walkPointsPool.GetChild(i).position;
        }
    }

    // interface impl

    public void Flashlighted()
    {
        Debug.Log("flashlight");
    }

    public void SuperFlashlighted()
    {
        Debug.Log("super flashlight");
    }

    // Logic


    // Unity msgs

    // Start is called before the first frame update
    void Start()
    {
        chosenPoint = GetRandomPoint;;
        agent.destination = chosenPoint;
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, chosenPoint) < 1)
        {
            chosenPoint = GetRandomPoint;
            agent.destination = chosenPoint;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        audioSource.Play();
        agent.destination = other.transform.position;
    }
}

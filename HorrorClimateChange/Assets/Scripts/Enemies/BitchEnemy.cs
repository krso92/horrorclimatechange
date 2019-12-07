using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BitchEnemy : MonoBehaviour, IEnemy
{
    [SerializeField]
    private EnemyState currentState;

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

    private void ProcessState()
    {
        switch(currentState)
        {
            case EnemyState.Idle:
                break;
            case EnemyState.Roaming:
                throw new System.NotImplementedException();
            case EnemyState.Attacking:
                break;
            case EnemyState.RunningAway:
                break;
        }
    }

    private void Startle()
    {
        currentState = EnemyState.Attacking;
    }

    private void DoAttack()
    {

    }

    private void DoRunAway()
    {
    }

    // Unity msgs

    // Start is called before the first frame update
    void Start()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        agent.destination = GameObject.Find("GOTO").transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        // ako udje player u enemijev trigger
        // promeni stanje
        // ako su uslovi za startle ispunjeni
        Startle();
    }
}

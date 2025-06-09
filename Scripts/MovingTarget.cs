// using UnityEngine;
// using UnityEngine.AI;

// public class MovingTarget : MonoBehaviour
// {
//     public float moveSpeed = 3f;
//     public float moveDuration = 10f;
//     public float idleDuration = 20f;
//     public float secondMoveDuration = 15f;
//     public float moveRadius = 60f;

//     private enum TargetState { Idle, Moving, SecondMoving }
//     private TargetState state = TargetState.Idle;
//     private float timer = 0f;
//     private NavMeshAgent agent;

//     void Start()
//     {
//         agent = GetComponent<NavMeshAgent>();
//         agent.speed = moveSpeed;
//         agent.isStopped = true;
//         state = TargetState.Idle;
//         timer = 0f;
//     }

//     void Update()
//     {
//         timer += Time.deltaTime;

//         switch (state)
//         {
//             case TargetState.Idle:
//                 agent.isStopped = true;
//                 if (timer >= idleDuration)
//                 {
//                     timer = 0f;
//                     OnNotFound();
//                 }
//                 break;
//             case TargetState.Moving:
//                 agent.isStopped = false;
//                 if (timer >= moveDuration)
//                 {
//                     timer = 0f;
//                     state = TargetState.Idle;
//                 }
//                 break;
//             case TargetState.SecondMoving:
//                 agent.isStopped = false;
//                 if (timer >= secondMoveDuration)
//                 {
//                     timer = 0f;
//                     state = TargetState.Idle;
//                 }
//                 break;
//         }
//     }

//     public void OnFound()
//     {
//         RespawnRandomly();
//         state = TargetState.Idle;
//         timer = 0f;
//     }

//     public void OnNotFound()
//     {
//         timer = 0f;
//         state = TargetState.SecondMoving;
//         MoveToRandomPosition();
//     }

//     private void MoveToRandomPosition()
//     {
//         Vector3 randomDirection = Random.insideUnitSphere * moveRadius;
//         randomDirection.y = 0;
//         Vector3 targetPos = transform.position + randomDirection;
//         NavMeshHit hit;
//         if (NavMesh.SamplePosition(targetPos, out hit, moveRadius, NavMesh.AllAreas))
//         {
//             agent.SetDestination(hit.position);
//         }
//     }

//     private void RespawnRandomly()
//     {
//         transform.position = SearchAgentTeam.GetRandomNavMeshPosition();
//         agent.isStopped = true;
//     }
// }
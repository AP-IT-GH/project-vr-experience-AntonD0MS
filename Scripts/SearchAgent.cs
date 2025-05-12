using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.IO.Abstractions;

public class SearchAgent : Agent
{

    /*[SerializeField]
    private float moveSpeed = 5f;*/

    [SerializeField]
    private Transform targetPosition;

    [SerializeField]
    //private GameObject checkPoint;

    //private bool touched = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
      /*  if(transform.localPosition.y < -1){
            EndEpisode();
        }*/
    }


    public override void CollectObservations(VectorSensor sensor){ 
        // Agent positie
        sensor.AddObservation(transform.localPosition);
    }

    public float speedMultiplier = 0.5f; // 0.5f
    public float rotationMultiplier = 5f;

    public override void OnActionReceived(ActionBuffers actionBuffers){
        // Acties, size = 2
        Debug.Log("Acties ontvangen!");

        Vector3 controlSignal = Vector3.zero;
        controlSignal.z = actionBuffers.ContinuousActions[0];
        transform.Translate(controlSignal * speedMultiplier);
        transform.Rotate(0.0f, rotationMultiplier* actionBuffers.ContinuousActions[1], 0.0f);
        // Beloningen
        float distanceToTarget = Vector3.Distance(transform.localPosition, targetPosition.localPosition);
        // target bereikt
        if (distanceToTarget < 0.5f /*&& !touched*/) {
            SetReward(1.0f);
            //touched = true;
            targetPosition.gameObject.SetActive(false);
            //Destroy(targetPosition.gameObject);
            //EndEpisode();
        }
        // if (touched){
        //     float distanceToCheckPoint = Vector3.Distance(transform.localPosition, checkPoint.transform.localPosition);
        //     if (distanceToCheckPoint < 1.42f)
        //     {
        //         SetReward(1.0f);
        //         EndEpisode();
        //     }
        // }
    
        if (transform.localPosition.y < -1){
            SetReward(-1f);
            EndEpisode();
        }

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy")){
            SetReward(1f);
            EndEpisode();            
        }
        // if (collision.gameObject.CompareTag("Checkpoint") && touched)
        // {
        //     SetReward(1f);
        //     EndEpisode();
        // }
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("Episode gestart!");

        InitializeEnvironment();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.Log("Heuristic mode actief!");

        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
    }

    private void InitializeEnvironment(){
        /*float randomX = Random.Range(-4.5f, 4.5f);
        float randomZ = Random.Range(-4.5f, 4.5f);
        targetPosition.localPosition = new Vector3(randomX, 0.5f, randomZ);*/
        targetPosition.gameObject.SetActive(true);

        if (transform.localPosition.y < -1){
            transform.localPosition = new Vector3(-15,0,10);
            transform.localRotation = Quaternion.identity;
            }

        // verplaats de target naar een nieuwe willekeurige locatie 
        targetPosition.localPosition = new Vector3(Random.value * 8 - 4,1.5f,Random.value * 8 - 4);

        //touched = false;
    }


}




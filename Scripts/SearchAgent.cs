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
    private Rigidbody rb;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
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
        // sensor.AddObservation(transform.forward);

        // sensor.AddObservation(targetPosition.localPosition);
        Vector3 toEnemy = (targetPosition.position - transform.position).normalized;
        sensor.AddObservation(toEnemy);

    }

    public float speedMultiplier = 10f; // 0.5f
    public float rotationMultiplier = 5f;

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        //Debug.Log("Acties ontvangen!");

        // AddReward(-0.001f); // Kleine straf per stap, zo blijft de agent niet doelloos rondlopen

        float currentDistance = Vector3.Distance(transform.localPosition, targetPosition.position);
        float delta = previousDistance - currentDistance;
        AddReward(delta * 0.2f);
        previousDistance = currentDistance;


        // Vector3 toTarget = (targetPosition.position - transform.position).normalized;
        // float alignment = Vector3.Dot(transform.forward, toTarget);
        // AddReward(alignment * 0.01f);



        Vector3 controlSignal = Vector3.zero;
        controlSignal.z = actionBuffers.ContinuousActions[0];
        // transform.Translate(controlSignal * speedMultiplier);
        // transform.Rotate(0.0f, rotationMultiplier* actionBuffers.ContinuousActions[1], 0.0f);
        Vector3 movement = transform.forward * controlSignal.z * speedMultiplier;
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);

        float rotation = rotationMultiplier * actionBuffers.ContinuousActions[1];
        Quaternion turn = Quaternion.Euler(0.0f, rotation, 0.0f);
        rb.MoveRotation(rb.rotation * turn);
        // Beloningen
        float distanceToTarget = Vector3.Distance(transform.localPosition, targetPosition.localPosition);
        // target bereikt

        if (transform.localPosition.y < -1)
        {
            SetReward(-1f);
            EndEpisode();
        }
        // if (StepCount >= MaxStep && MaxStep > 0)
        // {
        //     SetReward(-2f);
        //     EndEpisode();
        // }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy")){
            SetReward(3f);
            EndEpisode();            
        }
        if (collision.gameObject.CompareTag("Wall")) {
            AddReward(-0.05f);
        }
    }

    private float previousDistance;
    public override void OnEpisodeBegin()
    {
        //Debug.Log("Episode gestart!");

        InitializeEnvironment();
        previousDistance = Vector3.Distance(transform.localPosition, targetPosition.position);
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
            transform.localPosition = new Vector3(0,0,3);
            transform.localRotation = Quaternion.identity;
            }

        // verplaats de target naar een nieuwe willekeurige locatie 
        // targetPosition.localPosition = new Vector3(Random.value * 8 - 4,1.5f,Random.value * 8 - 4);
        // targetPosition.position = new Vector3(Random.Range(-12f, 12f), 1.5f, Random.Range(-12f, 12f));
        targetPosition.position = new Vector3(Random.Range(-24f, 24f), 1.5f, Random.Range(-24f, 24f));

        //touched = false;
    }



}




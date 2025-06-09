using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.IO.Abstractions;
using UnityEngine.AI;


public class SearchAgentTeam : Agent
{

    /*[SerializeField]
    private float moveSpeed = 5f;*/

    [SerializeField]
    private Transform targetPosition;

    private Rigidbody rb;

    [SerializeField] private float maxSearchDistance = 160f;  // Diagonal of your search area
    [SerializeField] private float maxTeamDistance = 100f;

    [SerializeField] private int agentID;
    private Vector3 assignedSearchZone;
    private bool hasAssignedZone = false;

    [SerializeField] private ZoneStrategy zoneStrategy = ZoneStrategy.AdaptiveQuadrants;
    public enum ZoneStrategy
    {
        Quadrants,
        Strips,
        RadialSectors,
        AdaptiveQuadrants
    }


    private float episodeTimer = 0f;
    public float maxTimeReward = 1f;
    public float maxEpisodeTime = 20f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate; // Voorkomt tunneling
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Betere collision detection

    }

    // Update is called once per frame
    void Update()
    {
        /*  if(transform.localPosition.y < -1){
              EndEpisode();
          }*/

        if (hasAssignedZone && assignedSearchZone != Vector3.zero)
        {
            // Teken lijn naar zone
            Debug.DrawLine(transform.position, assignedSearchZone, Color.red, 0.1f);

            // Teken een kruis op de zone locatie voor betere visualisatie
            Vector3 zonePos = assignedSearchZone;
            Debug.DrawLine(zonePos + Vector3.left * 2f, zonePos + Vector3.right * 2f, Color.yellow, 0.1f);
            Debug.DrawLine(zonePos + Vector3.forward * 2f, zonePos + Vector3.back * 2f, Color.yellow, 0.1f);

            // Teken cirkel rond de zone
            DrawCircle(zonePos, 5f, Color.green);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent state (3 + 3 = 6 observations)
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(rb.linearVelocity);

        // sensor.AddObservation(transform.forward);

        // sensor.AddObservation(targetPosition.localPosition);

        // Vector3 toEnemy = (targetPosition.position - transform.position).normalized;
        // sensor.AddObservation(toEnemy);

        // Target information (3 + 1 = 4 observations)
        Vector3 toTarget = targetPosition.position - transform.position;
        sensor.AddObservation(toTarget.normalized);
        sensor.AddObservation(toTarget.magnitude / maxSearchDistance);

        // Assigned search zone information (4 observations)
        if (hasAssignedZone)
        {
            Vector3 toZone = assignedSearchZone - transform.position;
            sensor.AddObservation(toZone.normalized);
            sensor.AddObservation(toZone.magnitude / maxSearchDistance);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }

        // Team coordination (for each teammate: 3 + 1 = 4 per teammate)
        foreach (var teammate in TeamManager.Instance.GetAgents())
        {
            if (teammate != this)
            {
                Vector3 toMate = teammate.transform.position - transform.position;
                sensor.AddObservation(toMate.normalized);
                sensor.AddObservation(toMate.magnitude / maxTeamDistance);
            }
        }

        // Search area coverage (add grid-based exploration tracking)
        //sensor.AddObservation(GetExplorationGrid());


        // foreach (var teammate in TeamManager.Instance.GetAgents())
        // {
        //     if (teammate != this)
        //     {
        //         Vector3 toMate = (teammate.transform.position - transform.position).normalized;
        //         sensor.AddObservation(toMate);
        //         sensor.AddObservation(Vector3.Distance(transform.position, teammate.transform.position));
        //     }
        // }

    }

    public float speedMultiplier = 8f; // 0.5f
    public float rotationMultiplier = 5f;

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        //Debug.Log("Acties ontvangen!");

        AddReward(-0.001f); // Kleine straf per stap, zo blijft de agent niet doelloos rondlopen

        // float currentDistance = Vector3.Distance(transform.localPosition, targetPosition.position);
        // float delta = previousDistance - currentDistance;
        // //Debug.Log(delta);
        // AddReward(delta * 0.5f);
        // previousDistance = currentDistance;

        float pathDistanceToTarget = GetPathDistanceToTarget();
        float deltaPath = previousPathDistance - pathDistanceToTarget;
        AddReward(deltaPath * 0.1f);
        previousPathDistance = pathDistanceToTarget;

        // float distanceToTarget = Vector3.Distance(transform.position, targetPosition.position);
        // float normalizedDistance = distanceToTarget / maxSearchDistance;
        // AddReward(-normalizedDistance * 0.001f);

        // if (previousDistance > 0)
        // {
        //     float deltaDistance = previousDistance - distanceToTarget;
        //     AddReward(deltaDistance * 0.01f);
        // }
        // previousDistance = distanceToTarget;

        if (hasAssignedZone)
        {
            float distanceToZone = Vector3.Distance(transform.position, assignedSearchZone);
            float normalizedZoneDistance = distanceToZone / maxSearchDistance;

            AddReward(-normalizedZoneDistance * 0.002f);

            if (distanceToZone < 20f)
            {
                AddReward(0.005f);
            }
        }

        float teamSpread = CalculateTeamSpread();
        if (teamSpread < 20f)
        {
            AddReward(-0.001f);
        }

        float movementMagnitude = Mathf.Abs(actionBuffers.ContinuousActions[0]) + Mathf.Abs(actionBuffers.ContinuousActions[1]);
        if (movementMagnitude < 0.1f)
            AddReward(-0.001f); // Stronger penalty for not moving
        else
        {
            AddReward(0.0001f);
        }

        RewardExplorationPattern();

        Vector3 controlSignal = Vector3.zero;
        controlSignal.z = actionBuffers.ContinuousActions[0];
        // transform.Translate(controlSignal * speedMultiplier);
        // transform.Rotate(0.0f, rotationMultiplier* actionBuffers.ContinuousActions[1], 0.0f);
        Vector3 movement = transform.forward * controlSignal.z * speedMultiplier;
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
        //rb.AddForce(movement, ForceMode.VelocityChange);

        float rotation = rotationMultiplier * actionBuffers.ContinuousActions[1];
        Quaternion turn = Quaternion.Euler(0.0f, rotation, 0.0f);
        rb.MoveRotation(rb.rotation * turn);

        RewardObstacleNavigation();

        episodeTimer += Time.fixedDeltaTime;

        // float distanceToTarget = Vector3.Distance(transform.localPosition, targetPosition.localPosition);

        // target bereikt
        //Debug.Log(transform.localPosition.y);
        //Debug.Log("Plane y-pos: " + GameObject.Find("PlaneNaam").transform.position.y);

        // if (Vector3.Distance(transform.position, targetPosition.position) < 5f)
        // {
        //     AddReward(0.01f);
        // }

        // if (actionBuffers.ContinuousActions[0] == 0 && actionBuffers.ContinuousActions[1] == 0)
        // {
        //     AddReward(-0.001f); // niet bewegen = strafje
        // }



        if (transform.localPosition.y < -2)
        {
            TeamManager.Instance.GiveTeamReward(-1f);
            TeamManager.Instance.EndTeamEpisode();
        }
        // if (StepCount >= MaxStep && MaxStep > 0)
        // {
        //     SetReward(-2f);
        //     EndEpisode();
        // }
    }

    private void RewardExplorationPattern()
    {
        var teammates = TeamManager.Instance.GetAgents();

        // Beloon spreiding over de kaart
        foreach (var teammate in teammates)
        {
            if (teammate != this)
            {
                float distance = Vector3.Distance(transform.position, teammate.transform.position);

                // Beloon voor goede afstand houden
                // if (distance >= 20f && distance <= 50f)
                // {
                //     AddReward(0.002f);
                // }
                // Straf voor te dicht bij elkaar
                if (distance < 15f)
                {
                    AddReward(-0.005f);
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (hasAssignedZone)
            {
                float distanceToZone = Vector3.Distance(transform.position, assignedSearchZone);
                if (distanceToZone < 30f)
                {
                    AddReward(0.5f);
                }
            }

            float timeReward = Mathf.Clamp01(1f - (episodeTimer / maxEpisodeTime)) * maxTimeReward;
            AddReward(timeReward);

            TeamManager.Instance.GiveTeamReward(2f);
            TeamManager.Instance.EndTeamEpisode();

            //collision.gameObject.GetComponent<MovingTarget>()?.OnFound();
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.05f);
        }
    }

    // private float previousDistance;
    private float previousPathDistance;
    public override void OnEpisodeBegin()
    {
        //Debug.Log("Episode gestart!");

        episodeTimer = 0f;

        TeamManager.Instance.RegisterAgent(this);
        AssignSearchZone();

        InitializeEnvironment();
        // previousDistance = Vector3.Distance(transform.localPosition, targetPosition.position);
        previousPathDistance = GetPathDistanceToTarget();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.Log("Heuristic mode actief!");

        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
    }




    private void InitializeEnvironment()
    {
        /*float randomX = Random.Range(-4.5f, 4.5f);
        float randomZ = Random.Range(-4.5f, 4.5f);
        targetPosition.localPosition = new Vector3(randomX, 0.5f, randomZ);*/
        targetPosition.gameObject.SetActive(true);

        if (transform.localPosition.y < -2)
        {
            // transform.localPosition = new Vector3(0, 1, 3);
            // transform.localRotation = Quaternion.identity;

            SetSpawnPosition();
        }

        // verplaats de target naar een nieuwe willekeurige locatie 
        // targetPosition.localPosition = new Vector3(Random.value * 8 - 4,1.5f,Random.value * 8 - 4);
        // targetPosition.position = new Vector3(Random.Range(-12f, 12f), 1.5f, Random.Range(-12f, 12f));
        // targetPosition.position = new Vector3(Random.Range(-24f, 24f), 1.5f, Random.Range(-24f, 24f));


        Vector3 randomNavMeshPos;
        bool validPositionFound = false;

        for (int i = 0; i < 10; i++) // probeer max 10 keer een geldige plek te vinden
        {
            Vector3 randomPoint = new Vector3(
                Random.Range(-48f, 48f),
                0f,
                Random.Range(-48f, 48f)
            );

            NavMeshHit hit;
            int walkableMask = NavMesh.GetAreaFromName("Walkable");
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.5f, 1 << walkableMask))
            //if (NavMesh.SamplePosition(randomPoint, out hit, 1.5f, NavMesh.AllAreas))
            {
                randomNavMeshPos = hit.position;
                targetPosition.position = randomNavMeshPos + Vector3.up * 1.5f; // optillen zodat hij niet in de grond zit
                validPositionFound = true;
                break;
            }
        }

        if (!validPositionFound)
        {
            Debug.LogWarning("Kon geen geldige spawnpositie vinden voor het target op de NavMesh.");
            targetPosition.position = new Vector3(0f, 1.5f, 0f);
        }

    }

    private void SetSpawnPosition()
    {
        var agents = TeamManager.Instance.GetAgents();
        int myIndex = agents.IndexOf(this);

        Vector3 spawnPosition;
        switch (myIndex)
        {
            case 0:
                spawnPosition = new Vector3(-20f, 1f, -20f);
                break;
            case 1:
                spawnPosition = new Vector3(20f, 1f, -20f);
                break;
            case 2:
                spawnPosition = new Vector3(-20f, 1f, 20f);
                break;
            case 3:
                spawnPosition = new Vector3(20f, 1f, 20f);
                break;
            default:
                // Voor meer dan 4 agents - circulaire spreiding
                float angle = (360f / agents.Count) * myIndex * Mathf.Deg2Rad;
                float spawnRadius = 25f;
                spawnPosition = new Vector3(
                    Mathf.Cos(angle) * spawnRadius,
                    1f,
                    Mathf.Sin(angle) * spawnRadius
                );
                break;
        }

        transform.localPosition = spawnPosition;
        transform.localRotation = Quaternion.identity;

        Debug.Log($"Agent {myIndex} spawned at: {spawnPosition}");
    }

    private float CalculateTeamSpread()
    {
        float totalDistance = 0f;
        var teammates = TeamManager.Instance.GetAgents();

        foreach (var teammate in teammates)
        {
            if (teammate != this)
                totalDistance += Vector3.Distance(transform.position, teammate.transform.position);
        }

        return totalDistance / (teammates.Count - 1);
    }

    private void DrawCircle(Vector3 center, float radius, Color color)
    {
        int segments = 16;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);

            Debug.DrawLine(point1, point2, color, 0.1f);
        }
    }

    private void AssignSearchZone()
    {
        var agents = TeamManager.Instance.GetAgents();
        int myIndex = agents.IndexOf(this);

        if (agents.Count == 0)
        {
            Debug.LogWarning($"Geen agents gevonden in TeamManager voor zone assignment!");
            return;
        }

        // VERBETERDE zone toewijzing - meer gespreid over de kaart
        switch (myIndex)
        {
            case 0:
                assignedSearchZone = new Vector3(-35f, 0, -35f); // Northwest
                Debug.Log($"Agent {myIndex} (ID: {gameObject.name}) assigned to NORTHWEST zone: {assignedSearchZone}");
                break;
            case 1:
                assignedSearchZone = new Vector3(35f, 0, -35f);  // Northeast
                Debug.Log($"Agent {myIndex} (ID: {gameObject.name}) assigned to NORTHEAST zone: {assignedSearchZone}");
                break;
            case 2:
                assignedSearchZone = new Vector3(-35f, 0, 35f);  // Southwest
                Debug.Log($"Agent {myIndex} (ID: {gameObject.name}) assigned to SOUTHWEST zone: {assignedSearchZone}");
                break;
            case 3:
                assignedSearchZone = new Vector3(35f, 0, 35f);   // Southeast
                Debug.Log($"Agent {myIndex} (ID: {gameObject.name}) assigned to SOUTHEAST zone: {assignedSearchZone}");
                break;
            default:
                // Voor meer dan 4 agents
                float angle = (360f / agents.Count) * myIndex * Mathf.Deg2Rad;
                float radius = 40f;
                assignedSearchZone = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                Debug.Log($"Agent {myIndex} (ID: {gameObject.name}) assigned to RADIAL zone: {assignedSearchZone}");
                break;
        }

        hasAssignedZone = true;
        Debug.Log($"Zone assignment complete for {gameObject.name}:");
        Debug.Log($"  - Agent Index: {myIndex}");
        Debug.Log($"  - Total Agents: {agents.Count}");
        Debug.Log($"  - Assigned Zone: {assignedSearchZone}");
        Debug.Log($"  - Current Position: {transform.position}");
    }

    private void RewardObstacleNavigation()
    {
        float closestObstacleDistance = GetClosestObstacleDistance();

        if (closestObstacleDistance < 1f)
        {
            AddReward(-0.01f * (1f - closestObstacleDistance));
        }
        else if (closestObstacleDistance > 3f && closestObstacleDistance < 8f)
        {
            AddReward(0.001f);
        }
    }

    private float GetClosestObstacleDistance()
    {
        float minDistance = float.MaxValue;
        float maxRayDistance = 100f;
        int raysPerDirection = 5;
        float maxDegrees = 80f;

        for (int i = 0; i < raysPerDirection; i++)
        {
            float angle = -maxDegrees / 2f + (maxDegrees / (raysPerDirection - 1)) * i;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, maxRayDistance))
            {
                if (hit.collider.CompareTag("Wall"))
                {
                    minDistance = Mathf.Min(minDistance, hit.distance);
                }
            }
        }

        return minDistance == float.MaxValue ? maxRayDistance : minDistance;
    }
    
    private float GetPathDistanceToTarget()
    {
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(transform.position, targetPosition.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            float distance = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            return distance;
        }
        return maxSearchDistance; // Grote waarde als er geen pad is
    }

    // public static Vector3 GetRandomNavMeshPosition(float rangeY = 1.5f)
    // {
    //     for (int i = 0; i < 10; i++)
    //     {
    //         Vector3 randomPoint = new Vector3(
    //             Random.Range(-48f, 48f),
    //             0f,
    //             Random.Range(-48f, 48f)
    //         );

    //         NavMeshHit hit;
    //         int walkableMask = 1 << NavMesh.GetAreaFromName("Walkable");
    //         if (NavMesh.SamplePosition(randomPoint, out hit, 48f, walkableMask))
    //         {
    //             return hit.position + Vector3.up * rangeY;
    //         }
    //     }
    //     // fallback
    //     return new Vector3(0f, rangeY, 0f);
    // }

    
}
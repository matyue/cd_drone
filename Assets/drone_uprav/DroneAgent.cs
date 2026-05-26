using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class DroneAgent : Agent
{
    public QuadcopterPhysics quadcopter;
    private Vector3 base_drone_pos;
    public SignalDetector signalDetector;

    [Header("Target")]
    public Transform target;
    public Transform target2;
    public Vector3 base_target_pos;

    private Rigidbody rb;
    private ConnectivityNode connectivity;


    private float[] connectionTimer = new float[2] { 0, 0 };
    private int episode = 0;
    private float reward = 0f;

    const int MaxContacts = 4;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        connectivity = GetComponent<ConnectivityNode>();
        base_drone_pos = rb.position;
        episode = 0;
        reward = 0;
        base_target_pos = target.position;
    }

    public override void OnEpisodeBegin()
    {


        episode++;
        Debug.Log($"EPISODE: {episode} ");
        reward = 0;
        connectionTimer[0] = 0f;
        connectionTimer[1] = 0f;

        quadcopter.SetInputs(
            0f,
            0f,
            0f,
            0f
        );

        Quaternion randomRotation = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up);

        quadcopter.ResetPhysics(base_drone_pos, randomRotation);

        target.GetComponent<MovingTarget>().UpdateCenter(base_target_pos + new Vector3(
            Random.Range(-5f, 5f),
            Random.Range(0f, 4f),
            Random.Range(-5f, 5f)
        ));
        target2.GetComponent<MovingTarget>().UpdateCenter(target2.GetComponent<MovingTarget>().center);


    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 relativeTarget = target.position - transform.position;


        sensor.AddObservation(rb.linearVelocity);
        sensor.AddObservation(rb.angularVelocity);
        sensor.AddObservation(transform.up);

        //для лучей рельефа
        sensor.AddObservation(0f);
        sensor.AddObservation(0f);

        for (int i = 0; i < MaxContacts; i++)
        {

            var best = signalDetector.GetBestContact(i);

            if (best != null)
            {
                Vector3 localDir =
                    transform.InverseTransformDirection(
                        best.transform.position - transform.position
                    ).normalized;
                //Debug.Log($"{i} {best.signalStrength}");
                sensor.AddObservation(localDir);
                sensor.AddObservation(best.signalStrength);
                sensor.AddObservation(best.flag / 3f); //флаг цели

            }
            else
            {
                //Debug.Log("no target");
                sensor.AddObservation(Vector3.zero);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }

        sensor.AddObservation(connectivity.CurrentFlag / 3f); // собственный флаг
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float throttle = actions.ContinuousActions[0];

        float pitch = actions.ContinuousActions[1];

        float roll = actions.ContinuousActions[2];

        float yaw = actions.ContinuousActions[3];

        quadcopter.SetInputs(
            throttle,
            pitch,
            roll,
            yaw
        );

        if (float.IsNaN(transform.position.x) ||
    float.IsNaN(transform.position.y) ||
    float.IsNaN(transform.position.z))
        {
            Debug.LogError("NaN position");
            EndEpisode();
        }

        if (float.IsNaN(rb.linearVelocity.x) ||
    float.IsNaN(rb.angularVelocity.x))
        {
            Debug.LogError("NaN velocity");
            EndEpisode();
        }

        if (rb.linearVelocity.magnitude > 50f)
        {
            AddReward(-1f);
            EndEpisode();
        }

        if (rb.angularVelocity.magnitude > 20f)
        {
            AddReward(-1f);
            EndEpisode();
        }


        float upDot = Vector3.Dot(transform.up, Vector3.up);
        if (upDot < -0.5f)
        {
            AddReward(-2f);
            EndEpisode();
        }
        if (transform.position.y < -5.75f)
        {
            AddReward(-2f);
            EndEpisode();
        }

        /*
        var a = signalDetector.GetBestContact(0);
        var b = signalDetector.GetBestContact(1);

        if (a != null && b != null)
        {
            AddReward(a.signalStrength * b.signalStrength * 0.04f);

            connectionTimer[0] += Time.fixedDeltaTime;
            connectionTimer[1] += Time.fixedDeltaTime;
        }
        else
        {
            AddReward(-0.01f);
            connectionTimer[0] = 0f;
            connectionTimer[1] = 0f;

        }

        if (connectionTimer[0] > 10f && connectionTimer[1] > 10f)
        {
            AddReward(8f);
            Debug.Log("victory!");
            EndEpisode();

        }

        
        foreach (var contact in signalDetector.VisibleContacts)
        {

            
               AddReward(contact.signalStrength * 0.003f);
            
        }
        if (signalDetector.VisibleContacts.Count > 0)
        {
            float minSignal = float.MaxValue;

            foreach (var contact in signalDetector.VisibleContacts)
            {
                minSignal = Mathf.Min(
                    minSignal,
                    contact.signalStrength
                );
            }

            AddReward(minSignal * 0.01f);
        } */
        if (connectivity.CurrentFlag == 3)
        {
            AddReward(0.02f);
            connectionTimer[0] += Time.fixedDeltaTime;
        }
        else
        {
            connectionTimer[0] = 0f;
        }
        if (connectionTimer[0] > 10f)
        {
            AddReward(10f);
            Debug.Log("victory!");
            EndEpisode();
        }
        switch (connectivity.CurrentFlag)
        {
            case 1:
                AddReward(0.005f);
                break;
            case 2:
                AddReward(0.002f);
                break;

        }
        foreach (var contact in signalDetector.VisibleContacts)
        {
            AddReward(contact.signalStrength * 0.001f);
        }
        if (signalDetector.VisibleContacts.Count > 0)
        {
            float minSignal = float.MaxValue;

            foreach (var contact in signalDetector.VisibleContacts)
            {
                minSignal = Mathf.Min(
                    minSignal,
                    contact.signalStrength
                );
            }

            AddReward(minSignal * 0.0025f);
        }

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;

        float throttle = 0f;

        if (Input.GetKey(KeyCode.Space))
            throttle = 1f;

        if (Input.GetKey(KeyCode.LeftShift))
            throttle = -1f;

        actions[0] = throttle;

        actions[1] =
            Input.GetKey(KeyCode.W) ? 1f :
            Input.GetKey(KeyCode.S) ? -1f : 0f;

        actions[2] =
            Input.GetKey(KeyCode.D) ? 1f :
            Input.GetKey(KeyCode.A) ? -1f : 0f;

        actions[3] =
            Input.GetKey(KeyCode.E) ? 1f :
            Input.GetKey(KeyCode.Q) ? -1f : 0f;
    }
}

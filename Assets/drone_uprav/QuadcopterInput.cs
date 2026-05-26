using UnityEngine;

public class QuadcopterInput : MonoBehaviour
{
    public QuadcopterPhysics quadcopter;

    [Header("Input Power")]
    public float throttleSpeed = 5f;
    public float controlPower = 2f;

    private float throttle = 0f;

    void Update()
    {
        // Throttle
        throttle = 0f;

        if (Input.GetKey(KeyCode.Space))
            throttle = 1f;

        if (Input.GetKey(KeyCode.LeftShift))
            throttle = -1f;

        throttle = Mathf.Clamp(throttle, -1f, 1f);

        // Pitch
        float pitch = 0f;
        if (Input.GetKey(KeyCode.W)) pitch = 1f;
        if (Input.GetKey(KeyCode.S)) pitch = -1f;

        // Roll
        float roll = 0f;
        if (Input.GetKey(KeyCode.D)) roll = 1f;
        if (Input.GetKey(KeyCode.A)) roll = -1f;

        // Yaw
        float yaw = 0f;
        if (Input.GetKey(KeyCode.E)) yaw = 1f;
        if (Input.GetKey(KeyCode.Q)) yaw = -1f;

        quadcopter.SetInputs(
            throttle,
            pitch * controlPower,
            roll * controlPower,
            yaw * controlPower
        );
    }
}

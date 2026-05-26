using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class QuadcopterPhysics : MonoBehaviour
{
    [Header("Motor Points (X layout)")]
    public Transform frontLeft;
    public Transform frontRight;
    public Transform rearLeft;
    public Transform rearRight;

    [Header("Motor Force")]
    public float maxMotorForce = 15f;
    public float intputCoef = 1f;

    [Header("Stabilization")]
    public float pitchStabilization = 4f;
    public float rollStabilization = 4f;
    public float yawPower = 2f;

    [Header("Hover")]
    public float hoverForceMultiplier = 1f;

    private Rigidbody rb;

    // Управляющие команды
    private float throttleInput;
    private float pitchInput;
    private float rollInput;
    private float yawInput;

    private float[] motorPower = new float[4];

    private float stabilizedPitch;
    private float stabilizedRoll;

    private bool isActive = false;
    private float startDelay = 0.05f;
    private float timer = 0f;

    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = true;
        rb.linearDamping = 0f;
        rb.angularDamping = 2f;
    }

    public void SetInputs(float throttle, float pitch, float roll, float yaw)
    {
        throttleInput = Mathf.Clamp(throttle, -1f, 1f);
        pitchInput = Mathf.Clamp(pitch, -1f, 1f);
        rollInput = Mathf.Clamp(roll, -1f, 1f);
        yawInput = Mathf.Clamp(yaw, -1f, 1f);
        
        /*Debug.Log(
            $"throttle: {throttleInput:F2} | " +
            $"pitch: {pitchInput:F2} | " +
            $"roll: {rollInput:F2} | " +
            $"yaw: {yawInput:F2}");*/ 
    }

    public void ResetPhysics(Vector3 position, Quaternion rotation)
    {
        rb.Sleep();
       
        // 1. Остановить физику
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 2. Сбросить силы (важно!)
        rb.ResetInertiaTensor();

        

        // 3. Переместить без физического "рывка"
        rb.position = position;
        rb.rotation = rotation;


        // 4. На всякий случай обнулить sleep state
        
        rb.WakeUp();
        DisablePhysics();

        // 5. Сброс внутренних команд управления
        throttleInput = 0f;
        pitchInput = 0f;
        rollInput = 0f;
        yawInput = 0f;

        for (int i = 0; i < 4; i++)
            motorPower[i] = 0f;

        //Debug.Log("Reset");
    }

    public void DisablePhysics()
    {
        isActive = false;
        timer = 0f;
    }

    void FixedUpdate()
    {
        if (!isActive)
        {
            timer += Time.fixedDeltaTime;

            if (timer >= startDelay)
                isActive = true;

            return; // ❗ полностью отключаем физику
        }

        stabilizedPitch = pitchInput;
        stabilizedRoll = rollInput;

        CalculateMotorPower();
        ApplyMotorForces();
        ApplyYawTorque();
        /*Debug.Log(
            $"FL: {motorPower[0]:F2} | " +
            $"FR: {motorPower[1]:F2} | " +
            $"RL: {motorPower[2]:F2} | " +
            $"RR: {motorPower[3]:F2}");*/
    }

    void Stabilize()
    {
        Vector3 localRotation = transform.localEulerAngles;

        // текущие углы тангажа (вперёд/назад) и крена (влево/вправо)
        float pitchAngle = NormalizeAngle(localRotation.x);
        float rollAngle = NormalizeAngle(localRotation.z);

        // желаемый угол наклона от стиков (15° при полном отклонении)
        float targetPitch = pitchInput * 15f;
        float targetRoll = -rollInput * 15f;   // знак зависит от осей модели, оставлен как в оригинале

        // ОШИБКА: было target - angle, теперь angle - target
        float pitchError = pitchAngle - targetPitch;
        float rollError = rollAngle - targetRoll;

        stabilizedPitch = pitchError * pitchStabilization;
        stabilizedRoll = rollError * rollStabilization;
    }

    void CalculateMotorPower()
    {
        float hoverBase = (rb.mass * Physics.gravity.magnitude / 4f) * hoverForceMultiplier;


        // X configuration:
        // FL = 0
        // FR = 1
        // RL = 2
        // RR = 3

        motorPower[0] = hoverBase + throttleInput + stabilizedPitch*intputCoef - stabilizedRoll * intputCoef;
        motorPower[1] = hoverBase + throttleInput + stabilizedPitch * intputCoef + stabilizedRoll * intputCoef;
        motorPower[2] = hoverBase + throttleInput - stabilizedPitch * intputCoef - stabilizedRoll * intputCoef;
        motorPower[3] = hoverBase + throttleInput - stabilizedPitch * intputCoef + stabilizedRoll * intputCoef;

        for (int i = 0; i < 4; i++)
        {
            motorPower[i] = Mathf.Clamp(motorPower[i], 0f, maxMotorForce);
        }
    }

    void ApplyMotorForces()
    {
        ApplyMotor(frontLeft, motorPower[0]);
        ApplyMotor(frontRight, motorPower[1]);
        ApplyMotor(rearLeft, motorPower[2]);
        ApplyMotor(rearRight, motorPower[3]);
    }

    void ApplyMotor(Transform motorPoint, float force)
    {
        rb.AddForceAtPosition(
            motorPoint.up * force,
            motorPoint.position,
            ForceMode.Force
        );
    }

    void ApplyYawTorque()
    {
        rb.AddRelativeTorque(
            Vector3.up * yawInput * yawPower,
            ForceMode.Force
        );
    }

    public float[] GetMotorPower()
    {
        return motorPower;
    }
}

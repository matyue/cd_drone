using UnityEngine;

public class MovingTarget : MonoBehaviour
{
    public float radius = 5f;
    public float speed = 1.5f;
    public float height = 2f;

    public Vector3 center;
    private float angle;

    void Start()
    {
        center = transform.position;
        angle = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        angle += speed * Time.deltaTime;

        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        transform.position = center + new Vector3(x, height, z);
    }

    public void UpdateCenter(Vector3 position)
    {
        transform.position = position;
        center = position;
        angle = Random.Range(0f, Mathf.PI * 2f);
    }
}

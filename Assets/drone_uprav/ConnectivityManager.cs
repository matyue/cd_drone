using UnityEngine;

public class ConnectivityManager : MonoBehaviour
{
    public int iterations = 8;

    void LateUpdate()
    {
        ConnectivityNode.UpdateNetwork(iterations);
    }
}

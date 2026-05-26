using System.Collections.Generic;
using UnityEngine;

public class ConnectivityNode : MonoBehaviour
{
    public enum NodeMode
    {
        Dynamic,
        Constant
    }

    [Header("Mode")]
    public NodeMode mode = NodeMode.Dynamic;

    [Range(0, 3)]
    public int constantFlag = 0;

    [Header("Runtime")]
    [SerializeField]
    private int currentFlag = 0;

    private SignalDetector signalDetector;

    public int CurrentFlag => currentFlag;

    private static readonly List<ConnectivityNode> allNodes =
        new();

    private void Awake()
    {
        signalDetector = GetComponent<SignalDetector>();
    }

    private void OnEnable()
    {
        allNodes.Add(this);
    }

    private void OnDisable()
    {
        allNodes.Remove(this);
    }

    public void ResetFlag()
    {
        if (mode == NodeMode.Constant)
        {
            currentFlag = constantFlag;
        }
        else
        {
            currentFlag = 0;
        }
    }

    public void PropagateFlag()
    {
        //if (mode == NodeMode.Constant)
            //return;

        if (signalDetector == null)
            return;

        foreach (var contact in signalDetector.VisibleContacts)
        {
            var node =
                contact.transform.GetComponent<ConnectivityNode>();

            if (node == null)
                continue;

            currentFlag |= node.currentFlag;
            //node.PropagateFlag();
        }
    }

    public static void UpdateNetwork(int iterations = 8)
    {
        foreach (var node in allNodes)
        {
            node.ResetFlag();
        }

        for (int i = 0; i < iterations; i++)
        {
            foreach (var node in allNodes)
            {
                node.PropagateFlag();
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

public class SignalDetector : MonoBehaviour
{
    [Header("Signal")]
    public float maxRange = 20f;

    [Header("Obstacles")]
    public LayerMask obstacleMask;

    private readonly List<Transform> contacts = new();

    public List<SignalContact> VisibleContacts { get; }
        = new();

    private void OnTriggerEnter(Collider other)
    {
        if (other == this) return;
        Debug.Log(name+" ENTER: " + other.name);
        if (!contacts.Contains(other.transform))
            contacts.Add(other.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log(name+" EXIT: " + other.name);
        contacts.Remove(other.transform);
    }

    private void Update()
    {
        VisibleContacts.Clear();

        foreach (Transform target in contacts)
        {
            if (target == null)
                continue;

            Vector3 from = transform.position;
            Vector3 to = target.position;

            float distance = Vector3.Distance(from, to);

            if (distance > maxRange)
                continue;

            if (Physics.Linecast(
                    from,
                    to,
                    obstacleMask))
            {
                continue;
            }

            float signal =
                Mathf.Clamp01(
                    1f - distance / maxRange
                );

            int flag = 0;
            var node =
    target.GetComponent<ConnectivityNode>();
            if (node != null)
            {
                flag = node.CurrentFlag;
            }


            VisibleContacts.Add(
                new SignalContact
                {
                    transform = target,
                    signalStrength = signal,
                    flag = flag
                });

        }
        VisibleContacts.Sort((a, b) => b.signalStrength.CompareTo(a.signalStrength));
    }

    public SignalContact GetBestContact(int index)
    {
        if (index < 0 || index >= VisibleContacts.Count)
            return null;

        return VisibleContacts[index];
    }

    int FlagPriority(int flag)
    {
        int count = 0;

        if ((flag & 1) != 0) count++;
        if ((flag & 2) != 0) count++;

        return count;
    }

}

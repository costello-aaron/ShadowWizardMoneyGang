using UnityEngine;

public class TriggerRelay : MonoBehaviour
{
    private RoomController room;

    void Log(string message)
    {
        Debug.Log($"[TriggerRelay:{name}] {message}");
    }

    void Start()
    {
        room = GetComponentInParent<RoomController>();
        Log($"Start() roomAssigned={(room != null)}");
    }

    void OnTriggerEnter(Collider other)
    {
        Log($"OnTriggerEnter(other={other.name}, tag={other.tag})");
        if (room == null)
        {
            Log("RoomController missing in parent.");
            return;
        }

        room.OnPlayerEnter(other);
    }
}
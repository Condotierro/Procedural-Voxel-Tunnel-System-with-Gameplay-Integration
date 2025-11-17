using UnityEngine;

public class DebrisLifetime : MonoBehaviour
{
    void Start()
    {
        Destroy(gameObject, 4f); // despawn after 4 sec
    }
}

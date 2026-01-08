using UnityEngine;

public class MetricsHotkey : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            RuntimeMetrics.SaveToFile();
        }
    }
}

using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class FPSLogger : MonoBehaviour
{
    private List<float> fpsValues = new List<float>();
    private float nextSampleTime;

    private void Start()
    {
        nextSampleTime = Time.time + 1f;
    }

    private void Update()
    {
        if (Time.time >= nextSampleTime)
        {
            float fps = 1f / Time.deltaTime;
            fpsValues.Add(fps);
            nextSampleTime += 1f;

            if (fpsValues.Count >= 10)
            {
                float average = fpsValues.Average();
                WriteToFile(average);
                enabled = false; // Stop logging after first 10 seconds
            }
        }
    }

    private void WriteToFile(float averageFps)
    {
#if UNITY_EDITOR
        Debug.Log($"{System.DateTime.Now}: Average FPS over first 10 seconds: {averageFps:F2}");
#else
        string exeFolder = Path.GetDirectoryName(Application.dataPath);
        string filePath = Path.Combine(exeFolder, "fps_log.txt");
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine($"{System.DateTime.Now}: Average FPS over first 10 seconds: {averageFps:F2}");
        }
#endif
    }
}

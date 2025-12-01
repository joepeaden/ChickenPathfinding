using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class FPSLogger : MonoBehaviour
{
    private List<float> fpsValues = new List<float>();
    private float nextSampleTime;
    private int sampleCount = 0;
    private const int totalSamples = 10;
    private const float sampleInterval = 5f;

    private void Start()
    {
        nextSampleTime = Time.time + sampleInterval;
        LogSeparator();
    }

    private void Update()
    {
        if (Time.time >= nextSampleTime && sampleCount < totalSamples)
        {
            float fps = 1f / Time.deltaTime;
            fpsValues.Add(fps);
            sampleCount++;
            nextSampleTime += sampleInterval;

            LogFPS(fps);

            if (sampleCount >= totalSamples)
            {
                float average = fpsValues.Average();
                LogAverage(average);
                enabled = false; // Stop logging after 10 samples
            }
        }
    }

    private void LogSeparator()
    {
        string message = $"---- TEST {System.DateTime.Now} ----";
#if UNITY_EDITOR
        Debug.Log(message);
#else
        string exeFolder = Path.GetDirectoryName(Application.dataPath);
        string filePath = Path.Combine(exeFolder, "fps_log.txt");
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine(message);
        }
#endif
    }

    private void LogFPS(float fps)
    {
        string message = $"{System.DateTime.Now}: FPS at {sampleCount * sampleInterval}s: {fps:F2}";
#if UNITY_EDITOR
        Debug.Log(message);
#else
        string exeFolder = Path.GetDirectoryName(Application.dataPath);
        string filePath = Path.Combine(exeFolder, "fps_log.txt");
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine(message);
        }
#endif
    }

    private void LogAverage(float averageFps)
    {
        string message = $"{System.DateTime.Now}: Average FPS over {totalSamples * sampleInterval} seconds: {averageFps:F2}";
#if UNITY_EDITOR
        Debug.Log(message);
#else
        string exeFolder = Path.GetDirectoryName(Application.dataPath);
        string filePath = Path.Combine(exeFolder, "fps_log.txt");
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine(message);
        }
#endif
    }
}

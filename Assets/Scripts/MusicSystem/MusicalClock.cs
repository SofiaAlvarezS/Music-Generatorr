using UnityEngine;

public class MusicalClock : MonoBehaviour
{
    [Header("Configuración")]
    [Min(0.05f)]
    public float startDelay = 0.15f;

    public double StartTime { get; private set; }

    public bool IsRunning { get; private set; }

    public double CurrentTime
    {
        get
        {
            if (!IsRunning)
                return 0;

            return System.Math.Max(
                0,
                AudioSettings.dspTime - StartTime
            );
        }
    }

    public double Begin()
    {
        StartTime =
            AudioSettings.dspTime + startDelay;

        IsRunning = true;

        return StartTime;
    }

    public void StopClock()
    {
        IsRunning = false;
        StartTime = 0;
    }

    public double GetScheduledTime(
        double eventPosition)
    {
        return StartTime + eventPosition;
    }
}
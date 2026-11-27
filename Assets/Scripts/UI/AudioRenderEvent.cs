using UnityEngine;

public struct AudioRenderEvent
{
    public AudioClip clip;

    public double startTime;

    public float volume;


    public AudioRenderEvent(
        AudioClip clip,
        double startTime,
        float volume)
    {
        this.clip = clip;

        this.startTime = startTime;

        this.volume = volume;
    }


    public double EndTime
    {
        get
        {
            if (clip == null)
            {
                return startTime;
            }

            return startTime + clip.length;
        }
    }


    public bool IsValid
    {
        get
        {
            return clip != null &&
                   startTime >= 0.0 &&
                   !double.IsNaN(startTime) &&
                   !double.IsInfinity(startTime) &&
                   !float.IsNaN(volume) &&
                   !float.IsInfinity(volume) &&
                   volume >= 0f;
        }
    }
}
using UnityEngine;
using UnityEditor;

/// <summary>
/// Keeps track of the current date and time in the world
/// </summary>
public class WorldDateTime
{
    //1 game hour is 60 seconds
    public const float HOUR = 6;
    public const float DAY = HOUR * 24;

    private float TotalTime;


    private float CurrentDayTime { get { return TotalTime % DAY; } }



    public WorldDateTime()
    {
        TotalTime = HOUR * 8;
    }


    public void Update(GameObject mainLight)
    {
        TotalTime += Time.deltaTime;

        mainLight.transform.rotation = Quaternion.Euler(CurrentDayTime/DAY * 360 - 120, 0, 0);
        DebugGUI.Instance.SetData("Time", CurrentDayTime);
        DebugGUI.Instance.SetData("IsNight", IsNight);
    }


    public bool IsNight { get { return TimeBetweenHours(20, 6); } }

    public void IncrimentTimr(float seconds)
    {
        TotalTime += seconds;
    }

    public bool TimeBetweenSeconds(float lowerSec, float upperSec)
    {
        float cur = CurrentDayTime;
        if (cur < lowerSec)
            return false;
        if (upperSec < lowerSec)
            return cur < (upperSec + DAY);
        return cur < upperSec;              

    }
    public bool TimeBetweenHours(int lowerHour, int upperHour)
    {
        return TimeBetweenSeconds(lowerHour * HOUR, upperHour * HOUR);

    }

}

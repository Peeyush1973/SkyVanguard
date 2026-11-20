using UnityEngine;

/// <summary>
/// Small mobile-safe startup helper.
/// Put this on a GameObject that exists when the game starts.
/// </summary>
public class MobilePerformanceBootstrap : MonoBehaviour
{
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private bool disableScreenSleep = true;

    private void Awake()
    {
        Application.targetFrameRate = Mathf.Clamp(targetFrameRate, 30, 120);

        if (disableScreenSleep)
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}

using UnityEngine;

public static class GameOptimizer
{
    // This runs automatically as soon as the game launches, without needing to be attached to a GameObject!
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void ApplyOptimizations()
    {
        // 1. Cap the framerate. 
        // By default, Unity runs uncapped (often 1000+ FPS on menus), which instantly maxes out the GPU/CPU, 
        // heats up the device, and then causes thermal throttling which results in severe stuttering and "lag".
        Application.targetFrameRate = 60;
        
        // 2. Ensure VSync is disabled so our target framerate is respected exactly
        QualitySettings.vSyncCount = 0;
        
        Debug.Log("Game Optimizer: Locked framerate to 60 FPS to prevent thermal throttling and stutter.");
    }
}

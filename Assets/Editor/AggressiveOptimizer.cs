using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class AggressiveOptimizer
{
    // This runs automatically the moment this script is saved and compiled!
    static AggressiveOptimizer()
    {
        EditorApplication.delayCall += ApplyOptimizations;
    }

    static void ApplyOptimizations()
    {
        // 1. TERRAIN OPTIMIZATIONS (The biggest culprit of open-world lag)
        // By default, Unity was trying to render every single tree up to 5000 meters away!
        QualitySettings.terrainTreeDistance = 350f; 
        
        // Convert distant trees into cheap 2D pictures (billboards) much earlier
        QualitySettings.terrainBillboardStart = 50f; 
        
        // Lower the geometry complexity of the terrain (1 was maximum detail, 10 is perfectly fine for low poly)
        QualitySettings.terrainPixelError = 10f; 
        
        // Only render grass and tiny details right around the player
        QualitySettings.terrainDetailDistance = 50f; 


        // 2. CAMERA OPTIMIZATIONS ("Don't render what I don't see")
        if (Camera.main != null)
        {
            // Stop rendering the world entirely after 350 meters! 
            // (Default was likely 1000+, which renders millions of invisible polygons)
            Camera.main.farClipPlane = 350f;
        }


        // 3. FOG SETUP
        // We turn on Fog to gracefully hide the fact that the world stops rendering at 350 meters.
        // It gives a beautiful atmospheric fade instead of objects just popping into existence!
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 200f;
        RenderSettings.fogEndDistance = 350f;
        
        // Set the fog to a nice atmospheric light blue/grey sky color
        RenderSettings.fogColor = new Color(0.65f, 0.75f, 0.85f);

        Debug.Log("🚀 Aggressive Optimizer has automatically fixed Terrain, Camera, and Fog settings for massive FPS boosts!");
    }
}

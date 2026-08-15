using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

[InitializeOnLoad]
public class LightingCopier : EditorWindow
{
    static LightingCopier()
    {
        EditorApplication.delayCall += CopyLighting;
    }

    [MenuItem("Tools/Copy Lighting from BigIsland to Village")]
    public static void CopyLighting()
    {
        if (SessionState.GetBool("LightingCopierRun", false)) return;
        SessionState.SetBool("LightingCopierRun", true);
        string sourceScenePath = "Assets/Scenes/BigIsland.unity";
        string targetScenePath = "Assets/Scenes/Village.unity";

        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(sourceScenePath))
        {
            Debug.LogError($"Source scene not found at {sourceScenePath}");
            return;
        }

        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(targetScenePath))
        {
            Debug.LogError($"Target scene not found at {targetScenePath}");
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            // Open Source Scene
            Scene sourceScene = EditorSceneManager.OpenScene(sourceScenePath, OpenSceneMode.Single);
            
            // Extract RenderSettings
            Material skybox = RenderSettings.skybox;
            Color ambientSkyColor = RenderSettings.ambientSkyColor;
            Color ambientEquatorColor = RenderSettings.ambientEquatorColor;
            Color ambientGroundColor = RenderSettings.ambientGroundColor;
            Color ambientLight = RenderSettings.ambientLight;
            AmbientMode ambientMode = RenderSettings.ambientMode;
            float ambientIntensity = RenderSettings.ambientIntensity;
            
            bool fog = RenderSettings.fog;
            Color fogColor = RenderSettings.fogColor;
            FogMode fogMode = RenderSettings.fogMode;
            float fogDensity = RenderSettings.fogDensity;
            float fogStartDistance = RenderSettings.fogStartDistance;
            float fogEndDistance = RenderSettings.fogEndDistance;
            
            DefaultReflectionMode reflectionMode = RenderSettings.defaultReflectionMode;
            int reflectionResolution = RenderSettings.defaultReflectionResolution;
            float reflectionIntensity = RenderSettings.reflectionIntensity;
            int reflectionBounces = RenderSettings.reflectionBounces;
            
            // Extract Directional Light
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            Light sourceDirLight = null;
            foreach(var l in lights) {
                if (l.type == LightType.Directional) {
                    sourceDirLight = l;
                    break;
                }
            }
            
            Vector3 dirLightEuler = Vector3.zero;
            Color dirLightColor = Color.white;
            float dirLightIntensity = 1f;
            LightShadows dirLightShadows = LightShadows.Soft;
            if (sourceDirLight != null) {
                dirLightEuler = sourceDirLight.transform.eulerAngles;
                dirLightColor = sourceDirLight.color;
                dirLightIntensity = sourceDirLight.intensity;
                dirLightShadows = sourceDirLight.shadows;
            }

            // Extract Volume
            Volume[] volumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Exclude);
            VolumeProfile sourceVolumeProfile = null;
            foreach(var v in volumes) {
                if (v.isGlobal && v.sharedProfile != null) {
                    sourceVolumeProfile = v.sharedProfile;
                    break;
                }
            }

            // Open Target Scene
            Scene targetScene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            
            // Apply RenderSettings
            RenderSettings.skybox = skybox;
            RenderSettings.ambientSkyColor = ambientSkyColor;
            RenderSettings.ambientEquatorColor = ambientEquatorColor;
            RenderSettings.ambientGroundColor = ambientGroundColor;
            RenderSettings.ambientLight = ambientLight;
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientIntensity = ambientIntensity;
            
            RenderSettings.fog = fog;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance = fogEndDistance;
            
            RenderSettings.defaultReflectionMode = reflectionMode;
            RenderSettings.defaultReflectionResolution = reflectionResolution;
            RenderSettings.reflectionIntensity = reflectionIntensity;
            RenderSettings.reflectionBounces = reflectionBounces;
            
            // Apply Directional Light
            Light[] targetLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            Light targetDirLight = null;
            foreach(var l in targetLights) {
                if (l.type == LightType.Directional) {
                    targetDirLight = l;
                    break;
                }
            }
            
            if (targetDirLight == null && sourceDirLight != null) {
                GameObject go = new GameObject("Directional Light");
                targetDirLight = go.AddComponent<Light>();
                targetDirLight.type = LightType.Directional;
            }
            
            if (targetDirLight != null) {
                targetDirLight.transform.eulerAngles = dirLightEuler;
                targetDirLight.color = dirLightColor;
                targetDirLight.intensity = dirLightIntensity;
                targetDirLight.shadows = dirLightShadows;
                RenderSettings.sun = targetDirLight;
            }

            // Apply Volume
            Volume[] targetVolumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Exclude);
            Volume targetVolume = null;
            foreach(var v in targetVolumes) {
                if (v.isGlobal) {
                    targetVolume = v;
                    break;
                }
            }
            
            if (targetVolume == null && sourceVolumeProfile != null) {
                GameObject go = new GameObject("Global Volume");
                targetVolume = go.AddComponent<Volume>();
                targetVolume.isGlobal = true;
            }
            
            if (targetVolume != null && sourceVolumeProfile != null) {
                targetVolume.sharedProfile = sourceVolumeProfile;
            }

            // Fix the Main Camera in Village so it follows the player and uses Post-Processing
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var camData = mainCam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (camData != null) camData.renderPostProcessing = true;

                if (mainCam.GetComponent("ThirdPersonCamera") == null)
                {
                    var type = System.Type.GetType("ThirdPersonCamera, Assembly-CSharp");
                    if (type != null) mainCam.gameObject.AddComponent(type);
                }
                
                // Remove old camera script if it exists
                var oldScript = mainCam.GetComponent("CameraFollow");
                if (oldScript != null) Object.DestroyImmediate(oldScript);
            }
            
            EditorSceneManager.SaveScene(targetScene);
            Debug.Log("Lighting and Bloom successfully copied from BigIsland to Village!");
        }
    }
}

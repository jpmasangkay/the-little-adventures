using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
public class MainMenuBuilder : EditorWindow
{
    [MenuItem("Tools/Build Main Menu UI")]
    public static void BuildMenu()
    {
        // 1. Create a new Scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            if (mainCam.GetComponent<AudioListener>() == null)
            {
                mainCam.gameObject.AddComponent<AudioListener>();
            }
            mainCam.transform.position = new Vector3(0, 3, -10);
            mainCam.transform.rotation = Quaternion.Euler(15, 0, 0);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.4f, 0.7f, 0.9f); // Beautiful sky blue
        }

        // 2. Set up a CUTE 3D Background Environment!
        GameObject env = new GameObject("3D Environment");
        
        // Ground
        GameObject groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/RPG Tiny Fantasy Forest PBR/Prefab/LandMass/LM40RND.prefab");
        GameObject ground = null;
        if (groundPrefab != null)
        {
            ground = (GameObject)PrefabUtility.InstantiatePrefab(groundPrefab, env.transform);
            ground.transform.position = new Vector3(0, 0f, 0); // Put it directly on 0 so slime doesn't float
            ground.transform.localScale = Vector3.one * 10f;
        }
        else
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.SetParent(env.transform);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10, 1, 10);
            if (GraphicsSettings.currentRenderPipeline != null)
                ground.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            else
                ground.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Standard"));
            ground.GetComponent<MeshRenderer>().sharedMaterial.color = new Color(0.3f, 0.8f, 0.3f); // Lush green grass
        }

        // Spawn Trees
        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/RPG Tiny Fantasy Forest PBR/Prefab/TreePlants/Tree02.prefab");
        if (treePrefab != null)
        {
            GameObject tree1 = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, env.transform);
            tree1.transform.position = new Vector3(-3, 0, 2);
            tree1.transform.localScale = Vector3.one * 1.5f;
            if (tree1.GetComponent<Collider>() == null) tree1.AddComponent<CapsuleCollider>();

            GameObject tree2 = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, env.transform);
            tree2.transform.position = new Vector3(3, 0, 5);
            tree2.transform.localScale = Vector3.one * 1.2f;
            if (tree2.GetComponent<Collider>() == null) tree2.AddComponent<CapsuleCollider>();
        }

        // Spawn a Cute Slime Monster!
        GameObject slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/RPG Monster DUO PBR Polyart/Prefabs/PolyartDefault/SlimePolyart.prefab");
        if (slimePrefab != null)
        {
            GameObject slime = (GameObject)PrefabUtility.InstantiatePrefab(slimePrefab, env.transform);
            slime.transform.position = new Vector3(-2, 0, -2);
            slime.transform.rotation = Quaternion.Euler(0, 150, 0);
            
            // Fix Pink Material Issue!
            Material polyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Assets/RPG Monster DUO PBR Polyart/Materials/PolyartDefault.mat");
            if (polyMat != null)
            {
                foreach(var smr in slime.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    smr.sharedMaterial = polyMat;
                }
            }
            
            ButtonAnimation bounce = slime.AddComponent<ButtonAnimation>();
            bounce.hoverScale = 1.2f;
            bounce.animationSpeed = 2f;
            
            // Add wander script so it moves bit by bit!
            slime.AddComponent<SlimeWander>();
        }

        // Floating Particles (Fireflies/Spores)
        GameObject particles = new GameObject("Fireflies");
        particles.transform.position = new Vector3(0, 2, 0);
        ParticleSystem ps = particles.AddComponent<ParticleSystem>();
        
        // Fix Pink Particles!
        ParticleSystemRenderer psRenderer = particles.GetComponent<ParticleSystemRenderer>();
        Material particleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
        if (particleMat == null)
        {
            if (GraphicsSettings.currentRenderPipeline != null)
                particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            else
                particleMat = new Material(Shader.Find("Particles/Standard Unlit"));
        }
        psRenderer.sharedMaterial = particleMat;

        var main = ps.main;
        main.startColor = new Color(1f, 0.9f, 0.4f, 0.8f);
        main.startSize = 0.4f;
        main.startSpeed = 0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;
        var emission = ps.emission;
        emission.rateOverTime = 20;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 10f;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.y = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);


        // 3. Set up the Canvas
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.layer = LayerMask.NameToLayer("UI");

        // 4. Set up EventSystem
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        // 5. Center Window Panel
        GameObject windowGO = new GameObject("WindowPanel");
        windowGO.transform.SetParent(canvasGO.transform, false);
        Image windowImg = windowGO.AddComponent<Image>();
        Sprite windowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets/UI/Panels/Panel Light.png");
        if (windowSprite != null)
        {
            windowImg.sprite = windowSprite;
            windowImg.type = Image.Type.Sliced;
        }
        else
        {
            windowImg.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        }
        RectTransform windowRect = windowGO.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.7f, 0.5f);
        windowRect.anchorMax = new Vector2(0.7f, 0.5f);
        windowRect.sizeDelta = new Vector2(500, 600);
        windowRect.anchoredPosition = Vector2.zero;

        // 6. Title Bar
        GameObject titleBarGO = new GameObject("TitleBar");
        titleBarGO.transform.SetParent(windowGO.transform, false);
        Image titleBarImg = titleBarGO.AddComponent<Image>();
        Sprite titleBarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets/UI/Panels/Title Bar Orange.png");
        if (titleBarSprite != null) { titleBarImg.sprite = titleBarSprite; }
        else { titleBarImg.color = new Color(1f, 0.5f, 0f, 1f); }
        RectTransform titleBarRect = titleBarGO.GetComponent<RectTransform>();
        titleBarRect.anchorMin = new Vector2(0.5f, 1f);
        titleBarRect.anchorMax = new Vector2(0.5f, 1f);
        titleBarRect.pivot = new Vector2(0.5f, 0.5f);
        titleBarRect.sizeDelta = new Vector2(700, 150); 
        titleBarRect.anchoredPosition = new Vector2(0, 0); 

        // 7. Title Text
        GameObject titleTextGO = new GameObject("TitleText");
        titleTextGO.transform.SetParent(titleBarGO.transform, false);
        Text titleText = titleTextGO.AddComponent<Text>();
        titleText.text = "THE LITTLE\nADVENTURERS"; 
        titleText.fontSize = 54;
        titleText.lineSpacing = 0.8f;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        Outline titleOutline = titleTextGO.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0, 0, 0, 0.6f);
        titleOutline.effectDistance = new Vector2(3, -3);
        RectTransform titleTextRect = titleTextGO.GetComponent<RectTransform>();
        titleTextRect.anchorMin = Vector2.zero;
        titleTextRect.anchorMax = Vector2.one;
        titleTextRect.sizeDelta = Vector2.zero;
        titleTextRect.anchoredPosition = new Vector2(0, 10); 

        // 8. Buttons Container
        GameObject buttonContainerGO = new GameObject("ButtonContainer");
        buttonContainerGO.transform.SetParent(windowGO.transform, false);
        RectTransform containerRect = buttonContainerGO.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(400, 400);
        containerRect.anchoredPosition = new Vector2(0, -40);
        
        VerticalLayoutGroup vlg = buttonContainerGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = false;
        vlg.spacing = 25;

        // 9. Create Buttons
        CreateButton(buttonContainerGO.transform, "PlayButton", "PLAY");
        CreateButton(buttonContainerGO.transform, "SettingsButton", "SETTINGS");
        CreateButton(buttonContainerGO.transform, "QuitButton", "QUIT");

        // 10. Load Settings Prefab
        GameObject settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/UI/Prefabs/Settings Panel.prefab");
        GameObject settingsInstance = null;
        if (settingsPrefab != null)
        {
            // Just instantiate directly, no wrapper! That's what broke it.
            settingsInstance = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab);
            settingsInstance.transform.SetParent(canvasGO.transform, false);
            settingsInstance.name = "SettingsPanel";
            settingsInstance.AddComponent<SettingsManager>();

            // Fix the broken Close button component from the prefab!
            Transform closeBtn = settingsInstance.transform.Find("Close");
            if (closeBtn != null)
            {
                var badClose = closeBtn.GetComponent("CartoonUI.Close");
                if (badClose != null) DestroyImmediate(badClose);

                foreach (var comp in closeBtn.GetComponents<MonoBehaviour>())
                {
                    if (comp != null && comp.GetType().Name == "Close")
                    {
                        DestroyImmediate(comp);
                    }
                }
            }
        }

        // 11. Fade to Black Panel
        GameObject fadeGO = new GameObject("FadePanel");
        fadeGO.transform.SetParent(canvasGO.transform, false);
        Image fadeImg = fadeGO.AddComponent<Image>();
        fadeImg.color = Color.black;
        fadeImg.raycastTarget = false; 
        RectTransform fadeRect = fadeGO.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.sizeDelta = Vector2.zero;
        CanvasGroup fadeGroup = fadeGO.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;

        // 12. Add MainMenuController
        MainMenuController mmc = canvasGO.AddComponent<MainMenuController>();
        mmc.mainPanel = windowGO;
        mmc.settingsPanel = settingsInstance;
        mmc.fadePanel = fadeGroup;

        // 13. Setup MusicManager
        GameObject bgm = new GameObject("MusicManager");
        MusicManager musicManager = bgm.AddComponent<MusicManager>();
        AudioClip menuClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Assets/Music/LOOP VERSIONS/01 - Intro Hopeful LOOP 74bpm.wav");
        if (menuClip != null)
        {
            musicManager.initialMusic = menuClip;
        }

        // 14. Save the scene
        string scenePath = "Assets/Scenes/MainMenu.unity";
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.SaveScene(newScene, scenePath);
        
        // 15. Add to Build Settings
        var originalBuildSettings = EditorBuildSettings.scenes;
        bool hasMainMenu = false;
        bool hasBigIsland = false;
        
        foreach (var scene in originalBuildSettings)
        {
            if (scene.path == scenePath) hasMainMenu = true;
            if (scene.path.Contains("BigIsland")) hasBigIsland = true;
        }

        System.Collections.Generic.List<EditorBuildSettingsScene> newSettings = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        if (!hasMainMenu) newSettings.Add(new EditorBuildSettingsScene(scenePath, true));
        newSettings.AddRange(originalBuildSettings);
        if (!hasBigIsland) newSettings.Add(new EditorBuildSettingsScene("Assets/Scenes/BigIsland.unity", true));
        
        EditorBuildSettings.scenes = newSettings.ToArray();

        Debug.Log("✅ 3D Main Menu UI Generated (with fixes!)");
    }

    private static GameObject CreateButton(Transform parent, string name, string text)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);
        Image img = buttonGO.AddComponent<Image>();
        
        Sprite btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets/UI/Buttons/Long Round/Long Round Blue.png");
        if (btnSprite != null)
        {
            img.sprite = btnSprite;
            img.type = Image.Type.Simple; 
        }
        else
        {
            img.color = new Color(0.2f, 0.6f, 1f, 1f);
        }
        
        Button btn = buttonGO.AddComponent<Button>();

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 80); 

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        Text btnText = textGO.AddComponent<Text>();
        btnText.text = text;
        btnText.fontSize = 32;
        btnText.fontStyle = FontStyle.Bold;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        
        Outline outline = textGO.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.6f);
        outline.effectDistance = new Vector2(2, -2);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = new Vector2(0, 5); 

        buttonGO.AddComponent<ButtonAnimation>();

        return buttonGO;
    }
}

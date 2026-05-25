#if UNITY_EDITOR
using System.IO;
using Mirror;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using MVRApp;
using NetworkTransform = Mirror.NetworkTransformReliable;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public static class MVRProjectSetup
{
    const string Root = "Assets";
    const string ScenesPath = Root + "/Scenes";
    const string PrefabsPath = Root + "/Prefabs";
    const string AnimationsPath = Root + "/Animations";
    const string AudioPath = Root + "/Audio";
    const string MaterialsPath = Root + "/Materials";

    public static void SetupFullProject()
    {
        RemoveMirrorExamplesFolder();
        EnsureFolders();
        AudioClip jump = CreateWavClip(AudioPath + "/jumpSound.wav", 440f, 0.15f);
        AudioClip collect = CreateWavClip(AudioPath + "/placementSound.wav", 660f, 0.2f);
        AudioClip music = CreateWavClip(AudioPath + "/backgroundMusic.wav", 220f, 1.5f, true);

        Material particleMat = CreateParticleMaterial();
        GameObject planePrefab = CreatePrimitivePrefab("PlanePrefab", PrimitiveType.Plane, Color.gray);
        GameObject capsulePrefab = CreatePrimitivePrefab("CapsulePrefab", PrimitiveType.Capsule, Color.cyan);
        GameObject cubePrefab = CreatePrimitivePrefab("CubePrefab", PrimitiveType.Cube, Color.red);

        AnimatorController playerAnimator = CreatePlayerAnimatorController();
        GameObject playerPrefab = CreatePlayerPrefab(playerAnimator);
        GameObject enemyPrefab = CreateEnemyPrefab();
        GameObject arPlacementPrefab = CreateARPlacementPrefab(particleMat);
        ParticleSystem placementParticles = CreatePlacementParticlePrefab(particleMat);

        GameObject gameSystems = CreateMainMenuScene(playerPrefab, jump, collect, music);
        CreateGameScene(
            planePrefab, capsulePrefab, cubePrefab, enemyPrefab,
            arPlacementPrefab, placementParticles);

        ConfigureBuildSettings();
        ConfigureAndroid();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(ScenesPath + "/MainMenu.unity");

        Selection.activeGameObject = gameSystems;
        Debug.Log("MVR: project setup complete. Open MainMenu scene and press Play.");
    }

    static void RemoveMirrorExamplesFolder()
    {
        const string examplesPath = "Assets/Mirror/Examples";
        if (AssetDatabase.IsValidFolder(examplesPath))
            AssetDatabase.DeleteAsset(examplesPath);
    }

    static void EnsureFolders()
    {
        CreateFolder(Root, "Scenes");
        CreateFolder(Root, "Scripts");
        CreateFolder(Root, "Scripts/Editor");
        CreateFolder(Root, "Prefabs");
        CreateFolder(Root, "Animations");
        CreateFolder(Root, "Audio");
        CreateFolder(Root, "Materials");
    }

    static void CreateFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    static Material CreateParticleMaterial()
    {
        string path = MaterialsPath + "/ParticleMaterial.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
            return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        mat = new Material(shader);
        mat.color = new Color(1f, 0.55f, 0.1f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static GameObject CreatePrimitivePrefab(string name, PrimitiveType type, Color color)
    {
        string path = PrefabsPath + "/" + name + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial.color = color;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static AnimatorController CreatePlayerAnimatorController()
    {
        string controllerPath = AnimationsPath + "/PlayerAnimator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller != null)
            return controller;

        AnimationClip idle = CreateAnimationClip("Idle", false, 0f);
        AnimationClip walk = CreateAnimationClip("Walk", false, 0.3f);
        AnimationClip jump = CreateAnimationClip("Jump", false, 0.6f);

        controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine root = controller.layers[0].stateMachine;
        AnimatorState idleState = root.AddState("Idle");
        idleState.motion = idle;
        AnimatorState walkState = root.AddState("Walk");
        walkState.motion = walk;
        AnimatorState jumpState = root.AddState("Jump");
        jumpState.motion = jump;
        root.defaultState = idleState;

        AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        toWalk.hasExitTime = false;
        toWalk.duration = 0.1f;

        AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        toIdle.hasExitTime = false;
        toIdle.duration = 0.1f;

        AnimatorStateTransition toJump = idleState.AddTransition(jumpState);
        toJump.AddCondition(AnimatorConditionMode.If, 0f, "IsJumping");
        toJump.hasExitTime = false;

        AnimatorStateTransition jumpToIdle = jumpState.AddTransition(idleState);
        jumpToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsJumping");
        jumpToIdle.hasExitTime = true;
        jumpToIdle.exitTime = 0.8f;

        return controller;
    }

    static AnimationClip CreateAnimationClip(string name, bool loop, float bobOffset)
    {
        string path = AnimationsPath + "/" + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null)
            return clip;

        clip = new AnimationClip();
        clip.name = name;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AnimationCurve curve = AnimationCurve.Linear(0f, bobOffset, 1f, bobOffset + 0.15f);
        clip.SetCurve("", typeof(Transform), "localPosition.y", curve);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static GameObject CreatePlayerPrefab(AnimatorController controller)
    {
        string path = PrefabsPath + "/Player.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        root.name = "Player";
        root.tag = "Player";

        Rigidbody rb = root.GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
        if (capsule != null)
            capsule.height = 2f;

        Animator animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        root.AddComponent<NetworkIdentity>();
        root.AddComponent<NetworkTransform>();
        root.AddComponent<DataPacketHandler>();
        root.AddComponent<PlayerController>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject CreateEnemyPrefab()
    {
        string path = PrefabsPath + "/Enemy.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "Enemy";
        root.GetComponent<Renderer>().sharedMaterial.color = Color.red;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        root.AddComponent<UnityEngine.AI.NavMeshAgent>();
        root.AddComponent<EnemyAI>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject CreateARPlacementPrefab(Material particleMat)
    {
        string path = PrefabsPath + "/ARPlacementObject.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "ARPlacementObject";
        root.transform.localScale = Vector3.one * 0.2f;
        root.GetComponent<Renderer>().sharedMaterial.color = Color.yellow;

        ConfigurePlacementParticleSystem(root.AddComponent<ParticleSystem>(), particleMat);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static ParticleSystem CreatePlacementParticlePrefab(Material particleMat)
    {
        string path = PrefabsPath + "/PlacementParticles.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing.GetComponent<ParticleSystem>();

        GameObject go = new GameObject("PlacementParticles");
        ConfigurePlacementParticleSystem(go.AddComponent<ParticleSystem>(), particleMat);
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<ParticleSystem>();
    }

    static void ConfigurePlacementParticleSystem(ParticleSystem ps, Material particleMat)
    {
        ParticleSystem.MainModule main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.6f, 0.1f));

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 20f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.5f, 0f), 0f), new GradientColorKey(Color.yellow, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = particleMat;
    }

    static GameObject CreateMainMenuScene(GameObject playerPrefab, AudioClip jump, AudioClip collect, AudioClip music)
    {
        UnityEngine.SceneManagement.Scene scene =
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject systems = new GameObject("GameSystems");
        systems.AddComponent<SceneManager>();

        SoundManager sound = systems.AddComponent<SoundManager>();
        SerializedObject soundSo = new SerializedObject(sound);
        soundSo.FindProperty("backgroundMusic").objectReferenceValue = music;
        soundSo.FindProperty("jumpSound").objectReferenceValue = jump;
        soundSo.FindProperty("collectSound").objectReferenceValue = collect;
        soundSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject networkGo = new GameObject("NetworkManager");
        networkGo.transform.SetParent(systems.transform);
        NetworkManager network = networkGo.AddComponent<NetworkManager>();
        TelepathyTransport transport = networkGo.AddComponent<TelepathyTransport>();
        transport.port = 7777;
        network.transport = transport;
        network.playerPrefab = playerPrefab;
        network.autoCreatePlayer = true;

        GameObject canvasGo = CreateUICanvas(out GameObject loginPanel, out GameObject gamePanel, network);
        canvasGo.transform.SetParent(systems.transform);

        SerializedObject netSo = new SerializedObject(network);
        netSo.FindProperty("loginPanel").objectReferenceValue = loginPanel;
        netSo.FindProperty("gamePanel").objectReferenceValue = gamePanel;
        netSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenesPath + "/MainMenu.unity");
        return systems;
    }

    static GameObject CreateUICanvas(out GameObject loginPanel, out GameObject gamePanel, NetworkManager network)
    {
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        loginPanel = CreatePanel(canvasGo.transform, "LoginPanel", true);
        CreateText(loginPanel.transform, "TitleText", "Multiplayer AR Game", 42, new Vector2(0, 180));
        CreateInputField(loginPanel.transform, "PlayerNameInput", new Vector2(0, 60));
        CreateButton(loginPanel.transform, "HostButton", "Host", new Vector2(-140, -60), network.StartHost);
        CreateButton(loginPanel.transform, "ClientButton", "Client", new Vector2(140, -60), network.StartClient);

        gamePanel = CreatePanel(canvasGo.transform, "GamePanel", false);
        Slider health = CreateHealthSlider(gamePanel.transform);
        Text score = CreateText(gamePanel.transform, "ScoreText", "Score: 0", 28, new Vector2(0, 180));

        UIManager ui = canvasGo.AddComponent<UIManager>();
        SerializedObject uiSo = new SerializedObject(ui);
        uiSo.FindProperty("loginPanel").objectReferenceValue = loginPanel;
        uiSo.FindProperty("gamePanel").objectReferenceValue = gamePanel;
        uiSo.FindProperty("healthSlider").objectReferenceValue = health;
        uiSo.FindProperty("scoreText").objectReferenceValue = score;
        uiSo.FindProperty("playerNameInput").objectReferenceValue =
            loginPanel.transform.Find("PlayerNameInput").GetComponent<InputField>();
        uiSo.ApplyModifiedPropertiesWithoutUndo();

        CreateButton(gamePanel.transform, "MenuButton", "Menu", new Vector2(0, -180), ui.OnMenuButton);

        return canvasGo;
    }

    static GameObject CreatePanel(Transform parent, string name, bool active)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);
        panel.SetActive(active);
        return panel;
    }

    static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(800, 80);
        rt.anchoredPosition = pos;
        Text text = go.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        return text;
    }

    static InputField CreateInputField(Transform parent, string name, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(420, 50);
        rt.anchoredPosition = pos;
        go.GetComponent<Image>().color = Color.white;

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10, 0);
        textRt.offsetMax = new Vector2(-10, 0);
        Text text = textGo.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.supportRichText = false;
        text.color = Color.black;

        GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        placeholderGo.transform.SetParent(go.transform, false);
        RectTransform phRt = placeholderGo.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = new Vector2(10, 0);
        phRt.offsetMax = new Vector2(-10, 0);
        Text ph = placeholderGo.GetComponent<Text>();
        ph.text = "Player name";
        ph.font = text.font;
        ph.color = new Color(0, 0, 0, 0.45f);

        InputField input = go.GetComponent<InputField>();
        input.textComponent = text;
        input.placeholder = ph;
        return input;
    }

    static void CreateButton(Transform parent, string name, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(220, 56);
        rt.anchoredPosition = pos;
        go.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.85f);
        Button button = go.GetComponent<Button>();
        button.onClick.AddListener(onClick);
        CreateText(go.transform, "Text", label, 24, Vector2.zero);
    }

    static Slider CreateHealthSlider(Transform parent)
    {
        GameObject sliderGo = new GameObject("HealthSlider", typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(parent, false);
        RectTransform rt = sliderGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, 30);
        rt.anchoredPosition = new Vector2(0, 120);

        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(sliderGo.transform, false);
        Stretch(bg.GetComponent<RectTransform>());
        bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>());

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        Stretch(fill.GetComponent<RectTransform>());
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = Color.green;

        Slider slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fillImage;
        return slider;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void CreateGameScene(
        GameObject planePrefab, GameObject capsulePrefab, GameObject cubePrefab,
        GameObject enemyPrefab, GameObject arPlacementPrefab,
        ParticleSystem placementParticles)
    {
        UnityEngine.SceneManagement.Scene scene =
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(3f, 1f, 3f);
        ground.isStatic = true;

        GameObject spawner = new GameObject("ObjectSpawner");
        ObjectSpawner os = spawner.AddComponent<ObjectSpawner>();
        SerializedObject osSo = new SerializedObject(os);
        osSo.FindProperty("prefabs").arraySize = 3;
        osSo.FindProperty("prefabs").GetArrayElementAtIndex(0).objectReferenceValue = planePrefab;
        osSo.FindProperty("prefabs").GetArrayElementAtIndex(1).objectReferenceValue = capsulePrefab;
        osSo.FindProperty("prefabs").GetArrayElementAtIndex(2).objectReferenceValue = cubePrefab;
        osSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
        enemy.transform.position = new Vector3(5f, 0.5f, 5f);

        GameObject spawnPoint = new GameObject("PlayerSpawn");
        spawnPoint.transform.position = new Vector3(0f, 1f, 0f);
        spawnPoint.AddComponent<NetworkStartPosition>();

        CreateARRig(arPlacementPrefab, placementParticles);

    var surface = GameObject.FindAnyObjectByType<Unity.AI.Navigation.NavMeshSurface>();
    if (surface != null)
    {
        surface.BuildNavMesh();
    }
    else
    {
        Debug.LogWarning("NavMeshSurface not found in scene. Creating one...");
        GameObject navObj = new GameObject("NavMeshSurface");
        var newSurface = navObj.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
        newSurface.BuildNavMesh();
    }

        EditorSceneManager.SaveScene(scene, ScenesPath + "/GameScene.unity");
    }

    static void CreateARRig(GameObject placementPrefab, ParticleSystem placementParticles)
    {
        GameObject arSession = new GameObject("AR Session");
        arSession.AddComponent<ARSession>();

        GameObject origin = new GameObject("AR Session Origin");
        origin.AddComponent<XROrigin>();
        ARRaycastManager raycastManager = origin.AddComponent<ARRaycastManager>();
        ARPlaneManager planeManager = origin.AddComponent<ARPlaneManager>();

        GameObject arControllerGo = new GameObject("ARController");
        ARController ar = arControllerGo.AddComponent<ARController>();
        SerializedObject arSo = new SerializedObject(ar);
        arSo.FindProperty("raycastManager").objectReferenceValue = raycastManager;
        arSo.FindProperty("planeManager").objectReferenceValue = planeManager;
        arSo.FindProperty("placementPrefab").objectReferenceValue = placementPrefab;
        arSo.FindProperty("placementParticles").objectReferenceValue = placementParticles;
        arSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenesPath + "/MainMenu.unity", true),
            new EditorBuildSettingsScene(ScenesPath + "/GameScene.unity", true)
        };
    }

    static void ConfigureAndroid()
    {
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
    }

    static AudioClip CreateWavClip(string assetPath, float frequency, float duration, bool loop = false)
    {
        AudioClip existing = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (existing != null)
            return existing;

        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * 0.25f;

        string fullPath = Path.Combine(Application.dataPath, assetPath.Substring(7));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        WriteWavFile(fullPath, samples, sampleRate);
        AssetDatabase.ImportAsset(assetPath);

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (loop && clip != null)
        {
            SerializedObject so = new SerializedObject(clip);
            so.FindProperty("m_Loop").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        return clip;
    }

    static void WriteWavFile(string path, float[] samples, int sampleRate)
    {
        using MemoryStream stream = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(stream);
        int sampleCount = samples.Length;
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + sampleCount * 2);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(sampleCount * 2);
        for (int i = 0; i < sampleCount; i++)
        {
            short value = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);
            writer.Write(value);
        }

        File.WriteAllBytes(path, stream.ToArray());
    }
}
#endif

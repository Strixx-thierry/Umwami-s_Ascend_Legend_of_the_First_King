using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;

/// <summary>Creates the game's scenes from code so they can be rebuilt any time:
/// menu Umwami > Rebuild Scenes, or batchmode -executeMethod ProjectBuilder.Build.
/// Marsh uses placeholder ground until the swamp model is imported.</summary>
public static class ProjectBuilder
{
    const string UI = "Assets/_Game/UI/";
    const string Scenes = "Assets/_Game/Scenes/";
    const string Mats = "Assets/_Game/Materials/";

    [MenuItem("Umwami/Rebuild Scenes")]
    public static void Build()
    {
        Directory.CreateDirectory(Scenes);
        Directory.CreateDirectory(Mats);
        PanelSettingsAsset();
        AssetDatabase.SaveAssets();
        // load shared assets per scene: opening a new scene can unload objects held from the previous one
        BuildFrontend(PanelSettingsAsset(), Fonts());
        BuildMarsh(PanelSettingsAsset(), Fonts());
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(Scenes + "Frontend.unity", true),
            new EditorBuildSettingsScene(Scenes + "Marsh.unity", true),
        };
        PlayerSettings.productName = "Umwami's Ascend";
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        AssetDatabase.SaveAssets();
        Debug.Log("[ProjectBuilder] Scenes rebuilt.");
    }

    [MenuItem("Umwami/Build Windows Player")]
    public static void BuildWindows()
    {
        var report = UnityEditor.BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { Scenes + "Frontend.unity", Scenes + "Marsh.unity" },
            locationPathName = "Builds/Windows/UmwamisAscend.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        });
        Debug.Log($"[ProjectBuilder] Build {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} errors");
    }

    static T Load<T>(string path) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) Debug.LogError($"[ProjectBuilder] Missing asset {path}");
        return a;
    }

    static FontSet Fonts() => new FontSet
    {
        display = Load<Font>(UI + "Fonts/MarcellusSC-Regular.ttf"),
        serif = Load<Font>(UI + "Fonts/Marcellus-Regular.ttf"),
        ui = Load<Font>(UI + "Fonts/Lato-Regular.ttf"),
        light = Load<Font>(UI + "Fonts/Lato-Light.ttf"),
        bold = Load<Font>(UI + "Fonts/Lato-Bold.ttf"),
    };

    static PanelSettings PanelSettingsAsset()
    {
        string path = UI + "PanelSettings.asset";
        var p = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
        if (p == null)
        {
            p = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(p, path);
        }
        p.themeStyleSheet = Load<ThemeStyleSheet>(UI + "Theme.tss");
        p.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        p.referenceResolution = new Vector2Int(1600, 900);
        p.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        p.match = 0.5f;
        EditorUtility.SetDirty(p);
        return p;
    }

    static void AddEventSystem()
    {
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    static GameObject AddUI(string name, PanelSettings panel, string uxml)
    {
        var go = new GameObject(name);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = panel;
        doc.visualTreeAsset = Load<VisualTreeAsset>(UI + uxml);
        return go;
    }

    static void BuildFrontend(PanelSettings panel, FontSet fonts)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera", typeof(Camera));
        cam.tag = "MainCamera";
        var c = cam.GetComponent<Camera>();
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = Color.black;
        AddEventSystem();
        var ui = AddUI("UI", panel, "Frontend.uxml");
        ui.AddComponent<FrontendController>().fonts = fonts;
        EditorSceneManager.SaveScene(scene, Scenes + "Frontend.unity");
    }

    static Material Mat(string name, Color c)
    {
        string path = Mats + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Marker(string name, Vector3 pos, Transform parent)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent);
        g.transform.position = pos;
        return g;
    }

    // Map coordinates (ArtSource/Maps/act1_maps.json, marsh): Unity x = map x, Unity z = map y.
    static Vector3 M(float x, float y, float h = 0) => new Vector3(x, h, y);

    static void BuildMarsh(PanelSettings panel, FontSet fonts)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.66f, 0.71f, 0.66f);
        RenderSettings.fogDensity = 0.012f;
        var sun = GameObject.Find("Directional Light");
        sun.transform.rotation = Quaternion.Euler(38, -145, 0);
        sun.GetComponent<Light>().color = new Color(1f, 0.91f, 0.76f);

        // placeholder world (replaced by the swamp model)
        var world = new GameObject("World (placeholder)").transform;
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(world);
        ground.transform.localScale = new Vector3(30, 1, 30);
        ground.GetComponent<Renderer>().sharedMaterial = Mat("Placeholder_Ground", new Color(0.35f, 0.42f, 0.2f));
        var ridge = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ridge.name = "Ridge";
        ridge.transform.SetParent(world);
        ridge.transform.position = M(-46, -26, -16);
        ridge.transform.localScale = new Vector3(42, 40, 42);
        Object.DestroyImmediate(ridge.GetComponent<SphereCollider>());
        ridge.AddComponent<MeshCollider>();
        ridge.GetComponent<Renderer>().sharedMaterial = Mat("Placeholder_Ridge", new Color(0.4f, 0.45f, 0.24f));
        var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = "Ikinani (placeholder)";
        rock.transform.SetParent(world);
        rock.transform.position = M(-15, -37, 1);
        rock.transform.localScale = new Vector3(13, 9, 10);
        rock.GetComponent<Renderer>().sharedMaterial = Mat("Placeholder_Rock", new Color(0.43f, 0.42f, 0.39f));

        // player
        var player = new GameObject("Player");
        player.layer = 2; // Ignore Raycast: the camera's collision check skips it
        player.transform.position = M(-6, -30, 0.1f);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0, 0.9f, 0);
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
        body.name = "Rudh (stand-in)";
        body.layer = 2;
        body.transform.SetParent(player.transform);
        body.transform.localPosition = new Vector3(0, 0.9f, 0);
        body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
        body.GetComponent<Renderer>().sharedMaterial = Mat("Placeholder_Rudh", new Color(0.55f, 0.18f, 0.12f));

        var cam = Camera.main.gameObject;
        cam.GetComponent<Camera>().farClipPlane = 2000;
        var tpc = cam.AddComponent<ThirdPersonCamera>();
        tpc.target = player.transform;
        var pc = player.AddComponent<PlayerController>();
        pc.cameraTransform = cam.transform;

        // the boy, with a torch
        var boy = new GameObject("Boy");
        boy.transform.position = M(-3, -26, 0);
        boy.transform.rotation = Quaternion.Euler(0, 200, 0);
        var bb = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Object.DestroyImmediate(bb.GetComponent<CapsuleCollider>());
        bb.name = "Boy (stand-in)";
        bb.transform.SetParent(boy.transform);
        bb.transform.localPosition = new Vector3(0, 0.65f, 0);
        bb.transform.localScale = new Vector3(0.45f, 0.65f, 0.45f);
        bb.GetComponent<Renderer>().sharedMaterial = Mat("Placeholder_Boy", new Color(0.75f, 0.5f, 0.25f));
        var torch = new GameObject("Torch", typeof(Light));
        torch.transform.SetParent(boy.transform);
        torch.transform.localPosition = new Vector3(0.35f, 1.4f, 0.2f);
        var tl = torch.GetComponent<Light>();
        tl.type = LightType.Point;
        tl.color = new Color(1f, 0.62f, 0.25f);
        tl.range = 7;
        tl.intensity = 4;

        // mission markers (positions from the marsh map)
        var m = new GameObject("Mission");
        var mission = m.AddComponent<MarshMission>();
        mission.player = player.transform;
        mission.boy = boy.transform;
        mission.boyPath = new[]
        {
            Marker("Reed 1", M(-6, -34), m.transform).transform,
            Marker("Reed 2", M(-2, -42), m.transform).transform,
            Marker("Reed 3", M(0, -52), m.transform).transform,
        };
        mission.exitHelp = Marker("Exit (with the boy)", M(0, -55), m.transform).transform;
        mission.camp = Marker("Bandit camp", M(-48, -24, 4), m.transform).transform;
        mission.exitAlone = Marker("Exit (alone, over the ridge)", M(-58, -55), m.transform).transform;
        var campFire = new GameObject("Camp fire", typeof(Light));
        campFire.transform.position = M(-48, -24, 7);
        var cl = campFire.GetComponent<Light>();
        cl.type = LightType.Point;
        cl.color = new Color(1f, 0.55f, 0.2f);
        cl.range = 12;
        cl.intensity = 6;

        AddEventSystem();
        var ui = AddUI("HUD", panel, "Game.uxml");
        ui.AddComponent<GameHUD>().fonts = fonts;
        EditorSceneManager.SaveScene(scene, Scenes + "Marsh.unity");
    }
}

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ProjetoGTA;

/// <summary>
/// Script de setup do Projeto GTA.
/// Menu: GTA → Setup Completo
/// Monta a GameScene (Player, carro, câmera, HUD, GameManager, música),
/// cria a MainMenu e registra ambas no Build Settings.
/// Execute UMA VEZ com o projeto aberto no Unity Editor.
/// </summary>
public static class GTAGameSetup
{
    // ── Caminhos de cena ──────────────────────────────────────────────────────
    private const string GameScenePath  = "Assets/_Projeto_GTA/Scenes/GameScene.unity";
    private const string MenuScenePath  = "Assets/_Projeto_GTA/Scenes/MainMenu.unity";

    // ── Prefabs dos assets importados ─────────────────────────────────────────
    // Player (Synty POLYGON Starter – personagem masculino)
    private const string PlayerPrefabPath = "Assets/Synty/PolygonStarter/Prefabs/Characters/SM_Chr_Male_01.prefab";
    // Carro (Drivable-Free Low Poly Cars – versão sem LOD)
    private const string CarPrefabPath = "Assets/Drivable-Free Low Poly Cars/Prefabs/WithoutLod/Pick Up_7NL.prefab";
    // Áudio
    private const string MusicPath = "Assets/_Projeto_GTA/Audio/menu_music.wav";

    [MenuItem("GTA/★ Rodar Tudo (Setup + Animações)", priority = 0)]
    public static void RunEverything()
    {
        RunFullSetup();
        GTAAnimationSetup.SetupAnimations();
    }

    [MenuItem("GTA/Setup Completo", priority = 1)]
    public static void RunFullSetup()
    {
        bool saved = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        if (!saved)
        {
            Debug.Log("[GTASetup] Setup cancelado pelo usuário.");
            return;
        }

        SetupGameScene();
        SetupMainMenu();
        RegisterBuildScenes();

        Debug.Log("[GTASetup] ✅ Setup concluído! Abra MainMenu.unity e pressione Play.");
        EditorUtility.DisplayDialog("GTA Setup",
            "Setup concluído!\n\n" +
            "1. Abra Assets/_Projeto_GTA/Scenes/MainMenu.unity\n" +
            "2. Pressione Play\n" +
            "3. Clique em 'Jogar' para ir ao jogo\n\n" +
            "Controles:\n  WASD – mover   |  Mouse – câmera\n" +
            "  Shift – correr  |  Espaço – pular\n" +
            "  E – entrar/sair do carro\n" +
            "  Esc – pausar   |  F12 – screenshot", "OK");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GAME SCENE
    // ═══════════════════════════════════════════════════════════════════════════

    private static void SetupGameScene()
    {
        // Abre (ou cria) a GameScene.
        if (!File.Exists(GameScenePath))
        {
            Debug.LogWarning("[GTASetup] GameScene não encontrada em " + GameScenePath);
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        AudioClip music = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);

        // ── 1. Garante chão (BoxCollider) caso a cena não tenha collider de chão ──
        EnsureGroundCollider();

        // ── 2. Player ────────────────────────────────────────────────────────────
        GameObject player = SetupPlayer();

        // ── 3. Câmera principal ───────────────────────────────────────────────────
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            mainCam = camGO.AddComponent<Camera>();
            mainCam.gameObject.AddComponent<AudioListener>();
        }
        var tpCam = GetOrAdd<ThirdPersonCamera>(mainCam.gameObject);
        tpCam.target = player.transform;
        tpCam.targetOffset = new Vector3(0f, 1.6f, 0f);
        tpCam.distance = 5f;

        // ── 4. Carro ──────────────────────────────────────────────────────────────
        SetupCar(player.transform.position + new Vector3(5f, 0.3f, 0f));

        // ── 5. GameManager + HUD + Pausa ─────────────────────────────────────────
        GameObject gmGO = SetupGameManagerHUD();

        // ── 6. MusicManager ───────────────────────────────────────────────────────
        SetupMusicManager(music);

        // ── 7. Screenshot helper ──────────────────────────────────────────────────
        if (gmGO.GetComponent<ScreenshotCapture>() == null)
            gmGO.AddComponent<ScreenshotCapture>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GTASetup] GameScene salva.");
    }

    private static void EnsureGroundCollider()
    {
        // Verifica se já existe um collider de chão adequado (plano em y≈0).
        // Se não houver, cria um plano invisível para o player não cair pelo cenário.
        bool found = false;
        foreach (var col in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (col is BoxCollider bc && bc.gameObject.name.Contains("Ground"))
            { found = true; break; }
        }

        if (!found)
        {
            var g = new GameObject("SafeGround");
            g.layer = 0; // Default
            var bc = g.AddComponent<BoxCollider>();
            bc.size = new Vector3(500f, 0.5f, 500f);
            bc.center = Vector3.zero;
            g.transform.position = new Vector3(0f, -0.25f, 0f);
            Debug.Log("[GTASetup] SafeGround criado (collider de chão de segurança).");
        }
    }

    private static GameObject SetupPlayer()
    {
        // Remove TODOS os Players existentes (por tag e por nome) — evita duplicatas.
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go == null) continue;
            bool isPlayer = false;
            try { isPlayer = go.CompareTag("Player"); } catch { /* tag não registrada */ }
            if (isPlayer || go.name == "Player")
                Object.DestroyImmediate(go);
        }

        // Tenta carregar o prefab Synty.
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        GameObject player;
        if (prefab != null)
        {
            player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Player";
        }
        else
        {
            // Fallback: cápsula simples.
            Debug.LogWarning("[GTASetup] Prefab do player não encontrado em " + PlayerPrefabPath + ". Usando cápsula.");
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
        }

        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1.1f, 0f);

        // Rigidbody.
        var rb = GetOrAdd<Rigidbody>(player);
        rb.mass = 70f;
        rb.linearDamping = 5f;
        rb.angularDamping = 10f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // CapsuleCollider (não sobrescreve se o prefab já tiver).
        var cap = player.GetComponent<CapsuleCollider>();
        if (cap == null) { cap = player.AddComponent<CapsuleCollider>(); cap.height = 1.8f; cap.radius = 0.3f; cap.center = new Vector3(0f, 0.9f, 0f); }

        // Controlador.
        var ctrl = GetOrAdd<ThirdPersonPlayerController>(player);
        ctrl.walkSpeed = 4f;
        ctrl.runSpeed = 7f;

        return player;
    }

    private static void SetupCar(Vector3 position)
    {
        // Remove carro anterior se existir.
        var oldCar = GameObject.Find("PlayerCar");
        if (oldCar != null) Object.DestroyImmediate(oldCar);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarPrefabPath);
        GameObject car;
        if (prefab != null)
        {
            car = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            car.name = "PlayerCar";
        }
        else
        {
            Debug.LogWarning("[GTASetup] Prefab de carro não encontrado em " + CarPrefabPath + ". Usando cubo.");
            car = GameObject.CreatePrimitive(PrimitiveType.Cube);
            car.name = "PlayerCar";
            car.transform.localScale = new Vector3(2f, 1f, 4f);
        }

        car.transform.position = position;

        var rb = GetOrAdd<Rigidbody>(car);
        rb.mass = 1200f;
        rb.linearDamping = 2f;
        rb.angularDamping = 5f;
        rb.centerOfMass = new Vector3(0f, -0.4f, 0f);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (car.GetComponent<BoxCollider>() == null)
        {
            var bc = car.AddComponent<BoxCollider>();
            bc.size = new Vector3(2f, 1.2f, 4f);
            bc.center = new Vector3(0f, 0.6f, 0f);
        }

        GetOrAdd<CarController>(car);
        GetOrAdd<VehicleEnterExit>(car);
    }

    private static GameObject SetupGameManagerHUD()
    {
        // GameManager.
        var gmGO = GameObject.Find("GameManager");
        if (gmGO == null) gmGO = new GameObject("GameManager");
        var gm = GetOrAdd<GameManager>(gmGO);

        // Canvas principal.
        var canvas = FindOrCreateCanvas("HUDCanvas");

        // Painel de HUD com texto de dica.
        var hintPanel = FindOrCreate(canvas.gameObject, "HintPanel");
        var hintRect = GetOrAdd<RectTransform>(hintPanel);
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 30f);
        hintRect.sizeDelta = new Vector2(400f, 50f);

        var hintText = FindOrCreateText(hintPanel, "HintText", "[E] Entrar no carro", 22);
        hintText.alignment = TextAnchor.MiddleCenter;
        hintText.color = Color.white;

        // Painel de pausa.
        var pausePanel = FindOrCreate(canvas.gameObject, "PausePanel");
        var pauseRect = GetOrAdd<RectTransform>(pausePanel);
        pauseRect.anchorMin = Vector2.zero; pauseRect.anchorMax = Vector2.one;
        pauseRect.offsetMin = Vector2.zero; pauseRect.offsetMax = Vector2.zero;
        var pauseBg = GetOrAdd<Image>(pausePanel);
        pauseBg.color = new Color(0f, 0f, 0f, 0.6f);
        var pausePMC = GetOrAdd<PauseMenuController>(pausePanel);

        // Título "PAUSADO".
        FindOrCreateText(pausePanel, "PauseTitle", "PAUSADO", 40);

        // Botão Continuar.
        var btnResume = CreateButton(pausePanel, "BtnResume", "Continuar", new Vector2(0f, 60f));
        var btnResumeUI = btnResume.GetComponent<UnityEngine.UI.Button>();
        btnResumeUI.onClick.AddListener(pausePMC.Resume);

        // Botão Voltar ao Menu.
        var btnMenu = CreateButton(pausePanel, "BtnMenu", "Voltar ao Menu", new Vector2(0f, -20f));
        var btnMenuUI = btnMenu.GetComponent<UnityEngine.UI.Button>();
        btnMenuUI.onClick.AddListener(pausePMC.ReturnToMenu);

        pausePanel.SetActive(false);

        // Wires GameManager.
        gm.hintText = hintText;
        gm.pausePanel = pausePanel;

        // EventSystem.
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        return gmGO;
    }

    private static void SetupMusicManager(AudioClip clip)
    {
        var mm = Object.FindAnyObjectByType<MusicManager>();
        if (mm == null)
        {
            var go = new GameObject("MusicManager");
            mm = go.AddComponent<MusicManager>();
            var src = go.GetComponent<AudioSource>();
            if (src == null) src = go.AddComponent<AudioSource>();
            if (clip != null) { src.clip = clip; src.loop = true; src.playOnAwake = true; }
        }
        else if (clip != null)
        {
            var src = mm.GetComponent<AudioSource>();
            if (src != null) { src.clip = clip; src.loop = true; src.playOnAwake = true; }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MAIN MENU
    // ═══════════════════════════════════════════════════════════════════════════

    private static void SetupMainMenu()
    {
        // Cria ou reabre a cena de menu.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera + AudioListener.
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.07f, 0.07f);
        camGO.AddComponent<AudioListener>();

        // Luz.
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // MusicManager com áudio.
        AudioClip music = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
        var mmGO = new GameObject("MusicManager");
        mmGO.AddComponent<MusicManager>();
        var src = mmGO.AddComponent<AudioSource>();
        src.loop = true;
        src.playOnAwake = true;
        if (music != null) src.clip = music;

        // Canvas.
        var canvas = FindOrCreateCanvas("MenuCanvas");
        canvas.gameObject.AddComponent<MainMenuController>();
        var mainMenu = canvas.gameObject.GetComponent<MainMenuController>();

        // Fundo degradê (imagem escura).
        var bg = FindOrCreate(canvas.gameObject, "Background");
        var bgRect = GetOrAdd<RectTransform>(bg);
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        var bgImg = GetOrAdd<Image>(bg);
        bgImg.color = new Color(0.05f, 0.05f, 0.15f);

        // Título.
        var title = FindOrCreateText(canvas.gameObject, "Title", "GTA 3D", 60);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -80f);
        titleRect.sizeDelta = new Vector2(600f, 80f);
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;
        title.fontStyle = FontStyle.Bold;

        // Sub-título.
        var sub = FindOrCreateText(canvas.gameObject, "Subtitle", "Open World Racing", 24);
        var subRect = sub.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 1f);
        subRect.anchorMax = new Vector2(0.5f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0f, -170f);
        subRect.sizeDelta = new Vector2(500f, 40f);
        sub.alignment = TextAnchor.MiddleCenter;
        sub.color = new Color(0.8f, 0.8f, 0.8f);

        // Botão Jogar.
        var btnPlay = CreateButton(canvas.gameObject, "BtnPlay", "JOGAR", new Vector2(0f, 30f));
        var btnPlayUI = btnPlay.GetComponent<UnityEngine.UI.Button>();
        btnPlayUI.onClick.AddListener(mainMenu.PlayGame);

        // Botão Sair.
        var btnQuit = CreateButton(canvas.gameObject, "BtnQuit", "SAIR", new Vector2(0f, -60f));
        var btnQuitUI = btnQuit.GetComponent<UnityEngine.UI.Button>();
        btnQuitUI.onClick.AddListener(mainMenu.QuitGame);

        // EventSystem.
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MenuScenePath);
        Debug.Log("[GTASetup] MainMenu salva em " + MenuScenePath);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BUILD SETTINGS
    // ═══════════════════════════════════════════════════════════════════════════

    private static void RegisterBuildScenes()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
        };
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[GTASetup] Build Settings atualizado: [0] MainMenu, [1] GameScene.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPERS UI
    // ═══════════════════════════════════════════════════════════════════════════

    private static Canvas FindOrCreateCanvas(string name)
    {
        var existing = Object.FindAnyObjectByType<Canvas>();
        if (existing != null && existing.name == name) return existing;

        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject FindOrCreate(GameObject parent, string childName)
    {
        var t = parent.transform.Find(childName);
        if (t != null) return t.gameObject;
        var go = new GameObject(childName);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static Text FindOrCreateText(GameObject parent, string name, string content, int size)
    {
        var go = FindOrCreate(parent, name);
        var txt = GetOrAdd<Text>(go);
        txt.text = content;
        txt.fontSize = size;
        if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var rect = GetOrAdd<RectTransform>(go);
        rect.sizeDelta = new Vector2(600f, size + 20f);
        return txt;
    }

    private static GameObject CreateButton(GameObject parent, string name, string label, Vector2 anchoredPos)
    {
        var go = FindOrCreate(parent, name);
        var img = GetOrAdd<Image>(go);
        img.color = new Color(0.15f, 0.15f, 0.4f);
        var btn = GetOrAdd<UnityEngine.UI.Button>(go);

        var rect = GetOrAdd<RectTransform>(go);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(260f, 60f);
        rect.anchoredPosition = anchoredPos;

        // Label.
        var lblGO = FindOrCreate(go, "Label");
        var txt = GetOrAdd<Text>(lblGO);
        txt.text = label;
        txt.fontSize = 24;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.fontStyle = FontStyle.Bold;
        if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var lblRect = GetOrAdd<RectTransform>(lblGO);
        lblRect.anchorMin = Vector2.zero; lblRect.anchorMax = Vector2.one;
        lblRect.offsetMin = Vector2.zero; lblRect.offsetMax = Vector2.zero;

        return go;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }
}

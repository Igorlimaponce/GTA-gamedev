using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Cria o Animator Controller do player com Blend Tree Idle/Walk/Run.
/// Menu: GTA → Setup Animações
/// Execute após importar as 3 animações do Mixamo em Assets/_Projeto_GTA/Animations/
/// </summary>
public static class GTAAnimationSetup
{
    private const string AnimFolder    = "Assets/_Projeto_GTA/Animations";
    private const string ControllerPath = "Assets/_Projeto_GTA/Animations/PlayerAnimator.controller";

    [MenuItem("GTA/Setup Animações (rodar após Mixamo)", priority = 2)]
    public static void SetupAnimations()
    {
        // ── 1. Carrega os clips importados do Mixamo ─────────────────────────
        AnimationClip idle = FindClip("idle");
        AnimationClip walk = FindClip("walk");
        AnimationClip run  = FindClip("run");

        if (idle == null || walk == null || run == null)
        {
            string missing = (idle == null ? "Idle " : "") + (walk == null ? "Walking " : "") + (run == null ? "Running " : "");
            EditorUtility.DisplayDialog("GTA Animações",
                $"Clips não encontrados: {missing}\n\n" +
                "Certifique-se de ter importado os FBX do Mixamo em:\n" +
                "Assets/_Projeto_GTA/Animations/\n\n" +
                "Nomes esperados (não sensível a maiúsculas):\n" +
                "  • Qualquer FBX com 'idle' no nome\n" +
                "  • Qualquer FBX com 'walk' no nome\n" +
                "  • Qualquer FBX com 'run' no nome", "OK");
            return;
        }

        // ── 2. Garante que os clips têm rig Humanoid ─────────────────────────
        EnsureHumanoid(idle);
        EnsureHumanoid(walk);
        EnsureHumanoid(run);

        // ── 3. Cria (ou recria) o Animator Controller ────────────────────────
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        // Parâmetro Speed (0 = parado, 0.5 = andando, 1 = correndo)
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);

        // ── 4. Blend Tree de locomoção ───────────────────────────────────────
        AnimatorStateMachine rootSM = ctrl.layers[0].stateMachine;

        BlendTree blendTree;
        AnimatorState locomotionState = ctrl.CreateBlendTreeInController("Locomotion", out blendTree);
        blendTree.blendType      = BlendTreeType.Simple1D;
        blendTree.blendParameter = "Speed";
        blendTree.useAutomaticThresholds = false;

        blendTree.AddChild(idle, 0.00f);
        blendTree.AddChild(walk, 0.50f);
        blendTree.AddChild(run,  1.00f);

        rootSM.defaultState = locomotionState;

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();

        // ── 5. Atribui o controller ao Animator do Player na cena ────────────
        bool assignedToPlayer = false;
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var anim = player.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.runtimeAnimatorController = ctrl;
                anim.applyRootMotion = false;   // root motion conflita com Rigidbody
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                assignedToPlayer = true;
            }
        }

        string msg = assignedToPlayer
            ? "Animator Controller criado e atribuído ao Player!\n\nPressione Play para ver as animações."
            : "Animator Controller criado em:\n" + ControllerPath +
              "\n\nPlayer não encontrado na cena. Arraste o controller manualmente para o componente Animator do Player.";

        Debug.Log("[GTAAnims] " + msg);
        EditorUtility.DisplayDialog("GTA Animações", msg, "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AnimationClip FindClip(string keyword)
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { AnimFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string lower = Path.GetFileNameWithoutExtension(path).ToLower();
            if (lower.Contains(keyword.ToLower()))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null) return clip;
            }
        }

        // Tenta também pelo nome do asset dentro do FBX
        string[] fbxGuids = AssetDatabase.FindAssets("t:GameObject", new[] { AnimFolder });
        foreach (string guid in fbxGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;
            string lower = Path.GetFileNameWithoutExtension(path).ToLower();
            if (!lower.Contains(keyword.ToLower())) continue;

            // Carrega todos os objetos dentro do FBX
            var objs = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var o in objs)
                if (o is AnimationClip ac && !ac.name.Contains("__preview__"))
                    return ac;
        }
        return null;
    }

    private static void EnsureHumanoid(AnimationClip clip)
    {
        string path = AssetDatabase.GetAssetPath(clip);
        if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) return;

        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;

        bool changed = false;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            changed = true;
        }
        if (importer.clipAnimations.Length > 0 && !importer.clipAnimations[0].loopTime)
        {
            var clips = importer.clipAnimations;
            foreach (var c in clips) c.loopTime = true;
            importer.clipAnimations = clips;
            changed = true;
        }
        if (changed)
        {
            importer.SaveAndReimport();
            Debug.Log($"[GTAAnims] Reimportado como Humanoid + loop: {path}");
        }
    }
}

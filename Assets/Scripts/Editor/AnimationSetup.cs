#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace VoidBound.Editor
{
    // Configures the rigged character FBX imports (Generic rig, avatar, loop
    // flags on Idle/Walk) and builds one AnimatorController per character:
    // states Idle/Walk/Attack/Hit/Death driven by params Speed(float),
    // Attack/Hit(trigger), Dead(bool). Idempotent — controllers are rebuilt.
    public static class AnimationSetup
    {
        private const string AnimDir = "Assets/Animation";

        [MenuItem("VoidBound/Animation - Setup Rigs + Controllers")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(AnimDir))
                AssetDatabase.CreateFolder("Assets", "Animation");

            ConfigureImporter("Assets/Art/Models/Hero.fbx");
            ConfigureImporter("Assets/Art/Models/Goblin.fbx");

            BuildController("Assets/Art/Models/Hero.fbx", $"{AnimDir}/HeroAnimator.controller");
            BuildController("Assets/Art/Models/Goblin.fbx", $"{AnimDir}/GoblinAnimator.controller");

            AssetDatabase.SaveAssets();
            Debug.Log("[AnimSetup] Rigs configured and AnimatorControllers built.");
        }

        private static void ConfigureImporter(string fbx)
        {
            var imp = AssetImporter.GetAtPath(fbx) as ModelImporter;
            if (imp == null) { Debug.LogWarning($"[AnimSetup] No importer for {fbx}"); return; }

            imp.animationType = ModelImporterAnimationType.Generic;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            // Mark Idle/Walk clips looping (copy defaults, set loopTime).
            var clips = imp.defaultClipAnimations;
            foreach (var c in clips)
            {
                bool loop = c.name.Contains("Idle") || c.name.Contains("Walk");
                c.loopTime = loop;
            }
            imp.clipAnimations = clips;
            imp.SaveAndReimport();
        }

        private static AnimationClip FindClip(string fbx, string contains)
        {
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbx))
                if (a is AnimationClip c && !c.name.StartsWith("__preview") && c.name.Contains(contains))
                    return c;
            return null;
        }

        private static void BuildController(string fbx, string path)
        {
            AssetDatabase.DeleteAsset(path); // rebuild clean
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            var sm = ac.layers[0].stateMachine;
            var idle = sm.AddState("Idle");   idle.motion = FindClip(fbx, "Idle");
            var walk = sm.AddState("Walk");   walk.motion = FindClip(fbx, "Walk");
            var attack = sm.AddState("Attack"); attack.motion = FindClip(fbx, "Attack");
            var hit = sm.AddState("Hit");     hit.motion = FindClip(fbx, "Hit");
            var death = sm.AddState("Death"); death.motion = FindClip(fbx, "Death");
            sm.defaultState = idle;

            // Idle <-> Walk on Speed
            var iw = idle.AddTransition(walk); iw.hasExitTime = false; iw.duration = 0.12f;
            iw.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            var wi = walk.AddTransition(idle); wi.hasExitTime = false; wi.duration = 0.12f;
            wi.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            // Any -> Attack / Hit (triggers), then return to Idle after playing
            AnyTo(sm, attack, "Attack");
            AnyTo(sm, hit, "Hit");
            Return(attack, idle);
            Return(hit, idle);

            // Any -> Death (bool), terminal
            var toDeath = sm.AddAnyStateTransition(death);
            toDeath.hasExitTime = false; toDeath.duration = 0.1f;
            toDeath.canTransitionToSelf = false;
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");

            EditorUtility.SetDirty(ac);
        }

        private static void AnyTo(AnimatorStateMachine sm, AnimatorState state, string trigger)
        {
            var t = sm.AddAnyStateTransition(state);
            t.hasExitTime = false;
            t.duration = 0.05f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void Return(AnimatorState from, AnimatorState to)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = true;
            t.exitTime = 0.85f;
            t.duration = 0.1f;
        }
    }
}
#endif

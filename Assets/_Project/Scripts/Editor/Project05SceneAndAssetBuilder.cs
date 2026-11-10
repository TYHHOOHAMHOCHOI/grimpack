using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Project05.Core;
using Project05.Data;
using Project05.UI;

namespace Project05.Editor
{
    /// <summary>
    /// Automates generating ScriptableObject catalog assets, configuring the 2D URP visual baseline,
    /// and building the playable MainGame.unity scene with ThreeColumnLayoutManager.
    ///
    /// NON-DESTRUCTIVE: only creates what is missing. Existing scenes and ScriptableObject assets are
    /// never overwritten, and existing Build Settings entries are preserved. To regenerate a scene or
    /// asset on purpose, delete that file first and run the menu item again.
    /// </summary>
    public static class Project05SceneAndAssetBuilder
    {
        private const string MainScenePath = "Assets/_Project/Scenes/MainGame.unity";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Project05/Setup Full Game Scene & Assets")]
        public static void SetupAll()
        {
            EnsureDirectories();
            int createdAssets = CreateMissingScriptableObjectAssets();
            ConfigureQualityAndPixelSettings();
            bool builtMain = BuildSceneIfMissing(MainScenePath);
            bool builtSample = BuildSceneIfMissing(SampleScenePath);
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Project05] Setup finished (non-destructive). New ScriptableObject assets: {createdAssets}, " +
                      $"MainGame scene built: {builtMain}, SampleScene built: {builtSample}. Existing files were left untouched.");
        }

        private static void EnsureDirectories()
        {
            string[] dirs =
            {
                "Assets/_Project",
                "Assets/_Project/Scenes",
                "Assets/_Project/ScriptableObjects",
                "Assets/_Project/ScriptableObjects/Items",
                "Assets/_Project/ScriptableObjects/Heroes",
                "Assets/_Project/ScriptableObjects/Monsters",
                "Assets/_Project/ScriptableObjects/Buffs"
            };

            for (int i = 0; i < dirs.Length; i++)
            {
                if (!Directory.Exists(dirs[i]))
                {
                    Directory.CreateDirectory(dirs[i]);
                }
            }
        }

        private static int CreateMissingScriptableObjectAssets()
        {
            DefaultGameCatalog.EnsureInitialized();
            int created = 0;

            foreach (ItemDefinitionSO item in DefaultGameCatalog.AllItems)
            {
                if (CreateAssetIfMissing(item, $"Assets/_Project/ScriptableObjects/Items/{item.ItemId}.asset")) created++;
            }

            foreach (HeroDefinitionSO hero in DefaultGameCatalog.AllHeroes)
            {
                if (CreateAssetIfMissing(hero, $"Assets/_Project/ScriptableObjects/Heroes/{hero.HeroId}.asset")) created++;
            }

            foreach (MonsterDefinitionSO monster in DefaultGameCatalog.AllMonsters)
            {
                if (CreateAssetIfMissing(monster, $"Assets/_Project/ScriptableObjects/Monsters/{monster.MonsterId}.asset")) created++;
            }

            foreach (BuffDefinitionSO buff in DefaultGameCatalog.AllBuffs)
            {
                if (CreateAssetIfMissing(buff, $"Assets/_Project/ScriptableObjects/Buffs/{buff.BuffId}.asset")) created++;
            }

            return created;
        }

        /// <summary>
        /// Creates a ScriptableObject asset only when no file exists at <paramref name="path"/>.
        /// Never overwrites: a file that exists on disk is kept even if Unity cannot load it as T.
        /// </summary>
        private static bool CreateAssetIfMissing<T>(T source, string path) where T : ScriptableObject
        {
            if (source == null) return false;
            if (File.Exists(path)) return false;

            T clone = Object.Instantiate(source);
            clone.name = source.name;
            AssetDatabase.CreateAsset(clone, path);
            return true;
        }

        private static void ConfigureQualityAndPixelSettings()
        {
            QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        }

        /// <summary>
        /// Builds the scene at <paramref name="scenePath"/> only if that file does not exist yet.
        /// Returns true when a new scene was written, false when the existing scene was left untouched.
        /// </summary>
        private static bool BuildSceneIfMissing(string scenePath)
        {
            if (File.Exists(scenePath))
            {
                Debug.Log($"[Project05] Scene already exists, left untouched: {scenePath}");
                return false;
            }

            // NewScene(Single) replaces whatever is currently open, so let the user save it first.
            // Batch mode has no dialogs and nothing to save, so it is skipped there.
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning($"[Project05] Scene build cancelled to keep unsaved changes: {scenePath}");
                return false;
            }

            string directory = Path.GetDirectoryName(scenePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            BuildMainGameScene(scenePath);
            return true;
        }

        private static void BuildMainGameScene(string scenePath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Main Camera with 2D URP + PixelPerfectCamera
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.625f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.AbyssBg;
            cam.allowMSAA = false;
            cam.allowHDR = false;

            UniversalAdditionalCameraData urpCamData = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCamData.renderPostProcessing = true;

            PixelPerfectCamera ppCam = camGo.AddComponent<PixelPerfectCamera>();
            ppCam.assetsPPU = 16;
            ppCam.refResolutionX = 640;
            ppCam.refResolutionY = 360;
            ppCam.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            ppCam.cropFrame = PixelPerfectCamera.CropFrame.None;

            // 2. Global 2D Light
            GameObject lightGo = new GameObject("Global Light 2D");
            Light2D light2D = lightGo.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = new Color(0.95f, 0.92f, 0.88f, 1f);
            light2D.intensity = 1.0f;

            // 3. Global Post-Processing Volume (Dark Fantasy Vignette + Subtle Bloom)
            GameObject volGo = new GameObject("Global Volume");
            Volume vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            string profilePath = "Assets/_Project/Scenes/DarkFantasyVolumeProfile.asset";
            VolumeProfile existingProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (existingProfile == null)
            {
                Bloom bloom = profile.Add<Bloom>(true);
                bloom.threshold.Override(0.9f);
                bloom.intensity.Override(0.45f);

                Vignette vignette = profile.Add<Vignette>(true);
                vignette.intensity.Override(0.28f);

                Tonemapping tonemap = profile.Add<Tonemapping>(true);
                tonemap.mode.Override(TonemappingMode.ACES);

                AssetDatabase.CreateAsset(profile, profilePath);
                existingProfile = profile;
            }
            vol.sharedProfile = existingProfile;

            // 4. ThreeColumnLayoutManager Root
            GameObject gameManagerGo = new GameObject("Project05_GameController");
            gameManagerGo.AddComponent<ThreeColumnLayoutManager>();

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        /// <summary>
        /// Adds MainGame/SampleScene to Build Settings only if they are not listed yet.
        /// Existing entries (including their order, enabled state and any other scenes) are preserved.
        /// </summary>
        private static void EnsureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool changed = false;
            changed |= AddSceneToBuildSettingsIfMissing(scenes, MainScenePath);
            changed |= AddSceneToBuildSettingsIfMissing(scenes, SampleScenePath);

            if (changed)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        private static bool AddSceneToBuildSettingsIfMissing(List<EditorBuildSettingsScene> scenes, string scenePath)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath) return false;
            }

            if (!File.Exists(scenePath)) return false;

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            return true;
        }
    }
}

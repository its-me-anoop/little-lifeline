using System;
using System.IO;
using IdleClinic.ProgressionView;
using OrbitOrchard.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IdleClinic.ProgressionEditor
{
    /// <summary>Scene and iOS simulator export for the boss progression prototype. Leaves the shipped Clinic scene alone.</summary>
    public static class ProgressionBuild
    {
        private const string ScenePath = "Assets/IdleClinic/Scenes/Progression.unity";

        [MenuItem("Idle Clinic/Progression/Create scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Progression").AddComponent<ProgressionStage>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
        }

        [MenuItem("Idle Clinic/Progression/Build iOS Simulator")]
        public static void BuildIOSSimulator()
        {
            var restoreScenes = EditorBuildSettings.scenes;
            OrchardBuild.Prepare();
            CreateScene();
            var output = Environment.GetEnvironmentVariable("PROGRESSION_IOS_EXPORT");
            if (string.IsNullOrEmpty(output)) output = "Builds/iOSProgression";
            var previousArchitecture = PlayerSettings.iOS.simulatorSdkArchitecture;
            try
            {
                PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
                PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64;
                Directory.CreateDirectory(output);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath }, locationPathName = output,
                    target = BuildTarget.iOS, options = BuildOptions.Development
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException("Progression simulator export failed: " + report.summary.result);
            }
            finally
            {
                PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
                PlayerSettings.iOS.simulatorSdkArchitecture = previousArchitecture;
                EditorBuildSettings.scenes = restoreScenes;
                AssetDatabase.SaveAssets();
            }
            Debug.Log("Progression iOS simulator export succeeded: " + output);
        }
    }
}

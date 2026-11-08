using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Farhat.EditorTools
{
    /// <summary>
    /// Command-line builds. Example:
    /// Unity -batchmode -nographics -quit -projectPath . -executeMethod Farhat.EditorTools.BuildScript.BuildWindows64 -buildOutput Build/Win64/Farhat.exe [-releaseBuild]
    /// A release build adds the RELEASE_BUILD define, which activates the story gate.
    /// </summary>
    public static class BuildScript
    {
        const string ReleaseDefine = "RELEASE_BUILD";

        public static void BuildWindows64()
        {
            Build(BuildTarget.StandaloneWindows64, NamedBuildTarget.Standalone, "Build/Win64/Farhat.exe");
        }

        public static void BuildLinux64()
        {
            Build(BuildTarget.StandaloneLinux64, NamedBuildTarget.Standalone, "Build/Linux64/Farhat.x86_64");
        }

        public static void BuildMacOS()
        {
            Build(BuildTarget.StandaloneOSX, NamedBuildTarget.Standalone, "Build/macOS/Farhat.app");
        }

        static void Build(BuildTarget target, NamedBuildTarget named, string defaultOutput)
        {
            string output = Arg("-buildOutput", defaultOutput);
            bool release = Array.IndexOf(Environment.GetCommandLineArgs(), "-releaseBuild") >= 0;

            string previousDefines = PlayerSettings.GetScriptingDefineSymbols(named);
            if (release && !previousDefines.Contains(ReleaseDefine))
            {
                PlayerSettings.SetScriptingDefineSymbols(named, string.IsNullOrEmpty(previousDefines) ? ReleaseDefine : previousDefines + ";" + ReleaseDefine);
            }

            List<string> scenes = new List<string>();
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                if (EditorBuildSettings.scenes[i].enabled) scenes.Add(EditorBuildSettings.scenes[i].path);
            }
            if (scenes.Count == 0)
            {
                Debug.LogError("[Farhat] No scenes in the build settings. Run Farhat > Content > Generate Greybox Scenes first.");
                Exit(2);
                return;
            }

            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = scenes.ToArray();
            options.locationPathName = output;
            options.target = target;
            options.options = BuildOptions.None;

            UnityEditor.Build.Reporting.BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                PlayerSettings.SetScriptingDefineSymbols(named, previousDefines);
            }
            bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            Debug.Log("[Farhat] Build " + (ok ? "succeeded" : "FAILED") + ": " + report.summary.totalErrors + " errors, " + report.summary.totalWarnings + " warnings, output " + output);
            Exit(ok ? 0 : 1);
        }

        static string Arg(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return fallback;
        }

        static void Exit(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}

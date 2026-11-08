using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Farhat.EditorTools
{
    /// <summary>
    /// Installs the packages the game needs by name, so Unity resolves the version verified for the editor in use.
    /// Batch: Unity -batchmode -nographics -quit -projectPath . -executeMethod Farhat.EditorTools.PackageInstaller.InstallAll
    /// </summary>
    public static class PackageInstaller
    {
        public static readonly string[] Packages =
        {
            "com.unity.render-pipelines.universal",
            "com.unity.inputsystem",
            "com.unity.cinemachine",
            "com.unity.ai.navigation",
            "com.unity.timeline",
            "com.unity.probuilder",
            "com.unity.addressables",
            "com.unity.localization",
            "com.unity.test-framework",
            "com.unity.visualeffectgraph",
            "com.unity.shadergraph"
        };

        [MenuItem("Farhat/Setup/Install Packages")]
        public static void InstallAll()
        {
            List<string> failed = new List<string>();
            for (int i = 0; i < Packages.Length; i++)
            {
                AddRequest request = Client.Add(Packages[i]);
                DateTime deadline = DateTime.UtcNow.AddMinutes(5);
                while (!request.IsCompleted && DateTime.UtcNow < deadline) Thread.Sleep(100);
                if (!request.IsCompleted || request.Status != StatusCode.Success)
                {
                    string reason = request.IsCompleted && request.Error != null ? request.Error.message : "timed out";
                    failed.Add(Packages[i] + ": " + reason);
                }
                else
                {
                    Debug.Log("[Farhat] Installed " + Packages[i] + " " + request.Result.version);
                }
            }
            if (failed.Count > 0) Debug.LogError("[Farhat] Package install problems:\n - " + string.Join("\n - ", failed.ToArray()));
            if (Application.isBatchMode) EditorApplication.Exit(failed.Count == 0 ? 0 : 1);
        }
    }
}

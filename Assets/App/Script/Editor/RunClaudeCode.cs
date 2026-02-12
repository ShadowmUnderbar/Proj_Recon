using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    public static class RunClaudeCode
    {
        [MenuItem("Tools/Run Claude")]
        static void RunBat()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (projectRoot == null)
            {
                return;
            }

            var batPath = Path.Combine(projectRoot, "Tools", "run_claude.bat");

            var psi = new ProcessStartInfo
            {
                FileName = batPath,
                UseShellExecute = true,
                CreateNoWindow = false
            };

            Process.Start(psi);
        }
    }
}
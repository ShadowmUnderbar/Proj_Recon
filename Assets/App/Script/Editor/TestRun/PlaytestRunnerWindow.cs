using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace App.Editor.TestRun
{
    public class PlaytestRunnerWindow : EditorWindow
    {
        private string[] _scenarioNames = Array.Empty<string>();
        private int _selectedScenarioIndex;
        private int _waves = 3;
        private Process _process;
        private string _lastReportSummary = "";

        [MenuItem("Tools/Playtest Runner")]
        private static void Open()
        {
            GetWindow<PlaytestRunnerWindow>("Playtest Runner");
        }

        private void OnEnable()
        {
            RefreshScenarios();
        }

        private void RefreshScenarios()
        {
            var scenariosDir = GetScenariosDir();
            if (!Directory.Exists(scenariosDir))
            {
                _scenarioNames = Array.Empty<string>();
                return;
            }

            _scenarioNames = Directory.GetFiles(scenariosDir, "*.ps1")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(name => name)
                .ToArray();
        }

        private static string GetProjectRoot()
        {
            return Directory.GetParent(Application.dataPath)?.FullName;
        }

        private static string GetScenariosDir()
        {
            return Path.Combine(GetProjectRoot(), "Tools", "Playtest", "Scenarios");
        }

        private static string GetReportsDir()
        {
            return Path.Combine(GetProjectRoot(), "Tools", "Playtest", "Reports");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("シナリオ", EditorStyles.boldLabel);

            if (_scenarioNames.Length == 0)
            {
                EditorGUILayout.HelpBox("Tools/Playtest/Scenarios/ にシナリオファイル(.ps1)が見つかりません", MessageType.Warning);
                if (GUILayout.Button("再読込"))
                {
                    RefreshScenarios();
                }
                return;
            }

            _selectedScenarioIndex = EditorGUILayout.Popup("シナリオ", _selectedScenarioIndex, _scenarioNames);
            _waves = EditorGUILayout.IntField("対象ウェーブ数", _waves);

            bool isRunning = _process != null && !_process.HasExited;

            using (new EditorGUI.DisabledScope(isRunning))
            {
                if (GUILayout.Button("テストラン実行"))
                {
                    RunPlaytest(_scenarioNames[_selectedScenarioIndex], _waves);
                }
            }

            if (isRunning)
            {
                EditorGUILayout.HelpBox("実行中...(PowerShellウィンドウで進行状況を確認できます)", MessageType.Info);
            }

            if (!string.IsNullOrEmpty(_lastReportSummary))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("直近の結果", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(_lastReportSummary, MessageType.None);
            }
        }

        private void RunPlaytest(string scenario, int waves)
        {
            var projectRoot = GetProjectRoot();
            var scriptPath = Path.Combine(projectRoot, "Tools", "Playtest", "run-playtest.ps1");

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Scenario {scenario} -Waves {waves}",
                UseShellExecute = true,
                CreateNoWindow = false
            };

            _process = Process.Start(psi);
            _lastReportSummary = "";
            EditorApplication.update += PollProcess;
        }

        private void PollProcess()
        {
            if (_process == null || !_process.HasExited)
            {
                Repaint();
                return;
            }

            EditorApplication.update -= PollProcess;
            _lastReportSummary = ReadLatestReportSummary();
            Repaint();
        }

        private static string ReadLatestReportSummary()
        {
            var reportsDir = GetReportsDir();
            if (!Directory.Exists(reportsDir))
            {
                return "レポートが見つかりませんでした";
            }

            var latest = Directory.GetFiles(reportsDir, "playtest_*.json")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (latest == null)
            {
                return "レポートが見つかりませんでした";
            }

            return File.ReadAllText(latest);
        }
    }
}

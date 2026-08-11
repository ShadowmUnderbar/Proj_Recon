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
        private string[] _probeNames = Array.Empty<string>();
        private int _selectedProbeIndex;
        private Process _process;
        private string _lastReportSummary = "";

        // 直近に走らせたのがテストランかプローブかで読むレポートのファイル名が変わる
        private string _lastReportPrefix = "playtest_";

        [MenuItem("Tools/Playtest Runner")]
        private static void Open()
        {
            GetWindow<PlaytestRunnerWindow>("Playtest Runner");
        }

        private void OnEnable()
        {
            RefreshFileLists();
        }

        private void RefreshFileLists()
        {
            _scenarioNames = ListPs1Names(GetScenariosDir());
            _probeNames = ListPs1Names(GetProbesDir());
        }

        private static string[] ListPs1Names(string dir)
        {
            if (!Directory.Exists(dir))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(dir, "*.ps1")
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

        private static string GetProbesDir()
        {
            return Path.Combine(GetProjectRoot(), "Tools", "Playtest", "Probes");
        }

        private static string GetReportsDir()
        {
            return Path.Combine(GetProjectRoot(), "Tools", "Playtest", "Reports");
        }

        private void OnGUI()
        {
            bool isRunning = _process != null && !_process.HasExited;

            if (GUILayout.Button("一覧を再読込"))
            {
                RefreshFileLists();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("テストラン（バトルを自動プレイしてエラーを検出）", EditorStyles.boldLabel);

            if (_scenarioNames.Length == 0)
            {
                EditorGUILayout.HelpBox("Tools/Playtest/Scenarios/ にシナリオファイル(.ps1)が見つかりません", MessageType.Warning);
            }
            else
            {
                _selectedScenarioIndex = EditorGUILayout.Popup("シナリオ", _selectedScenarioIndex, _scenarioNames);
                _waves = EditorGUILayout.IntField("対象ウェーブ数", _waves);

                using (new EditorGUI.DisabledScope(isRunning))
                {
                    if (GUILayout.Button("テストラン実行"))
                    {
                        RunPlaytest(_scenarioNames[_selectedScenarioIndex], _waves);
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("演出の数値検証（プローブ）", EditorStyles.boldLabel);

            if (_probeNames.Length == 0)
            {
                EditorGUILayout.HelpBox("Tools/Playtest/Probes/ にプローブファイル(.ps1)が見つかりません", MessageType.Warning);
            }
            else
            {
                _selectedProbeIndex = EditorGUILayout.Popup("プローブ", _selectedProbeIndex, _probeNames);

                using (new EditorGUI.DisabledScope(isRunning))
                {
                    if (GUILayout.Button("プローブ実行"))
                    {
                        RunProbe(_probeNames[_selectedProbeIndex]);
                    }
                }
            }

            if (isRunning)
            {
                EditorGUILayout.Space();
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
            StartRunner($"-Scenario {scenario} -Waves {waves}", "run-playtest.ps1", "playtest_");
        }

        private void RunProbe(string probe)
        {
            StartRunner($"-Probe {probe}", "probe-effect.ps1", "probe_");
        }

        private void StartRunner(string scriptArgs, string scriptName, string reportPrefix)
        {
            var scriptPath = Path.Combine(GetProjectRoot(), "Tools", "Playtest", scriptName);

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" {scriptArgs}",
                UseShellExecute = true,
                CreateNoWindow = false
            };

            _process = Process.Start(psi);
            _lastReportPrefix = reportPrefix;
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
            _lastReportSummary = ReadLatestReportSummary(_lastReportPrefix);
            Repaint();
        }

        private static string ReadLatestReportSummary(string reportPrefix)
        {
            var reportsDir = GetReportsDir();
            if (!Directory.Exists(reportsDir))
            {
                return "レポートが見つかりませんでした";
            }

            var latest = Directory.GetFiles(reportsDir, reportPrefix + "*.json")
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

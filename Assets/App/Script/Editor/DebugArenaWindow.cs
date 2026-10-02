using System.Collections.Generic;
using System.Linq;
using App.Battle.Data;
using App.Common.Data;
using App.Common.Data.Database;
using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    /// <summary>
    /// デバッグ用: 通常のプレイ（メインメニュー → ウェーブ進行）とは別の入口で、任意の敵・ボスグループと戦うウィンドウ。
    /// 「対戦開始」で Battle シーンを再生し、セット選択・ウェーブ進行・周期スポーンを飛ばして選んだ相手だけを出す。
    /// 配置はボスウェーブと同じ（BossWaveConfig のプレイヤー位置・ボスの基準点）。開始時アップグレードはそのまま付与される。
    /// 入力内容は EditorPrefs に残す。起動の実体は <see cref="DebugArenaLauncher"/>。
    /// </summary>
    public class DebugArenaWindow : EditorWindow
    {
        private const string MenuName = "App/デバッグ: 敵と対戦";
        private const string WindowStateKey = "DebugArenaWindow.Request";
        private const string IsBossGroupKey = "DebugArenaWindow.IsBossGroup";
        private const int MaxEnemyCount = 10;

        private static readonly string[] TargetKindLabels = { "ボスグループ", "敵（1種類）" };

        private DebugArenaRequest _request;
        private bool _isBossGroup = true;

        private string[] _bossGroupPaths = new string[0];
        private string[] _bossGroupLabels = new string[0];
        private string[] _enemyCodes = new string[0];
        private string[] _enemyLabels = new string[0];

        // BossWaveConfig のボスウェーブの番号（見つからなければ 0）。「ボスウェーブ」ボタンに使う
        private int _bossWaveNumber;

        [MenuItem(MenuName)]
        private static void Open()
        {
            var window = GetWindow<DebugArenaWindow>();
            window.titleContent = new GUIContent("敵と対戦");
            window.minSize = new Vector2(340f, 300f);
            window.Show();
        }

        private void OnEnable()
        {
            _request = LoadState();
            _isBossGroup = EditorPrefs.GetBool(IsBossGroupKey, true);
            RefreshCandidates();
        }

        private void OnFocus()
        {
            // ボスグループ・敵のアセットを追加したあとでも、開き直さずに候補へ出す
            RefreshCandidates();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Battle シーンを再生し、ウェーブ進行・周期スポーン・セット選択を止めて、選んだ相手とだけ戦います。\n" +
                "・配置はボスウェーブと同じ（BossWaveConfig のプレイヤー位置・ボスの基準点）\n" +
                "・ウェーブ番号は敵の強さの倍率に使います（ボスウェーブの番号でもボスウェーブにはしません）\n" +
                "・開始時アップグレード（App/デバッグ: 開始時アップグレード）は付与されます\n" +
                "・ゲームオーバーからのリスタートも同じ相手で始まります。予約は再生1回ぶんだけ有効です",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("相手", EditorStyles.boldLabel);
            _isBossGroup = GUILayout.Toolbar(_isBossGroup ? 0 : 1, TargetKindLabels) == 0;

            if (_isBossGroup)
            {
                DrawBossGroupSelector();
            }
            else
            {
                DrawEnemySelector();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("対戦中", EditorStyles.boldLabel);
            DrawWaveField();
            _request.AutoRespawn = EditorGUILayout.Toggle("倒したら出し直す", _request.AutoRespawn);
            using (new EditorGUI.DisabledScope(!_request.AutoRespawn))
            {
                _request.RespawnDelaySeconds = Mathf.Max(0f,
                    EditorGUILayout.FloatField("出し直すまでの秒数", _request.RespawnDelaySeconds));
            }

            _request.Invincible = EditorGUILayout.Toggle(
                new GUIContent("プレイヤー無敵", "HP を減らさない。被弾の通知（被弾条件のバフ・バリアの吸収）は通常どおり"),
                _request.Invincible);

            if (EditorGUI.EndChangeCheck())
            {
                SaveState();
            }

            EditorGUILayout.Space();
            DrawLaunchButton();
        }

        private void DrawBossGroupSelector()
        {
            if (_bossGroupPaths.Length == 0)
            {
                EditorGUILayout.HelpBox("BossGroupConfig のアセットが見つかりません", MessageType.Warning);
                return;
            }

            var index = Mathf.Max(0, System.Array.IndexOf(_bossGroupPaths, _request.BossGroupAssetPath));
            index = EditorGUILayout.Popup("ボスグループ", index, _bossGroupLabels);
            _request.BossGroupAssetPath = _bossGroupPaths[index];
        }

        private void DrawEnemySelector()
        {
            if (_enemyCodes.Length == 0)
            {
                EditorGUILayout.HelpBox("EnemyDatabase に Boss ランク以外の敵が見つかりません", MessageType.Warning);
                return;
            }

            var index = Mathf.Max(0, System.Array.IndexOf(_enemyCodes, _request.EnemyCode));
            index = EditorGUILayout.Popup("敵", index, _enemyLabels);
            _request.EnemyCode = _enemyCodes[index];
            _request.EnemyCount = EditorGUILayout.IntSlider("同時に出す数", _request.EnemyCount, 1, MaxEnemyCount);

            EditorGUILayout.HelpBox("Boss ランクの個体は台本の命令でしか動かないため、ボスグループから選んでください", MessageType.None);
        }

        private void DrawWaveField()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _request.Wave = Mathf.Max(1, EditorGUILayout.IntField(
                    new GUIContent("ウェーブ番号", "敵の HP・攻撃力はこのウェーブの倍率で出る。ウェーブは進まない"),
                    _request.Wave));

                using (new EditorGUI.DisabledScope(_bossWaveNumber <= 0))
                {
                    var label = _bossWaveNumber > 0 ? $"ボスウェーブ（{_bossWaveNumber}）" : "ボスウェーブ";
                    if (GUILayout.Button(label, GUILayout.Width(110f)))
                    {
                        _request.Wave = _bossWaveNumber;
                        GUI.FocusControl(null);
                    }
                }
            }
        }

        /// <summary>BossWaveConfig からボスウェーブの番号を探す（ボスグループ未設定などで無ければ 0）</summary>
        private static int FindBossWaveNumber()
        {
            const int maxSearchWave = 99;
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(BossWaveConfig)}"))
            {
                var config = AssetDatabase.LoadAssetAtPath<BossWaveConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (config == null)
                {
                    continue;
                }

                for (var wave = 1; wave <= maxSearchWave; wave++)
                {
                    if (config.IsBossWave(wave))
                    {
                        return wave;
                    }
                }
            }

            return 0;
        }

        private void DrawLaunchButton()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("再生中です。相手を変えるときは再生を止めてから開始してください", MessageType.None);
                return;
            }

            var hasTarget = _isBossGroup ? _bossGroupPaths.Length > 0 : _enemyCodes.Length > 0;
            using (new EditorGUI.DisabledScope(!hasTarget))
            {
                if (GUILayout.Button("対戦開始（Battle シーンで再生）", GUILayout.Height(32f)))
                {
                    DebugArenaLauncher.Launch(BuildLaunchRequest());
                }
            }
        }

        /// <summary>選んでいない側（ボスグループ／敵）の値を空にした予約を作る</summary>
        private DebugArenaRequest BuildLaunchRequest()
        {
            return new DebugArenaRequest
            {
                BossGroupAssetPath = _isBossGroup ? _request.BossGroupAssetPath : string.Empty,
                EnemyCode = _isBossGroup ? string.Empty : _request.EnemyCode,
                EnemyCount = _request.EnemyCount,
                Wave = _request.Wave,
                AutoRespawn = _request.AutoRespawn,
                RespawnDelaySeconds = _request.RespawnDelaySeconds,
                Invincible = _request.Invincible
            };
        }

        private void RefreshCandidates()
        {
            _bossGroupPaths = AssetDatabase.FindAssets($"t:{nameof(BossGroupConfig)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .ToArray();
            _bossGroupLabels = _bossGroupPaths
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .ToArray();

            var enemies = new List<(string Code, string Label)>();
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(EnemyDatabase)}"))
            {
                var database = AssetDatabase.LoadAssetAtPath<EnemyDatabase>(AssetDatabase.GUIDToAssetPath(guid));
                if (database == null || database.EnemyMasterData == null)
                {
                    continue;
                }

                foreach (var enemy in database.EnemyMasterData)
                {
                    if (enemy == null || enemy.EnemyRankType == EnemyRankType.Boss
                        || enemies.Any(e => e.Code == enemy.EnemyMasterDataId))
                    {
                        continue;
                    }

                    enemies.Add((enemy.EnemyMasterDataId, $"{enemy.EnemyMasterDataId}（{enemy.EnemyRankType} / {enemy.name}）"));
                }
            }

            enemies.Sort((a, b) => string.CompareOrdinal(a.Code, b.Code));
            _enemyCodes = enemies.Select(e => e.Code).ToArray();
            _enemyLabels = enemies.Select(e => e.Label).ToArray();

            _bossWaveNumber = FindBossWaveNumber();
        }

        private static DebugArenaRequest LoadState()
        {
            var json = EditorPrefs.GetString(WindowStateKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonUtility.FromJson<DebugArenaRequest>(json) ?? new DebugArenaRequest();
                }
                catch (System.ArgumentException)
                {
                    // 壊れた保存内容は捨てて既定値から始める
                }
            }

            return new DebugArenaRequest();
        }

        private void SaveState()
        {
            // ボスグループと敵の両方の選択を残し、切り替えても選び直さずに済むようにする
            EditorPrefs.SetString(WindowStateKey, JsonUtility.ToJson(_request));
            EditorPrefs.SetBool(IsBossGroupKey, _isBossGroup);
        }
    }
}

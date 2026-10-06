#
# プレイヤー弾の貫通数（PlayerBaseParameterConfig の _basePenetration / _mergePenetration）と、
# フォーカス弾の「フォーカス対象に当たるまで貫通数に関係なく貫通し続ける」仕様を確かめるプローブ。
#
# デバッグ対戦（Request-DebugArena）で耐性方向の無い周回型の雑魚（CommonOrbit）を5体出し、
# プレイヤーの正面に一列（2.5m 間隔）に並べてから、即着弾の弾を手前から撃ち、何体目まで当たったかを数える。
#   - 弾のパラメータは IPlayerBulletParameterDataStore.GetBulletData から取る（貫通数は設定値のまま）。
#     敵を倒して列が崩れないよう、ダメージは極小に・爆風は無しに差し替え、撃つたびに敵の体力を戻す
#     （被弾ダメージには最低保証1があり、ダメージを極小にしただけでは低HPの雑魚が倒れる）
#   - 貫通数 N の弾は手前から N+1 体に当たって止まる（貫通数＝突き抜ける敵の数）
#   - フォーカス弾は貫通数を超えてもフォーカス対象に当たるまで貫通し、対象に当たったら止まる
#   - ブルズアイ（PenetrationCount 条件バフ）の倍率が「貫通した数」で段階的に上がる（1体目は常に1.0倍）
# 敵を動かしてから撃つまでを同じスニペット内で行う（即着弾は Spawn の中で同期的に判定される）。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

# 突撃型（CommonRush）は接近して自爆し列が欠けるため、近づいてこない周回型を使う
$Global:PenetrationEnemyCode = 'O-001'
$Global:PenetrationEnemyCount = 5

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -EnemyCode $Global:PenetrationEnemyCode -EnemyCount $Global:PenetrationEnemyCount -AutoRespawn $false
    }
    catch {
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Exit-BossProbeScene
}

function Invoke-PenetrationShot {
    # 敵を一列に並べて1発撃ち、当たった敵の列番号（手前から0始まり）を返す
    param(
        [Parameter(Mandatory)] [string]$ShotType,
        [Parameter(Mandatory)] [string]$FocusType,
        # フォーカス対象にする列番号。-1 ならフォーカス対象なし
        [int]$FocusIndex = -1,
        # 0以上なら貫通数を上書きする（列の組み立てが正しいかの対照用）
        [int]$PenetrationOverride = -1
    )

    $body = @'
var bulletParams = scope.Container.Resolve<IPlayerBulletParameterDataStore>();
var bulletFactory = scope.Container.Resolve<App.Framework.Utilities.ISimpleObjectFactory<IBulletView>>();
var playerView = scope.Container.Resolve<IBattlePlayerView>();

var groups = UnityEngine.Object.FindObjectsOfType<App.Battle.Views.HitBoxView>()
    .Where(h => h.Id != PlayerConstants.PlayerId && h.GetComponent<Collider>() != null)
    .GroupBy(h => h.Id)
    .Take(__COUNT__)
    .ToList();

if (groups.Count < __COUNT__)
{
    return $"{{\"error\":\"敵が{groups.Count}体しかいません\"}}";
}

var forward = playerView.PlayerTransform.forward;
forward.y = 0f;
forward.Normalize();
var lineStart = playerView.PlayerTransform.position + forward * 5f + Vector3.up * 1f;

// 敵ごとに当たり判定の中心が弾道上（2.5m 間隔）に来るよう、敵の根元ごと動かす
var ids = new List<int>();
for (var i = 0; i < groups.Count; i++)
{
    var box = groups[i].First();
    var root = (box.GetComponentInParent<IEnemyView>() as Component)?.transform ?? box.transform.root;
    var center = box.GetComponent<Collider>().bounds.center;
    root.position += lineStart + forward * (2.5f * i) - center;
    ids.Add(groups[i].Key);

    // 被弾ダメージには最低保証（1）があるため、列が欠けないよう撃つたびに体力を十分に戻す
    if (enemies.TryGetEnemyData(groups[i].Key, out var enemyData))
    {
        enemyData.MaxHp = 1000f;
        enemyData.Hp = 1000f;
    }
}
Physics.SyncTransforms();

var data = bulletParams.GetBulletData(App.Common.Data.ShotType.__SHOT__, App.Common.Data.AimFocusType.__FOCUS__);
var configPenetration = data.Penetration;
if (__OVERRIDE__ >= 0) data.Penetration = __OVERRIDE__;
data.Damage = 0.001f;
data.Speed = 0f;
data.Explosive = 0f;
data.ExplosiveDamage = 0f;

var focusIndex = __FOCUS_INDEX__;
var focusId = focusIndex >= 0 ? ids[focusIndex] : -1;

var bullet = bulletFactory.Instantiate(null);
bullet.Spawn(PlayerConstants.PlayerId, new Pose(lineStart - forward * 3f, Quaternion.LookRotation(forward)), data, focusId);

var hitIds = (List<int>)typeof(App.Battle.Views.Enemy.Bullet.BaseBulletView)
    .GetField("_hitTargetIds", BindingFlags.NonPublic | BindingFlags.Instance)
    .GetValue(bullet);
var hits = string.Join(",", hitIds.Select(id => ids.IndexOf(id)));

return $"{{\"configPenetration\":{configPenetration},\"hits\":\"{hits}\"}}";
'@
    $body = $body.Replace('__COUNT__', "$Global:PenetrationEnemyCount").
        Replace('__SHOT__', $ShotType).
        Replace('__FOCUS__', $FocusType).
        Replace('__FOCUS_INDEX__', "$FocusIndex").
        Replace('__OVERRIDE__', "$PenetrationOverride")

    $result = Invoke-BossSnippet -Body $body
    if ($result.error) { throw "貫通の検証用の配置に失敗しました: $($result.error)" }
    Write-Host "  $ShotType/$FocusType focus=$FocusIndex override=$PenetrationOverride → 貫通数=$($result.configPenetration) 命中=[$($result.hits)]"
    return $result
}

function ProbeRun {
    # 敵の出現を待つ
    Start-Sleep -Seconds 2

    # 対照: 貫通数を十分に大きくすれば5体すべてに当たる（列の組み立てと弾道が正しいことの確認）
    $control = Invoke-PenetrationShot -ShotType Normal -FocusType NotFocus -PenetrationOverride 10
    Assert-ProbeTrue -Name '対照: 貫通数10なら列の5体すべてに手前から当たる' -Condition ($control.hits -eq '0,1,2,3,4') -Detail "命中=[$($control.hits)]" | Out-Null

    $normal = Invoke-PenetrationShot -ShotType Normal -FocusType NotFocus
    Assert-ProbeValue -Name 'ノーマルの貫通数は1' -Actual $normal.configPenetration -Expected 1 | Out-Null
    Assert-ProbeTrue -Name 'ノーマル(非フォーカス)は2体に当たって止まる' -Condition ($normal.hits -eq '0,1') -Detail "命中=[$($normal.hits)]" | Out-Null

    $waltz = Invoke-PenetrationShot -ShotType Waltz -FocusType NotFocus
    Assert-ProbeValue -Name 'ワルツの貫通数は1（ノーマルと共通）' -Actual $waltz.configPenetration -Expected 1 | Out-Null
    Assert-ProbeTrue -Name 'ワルツ(非フォーカス)は2体に当たって止まる' -Condition ($waltz.hits -eq '0,1') -Detail "命中=[$($waltz.hits)]" | Out-Null

    $merge = Invoke-PenetrationShot -ShotType Merge -FocusType NotFocus
    Assert-ProbeValue -Name 'マージの貫通数は3' -Actual $merge.configPenetration -Expected 3 | Out-Null
    Assert-ProbeTrue -Name 'マージ(非フォーカス)は4体に当たって止まる' -Condition ($merge.hits -eq '0,1,2,3') -Detail "命中=[$($merge.hits)]" | Out-Null

    $focusLast = Invoke-PenetrationShot -ShotType Normal -FocusType Focus -FocusIndex 4
    Assert-ProbeTrue -Name 'ノーマルのフォーカス弾は貫通数を超えても5体目のフォーカス対象まで貫通する' -Condition ($focusLast.hits -eq '0,1,2,3,4') -Detail "命中=[$($focusLast.hits)]" | Out-Null

    $focusMiddle = Invoke-PenetrationShot -ShotType Normal -FocusType Focus -FocusIndex 2
    Assert-ProbeTrue -Name 'フォーカス弾はフォーカス対象（3体目）に当たったら止まる' -Condition ($focusMiddle.hits -eq '0,1,2') -Detail "命中=[$($focusMiddle.hits)]" | Out-Null

    $mergeFocus = Invoke-PenetrationShot -ShotType Merge -FocusType Focus -FocusIndex 4
    Assert-ProbeTrue -Name 'マージのフォーカス弾も貫通数を超えて5体目のフォーカス対象まで貫通する' -Condition ($mergeFocus.hits -eq '0,1,2,3,4') -Detail "命中=[$($mergeFocus.hits)]" | Out-Null

    Invoke-BullseyeCheck
}

function Invoke-BullseyeCheck {
    # ブルズアイ（PenetrationCount 条件バフ）の倍率。貫通した数（＝何体目か - 1）を ConditionValue で割った段数ぶん
    # (EffectValue - 1) を加算する。期待値は CSV と同じ値（間隔・倍率）からここで独立に計算する。
    # バフはレベルごとに単独で付ける（ResetRun で付け直す。最後にも外して後片付けする）
    $levels = @(
        @{ Name = 'L1'; Interval = 1; Effect = 1.3 },
        @{ Name = 'L2'; Interval = 1; Effect = 1.5 },
        @{ Name = 'L3'; Interval = 1; Effect = 1.5 }
    )

    foreach ($level in $levels) {
        $body = @'
var buffs = scope.Container.Resolve<IBuffStateDataStore>();
var resettable = (IRunResettable)buffs;
var master = UnityEditor.AssetDatabase.LoadAssetAtPath<App.Common.Data.MasterData.BuffMasterData>("Assets/App/MasterData/Buff/BullseyeBuff__LEVEL__.asset");

resettable.ResetRun();
buffs.AddBuff(master);
var values = string.Join(",", Enumerable.Range(1, 4).Select(i => buffs.CalcPenetrationMultiply(i).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)));
resettable.ResetRun();

return $"{{\"values\":\"{values}\"}}";
'@
        $result = Invoke-BossSnippet -Body $body.Replace('__LEVEL__', $level.Name)
        $actual = $result.values -split ','
        Write-Host "  ブルズアイ$($level.Name) 1〜4体目の倍率: [$($result.values)]"

        for ($index = 1; $index -le 4; $index++) {
            $stack = [math]::Floor(($index - 1) / $level.Interval)
            $expected = 1 + $stack * ($level.Effect - 1)
            Assert-ProbeValue -Name "ブルズアイ$($level.Name): $index 体目の倍率" `
                -Actual ([double]::Parse($actual[$index - 1], [System.Globalization.CultureInfo]::InvariantCulture)) `
                -Expected $expected | Out-Null
        }
    }
}

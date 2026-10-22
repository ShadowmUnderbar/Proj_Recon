#
# 弾のオブジェクトプール（BulletStoreView / ViewPool）を確かめるプローブ。
#
# デバッグ対戦（Request-DebugArena）で周回型の雑魚（O-001）を3体出し、プレイヤーの弾を連射して
#   - 即着弾（Speed=0）を連射しても、曳光弾の縮退が終われば場の弾が 0 に戻り、作られた弾の総数が増えない
#   - 飛翔弾（マージ、Speed>0）を連射しても、寿命（5秒）が過ぎれば場の弾が 0 に戻り、総数が増えない
#   - 曳光弾（BulletTracerView）も使い回され、総数が増えない
#   - 貸し出し直した弾は前回の命中（_hitTargetIds）を引きずらず、通し番号（BulletId）が進む
#   - 一括消去（IBulletStoreView.AllRemove）で場の弾が即座に 0 になり、その後も貸し出せる
# を見る。弾は BulletPenetration と同じく IPlayerBulletParameterDataStore の値を使い、ダメージ・爆風だけ差し替える。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

$Global:BulletPoolEnemyCode = 'O-001'
$Global:BulletPoolEnemyCount = 3

# 1回の連射で撃つ数。プールの初期数（BulletStoreView の既定 32）を超える数にして、足りなければ増える経路も通す
$Global:BulletPoolBurstCount = 40

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -EnemyCode $Global:BulletPoolEnemyCode -EnemyCount $Global:BulletPoolEnemyCount -AutoRespawn $false
    }
    catch {
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Exit-BossProbeScene
}

# プレイヤーの弾・曳光弾の数を読む（active=場にあるもの / total=プールの中身も含む全部）
function Get-BulletCounts {
    $body = @'
// 敵（周回型は弾を撃つ）の弾は別プールなので、プレイヤーの弾（PlayerBulletView）だけを数える
var bullets = UnityEngine.Object.FindObjectsOfType<App.Battle.Views.PlayerBulletView>(true);
var tracers = UnityEngine.Object.FindObjectsOfType<App.Battle.Views.BulletTracerView>(true);
return $"{{\"active\":{bullets.Count(b => b.gameObject.activeInHierarchy)},\"total\":{bullets.Length},\"tracerActive\":{tracers.Count(t => t.gameObject.activeInHierarchy)},\"tracerTotal\":{tracers.Length}}}";
'@
    return Invoke-BossSnippet -Body $body
}

# プレイヤーの弾を敵の方向へ count 発撃つ。speed=0 なら即着弾、>0 なら飛翔弾。最後に撃った弾の BulletId と命中数を返す
function Invoke-Burst {
    param(
        [Parameter(Mandatory)] [int]$Count,
        [Parameter(Mandatory)] [string]$ShotType,
        [Parameter(Mandatory)] [float]$Speed
    )

    $body = @'
var bulletParams = scope.Container.Resolve<IPlayerBulletParameterDataStore>();
var bulletFactory = scope.Container.Resolve<App.Framework.Utilities.ISimpleObjectFactory<IBulletView>>();
var playerView = scope.Container.Resolve<IBattlePlayerView>();

var forward = playerView.PlayerTransform.forward;
forward.y = 0f;
forward.Normalize();
var origin = playerView.PlayerTransform.position + forward * 1f + Vector3.up * 1f;

// 敵を弾道上に並べる（撃つたびに体力を戻して倒さない）
var groups = UnityEngine.Object.FindObjectsOfType<App.Battle.Views.HitBoxView>()
    .Where(h => h.Id != PlayerConstants.PlayerId && h.GetComponent<Collider>() != null)
    .GroupBy(h => h.Id)
    .ToList();
for (var i = 0; i < groups.Count; i++)
{
    var box = groups[i].First();
    var root = (box.GetComponentInParent<IEnemyView>() as Component)?.transform ?? box.transform.root;
    var center = box.GetComponent<Collider>().bounds.center;
    root.position += origin + forward * (4f + 2.5f * i) - center;
    if (enemies.TryGetEnemyData(groups[i].Key, out var enemyData))
    {
        enemyData.MaxHp = 100000f;
        enemyData.Hp = 100000f;
    }
}
Physics.SyncTransforms();

var data = bulletParams.GetBulletData(App.Common.Data.ShotType.__SHOT__, App.Common.Data.AimFocusType.NotFocus);
data.Damage = 0.001f;
data.Speed = __SPEED__f;
data.Explosive = 0f;
data.ExplosiveDamage = 0f;

var lastId = 0;
var lastHits = 0;
var fieldInfo = typeof(App.Battle.Views.Enemy.Bullet.BaseBulletView).GetField("_hitTargetIds", BindingFlags.NonPublic | BindingFlags.Instance);
for (var i = 0; i < __COUNT__; i++)
{
    var bullet = bulletFactory.Instantiate(null);
    var view = (App.Battle.Views.Enemy.Bullet.BaseBulletView)bullet;
    // 貸出直後は前回の命中を引きずっていない
    if (((List<int>)fieldInfo.GetValue(view)).Count != 0)
    {
        return "{\"error\":\"貸出直後の弾に前回の命中が残っています\"}";
    }
    bullet.Spawn(PlayerConstants.PlayerId, new Pose(origin, Quaternion.LookRotation(forward)), data, -1);
    lastId = view.BulletId;
    lastHits = ((List<int>)fieldInfo.GetValue(view)).Count;
}

return $"{{\"lastId\":{lastId},\"lastHits\":{lastHits}}}";
'@
    $body = $body.Replace('__COUNT__', "$Count").Replace('__SHOT__', $ShotType).Replace('__SPEED__', ([string]$Speed))
    $result = Invoke-BossSnippet -Body $body
    if ($result.error) { throw "連射に失敗しました: $($result.error)" }
    return $result
}

function ProbeRun {
    # 敵の出現を待つ
    Start-Sleep -Seconds 2

    # ---- 即着弾の連射 ----
    $burst1 = Invoke-Burst -Count $Global:BulletPoolBurstCount -ShotType Normal -Speed 0
    Write-Host "  即着弾 $($Global:BulletPoolBurstCount) 発: 最後の弾 BulletId=$($burst1.lastId) 命中数=$($burst1.lastHits)"
    Assert-ProbeTrue -Name '即着弾が敵に当たっている（列の配置が正しい）' -Condition ($burst1.lastHits -ge 1) -Detail "命中数=$($burst1.lastHits)" | Out-Null

    # 曳光弾の縮退（トレイル寿命の半分）を待つ
    Start-Sleep -Seconds 2
    $after1 = Get-BulletCounts
    Write-Host "  即着弾の後: active=$($after1.active) total=$($after1.total) tracerActive=$($after1.tracerActive) tracerTotal=$($after1.tracerTotal)"
    Assert-ProbeValue -Name '即着弾: 縮退が終われば場の弾は 0' -Actual $after1.active -Expected 0 | Out-Null
    Assert-ProbeValue -Name '即着弾: 曳光弾も場から消える' -Actual $after1.tracerActive -Expected 0 | Out-Null
    Assert-ProbeTrue -Name '即着弾: 作られた弾の総数が連射数以下（都度生成していない）' -Condition ($after1.total -le $Global:BulletPoolBurstCount) -Detail "total=$($after1.total)" | Out-Null

    # ---- 2回目の即着弾の連射: 総数が増えない・通し番号が進む ----
    $burst2 = Invoke-Burst -Count $Global:BulletPoolBurstCount -ShotType Normal -Speed 0
    Assert-ProbeTrue -Name '貸し出し直した弾の通し番号が進んでいる' -Condition ($burst2.lastId -gt $burst1.lastId) -Detail "1回目=$($burst1.lastId) 2回目=$($burst2.lastId)" | Out-Null
    Start-Sleep -Seconds 2
    $after2 = Get-BulletCounts
    Write-Host "  即着弾2回目の後: active=$($after2.active) total=$($after2.total) tracerTotal=$($after2.tracerTotal)"
    Assert-ProbeValue -Name '即着弾2回目: 弾の総数が増えない（使い回されている）' -Actual $after2.total -Expected $after1.total | Out-Null
    Assert-ProbeValue -Name '即着弾2回目: 曳光弾の総数が増えない' -Actual $after2.tracerTotal -Expected $after1.tracerTotal | Out-Null

    # ---- 飛翔弾の連射 ----
    $burst3 = Invoke-Burst -Count $Global:BulletPoolBurstCount -ShotType Merge -Speed 50
    $flying = Get-BulletCounts
    Write-Host "  飛翔弾 $($Global:BulletPoolBurstCount) 発の直後: active=$($flying.active) total=$($flying.total)"
    Assert-ProbeTrue -Name '飛翔弾: 撃った直後は場に弾がある' -Condition ($flying.active -ge 1) -Detail "active=$($flying.active)" | Out-Null

    # 寿命（5秒）＋トレイルの縮退を待つ
    Start-Sleep -Seconds 7
    $after3 = Get-BulletCounts
    Write-Host "  飛翔弾の後: active=$($after3.active) total=$($after3.total)"
    Assert-ProbeValue -Name '飛翔弾: 寿命が過ぎれば場の弾は 0' -Actual $after3.active -Expected 0 | Out-Null
    Assert-ProbeValue -Name '飛翔弾: 弾の総数が増えない' -Actual $after3.total -Expected $after1.total | Out-Null

    # ---- 一括消去 ----
    Invoke-Burst -Count 20 -ShotType Merge -Speed 50 | Out-Null
    $removed = Invoke-BossSnippet -Body @'
scope.Container.Resolve<IBulletStoreView>().AllRemove();
var bullets = UnityEngine.Object.FindObjectsOfType<App.Battle.Views.PlayerBulletView>(true);
return $"{{\"active\":{bullets.Count(b => b.gameObject.activeInHierarchy)}}}";
'@
    Assert-ProbeValue -Name '一括消去の直後に場の弾は 0' -Actual $removed.active -Expected 0 | Out-Null

    $burst4 = Invoke-Burst -Count 5 -ShotType Normal -Speed 0
    Assert-ProbeTrue -Name '一括消去の後も貸し出せて敵に当たる' -Condition ($burst4.lastHits -ge 1 -and $burst4.lastId -gt $burst3.lastId) -Detail "命中数=$($burst4.lastHits) BulletId=$($burst4.lastId)" | Out-Null
}

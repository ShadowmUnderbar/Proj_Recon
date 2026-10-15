#
# サモンドローン（SummonDroneDataStore / SummonDroneUseCase / AllyDroneView）を確かめるプローブ。
#
# デバッグ対戦（Request-DebugArena）で周回型の雑魚（O-001）を出し、次を見る。
#   - 未所持では撃破してもドローンが出ず、撃破数も数えない
#   - L1（40体ごとに1体）: 39体目までは出ず、40体目の撃破でプレイヤーの近くに1体出る・撃破数が0に戻る
#   - L3（30体ごとに2体）: 30体目の撃破で2体出る（最高レベルの Value1 / Value2）
#   - 複数のドローンは扇に等分して重ならずに並ぶ・同時に出せる数は AllyConfig の最大数まで
#   - ドローンはプレイヤーの後ろ上方（射撃の向きの反対側・設定の高さと距離）に浮かび、プレイヤーが動くと付いてくる
#   - ドローンのコライダーは Player レイヤーのトリガー（プレイヤーの移動・射撃を阻害しない）。プレイヤーの弾はすり抜ける
#   - 射程内の敵を撃ってダメージを与える
#   - 最大体力は AllyConfig の値（体力アップの倍率は SummonTurret プローブで確認済み）
#   - ウェーブ間ポーズで全員消え、撃破数は持ち越す
# 撃破数は KillCount を直接進めてから、最後の1体だけ実際に撃破して（EnemyDataStore.Damage → OnEnemyDead）経路を通す。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -EnemyCode 'O-001' -EnemyCount 3 -AutoRespawn $true -RespawnDelaySeconds 1
    }
    catch {
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Exit-BossProbeScene
}

$Global:DronePrelude = @'
var allies = scope.Container.Resolve<IAllyDataStore>();
var summonDrone = scope.Container.Resolve<ISummonDroneDataStore>();
var aim = scope.Container.Resolve<IPlayerAimDataStore>();
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var playerView = scope.Container.Resolve<IBattlePlayerView>();
var allyConfig = scope.Container.Resolve<AllyConfig>();
var store = UnityEngine.Object.FindObjectOfType<App.Battle.Views.Ally.AllyStoreView>();
var storeAllies = (System.Collections.IDictionary)typeof(App.Battle.Views.Ally.AllyStoreView)
    .GetField("_allies", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(store);
var killCountProp = typeof(App.Battle.DataStore.SummonDroneDataStore).GetProperty("KillCount");
var ci = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.###", ci);

List<App.Battle.Views.Ally.AllyDroneView> Drones()
{
    var list = new List<App.Battle.Views.Ally.AllyDroneView>();
    foreach (System.Collections.DictionaryEntry e in storeAllies)
    {
        if (e.Value is App.Battle.Views.Ally.AllyDroneView d && d != null) list.Add(d);
    }
    return list;
}

// 生きている敵を1体、実際の撃破経路（EnemyDataStore.Damage → OnEnemyDead）で倒す。倒せたら true
bool KillOneEnemy()
{
    var target = enemies.Enemies.FirstOrDefault(e => e.Hp > 0f);
    if (target == null) return false;
    enemies.Damage(new HitData(target.Id, 1e9f, App.Common.Data.HitDirectionType.None));
    return true;
}

void AddDrone(int level)
{
    session.AddUpgrade(db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.SummonDrone && u.Level == level));
}
'@

function Invoke-DroneSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    return Invoke-BossSnippet -Body ($Global:DronePrelude + "`n" + $Body)
}

# 敵がいない（出し直しの待ち中）ときに少し待って取り直す
function Invoke-DroneKillSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    for ($try = 0; $try -lt 5; $try++) {
        $r = Invoke-DroneSnippet -Body $Body
        if (-not $r.error) { return $r }
        Start-Sleep -Milliseconds 1500
    }
    throw $r.error
}

function ProbeRun {
    Start-Sleep -Seconds 2

    # --- 未所持 ---
    $r = Invoke-DroneKillSnippet -Body @'
if (!KillOneEnemy()) return "{\"error\":\"敵がいません\"}";
return $"{{\"drones\":{Drones().Count},\"kills\":{summonDrone.KillCount}}}";
'@
    Assert-ProbeValue -Name '未所持では撃破してもドローンが出ない' -Actual $r.drones -Expected 0 | Out-Null
    Assert-ProbeValue -Name '未所持のあいだの撃破は数えない' -Actual $r.kills -Expected 0 | Out-Null

    # --- L1: 40体ごとに1体 ---
    $r = Invoke-DroneKillSnippet -Body @'
if (enemies.Enemies.Count(e => e.Hp > 0f) < 2) return "{\"error\":\"敵が足りません\"}";
AddDrone(1);
killCountProp.SetValue(summonDrone, 38);
KillOneEnemy();
var after39 = Drones().Count;
var kills39 = summonDrone.KillCount;
KillOneEnemy();
var drones = Drones();
var drone = drones.FirstOrDefault();
var flat = drone != null ? drone.transform.position - player.Position.Value : Vector3.zero; flat.y = 0f;
var maxHp = drone != null && allies.TryGetAllyData(drone.Id, out var ad) ? ad.MaxHp : -1f;
allyConfig.TryGetEntry(AllyType.Drone, out var entry);
var col = drone != null ? drone.GetComponent<Collider>() : null;
return $"{{\"after39\":{after39},\"kills39\":{kills39},\"drones\":{drones.Count},\"kills\":{summonDrone.KillCount},\"spawnDist\":{F(flat.magnitude)},\"maxHp\":{F(maxHp)},\"configHp\":{F(entry.MaxHp)},\"isTrigger\":{(col != null && col.isTrigger).ToString().ToLower()},\"layer\":{(drone != null ? drone.gameObject.layer : -1)}}}";
'@
    Assert-ProbeValue -Name 'L1: 39体目の撃破ではまだ出ない' -Actual $r.after39 -Expected 0 | Out-Null
    Assert-ProbeValue -Name 'L1: 39体目までは撃破数を数えている' -Actual $r.kills39 -Expected 39 | Out-Null
    Assert-ProbeValue -Name 'L1: 40体目の撃破でドローンが1体出る' -Actual $r.drones -Expected 1 | Out-Null
    Assert-ProbeValue -Name 'L1: 召喚したら撃破数が0に戻る' -Actual $r.kills -Expected 0 | Out-Null
    Assert-ProbeTrue -Name 'ドローンはプレイヤーの近く（水平3m以内）に出る' -Condition ($r.spawnDist -le 3) -Detail "距離=$($r.spawnDist)" | Out-Null
    Assert-ProbeValue -Name 'ドローンの最大体力は AllyConfig の値' -Actual $r.maxHp -Expected $r.configHp | Out-Null
    Assert-ProbeTrue -Name 'ドローンのコライダーはトリガー（プレイヤーの移動を阻害しない）' -Condition $r.isTrigger | Out-Null
    Assert-ProbeValue -Name 'ドローンは Player レイヤー（プレイヤーの弾がすり抜ける）' -Actual $r.layer -Expected 3 | Out-Null

    # --- 追従: プレイヤーの後ろ上方に並び、プレイヤーが動くと付いてくる ---
    Start-Sleep -Milliseconds 1500
    $followBody = @'
var drone = Drones().First();
var hover = (float)typeof(App.Battle.Views.Ally.AllyDroneView).GetField("_hoverHeight", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(drone);
var distance = (float)typeof(App.Battle.Views.Ally.AllyDroneView).GetField("_followDistance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(drone);
var forward = (Vector3)typeof(App.Battle.Views.Ally.AllyViewBase).GetProperty("PlayerForward", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(drone);
var offset = drone.transform.position - player.Position.Value;
var flat = offset; flat.y = 0f;
forward.y = 0f;
var behindDot = forward.sqrMagnitude > 0f && flat.sqrMagnitude > 0f ? Vector3.Dot(forward.normalized, flat.normalized) : 1f;
return $"{{\"height\":{F(offset.y)},\"hover\":{F(hover)},\"dist\":{F(flat.magnitude)},\"followDist\":{F(distance)},\"behindDot\":{F(behindDot)},\"hasForward\":{(forward.sqrMagnitude > 0f).ToString().ToLower()}}}";
'@
    $r = Invoke-DroneSnippet -Body $followBody
    Write-Host "  追従: 高さ $($r.height)m（設定 $($r.hover)） / 水平距離 $($r.dist)m（設定 $($r.followDist)） / 射撃の向きとの内積 $($r.behindDot)"
    Assert-ProbeTrue -Name 'ドローンにプレイヤーの向き（射撃の向き）が届いている' -Condition $r.hasForward | Out-Null
    Assert-ProbeValue -Name 'ドローンは設定の高さに浮かぶ' -Actual $r.height -Expected $r.hover -Tolerance 0.2 | Out-Null
    Assert-ProbeValue -Name 'ドローンはプレイヤーから設定の距離に並ぶ' -Actual $r.dist -Expected $r.followDist -Tolerance 0.3 | Out-Null
    Assert-ProbeTrue -Name 'ドローンはプレイヤーの後ろ側（射撃の向きの反対）にいる' -Condition ($r.behindDot -lt 0) -Detail "内積=$($r.behindDot)" | Out-Null

    $r = Invoke-DroneSnippet -Body @'
player.WarpTo(player.Position.Value + new Vector3(6f, 0f, 0f));
return "{}";
'@
    Start-Sleep -Milliseconds 1500
    $r = Invoke-DroneSnippet -Body $followBody
    Write-Host "  移動後: 高さ $($r.height)m / 水平距離 $($r.dist)m"
    Assert-ProbeValue -Name 'プレイヤーが動くと付いてくる（高さ）' -Actual $r.height -Expected $r.hover -Tolerance 0.2 | Out-Null
    Assert-ProbeValue -Name 'プレイヤーが動くと付いてくる（距離）' -Actual $r.dist -Expected $r.followDist -Tolerance 0.3 | Out-Null

    # --- プレイヤーの弾はドローンをすり抜ける ---
    $r = Invoke-DroneSnippet -Body @'
var drone = Drones().First();
var bulletParams = scope.Container.Resolve<IPlayerBulletParameterDataStore>();
var bulletFactory = scope.Container.Resolve<App.Framework.Utilities.ISimpleObjectFactory<IBulletView>>();
var data = bulletParams.GetBulletData(App.Common.Data.ShotType.Normal, App.Common.Data.AimFocusType.NotFocus);
data.Damage = 0.001f;
data.Speed = 0f;
allies.TryGetAllyData(drone.Id, out var ad);
ad.Hp = 100000f;
var bullet = bulletFactory.Instantiate(null);
bullet.Spawn(PlayerConstants.PlayerId, new Pose(drone.transform.position - Vector3.right * 3f, Quaternion.LookRotation(Vector3.right)), data, -1);
var hitIds = (List<int>)typeof(App.Battle.Views.Enemy.Bullet.BaseBulletView)
    .GetField("_hitTargetIds", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bullet);
return $"{{\"hitAlly\":{hitIds.Contains(drone.Id).ToString().ToLower()},\"hp\":{F(ad.Hp)}}}";
'@
    Assert-ProbeTrue -Name 'プレイヤーの弾はドローンに当たらない' -Condition (-not $r.hitAlly) | Out-Null
    Assert-ProbeValue -Name 'プレイヤーの弾でドローンの体力は減らない' -Actual $r.hp -Expected 100000 | Out-Null

    # --- 射程内の敵を撃つ（敵はスタンで止める） ---
    $setup = @'
var drone = Drones().First();
var enemyAi = UnityEngine.Object.FindObjectsOfType<App.Battle.Interface.EnemyAI.EnemyAIBase>().FirstOrDefault(e => enemies.TryGetEnemyData(e.EnemyId, out var d) && d.Hp > 0f);
if (enemyAi == null) return "{\"error\":\"敵がいません\"}";
presenter.SetStun(enemyAi.EnemyId, true);
var ground = drone.transform.position; ground.y = player.Position.Value.y;
enemyAi.WarpTo(ground + Vector3.forward * 5f);
Physics.SyncTransforms();
enemies.TryGetEnemyData(enemyAi.EnemyId, out var ed);
ed.MaxHp = 100000f;
ed.Hp = 100000f;
AppDomain.CurrentDomain.SetData("drone.enemyId", enemyAi.EnemyId);
return $"{{\"hp\":{F(ed.Hp)}}}";
'@
    $r = Invoke-DroneKillSnippet -Body $setup
    $hpBefore = $r.hp
    Start-Sleep -Seconds 2
    $r = Invoke-DroneSnippet -Body @'
var enemyId = (int)AppDomain.CurrentDomain.GetData("drone.enemyId");
var hp = enemies.TryGetEnemyData(enemyId, out var ed) ? ed.Hp : -1f;
presenter.SetStun(enemyId, false);
return $"{{\"hp\":{F(hp)}}}";
'@
    Assert-ProbeTrue -Name 'ドローンが射程内の敵を撃ってダメージを与える' -Condition (($r.hp -ge 0) -and ($r.hp -lt $hpBefore)) -Detail "HP $hpBefore → $($r.hp)" | Out-Null

    # --- L3: 30体ごとに2体 ---
    $r = Invoke-DroneKillSnippet -Body @'
if (enemies.Enemies.Count(e => e.Hp > 0f) < 1) return "{\"error\":\"敵がいません\"}";
AddDrone(3);
var before = Drones().Count;
killCountProp.SetValue(summonDrone, 29);
KillOneEnemy();
return $"{{\"before\":{before},\"after\":{Drones().Count},\"kills\":{summonDrone.KillCount}}}";
'@
    Assert-ProbeValue -Name 'L1とL3を所持: 30体目の撃破で2体出る（最高レベルの Value1=30 / Value2=2）' -Actual ($r.after - $r.before) -Expected 2 | Out-Null
    Assert-ProbeValue -Name 'L1とL3を所持: 召喚したら撃破数が0に戻る' -Actual $r.kills -Expected 0 | Out-Null

    # --- 複数のドローンは扇に等分して並び、重ならない ---
    Start-Sleep -Milliseconds 1500
    $r = Invoke-DroneSnippet -Body @'
var drones = Drones();
var minDist = float.MaxValue;
for (var i = 0; i < drones.Count; i++)
for (var j = i + 1; j < drones.Count; j++)
    minDist = Mathf.Min(minDist, Vector3.Distance(drones[i].transform.position, drones[j].transform.position));
return $"{{\"count\":{drones.Count},\"minDist\":{F(minDist)}}}";
'@
    Write-Host "  ドローン $($r.count) 体 / 最も近い2体の距離 $($r.minDist)m"
    Assert-ProbeTrue -Name '複数のドローンは重ならずに並ぶ（最も近い2体が1m以上離れる）' -Condition (($r.count -ge 2) -and ($r.minDist -ge 1)) -Detail "数=$($r.count) 距離=$($r.minDist)" | Out-Null

    # --- 同時に出せる数は AllyConfig の最大数まで ---
    $r = Invoke-DroneSnippet -Body @'
allyConfig.TryGetEntry(AllyType.Drone, out var entry);
for (var i = 0; i < entry.MaxCount + 3; i++) allies.TrySpawn(AllyType.Drone, player.Position.Value, Vector3.zero, out _);
var refused = !allies.TrySpawn(AllyType.Drone, player.Position.Value, Vector3.zero, out _);
return $"{{\"count\":{Drones().Count},\"max\":{entry.MaxCount},\"refused\":{refused.ToString().ToLower()}}}";
'@
    Assert-ProbeTrue -Name 'ドローンの最大数が設定されている' -Condition ($r.max -gt 0) -Detail "最大数=$($r.max)" | Out-Null
    Assert-ProbeValue -Name 'ドローンは最大数までしか出ない' -Actual $r.count -Expected $r.max | Out-Null
    Assert-ProbeTrue -Name '最大数に達している間は召喚を断る' -Condition $r.refused | Out-Null

    # --- ウェーブ間ポーズで全員消え、撃破数は持ち越す ---
    $r = Invoke-DroneSnippet -Body @'
killCountProp.SetValue(summonDrone, 12);
var before = Drones().Count;
wave.SetWavePause(true);
var after = storeAllies.Count;
var kills = summonDrone.KillCount;
wave.SetWavePause(false);
return $"{{\"before\":{before},\"after\":{after},\"kills\":{kills}}}";
'@
    Assert-ProbeTrue -Name 'ウェーブ間ポーズの前にドローンが残っている' -Condition ($r.before -ge 1) -Detail "数=$($r.before)" | Out-Null
    Assert-ProbeValue -Name 'ウェーブ間ポーズでドローンが全員消える' -Actual $r.after -Expected 0 | Out-Null
    Assert-ProbeValue -Name 'ウェーブ間ポーズでも撃破数は持ち越す' -Actual $r.kills -Expected 12 | Out-Null
}

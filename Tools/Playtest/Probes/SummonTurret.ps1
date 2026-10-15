#
# 召喚した味方の基盤（AllyDataStore / AllyUseCase / AllyStoreView）とサモンタレット（SummonTurretDataStore / SummonTurretUseCase）を確かめるプローブ。
#
# デバッグ対戦（Request-DebugArena）で周回型の雑魚（O-001）を出し、次を見る。
#   - 未所持ではマージショットでタレットが出ない
#   - 所持していればマージショットで照準位置へタレットが出る（NavMesh 上・照準位置の近く・撃った手のエイム方向を向く）・クールダウンは Value1 秒
#   - クールダウン中は出ない・ノーマルショットでは出ない・置ける場所が無ければ出ずクールダウンも消費しない・レベルを重ねると最高レベルの Value1 になる
#   - タレットは旋回速度（AllyConfig）で砲身を向け、向き切るまで撃たない・向き切ったら射程内の敵を撃つ
#   - 敵 AI はプレイヤーより近いタレットを狙い、盾・ボスは狙わない（盾はここでは見ない）
#   - プレイヤーの弾はタレットをすり抜ける
#   - 敵の攻撃でタレットの体力が減り、0で消える
#   - フリーズ中はタレットが止まる・ウェーブ間ポーズで全員消える
# マージショットは IPlayerBulletParameterDataStore.SetCoolDownTime（実際の射撃でも呼ばれる、OnShot の発火元）で起こす。
# 照準位置は PlayerAimUseCase が毎フレーム上書きするため、同じスニペット内で SetAimPosition してから撃つ。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

$Global:SummonEnemyCode = 'O-001'
$Global:SummonEnemyCount = 3

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -EnemyCode $Global:SummonEnemyCode -EnemyCount $Global:SummonEnemyCount -AutoRespawn $true -RespawnDelaySeconds 1
    }
    catch {
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Exit-BossProbeScene
}

# 各スニペット共通の前置き（BossProbePrelude のあとに足す）
$Global:SummonPrelude = @'
var allies = scope.Container.Resolve<IAllyDataStore>();
var summon = scope.Container.Resolve<ISummonTurretDataStore>();
var bulletParams = scope.Container.Resolve<IPlayerBulletParameterDataStore>();
var aim = scope.Container.Resolve<IPlayerAimDataStore>();
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var playerView = scope.Container.Resolve<IBattlePlayerView>();
var store = UnityEngine.Object.FindObjectOfType<App.Battle.Views.Ally.AllyStoreView>();
var alliesField = typeof(App.Battle.Views.Ally.AllyStoreView).GetField("_allies", BindingFlags.NonPublic | BindingFlags.Instance);
var storeAllies = (System.Collections.IDictionary)alliesField.GetValue(store);
var ci = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.###", ci);

// プレイヤーの正面 6m の地面
Vector3 AimPoint()
{
    var t = playerView.PlayerTransform;
    var f = t.forward; f.y = 0f; f.Normalize();
    var p = t.position + f * 6f;
    p.y = 0f;
    return p;
}

void Shot(App.Common.Data.ShotType shotType, Vector3 aimPosition)
{
    var hand = App.Common.Data.HandType.Right;
    aim.SetAimPosition(hand, aimPosition);
    bulletParams.SetCoolDownTime(hand, shotType, App.Common.Data.AimFocusType.NotFocus);
}

void AddSummon(int level)
{
    session.AddUpgrade(db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.SummonTurret && u.Level == level));
}

App.Battle.Views.Ally.AllyViewBase FirstAllyView()
{
    foreach (System.Collections.DictionaryEntry e in storeAllies) return (App.Battle.Views.Ally.AllyViewBase)e.Value;
    return null;
}
'@

function Invoke-SummonSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    return Invoke-BossSnippet -Body ($Global:SummonPrelude + "`n" + $Body)
}

function ProbeRun {
    # 敵の出現を待つ
    Start-Sleep -Seconds 2

    # --- 未所持 ---
    $r = Invoke-SummonSnippet -Body @'
Shot(App.Common.Data.ShotType.Merge, AimPoint());
return $"{{\"count\":{storeAllies.Count},\"cd\":{F(summon.RemainingCoolDown)}}}";
'@
    Assert-ProbeValue -Name '未所持ではマージショットでタレットが出ない' -Actual $r.count -Expected 0 | Out-Null

    # --- L1 を所持してマージショット ---
    # 射程の設定しだいではタレットが検証の途中で敵を全滅させるため、出す前に敵の体力を上げておく
    $r = Invoke-SummonSnippet -Body @'
foreach (var e in enemies.Enemies) { e.MaxHp = 100000f; e.Hp = 100000f; }
AddSummon(1);
var target = AimPoint();
Shot(App.Common.Data.ShotType.Merge, target);
var view = FirstAllyView();
var pos = view != null ? view.transform.position : Vector3.zero;
var flat = pos - target; flat.y = 0f;
var onNavMesh = UnityEngine.AI.NavMesh.SamplePosition(pos, out _, 0.1f, UnityEngine.AI.NavMesh.AllAreas);
var layer = view != null ? view.gameObject.layer : -1;
// 向きは撃った手のエイム方向（プレイヤー→照準位置の水平方向）と同じ
var aimForward = target - player.Position.Value; aimForward.y = 0f;
var turretForward = view != null ? view.transform.forward : Vector3.zero; turretForward.y = 0f;
var facingAngle = Vector3.Angle(aimForward, turretForward);
var headField = typeof(App.Battle.Views.Ally.AllyTurretView).GetField("_head", BindingFlags.NonPublic | BindingFlags.Instance);
var headForward = view != null ? ((Transform)headField.GetValue(view)).forward : Vector3.zero; headForward.y = 0f;
var headAngle = Vector3.Angle(aimForward, headForward);
var id = view != null ? view.Id : 0;
var hasData = allies.TryGetAllyData(id, out var data);
// 検証中に敵の攻撃で倒れないよう体力を上げておく（体力の減り方は後で確かめる）
if (hasData) data.Hp = 100000f;
return $"{{\"count\":{storeAllies.Count},\"cd\":{F(summon.RemainingCoolDown)},\"dist\":{F(flat.magnitude)},\"onNavMesh\":{onNavMesh.ToString().ToLower()},\"layer\":{layer},\"id\":{id},\"hasData\":{hasData.ToString().ToLower()},\"facingAngle\":{F(facingAngle)},\"headAngle\":{F(headAngle)}}}";
'@
    Assert-ProbeValue -Name 'L1: マージショットでタレットが1体出る' -Actual $r.count -Expected 1 | Out-Null
    Assert-ProbeValue -Name 'L1: クールダウンは Value1=120秒' -Actual $r.cd -Expected 120 -Tolerance 0.1 | Out-Null
    Assert-ProbeTrue -Name 'タレットは照準位置の近く（水平3m以内）に出る' -Condition ($r.dist -le 3) -Detail "距離=$($r.dist)" | Out-Null
    Assert-ProbeTrue -Name 'タレットは NavMesh 上に置かれる' -Condition $r.onNavMesh | Out-Null
    Assert-ProbeValue -Name 'タレットは Player レイヤー（プレイヤーの弾がすり抜ける）' -Actual $r.layer -Expected 3 | Out-Null
    Assert-ProbeTrue -Name '味方のIdは負の値でデータと対応する' -Condition (($r.id -lt 0) -and $r.hasData) -Detail "Id=$($r.id)" | Out-Null
    Assert-ProbeValue -Name 'タレットは撃った手のエイム方向を向いて出る（本体）' -Actual $r.facingAngle -Expected 0 -Tolerance 1 | Out-Null
    Assert-ProbeValue -Name 'タレットは撃った手のエイム方向を向いて出る（砲身）' -Actual $r.headAngle -Expected 0 -Tolerance 1 | Out-Null
    $turretId = $r.id

    # --- クールダウン中・ノーマルショット ---
    $r = Invoke-SummonSnippet -Body @'
Shot(App.Common.Data.ShotType.Merge, AimPoint());
var afterMerge = storeAllies.Count;
typeof(App.Battle.DataStore.SummonTurretDataStore).GetProperty("RemainingCoolDown").SetValue(summon, 0f);
Shot(App.Common.Data.ShotType.Normal, AimPoint());
Shot(App.Common.Data.ShotType.Waltz, AimPoint());
return $"{{\"afterMerge\":{afterMerge},\"afterNormal\":{storeAllies.Count},\"cd\":{F(summon.RemainingCoolDown)}}}";
'@
    Assert-ProbeValue -Name 'クールダウン中はマージショットでも出ない' -Actual $r.afterMerge -Expected 1 | Out-Null
    Assert-ProbeValue -Name 'ノーマル・ワルツでは出ない（クールダウンも始まらない）' -Actual $r.afterNormal -Expected 1 | Out-Null
    Assert-ProbeValue -Name 'ノーマル・ワルツではクールダウンが0のまま' -Actual $r.cd -Expected 0 | Out-Null

    # --- 置ける場所が無い（NavMesh から遠い）ときは出ず、クールダウンも消費しない ---
    $r = Invoke-SummonSnippet -Body @'
Shot(App.Common.Data.ShotType.Merge, AimPoint() + Vector3.down * 500f);
return $"{{\"count\":{storeAllies.Count},\"cd\":{F(summon.RemainingCoolDown)}}}";
'@
    Assert-ProbeValue -Name '置ける場所が無いときは出ない' -Actual $r.count -Expected 1 | Out-Null
    Assert-ProbeValue -Name '出せなかったときはクールダウンを消費しない' -Actual $r.cd -Expected 0 | Out-Null

    # --- L3 を重ねる ---
    $r = Invoke-SummonSnippet -Body @'
AddSummon(3);
var p = AimPoint();
var right = playerView.PlayerTransform.right; right.y = 0f; right.Normalize();
Shot(App.Common.Data.ShotType.Merge, p + right * 4f);
foreach (System.Collections.DictionaryEntry e in storeAllies)
{
    if (allies.TryGetAllyData((int)e.Key, out var d)) d.Hp = 100000f;
}
return $"{{\"count\":{storeAllies.Count},\"cd\":{F(summon.RemainingCoolDown)}}}";
'@
    Assert-ProbeValue -Name 'L1とL3を所持: クールダウン明けのマージショットで2体目が出る' -Actual $r.count -Expected 2 | Out-Null
    Assert-ProbeValue -Name 'L1とL3を所持: クールダウンは最高レベルの Value1=60秒' -Actual $r.cd -Expected 60 -Tolerance 0.1 | Out-Null

    # --- 旋回速度・向き切るまで撃たない・敵を撃つ・敵が近いタレットを狙う ---
    # 1体のタレットと1体の敵だけにし、敵はスタンで止める（動くと向きの変化が旋回速度だけで決まらない）。
    # 真横（90度）に置くので、旋回速度が 90度/秒 を超える設定だと1秒の待ちの間に向き切ってしまう
    $setupBody = @'
var turret = FirstAllyView();
var enemyAis = UnityEngine.Object.FindObjectsOfType<App.Battle.Interface.EnemyAI.EnemyAIBase>().Where(e => !(e is BossAIBase)).ToList();
if (turret == null || enemyAis.Count == 0) return "{\"error\":\"タレットか敵がいません\"}";

var otherAllyIds = new List<int>();
foreach (System.Collections.DictionaryEntry e in storeAllies) if ((int)e.Key != turret.Id) otherAllyIds.Add((int)e.Key);
foreach (var id in otherAllyIds) allies.Remove(id);

var enemyAi = enemyAis[0];
foreach (var other in enemyAis.Skip(1))
{
    enemies.RemoveEnemyData(other.EnemyId);
    presenter.UnSpawn(other.EnemyId);
}
presenter.SetStun(enemyAi.EnemyId, true);

// 砲身の真横（90度）に置く。15度/秒なら向き切るまで6秒かかる
var head = (Transform)typeof(App.Battle.Views.Ally.AllyTurretView).GetField("_head", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(turret);
var side = head.right; side.y = 0f; side.Normalize();
enemyAi.WarpTo(turret.transform.position + side * 3f);
Physics.SyncTransforms();
enemies.TryGetEnemyData(enemyAi.EnemyId, out var ed);
ed.MaxHp = 100000f;
ed.Hp = 100000f;
allies.TryGetAllyData(turret.Id, out var ad);
var turnSpeed = ad.TurnSpeed;
var allyConfig = scope.Container.Resolve<AllyConfig>();
allyConfig.TryGetEntry(AllyType.Turret, out var turretEntry);
var configTurnSpeed = turretEntry.TurnSpeed;

AppDomain.CurrentDomain.SetData("summon.enemyId", enemyAi.EnemyId);
AppDomain.CurrentDomain.SetData("summon.turretPos", turret.transform.position);
AppDomain.CurrentDomain.SetData("summon.yaw", head.eulerAngles.y);
AppDomain.CurrentDomain.SetData("summon.time", Time.time);
return $"{{\"enemyId\":{enemyAi.EnemyId},\"hp\":{F(ed.Hp)},\"turnSpeed\":{F(turnSpeed)},\"configTurnSpeed\":{F(configTurnSpeed)},\"others\":{otherAllyIds.Count}}}";
'@
    # 敵がいない（出し直しの待ち中）ときは少し待って取り直す
    for ($try = 0; $try -lt 5; $try++) {
        $r = Invoke-SummonSnippet -Body $setupBody
        if (-not $r.error) { break }
        Start-Sleep -Milliseconds 1500
    }
    if ($r.error) { throw $r.error }
    $hpBefore = $r.hp
    $turnSpeed = [double]$r.turnSpeed
    Assert-ProbeValue -Name 'タレットの旋回速度は AllyConfig の値' -Actual $turnSpeed -Expected ([double]$r.configTurnSpeed) | Out-Null
    Assert-ProbeTrue -Name '旋回速度は0より大きい（即座に向く設定では旋回の検証にならない）' -Condition ($turnSpeed -gt 0) | Out-Null
    Start-Sleep -Seconds 1

    $r = Invoke-SummonSnippet -Body @'
var enemyId = (int)AppDomain.CurrentDomain.GetData("summon.enemyId");
var turretPos = (Vector3)AppDomain.CurrentDomain.GetData("summon.turretPos");
var yaw0 = (float)AppDomain.CurrentDomain.GetData("summon.yaw");
var time0 = (float)AppDomain.CurrentDomain.GetData("summon.time");
var turret = FirstAllyView();
var head = (Transform)typeof(App.Battle.Views.Ally.AllyTurretView).GetField("_head", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(turret);
var turned = Mathf.Abs(Mathf.DeltaAngle(yaw0, head.eulerAngles.y));
var elapsed = Time.time - time0;
var enemyAi = UnityEngine.Object.FindObjectsOfType<App.Battle.Interface.EnemyAI.EnemyAIBase>().FirstOrDefault(e => e.EnemyId == enemyId);
var hp = enemies.TryGetEnemyData(enemyId, out var ed) ? ed.Hp : -1f;
var targetProp = typeof(App.Battle.Interface.EnemyAI.EnemyAIBase).GetProperty("TargetTransform", BindingFlags.NonPublic | BindingFlags.Instance);
var target = enemyAi != null ? (Transform)targetProp.GetValue(enemyAi) : null;
var targetsAlly = target != null && target.GetComponent<App.Battle.Views.Ally.AllyViewBase>() != null;
var playerDist = enemyAi != null ? Vector3.Distance(enemyAi.transform.position, playerView.PlayerTransform.position) : -1f;
var turretDist = enemyAi != null ? Vector3.Distance(enemyAi.transform.position, turretPos) : -1f;

// 次の段階: 砲身の正面へ置き直す（向きの差が射撃角度以内なので撃ち始める）
var front = head.forward; front.y = 0f; front.Normalize();
enemyAi.WarpTo(turretPos + front * 3f);
Physics.SyncTransforms();

return $"{{\"turned\":{F(turned)},\"elapsed\":{F(elapsed)},\"hp\":{F(hp)},\"targetsAlly\":{targetsAlly.ToString().ToLower()},\"playerDist\":{F(playerDist)},\"turretDist\":{F(turretDist)}}}";
'@
    $rate = [double]$r.turned / [double]$r.elapsed
    Write-Host "  旋回: $($r.turned)度 / $($r.elapsed)秒 = $([Math]::Round($rate, 2))度/秒 / 敵HP $hpBefore → $($r.hp)"
    Assert-ProbeValue -Name '砲身は旋回速度で向き直る（度/秒）' -Actual $rate -Expected $turnSpeed -Tolerance 1.5 | Out-Null
    Assert-ProbeValue -Name '向き直っている途中（射撃角度の外）は撃たない' -Actual $r.hp -Expected $hpBefore | Out-Null
    Assert-ProbeTrue -Name '敵はプレイヤーより近いタレットを狙う' -Condition ($r.targetsAlly -or ($r.turretDist -ge $r.playerDist)) -Detail "敵→タレット $($r.turretDist)m / 敵→プレイヤー $($r.playerDist)m / 狙い=味方:$($r.targetsAlly)" | Out-Null

    Start-Sleep -Milliseconds 1500
    $r = Invoke-SummonSnippet -Body @'
var enemyId = (int)AppDomain.CurrentDomain.GetData("summon.enemyId");
var hp = enemies.TryGetEnemyData(enemyId, out var ed) ? ed.Hp : -1f;
presenter.SetStun(enemyId, false);
return $"{{\"hp\":{F(hp)}}}";
'@
    Assert-ProbeTrue -Name '向き切ったら射程内の敵を撃ってダメージを与える' -Condition (($r.hp -ge 0) -and ($r.hp -lt $hpBefore)) -Detail "HP $hpBefore → $($r.hp)" | Out-Null

    # --- プレイヤーの弾はタレットをすり抜ける ---
    $r = Invoke-SummonSnippet -Body @'
var turret = FirstAllyView();
var bulletFactory = scope.Container.Resolve<App.Framework.Utilities.ISimpleObjectFactory<IBulletView>>();
var data = bulletParams.GetBulletData(App.Common.Data.ShotType.Normal, App.Common.Data.AimFocusType.NotFocus);
data.Damage = 0.001f;
data.Speed = 0f;
// タレットの胴を水平に貫く弾道で撃つ
var center = turret.transform.position + Vector3.up * 0.8f;
var dir = turret.transform.right;
// 敵弾で減った分を戻し、この1発（即着弾で同期的に判定される）の前後だけを比べる
allies.TryGetAllyData(turret.Id, out var ad);
ad.Hp = 100000f;
var bullet = bulletFactory.Instantiate(null);
bullet.Spawn(PlayerConstants.PlayerId, new Pose(center - dir * 3f, Quaternion.LookRotation(dir)), data, -1);
var hitIds = (List<int>)typeof(App.Battle.Views.Enemy.Bullet.BaseBulletView)
    .GetField("_hitTargetIds", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bullet);
return $"{{\"hitAlly\":{hitIds.Contains(turret.Id).ToString().ToLower()},\"hp\":{F(ad.Hp)}}}";
'@
    Assert-ProbeTrue -Name 'プレイヤーの弾はタレットに当たらない' -Condition (-not $r.hitAlly) | Out-Null
    Assert-ProbeValue -Name 'プレイヤーの弾でタレットの体力は減らない' -Actual $r.hp -Expected 100000 | Out-Null

    # --- フリーズで止まる ---
    $r = Invoke-SummonSnippet -Body @'
var isPauseProp = typeof(App.Battle.Views.Ally.AllyViewBase).GetProperty("IsPause", BindingFlags.NonPublic | BindingFlags.Instance);
freeze.Freeze(5f);
var during = (bool)isPauseProp.GetValue(FirstAllyView());
freeze.Cancel();
var after = (bool)isPauseProp.GetValue(FirstAllyView());
return $"{{\"during\":{during.ToString().ToLower()},\"after\":{after.ToString().ToLower()}}}";
'@
    Assert-ProbeTrue -Name 'フリーズ中はタレットが止まる' -Condition $r.during | Out-Null
    Assert-ProbeTrue -Name 'フリーズが明けるとタレットが動き出す' -Condition (-not $r.after) | Out-Null

    # --- 敵の攻撃で体力が減り、0で消える ---
    $r = Invoke-SummonSnippet -Body @'
var turret = FirstAllyView();
var id = turret.Id;
allies.TryGetAllyData(id, out var ad);
ad.Hp = ad.MaxHp;
var receiver = turret.GetComponentInChildren<App.Battle.Views.Ally.AllyDamageReceiverView>();
// 敵の近接・敵弾と同じ入口（IHitBoxView.OnHit）を通す
receiver.OnHit(ad.MaxHp * 0.25f, 12345, turret.transform.position + Vector3.forward, out var penetrable);
var hpAfterHit = ad.Hp;
var maxHp = ad.MaxHp;
var before = storeAllies.Count;
receiver.OnHit(ad.MaxHp, 12345, turret.transform.position + Vector3.forward, out _);
var exists = allies.TryGetAllyData(id, out _);
AppDomain.CurrentDomain.SetData("summon.deadTurret", turret);
return $"{{\"hp\":{F(hpAfterHit)},\"maxHp\":{F(maxHp)},\"penetrable\":{penetrable.ToString().ToLower()},\"before\":{before},\"after\":{storeAllies.Count},\"exists\":{exists.ToString().ToLower()}}}";
'@
    Assert-ProbeValue -Name '敵の攻撃でタレットの体力が減る（最大の25%）' -Actual $r.hp -Expected ($r.maxHp * 0.75) | Out-Null
    Assert-ProbeTrue -Name '敵弾はタレットを貫通しない' -Condition (-not $r.penetrable) | Out-Null
    Assert-ProbeValue -Name '体力が0になったタレットは消える（ビュー）' -Actual $r.after -Expected ($r.before - 1) | Out-Null
    Assert-ProbeTrue -Name '体力が0になったタレットは消える（データ）' -Condition (-not $r.exists) | Out-Null

    Start-Sleep -Milliseconds 300
    $r = Invoke-SummonSnippet -Body @'
var dead = (UnityEngine.Object)AppDomain.CurrentDomain.GetData("summon.deadTurret");
return $"{{\"destroyed\":{(dead == null).ToString().ToLower()}}}";
'@
    Assert-ProbeTrue -Name '撃破されたタレットの GameObject が破棄される' -Condition $r.destroyed | Out-Null

    # --- ウェーブ間ポーズで全員消える ---
    $r = Invoke-SummonSnippet -Body @'
// 撃破の検証で0体になっているので、もう1体出しておく
allies.TrySpawn(AllyType.Turret, AimPoint(), Vector3.zero, out _);
var before = storeAllies.Count;
wave.SetWavePause(true);
var after = storeAllies.Count;
var dataLeft = 0;
for (var id = -100; id > -120; id--) if (allies.TryGetAllyData(id, out _)) dataLeft++;
wave.SetWavePause(false);
return $"{{\"before\":{before},\"after\":{after},\"dataLeft\":{dataLeft}}}";
'@
    Assert-ProbeTrue -Name 'ウェーブ間ポーズの前にタレットが残っている' -Condition ($r.before -ge 1) -Detail "数=$($r.before)" | Out-Null
    Assert-ProbeValue -Name 'ウェーブ間ポーズでタレットが全員消える（ビュー）' -Actual $r.after -Expected 0 | Out-Null
    Assert-ProbeValue -Name 'ウェーブ間ポーズでタレットが全員消える（データ）' -Actual $r.dataLeft -Expected 0 | Out-Null
}

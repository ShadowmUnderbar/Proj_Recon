#
# コーラサワー（ColasourDataStore / PlayerStateDataStore.TakeDamage）を確かめるプローブ。
#
# 致死ダメージを受けたとき、確率でダメージを無効化する（体力は減らない）。確率は Value1 から始まり、発動するたびに Value2 ずつ下がる。
# ウェーブが変わると元に戻る。復活系の中で最優先（食いしばり・エマージェンシー・ノードより先に判定する）。
# デバッグ対戦（無敵）で始め、被弾を確かめるスニペットの中だけ無敵を外す（スニペットの間に敵の攻撃で死なないように）。
# ウェーブの切り替えは WaveManagerDataStore の現在ウェーブを直接書き換えて再現する（AdvanceWave の購読先を動かさないため）。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -EnemyCode 'O-001' -EnemyCount 1 -AutoRespawn $true -RespawnDelaySeconds 1 -Invincible $true
    }
    catch {
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Exit-BossProbeScene
}

$Global:ColasourPrelude = @'
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var cola = scope.Container.Resolve<IColasourDataStore>();
var grit = scope.Container.Resolve<IGritDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var arena = scope.Container.Resolve<DebugArenaSettings>();
var ci = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.###", ci);

// 無敵（デバッグ対戦の設定）を被弾の確認中だけ外す
var invincibleField = typeof(DebugArenaSettings).GetField("<Invincible>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
void SetInvincible(bool value) => invincibleField.SetValue(arena, value);

// ウェーブの切り替えを再現する
var waveField = wave.GetType().GetField("_currentWave", BindingFlags.NonPublic | BindingFlags.Instance);
void NextWave() { var p = (ReactiveProperty<int>)waveField.GetValue(wave); p.Value = p.Value + 1; }

App.Common.Data.MasterData.UpgradeMasterData Cola(int level) =>
    db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.Colasour && u.Level == level);

// 体力を hp にしてから致死ダメージを受け、その後の体力を返す
float LethalHit(float hp)
{
    player.Health.Value = hp;
    SetInvincible(false);
    try { player.TakeDamage(100000f); }
    finally { SetInvincible(true); }
    return player.Health.Value;
}

// 当たるまで TryActivate を繰り返し、確率の推移を「確率:試行回数」の並びで返す（確率0になったら止める）
string DrainChances()
{
    var parts = new List<string>();
    for (var n = 0; n < 10; n++)
    {
        var chance = cola.GetChance();
        if (chance <= 0f) { parts.Add(F(chance)); break; }
        var tries = 1;
        while (!cola.TryActivate() && tries < 10000) tries++;
        parts.Add($"{F(chance)}:{tries}");
    }
    return string.Join(",", parts);
}

// 確率0のとき、何度試しても発動しないか（発動した回数を返す）
int CountActivations(int tries)
{
    var hit = 0;
    for (var i = 0; i < tries; i++) if (cola.TryActivate()) hit++;
    return hit;
}
'@

function Invoke-ColasourSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    $r = Invoke-BossSnippet -Body ($Global:ColasourPrelude + "`n" + $Body)
    if ($r.error) { throw $r.error }
    return $r
}

function Assert-Near {
    param([string]$Name, [double]$Actual, [double]$Expected, [double]$Tolerance = 0.001)
    Assert-ProbeTrue -Name $Name -Condition ([math]::Abs($Actual - $Expected) -lt $Tolerance) -Detail "実測=$Actual 期待=$Expected" | Out-Null
}

# 「確率:試行回数,…,0」の並びから確率だけを取り出す
function Get-ChanceSequence {
    param([string]$Text)
    return @($Text.Split(',') | ForEach-Object { [double]($_.Split(':')[0]) })
}

function ProbeRun {
    Start-Sleep -Seconds 2

    # --- 未所持: 発動しない（実際に死なないよう TryActivate だけを見る） ---
    $r = Invoke-ColasourSnippet -Body @'
return $"{{\"chance\":{F(cola.GetChance())},\"ok\":{cola.TryActivate().ToString().ToLower()}}}";
'@
    Assert-Near -Name '未所持では確率0' -Actual $r.chance -Expected 0
    Assert-ProbeValue -Name '未所持では発動しない' -Actual $r.ok -Expected $false | Out-Null

    # --- L1: 確率は Value1 から Value2 ずつ下がり、3回で0になる ---
    $r = Invoke-ColasourSnippet -Body @'
session.AddUpgrade(Cola(1));
NextWave();
var u = Cola(1);
var seq = DrainChances();
var after = CountActivations(200);
return $"{{\"seq\":\"{seq}\",\"after\":{after},\"v1\":{F(u.Value1.value)},\"v2\":{F(u.Value2.value)}}}";
'@
    Write-Host "  L1: Value1=$($r.v1) Value2=$($r.v2) 推移(確率:当たるまでの試行回数)=$($r.seq)"
    $seq = Get-ChanceSequence $r.seq
    Assert-Near -Name 'L1: 3回発動した後に確率0になる（推移の長さ）' -Actual $seq.Count -Expected 4
    Assert-Near -Name 'L1: 最初の確率は Value1' -Actual $seq[0] -Expected $r.v1
    Assert-Near -Name 'L1: 1回発動すると Value2 下がる' -Actual $seq[1] -Expected ([double]$r.v1 - [double]$r.v2)
    Assert-Near -Name 'L1: 確率0では200回試しても発動しない' -Actual $r.after -Expected 0
    $l1V1 = [double]$r.v1

    # --- ウェーブが変わると確率が元に戻る ---
    $r = Invoke-ColasourSnippet -Body @'
NextWave();
return $"{{\"chance\":{F(cola.GetChance())}}}";
'@
    Assert-Near -Name 'L1: ウェーブが変わると確率が Value1 に戻る' -Actual $r.chance -Expected $l1V1

    # --- L1: 確率どおりに当たる（発動回数を戻しながら最初の1回を2000回試す。ウェーブは書き換えない） ---
    $r = Invoke-ColasourSnippet -Body @'
var resettable = (IRunResettable)cola;
var hit = 0;
for (var i = 0; i < 2000; i++) { resettable.ResetRun(); if (cola.TryActivate()) hit++; }
resettable.ResetRun();
return $"{{\"rate\":{F(hit / 2000f)}}}";
'@
    Assert-Near -Name 'L1: 当たる割合が Value1 に近い（±5%）' -Actual $r.rate -Expected $l1V1 -Tolerance 0.05

    # --- L3（L1 と同時所持）: 最高レベルの値で、4回発動しうる ---
    $r = Invoke-ColasourSnippet -Body @'
session.AddUpgrade(Cola(3));
NextWave();
var u = Cola(3);
var seq = DrainChances();
return $"{{\"seq\":\"{seq}\",\"v1\":{F(u.Value1.value)},\"v2\":{F(u.Value2.value)}}}";
'@
    Write-Host "  L3: Value1=$($r.v1) Value2=$($r.v2) 推移(確率:当たるまでの試行回数)=$($r.seq)"
    $seq = Get-ChanceSequence $r.seq
    Assert-Near -Name 'L1とL3: 4回発動した後に確率0になる（推移の長さ）' -Actual $seq.Count -Expected 5
    $l3V1 = [double]$r.v1
    $l3V2 = [double]$r.v2
    Assert-Near -Name 'L1とL3: 最初の確率は L3 の Value1' -Actual $seq[0] -Expected $l3V1
    Assert-Near -Name 'L1とL3: 4回目の確率は Value1 - Value2 × 3' -Actual $seq[3] -Expected ($l3V1 - $l3V2 * 3)

    # --- 実際の致死ダメージ: 体力が減らない・食いしばりより先に使われる ---
    $r = Invoke-ColasourSnippet -Body @'
session.AddUpgrade(db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.Grit && u.Level == 1));
NextWave();
var hp = LethalHit(50f);
var chance = cola.GetChance();
player.Health.Value = player.MaxHealth.Value;
return $"{{\"hp\":{F(hp)},\"chance\":{F(chance)}}}";
'@
    Assert-Near -Name '致死ダメージを無効化して体力が減らない（食いしばりの体力1にならない）' -Actual $r.hp -Expected 50
    Assert-Near -Name '発動すると確率が Value2 下がる' -Actual $r.chance -Expected ($l3V1 - $l3V2)

    # --- 確率0になったら食いしばりが発動する ---
    $r = Invoke-ColasourSnippet -Body @'
DrainChances();
var hp = LethalHit(50f);
player.Health.Value = player.MaxHealth.Value;
return $"{{\"hp\":{F(hp)}}}";
'@
    Assert-Near -Name '確率0になった後は食いしばりで体力1になる' -Actual $r.hp -Expected 1
}

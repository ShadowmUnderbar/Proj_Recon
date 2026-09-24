#
# ポイント粒子ドロップ（PointDropUseCase / PointParticleStoreView）を数値で検証するプローブ。
#
# 目視では「何か光っている」ことしか分からないため、
# ドロップ量の分割結果・撃破時の粒子生成数・接触回収・弾の通過回収を実測値で確認する。
#

function ProbeRun {
    # --- 1. ドロップ量の分割（大単位から貪欲に分割し、個数上限で繰り上げる） ---
    $split = Invoke-UnityJson -Snippet @'
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Data;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var calculator = scope.Container.Resolve<IPointDropCalculatorDataStore>();

var units = new List<PointUnitData>();
calculator.Split(137, units);

var values = new List<string>();
var total = 0;
foreach (var unit in units)
{
    values.Add(unit.Value.ToString());
    total += unit.Value;
}

// そのまま分割すると上限（既定12個）を超える量。小さい単位から繰り上げて丸められるはず
var largeUnits = new List<PointUnitData>();
calculator.Split(999, largeUnits);
var largeTotal = 0;
foreach (var unit in largeUnits)
{
    largeTotal += unit.Value;
}

return $"{{\"values\":\"{string.Join(",", values)}\",\"count\":{units.Count},\"total\":{total},\"largeCount\":{largeUnits.Count},\"largeTotal\":{largeTotal}}}";
'@

    Assert-ProbeTrue -Name '137の分割内訳が大単位から貪欲' -Condition ($split.values -eq '100,20,10,5,1,1') `
        -Detail "(実測: $($split.values))" | Out-Null
    Assert-ProbeValue -Name '137の分割合計' -Actual ([double]$split.total) -Expected 137 | Out-Null
    Assert-ProbeValue -Name '137の粒子数' -Actual ([double]$split.count) -Expected 6 | Out-Null
    Assert-ProbeTrue -Name '999の粒子数が上限以内' -Condition ([int]$split.largeCount -le 12) `
        -Detail "(実測: $($split.largeCount)個)" | Out-Null
    Assert-ProbeTrue -Name '999の分割合計が元の量を下回らない' -Condition ([int]$split.largeTotal -ge 999) `
        -Detail "(実測: $($split.largeTotal))" | Out-Null

    # --- 2. 敵の撃破で粒子が生成される ---
    $drop = Invoke-UnityJson -Snippet @'
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var enemyDataStore = scope.Container.Resolve<IEnemyDataStore>();
var calculator = scope.Container.Resolve<IPointDropCalculatorDataStore>();
var pointDataStore = scope.Container.Resolve<IPointDataStore>();
var storeView = scope.Container.Resolve<IPointParticleStoreView>() as MonoBehaviour;
if (storeView == null) throw new System.Exception("IPointParticleStoreViewを解決できません");

// 既に漂っている粒子があっても差分で見られるよう、撃破前の個数を控える
var before = storeView.transform.childCount;
var pointBefore = pointDataStore.CurrentPoint.CurrentValue;

if (!enemyDataStore.TryGetRandomEnemyMasterData(EnemyRankType.Common, UnlockCoreSkillType.First, out var master))
{
    throw new System.Exception("Commonランクの敵マスターデータを取得できません");
}

var dropPoint = calculator.GetDropPoint(master);
var expectedUnits = new List<PointUnitData>();
calculator.Split(dropPoint, expectedUnits);
var expectedTotal = 0;
foreach (var unit in expectedUnits)
{
    expectedTotal += unit.Value;
}

// 敵を1体だけ足して即撃破する（ビューを介さずDataStoreの撃破通知だけを起こす）
var enemyData = enemyDataStore.AddEnemyData(master, new Pose(new Vector3(0f, 0f, 5f), Quaternion.identity));
enemyDataStore.Damage(new HitData(enemyData.Id, 99999f, HitDirectionType.None));

var spawned = storeView.transform.childCount - before;

return $"{{\"dropPoint\":{dropPoint},\"expectedCount\":{expectedUnits.Count},\"expectedTotal\":{expectedTotal},\"spawned\":{spawned},\"pointBefore\":{pointBefore}}}";
'@

    Assert-ProbeTrue -Name 'Common敵のドロップ量が1以上' -Condition ([int]$drop.dropPoint -ge 1) `
        -Detail "(実測: $($drop.dropPoint))" | Out-Null
    Assert-ProbeValue -Name '撃破で生成された粒子数' -Actual ([double]$drop.spawned) -Expected ([double]$drop.expectedCount) | Out-Null

    # --- 3. プレイヤーへの接触で回収される ---
    Invoke-UnityCode -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var storeView = scope.Container.Resolve<IPointParticleStoreView>() as MonoBehaviour;
var playerView = scope.Container.Resolve<IBattlePlayerView>();

// 漂っている粒子をプレイヤーの胴体位置へ運び、接触回収を起こす
var playerCenter = playerView.PlayerTransform.position + Vector3.up;
foreach (Transform particle in storeView.transform)
{
    particle.position = playerCenter;
}

return storeView.transform.childCount.ToString();
'@ | Out-Null

    # 回収は PointParticleStoreView.Update でまとめて処理されるため1フレーム以上待つ
    Start-Sleep -Milliseconds 500

    $collected = Invoke-UnityJson -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var storeView = scope.Container.Resolve<IPointParticleStoreView>() as MonoBehaviour;
var pointDataStore = scope.Container.Resolve<IPointDataStore>();

return $"{{\"remain\":{storeView.transform.childCount},\"point\":{pointDataStore.CurrentPoint.CurrentValue}}}";
'@

    Assert-ProbeValue -Name '接触後に残っている粒子数' -Actual ([double]$collected.remain) -Expected 0 | Out-Null
    Assert-ProbeValue -Name '接触回収で増えたポイント' `
        -Actual ([double]$collected.point - [double]$drop.pointBefore) -Expected ([double]$drop.expectedTotal) | Out-Null

    # --- 4. プレイヤーの弾が通過しても弾は消えず、粒子だけが回収される ---
    $shot = Invoke-UnityJson -Snippet @'
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Framework.Utilities;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var waveDataStore = scope.Container.Resolve<IWaveManagerDataStore>();
var enemyDataStore = scope.Container.Resolve<IEnemyDataStore>();
var pointDataStore = scope.Container.Resolve<IPointDataStore>();
var calculator = scope.Container.Resolve<IPointDropCalculatorDataStore>();
var presenter = scope.Container.Resolve<IPointParticlePresenter>();
var playerView = scope.Container.Resolve<IBattlePlayerView>();
var bulletFactory = scope.Container.Resolve<ISimpleObjectFactory<IBulletView>>();

// 敵や敵弾に邪魔されずに弾道を確かめるため、ウェーブを止めて敵を消しておく
waveDataStore.SetWavePause(true);
enemyDataStore.RemoveAllEnemyData();

// 当たり判定が大きく狙いやすい最大単位の粒子を1個だけ出す
var units = new List<PointUnitData>();
calculator.Split(100, units);
if (units.Count != 1) throw new System.Exception("100ポイントの分割が1個になりません: " + units.Count);

var player = playerView.PlayerTransform;
var spawnPosition = player.position + player.forward * 8f;
presenter.Spawn(spawnPosition, units);

var storeView = scope.Container.Resolve<IPointParticleStoreView>() as MonoBehaviour;
var particle = storeView.transform.GetChild(storeView.transform.childCount - 1);

// 粒子と同じ高さの水平弾道にする（斜め撃ちだと銃口が地面に埋まって弾が即消えてしまう）
var direction = player.forward;
var muzzlePosition = particle.position - direction * 3f;

var bullet = bulletFactory.Instantiate(null);
var bulletObject = ((MonoBehaviour)bullet).gameObject;
bulletObject.name = "ProbeBullet";
bullet.Spawn(
    BasePlayerParameter.PlayerId,
    new Pose(muzzlePosition, Quaternion.LookRotation(direction)),
    new BulletData { Speed = 10f, Damage = 1f, Size = 0.2f },
    -1);

return $"{{\"pointBefore\":{pointDataStore.CurrentPoint.CurrentValue},\"particleCount\":{storeView.transform.childCount},\"unitValue\":{units[0].Value}}}";
'@

    Assert-ProbeValue -Name '弾の通過を試す粒子数' -Actual ([double]$shot.particleCount) -Expected 1 | Out-Null

    # 弾速10m/sで3m先の粒子を通過する時間ぶん待つ
    Start-Sleep -Milliseconds 600

    $passed = Invoke-UnityJson -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var storeView = scope.Container.Resolve<IPointParticleStoreView>() as MonoBehaviour;
var pointDataStore = scope.Container.Resolve<IPointDataStore>();
var bulletObject = GameObject.Find("ProbeBullet");

return $"{{\"remain\":{storeView.transform.childCount},\"point\":{pointDataStore.CurrentPoint.CurrentValue},\"bulletAlive\":{(bulletObject != null).ToString().ToLower()}}}";
'@

    Assert-ProbeValue -Name '弾の通過後に残っている粒子数' -Actual ([double]$passed.remain) -Expected 0 | Out-Null
    Assert-ProbeValue -Name '弾の通過で増えたポイント' `
        -Actual ([double]$passed.point - [double]$shot.pointBefore) -Expected ([double]$shot.unitValue) | Out-Null
    Assert-ProbeTrue -Name '粒子を通過した弾が消えていない' -Condition ([bool]$passed.bulletAlive) `
        -Detail '(粒子で弾が止まると貫通仕様が壊れる)' | Out-Null

    # --- 5. 即着弾（弾速0）の弾道上の粒子も回収される ---
    Invoke-UnityCode -Snippet @'
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var calculator = scope.Container.Resolve<IPointDropCalculatorDataStore>();
var presenter = scope.Container.Resolve<IPointParticlePresenter>();
var playerView = scope.Container.Resolve<IBattlePlayerView>();

var units = new List<PointUnitData>();
calculator.Split(50, units);
if (units.Count != 1) throw new System.Exception("50ポイントの分割が1個になりません: " + units.Count);

var player = playerView.PlayerTransform;
presenter.Spawn(player.position + player.forward * 8f, units);

return units[0].Value.ToString();
'@ | Out-Null

    # 生成した粒子のコライダーが物理エンジンへ反映されるまで1フレーム待ってから撃つ
    Start-Sleep -Milliseconds 300

    $instant = Invoke-UnityJson -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Framework.Utilities;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var pointDataStore = scope.Container.Resolve<IPointDataStore>();
var playerView = scope.Container.Resolve<IBattlePlayerView>();
var bulletFactory = scope.Container.Resolve<ISimpleObjectFactory<IBulletView>>();
var storeView = scope.Container.Resolve<IPointParticleStoreView>() as MonoBehaviour;

var particle = storeView.transform.GetChild(storeView.transform.childCount - 1);
var particleView = particle.GetComponent<IPointParticleView>();

var player = playerView.PlayerTransform;
var direction = player.forward;
var muzzlePosition = particle.position - direction * 3f;
var pointBefore = pointDataStore.CurrentPoint.CurrentValue;

// 弾速0＝即着弾。弾道上をSphereCastでまとめて判定する経路を通す
var bullet = bulletFactory.Instantiate(null);
bullet.Spawn(
    BasePlayerParameter.PlayerId,
    new Pose(muzzlePosition, Quaternion.LookRotation(direction)),
    new BulletData { Speed = 0f, Damage = 1f, Size = 0.2f },
    -1);

return $"{{\"pointBefore\":{pointBefore},\"collectedImmediately\":{particleView.IsCollected.ToString().ToLower()}}}";
'@

    Assert-ProbeTrue -Name '即着弾の判定で粒子が回収済みになる' -Condition ([bool]$instant.collectedImmediately) | Out-Null

    # 回収の反映（PointParticleStoreView.Update）を1フレーム以上待つ
    Start-Sleep -Milliseconds 400

    $instantResult = Invoke-UnityJson -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var storeView = scope.Container.Resolve<IPointParticleStoreView>() as MonoBehaviour;
var pointDataStore = scope.Container.Resolve<IPointDataStore>();

return $"{{\"remain\":{storeView.transform.childCount},\"point\":{pointDataStore.CurrentPoint.CurrentValue}}}";
'@

    Assert-ProbeValue -Name '即着弾の通過後に残っている粒子数' -Actual ([double]$instantResult.remain) -Expected 0 | Out-Null
    Assert-ProbeValue -Name '即着弾で増えたポイント' `
        -Actual ([double]$instantResult.point - [double]$instant.pointBefore) -Expected 50 | Out-Null
}

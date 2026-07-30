using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Data.MasterData;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// ピースメイカーの実行時状態。
    /// ノーマルショットのクールダウンを Value2 倍に短縮するが、
    /// ノーマルショットを Value1 回連続で撃つと次の1発だけ Value3 倍に伸びる。
    /// マージ／ワルツを撃つと連続数はリセットされる（＝フォームを混ぜればペナルティを回避できる）。
    /// レベルは累積せず、所持中の最高レベルのみを採用する。
    /// </summary>
    public class PeaceMakerDataStore : IPeaceMakerDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        // 連続して撃ったノーマルショット数
        private int _consecutiveNormalShotCount;

        [Inject]
        public PeaceMakerDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        public float GetCoolDownMultiplier(ShotType shotType)
        {
            if (shotType != ShotType.Normal)
            {
                return 1f;
            }

            if (!TryGetUpgrade(out var upgrade))
            {
                return 1f;
            }

            // 規定の連続数に達した直後の1発だけペナルティ倍率（連射性能ダウン）
            return IsPenaltyShot(upgrade)
                ? upgrade.Value3.value
                : upgrade.Value2.value;
        }

        public void NotifyShot(ShotType shotType)
        {
            if (shotType != ShotType.Normal)
            {
                // 別フォームを撃ったら連続が途切れる
                _consecutiveNormalShotCount = 0;
                return;
            }

            if (!TryGetUpgrade(out var upgrade))
            {
                return;
            }

            if (IsPenaltyShot(upgrade))
            {
                // ペナルティの1発を撃ち終えたので連続数をリセットする
                _consecutiveNormalShotCount = 0;
                return;
            }

            _consecutiveNormalShotCount++;
        }

        private bool TryGetUpgrade(out UpgradeMasterData upgrade)
        {
            return _upgradeEffectSimpleCalculatorDataStore
                .TryGetHighestLevelUpgrade(UpgradeType.PeaceMaker, out upgrade);
        }

        private bool IsPenaltyShot(UpgradeMasterData upgrade)
        {
            var requiredCount = Mathf.Max(1, Mathf.RoundToInt(upgrade.Value1.value));
            return _consecutiveNormalShotCount >= requiredCount;
        }
    }
}

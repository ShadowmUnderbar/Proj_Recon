using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 毒舌の効果判定。
    /// 射撃（プレイヤー・味方の弾）で敵にダメージを与えるたびに、与えたダメージ × Value1 を威力・効果時間（秒）とするウイルスを付与する。
    /// レイジ発動中は威力だけ Value2 倍にする（効果時間はそのまま）。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class InvectiveDataStore : IInvectiveDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IBuffStateDataStore _buffStateDataStore;
        private readonly IVirusDataStore _virusDataStore;

        [Inject]
        public InvectiveDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IBuffStateDataStore buffStateDataStore,
            IVirusDataStore virusDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _buffStateDataStore = buffStateDataStore;
            _virusDataStore = virusDataStore;
        }

        public void Infect(int enemyId, float dealtDamage)
        {
            if (dealtDamage <= 0f)
            {
                return;
            }

            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Invective, out var upgrade))
            {
                return;
            }

            var amount = dealtDamage * Mathf.Max(0f, upgrade.Value1.value);
            var power = _buffStateDataStore.IsRageActive ? amount * Mathf.Max(0f, upgrade.Value2.value) : amount;

            // 威力・効果時間が0以下なら Apply 側で何もしない
            _virusDataStore.Apply(enemyId, power, amount);
        }
    }
}

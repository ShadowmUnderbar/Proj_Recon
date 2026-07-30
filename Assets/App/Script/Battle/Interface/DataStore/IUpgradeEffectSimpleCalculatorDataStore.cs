using App.Common.Data.MasterData;

namespace App.Battle.Interface.DataStore
{
    public interface IUpgradeEffectSimpleCalculatorDataStore
    {
        float CalcMultiply(UpgradeType upgradeType);
        float CalcAdd(UpgradeType upgradeType);

        /// <summary>
        /// 指定タイプの全アップグレードのうち Value1 の最大値を返す。
        /// レベルが累積せず「最高レベルのみ採用」したい効果（バリア等）に使う。効果なし時は 0f。
        /// </summary>
        float CalcMax(UpgradeType upgradeType);

        /// <summary>
        /// 指定タイプで所持中の最高レベルを返す。効果なし時は 0。
        /// レベルによって挙動が変わる効果（チョークポイントのLv3など）の判定に使う。
        /// </summary>
        int CalcMaxLevel(UpgradeType upgradeType);

        /// <summary>
        /// 指定タイプで所持中の最高レベルのマスターデータを返す。未所持なら false。
        /// レベルが累積せず、複数のValueを参照したい効果（ピースメイカー・雪崩等）に使う。
        /// </summary>
        bool TryGetHighestLevelUpgrade(UpgradeType upgradeType, out UpgradeMasterData upgradeMasterData);
    }
}
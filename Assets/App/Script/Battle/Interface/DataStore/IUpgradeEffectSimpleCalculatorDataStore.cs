namespace App.Battle.Interface.DataStore
{
    public interface IUpgradeEffectSimpleCalculatorDataStore
    {
        float CalcMultiply(UpgradeType upgradeType);
        float CalcAdd(UpgradeType upgradeType);
    }
}
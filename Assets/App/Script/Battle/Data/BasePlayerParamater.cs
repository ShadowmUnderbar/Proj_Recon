namespace App.Battle.Data
{
    public static class BasePlayerParameter
    {
        #region BasePlayerParameter

        public static float Health => 100f;
        public static float MoveSpeed => 1f;

        public static float BaseDamage => 4f;
        public static float BaseShotRate => 1f;
        public static float BaseBulletSpeed => 20f;
        public static float BaseFireRate => 0.3f;
        public static int BasePenetration => 0;

        #endregion

        #region Focus

        public static float FocusDamageMagnification => 1.1f;
        public static float FocusFireRateMagnification => 0.9f;
        public static float FocusBulletSpeedMagnification => 0.9f;
        public static float FocusPenetration => 3f;
        public static float FocusExplosiveMagnification => 1.5f;

        #endregion

        #region LongFocus

        public static float LongFocusDamageMagnification => 3f;
        public static float LongFocusFireRateMagnification => 2.5f;
        public static float LongFocusBulletSpeedMagnification => 2.5f;
        public static float LongFocusPenetrationMagnification => 3f;
        public static float LongFocusExplosiveMagnification => 3f;

        #endregion

        #region Waltz

        public static float WaltzDamageMagnification => 0.15f;
        public static float WaltzFireRateMagnification => 0.3f;
        public static float WaltzBulletSpeedMagnification => 0.8f;

        #endregion

        #region Merge

        public static float MergeDamageMagnification => 2f;
        public static float MergeFireRateMagnification => 1.5f;
        public static float MergeBulletSpeedMagnification => 1.5f;
        public static float MergeExplosiveScale => 2f;

        #endregion

        #region Dodge

        public static float DodgeRange => 1f;
        public static float DodgeDamage => 10f;
        public static int DodgeCount => 2;
        public static float DodgeCooldown => 3f;

        #endregion
    }
}
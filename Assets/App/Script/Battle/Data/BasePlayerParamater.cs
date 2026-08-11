namespace App.Battle.Data
{
    public static class BasePlayerParameter
    {
        public static int PlayerId = 1;

        #region BasePlayerParameter

        public static float Health => 100f;
        public static float MoveSpeed => 1f;

        public static float BaseDamage => 4f;
        public static float BaseShotRate => 1f;
        public static float BaseFireRate => 0.3f;
        public static int BasePenetration => 0;

        #endregion

        #region Focus

        public static float FocusDamageMagnification => 1.1f;
        public static float FocusFireRateMagnification => 0.9f;
        public static float FocusPenetration => 3f;
        public static float FocusExplosiveMagnification => 1.5f;

        #endregion

        #region LongFocus

        public static float LongFocusDamageMagnification => 3f;
        public static float LongFocusFireRateMagnification => 2.5f;
        public static float LongFocusPenetrationMagnification => 3f;
        public static float LongFocusExplosiveMagnification => 3f;

        #endregion

        #region Waltz

        // 片手1ストリーム基準で、両手発射のノーマルショット（26.7DPS）の約0.83倍（22.2DPS）になる倍率。
        // 左右で別方向を狙う仕様のため、単体火力は最下位だが左右合計の面制圧力で差別化する
        public static float WaltzDamageMagnification => 0.5f;
        public static float WaltzFireRateMagnification => 0.3f;

        #endregion

        #region Merge

        // 利き手のみの1ストリーム基準で、両手発射のノーマルショット（26.7DPS）の約1.33倍（35.6DPS）になる倍率。
        // 両手を寄せる必要がありアキンボを捨てる代償として、単体火力を最上位に置く
        public static float MergeDamageMagnification => 4f;
        public static float MergeFireRateMagnification => 1.5f;
        public static float MergeBulletSpeed => 40f;
        public static float MergeExplosiveScale => 2f;

        #endregion

        #region Dodge

        public static float DodgeRange => 10f;
        public static float DodgeDamage => 10f;
        public static int DodgeCount => 2;
        public static float DodgeCooldown => 3f;

        #endregion
    }
}
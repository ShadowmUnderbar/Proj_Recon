namespace App.Battle.Data
{
    /// <summary>
    /// ウイルスのデバッグ設定。
    /// 値は LifetimeScope が再生開始時に DebugConfig から読んで注入する（利用側は DebugConfig を直接参照しない）
    /// </summary>
    public class VirusDebugSettings
    {
        /// <summary>ウイルスの発生・重複（効果時間・最長効果時間・威力）と、1回ごとのダメージ量をログに出す</summary>
        public bool LogEnabled { get; }

        public VirusDebugSettings(bool logEnabled)
        {
            LogEnabled = logEnabled;
        }
    }
}

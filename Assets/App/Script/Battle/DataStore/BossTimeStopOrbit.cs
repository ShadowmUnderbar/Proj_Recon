namespace App.Battle.DataStore
{
    /// <summary>
    /// 台本の TimeStopOrbit ステップの進み具合。
    /// 進め方は <see cref="BossPatternRunner"/> が決め、ここは状態だけを持つ。
    /// </summary>
    public class BossTimeStopOrbit
    {
        /// <summary>ステップ全体の段階</summary>
        public enum Stage
        {
            /// <summary>始まっていない（全員の行動が明けるのを待つ）</summary>
            None,

            /// <summary>時を止めて、対象が回りこみ終えるのを待っている</summary>
            Orbit,

            /// <summary>時止めを解いて、次のステップへ進むまでの一息</summary>
            Breath
        }

        public Stage CurrentStage { get; private set; } = Stage.None;

        /// <summary>Breath の経過秒数</summary>
        public float BreathElapsed { get; set; }

        /// <summary>時を止めている間か（回りこんでいる段階）</summary>
        public bool IsTimeStopping => CurrentStage == Stage.Orbit;

        public void BeginOrbit()
        {
            CurrentStage = Stage.Orbit;
        }

        public void BeginBreath()
        {
            CurrentStage = Stage.Breath;
            BreathElapsed = 0f;
        }

        public void Reset()
        {
            CurrentStage = Stage.None;
            BreathElapsed = 0f;
        }
    }
}

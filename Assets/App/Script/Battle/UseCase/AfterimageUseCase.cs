using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// アップグレードによる一時強化（レイジ・カウンターステップ等、効果時間の間だけ効くバフ）の発動中、
    /// 一定距離進むか一定時間たつごとにプレイヤーの残像を残すよう指示する。
    /// 形状の取得・寿命・描画は <see cref="IAfterimageStoreView"/> 側が持ち、ここは「いつ残すか」だけを決める。
    /// </summary>
    public class AfterimageUseCase : ITickable
    {
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IBuffStateDataStore _buffStateDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IFreezeDataStore _freezeDataStore;
        private readonly ITimeStopDataStore _timeStopDataStore;
        private readonly IAfterimagePresenter _afterimagePresenter;
        private readonly AfterimageConfig _config;

        private readonly AfterimageSpawnScheduler _scheduler = new();

        [Inject]
        public AfterimageUseCase(
            IPlayerStateDataStore playerStateDataStore,
            IBuffStateDataStore buffStateDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IFreezeDataStore freezeDataStore,
            ITimeStopDataStore timeStopDataStore,
            IAfterimagePresenter afterimagePresenter,
            AfterimageConfig config
        )
        {
            _playerStateDataStore = playerStateDataStore;
            _buffStateDataStore = buffStateDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _freezeDataStore = freezeDataStore;
            _timeStopDataStore = timeStopDataStore;
            _afterimagePresenter = afterimagePresenter;
            _config = config;
        }

        public void Tick()
        {
            var position = _playerStateDataStore.Position.Value;

            // 一時強化の発動・終了を毎フレームの状態で見る（発動元がレイジ・各バフと複数あり、通知の口が揃っていないため）
            var isBuffed = _buffStateDataStore.IsTimedBuffActive;
            if (isBuffed != _scheduler.IsActive)
            {
                if (isBuffed)
                {
                    // 発動した地点・時点を起点にする（発動した瞬間は実体と重なるので残さない）
                    _scheduler.Begin(position);
                }
                else
                {
                    _scheduler.End();
                }

                return;
            }

            // プレイヤーの移動が止まる間（ショップ・フリーズ・時止め）は時間でも残さない。
            // レイジはショップ中も切れないので、止めないとショップで残像が出続ける
            var isPlayerPaused = _waveManagerDataStore.IsWavePause.Value ||
                                 _freezeDataStore.IsFreezing.CurrentValue ||
                                 _timeStopDataStore.IsTimeStopped.CurrentValue;
            var deltaTime = isPlayerPaused ? 0f : Time.deltaTime;

            if (_scheduler.Advance(position, deltaTime, _config.SpawnDistance, _config.SpawnInterval,
                    _config.WarpDistance))
            {
                Spawn();
            }
        }

        private void Spawn()
        {
            // 見た目は残す時点の設定を読む。設定アセットを実行中に変えると、次に残す残像から反映される
            _afterimagePresenter.SpawnPlayerAfterimage(_config.Style);
        }
    }
}

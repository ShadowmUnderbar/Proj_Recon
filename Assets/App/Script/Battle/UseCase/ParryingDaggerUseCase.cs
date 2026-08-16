using System;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Interface;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// パリングダガー。
    /// 回避中に無効化した敵弾に対して、撃ってきた敵へパリィ弾（即着弾のフォーカスショット）を撃ち返す。
    /// 近接攻撃・爆風は弾ではないため対象外。
    /// </summary>
    public class ParryingDaggerUseCase : IInitializable, IDisposable
    {
        // パリィ弾を敵のどの高さへ向けるか（敵Poseは足元基準のため胴体あたりを狙う）
        private const float AimHeight = 1f;

        private readonly IPlayerDodgeParameterDataStore _playerDodgeParameterDataStore;
        private readonly IParryingDaggerDataStore _parryingDaggerDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IPlayerSettingDataStore _playerSettingDataStore;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public ParryingDaggerUseCase(
            IPlayerDodgeParameterDataStore playerDodgeParameterDataStore,
            IParryingDaggerDataStore parryingDaggerDataStore,
            IEnemyDataStore enemyDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IPlayerSettingDataStore playerSettingDataStore
        )
        {
            _playerDodgeParameterDataStore = playerDodgeParameterDataStore;
            _parryingDaggerDataStore = parryingDaggerDataStore;
            _enemyDataStore = enemyDataStore;
            _playerControlPresenter = playerControlPresenter;
            _playerSettingDataStore = playerSettingDataStore;
        }

        public void Initialize()
        {
            _playerDodgeParameterDataStore.OnDamagedDuringDodge
                .Subscribe(OnDamageBlocked)
                .AddTo(_disposable);
        }

        private void OnDamageBlocked(PlayerDamagedData damagedData)
        {
            // 跳ね返せるのは弾のみ（近接攻撃・爆風は無効化だけで反撃しない）
            if (!damagedData.IsProjectile)
            {
                return;
            }

            if (!_parryingDaggerDataStore.TryGetParryBulletData(out var bulletData))
            {
                return;
            }

            // 撃ってきた敵が既に倒れている場合は撃ち返さない
            if (!_enemyDataStore.TryGetEnemyData(damagedData.AttackerId, out var enemyData))
            {
                return;
            }

            var targetPosition = enemyData.Pose.position + Vector3.up * AimHeight;

            _playerControlPresenter.ShotToward(
                _playerSettingDataStore.DominantHand.Value,
                bulletData,
                damagedData.AttackerId,
                targetPosition);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}

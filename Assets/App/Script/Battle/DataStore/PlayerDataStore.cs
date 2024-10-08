using App.Common.Data;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    public class PlayerDataStore : IPlayerDataStore, ITickable
    {
        public ReactiveProperty<float> Health { get; } = new();
        public ReactiveProperty<float> MaxHealth { get; } = new();
        public ReactiveProperty<int> FocusLeftTargetId { get; } = new();
        public ReactiveProperty<int> FocusRightTargetId { get; } = new();

        public float NormalFireRate => 0.6f;

        public float MergeFireRate => 1.2f;

        public float WaltzFireRate => 0.3f;

        private float _leftNomalShotCoolDown;
        private float _rightNomalShotCoolDown;
        private float _mergeShotCoolDown;
        private float _leftWaltzShotCoolDown;
        private float _rightWaltzShotCoolDown;


        public void Tick()
        {
            if (_leftNomalShotCoolDown > 0)
            {
                _leftNomalShotCoolDown -= Time.deltaTime;
            }

            if (_rightNomalShotCoolDown > 0)
            {
                _rightNomalShotCoolDown -= Time.deltaTime;
            }

            if(_mergeShotCoolDown > 0)
            {
                _mergeShotCoolDown -= Time.deltaTime;
            }

            if (_leftWaltzShotCoolDown > 0)
            {
                _leftWaltzShotCoolDown -= Time.deltaTime;
            }

            if (_rightWaltzShotCoolDown > 0)
            {
                _rightWaltzShotCoolDown -= Time.deltaTime;
            }
        }

        public bool CanShotCoolDown(bool isLeft, ShotType shotType)
        {
            switch (shotType)
            {
                case ShotType.Normal:
                    return isLeft ? CanLeftNormalShot : CanRightNormalShot;
                case ShotType.Merge:
                    return CanMergeShot;
                case ShotType.Waltz:
                    return isLeft ? CanLeftWaltzShot : CanRightWaltzShot;
                default:
                   return false;
            }
        }

        private bool CanLeftNormalShot => _leftNomalShotCoolDown <= 0;
        private bool CanRightNormalShot => _rightNomalShotCoolDown <= 0;
        private bool CanMergeShot => _mergeShotCoolDown <= 0;
        private bool CanLeftWaltzShot => _leftWaltzShotCoolDown <= 0;
        private bool CanRightWaltzShot => _rightWaltzShotCoolDown <= 0;

        public void SetLeftNormalShotCoolDown(float time)
        {
            _leftNomalShotCoolDown = time;
        }

        public void SetRightNormalShotCoolDown(float time)
        {
            _rightNomalShotCoolDown = time;
        }

        public void SetMergeShotCoolDown(float time)
        {
            _mergeShotCoolDown = time;
        }

        public void SetLeftWaltzShotCoolDown(float time)
        {
            _leftWaltzShotCoolDown = time;
        }

        public void SetRightWaltzShotCoolDown(float time)
        {
            _rightWaltzShotCoolDown = time;
        }

        public ShotType GetShotType(bool isLeft)
        {
            return ShotType.Normal;
        }
    }
}
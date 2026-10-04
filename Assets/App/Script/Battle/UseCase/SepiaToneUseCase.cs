using System;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// セピア調の強さの変化を、シェーダへ配る View に流す。
    /// どのプリセットをかけるかは演出の発生源（<see cref="OverclockUseCase"/> など）が <see cref="ISepiaToneDataStore"/> へ指定する。
    /// </summary>
    public class SepiaToneUseCase : IInitializable, IDisposable
    {
        private readonly ISepiaToneDataStore _sepiaToneDataStore;
        private readonly ISepiaToneView _sepiaToneView;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public SepiaToneUseCase(ISepiaToneDataStore sepiaToneDataStore, ISepiaToneView sepiaToneView)
        {
            _sepiaToneDataStore = sepiaToneDataStore;
            _sepiaToneView = sepiaToneView;
        }

        public void Initialize()
        {
            // 初期値（効果なし）も配って、前回の再生で残ったグローバル変数を打ち消す
            _sepiaToneDataStore.Weights
                .Subscribe(_sepiaToneView.SetWeights)
                .AddTo(_disposable);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}

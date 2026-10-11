using System;
using System.Threading;
using App.Common.Interface;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Settings;
using VContainer.Unity;

namespace App.Boot
{
    /// <summary>
    /// 起動時の共通初期化を順に待ち、終わったらメインメニューへ遷移する。
    ///
    /// セーブデータの読み込みや入力・マスターデータは常駐スコープの生成時に済んでいる。
    /// ここでは非同期で終わるもの（Addressables のカタログ、Localization、XR）を待ち、
    /// メインメニューが「まだ準備できていない」状態で始まらないようにする。
    ///
    /// どれかが失敗しても起動を止めない（エラーを出してメインメニューへ進む）。
    /// 起動シーンで止まると実機では復帰手段がないため。
    /// </summary>
    public class BootUseCase : IAsyncStartable
    {
        // XR の起動を待つ上限。ヘッドセット未接続などで終わらない場合でもメインメニューへ進む
        private static readonly TimeSpan XRInitTimeout = TimeSpan.FromSeconds(10);

        private readonly IXRInitUseCase _xrInitUseCase;
        private readonly ISceneTransitionUseCase _sceneTransitionUseCase;

        public BootUseCase(IXRInitUseCase xrInitUseCase, ISceneTransitionUseCase sceneTransitionUseCase)
        {
            _xrInitUseCase = xrInitUseCase;
            _sceneTransitionUseCase = sceneTransitionUseCase;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            await InitializeAddressables(cancellation);
            await InitializeLocalization(cancellation);
            await WaitXRInitialized(cancellation);

            if (!_sceneTransitionUseCase.LoadMainMenu())
            {
                Debug.LogError("[BootUseCase] メインメニューへ遷移できませんでした。Build Settings を確認してください");
            }
        }

        private static async UniTask InitializeAddressables(CancellationToken cancellation)
        {
            // 自動解放にすると同期完了時にハンドルが先に無効化され Task を取れないため、手動で解放する
            var handle = Addressables.InitializeAsync(false);
            try
            {
                await handle.Task.AsUniTask().AttachExternalCancellation(cancellation);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[BootUseCase] Addressables の初期化に失敗しました\n{e}");
            }
            finally
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }

        private static async UniTask InitializeLocalization(CancellationToken cancellation)
        {
            try
            {
                await LocalizationSettings.InitializationOperation.Task.AsUniTask().AttachExternalCancellation(cancellation);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[BootUseCase] Localization の初期化に失敗しました\n{e}");
            }
        }

        private async UniTask WaitXRInitialized(CancellationToken cancellation)
        {
            var isTimeout = await UniTask.WaitUntil(() => _xrInitUseCase.IsInitialized, cancellationToken: cancellation)
                .TimeoutWithoutException(XRInitTimeout);

            if (isTimeout)
            {
                Debug.LogWarning("[BootUseCase] XR の起動が時間内に終わらなかったため、待たずにメインメニューへ進みます");
            }
        }
    }
}

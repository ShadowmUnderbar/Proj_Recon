using System;
using App.Common.Data;
using App.Common.Interface;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace App.Common.UseCase
{
    /// <summary>
    /// シーン遷移を実行する。常駐スコープ（CommonLifetimeScope）に登録され、
    /// シーンをまたいで同じインスタンスが使われる。
    ///
    /// ボタン連打などで<see cref="SceneManager.LoadSceneAsync(string)"/>が重なると
    /// 読み込み中のシーンが二重に積まれるため、遷移が終わるまで後続の要求を無視する。
    ///
    /// 遷移を開始できたかを戻り値で返す。呼び出し側は失敗時にUIを元へ戻せるようにしている
    /// （Build Settings未登録などで遷移できないとき、画面を畳んだまま操作不能になるのを防ぐ）。
    /// </summary>
    public class SceneTransitionUseCase : ISceneTransitionUseCase
    {
        public bool IsTransitioning { get; private set; }

        public bool LoadMainMenu() => Load(SceneNames.MainMenu);

        public bool LoadBattle() => Load(SceneNames.Battle);

        private bool Load(string sceneName)
        {
            if (IsTransitioning)
            {
                return false;
            }

            IsTransitioning = true;

            try
            {
                var operation = SceneManager.LoadSceneAsync(sceneName);

                if (operation == null)
                {
                    // Build Settingsに未登録・無効の場合はnullが返る
                    Debug.LogError($"[SceneTransitionUseCase] シーンを読み込めませんでした: {sceneName}");
                    IsTransitioning = false;
                    return false;
                }

                operation.completed += _ => IsTransitioning = false;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneTransitionUseCase] シーン読み込みに失敗しました: {sceneName}\n{e}");
                IsTransitioning = false;
                return false;
            }
        }
    }
}

using App.Battle.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// セピア調の演出の実行時状態。
    /// 演出の発生源はプリセットを指定してかけ・外すだけで、フェードと重ね合わせはここで計算する。
    /// シェーダへの反映は <c>SepiaToneView</c> が担う。
    /// </summary>
    public interface ISepiaToneDataStore
    {
        /// <summary>グループごとの現在のセピアの強さ（0〜1）。x:背景 y:敵 z:プレイヤー w:UI</summary>
        ReadOnlyReactiveProperty<Vector4> Weights { get; }

        /// <summary>プリセットのセピア調をかける。フェードアウト中のものをかけ直した場合は、その強さから戻し始める</summary>
        void Apply(SepiaTonePreset preset);

        /// <summary>プリセットのセピア調を外す。かかっていなければ何もしない</summary>
        void Release(SepiaTonePreset preset);
    }
}

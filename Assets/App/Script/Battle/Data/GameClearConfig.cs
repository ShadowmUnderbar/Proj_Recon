using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// クリア演出の調整パラメータ。
    /// ボスを倒してから、見出しだけのクリア表示を経て結果画面（保存・リスタート・メインメニュー）を出すまでを外部化する。
    /// </summary>
    [CreateAssetMenu(fileName = "GameClearConfig", menuName = "Config/GameClearConfig")]
    public class GameClearConfig : ScriptableObject
    {
        [SerializeField, Tooltip("クリア表示の見出し")]
        private string _headline = "STAGE CLEAR";

        [SerializeField, Tooltip("クリア表示（見出しだけ）を出してから結果画面のボタンを出すまでの秒数。0なら即座に出す")]
        private float _headlineOnlyDuration = 3f;

        /// <summary>クリア表示の見出し（結果画面の1行目にも使う）</summary>
        public string Headline => _headline;

        /// <summary>クリア表示を出してから結果画面のボタンを出すまでの秒数</summary>
        public float HeadlineOnlyDuration => _headlineOnlyDuration;
    }
}

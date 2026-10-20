using System.Collections.Generic;
using App.Battle.Data;
using R3;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 敵に付く継続ダメージのデバフ「ウイルス」の実行時状態。
    /// ウイルスは威力・残り効果時間・最長効果時間を持ち、効果時間のあいだ1秒ごとに「威力÷最長効果時間」のダメージを与える。
    /// 時間の進行とダメージの適用は <c>VirusUseCase</c> が駆動する。
    /// </summary>
    public interface IVirusDataStore
    {
        /// <summary>
        /// 敵にウイルスを付与する。すでに付いている場合は威力を加算し、
        /// 新しい効果時間が残り効果時間より長いときだけ、最長効果時間と残り効果時間をその値にする。
        /// 威力・効果時間が0以下なら何もしない。
        /// </summary>
        void Apply(int enemyId, float power, float duration);

        /// <summary>付与するたびに、発生か重複かと付与前後の状態を流す（デバッグログ等の確認用）</summary>
        Observable<VirusApplyResult> OnApplied { get; }

        /// <summary>敵に付いているウイルスの状態を返す。付いていなければ false</summary>
        bool TryGetVirus(int enemyId, out VirusStatus status);

        /// <summary>
        /// 全ウイルスの時間を進め、この間に発生した毎秒ダメージを results に詰める（results は先に空にする）。
        /// 効果時間が切れたウイルスは取り除く。
        /// </summary>
        void Advance(float deltaTime, List<VirusDamage> results);

        /// <summary>敵のウイルスを取り除く（敵が消えたとき）</summary>
        void Remove(int enemyId);
    }
}

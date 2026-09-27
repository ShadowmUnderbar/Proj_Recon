using System;

namespace App.Common.Data
{
    /// <summary>
    /// チュートリアル1種類分の閲覧記録。
    /// JsonUtility は Dictionary を扱えないため、種類と回数の組をリストで保存する。
    /// </summary>
    [Serializable]
    public class TutorialViewRecord
    {
        public TutorialType Type;

        // このチュートリアルを最後まで閲覧した回数
        public int ViewCount;
    }
}

using System.Collections.Generic;
using App.Battle.Interface;
using App.Common.Data.MasterData;
using R3;
using UnityEngine;

namespace App.Battle.Views
{
    public class RunLevelView : MonoBehaviour, IRunLevelView
    {
        private readonly Subject<int> _onUpgradeChosen = new();
        public Observable<int> OnUpgradeChosen => _onUpgradeChosen;

        public void ShowUpgrades(IReadOnlyList<UpgradeMasterData> options)
        {
            gameObject.SetActive(true);
            // TODO: アップグレードカードUIを options に合わせて更新
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// UIボタンから呼び出す。index は 0〜choiceCount-1
        /// </summary>
        public void OnButtonClicked(int index)
        {
            _onUpgradeChosen.OnNext(index);
        }

        private void OnDestroy()
        {
            _onUpgradeChosen?.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace App.Battle.Views
{
    /// <summary>
    /// ボード上のカードを「手を重ねる（近接）」「レイで狙う（遠隔）」のどちらかで探す。
    /// 判定用のバッファをここに閉じ込め、毎フレームのアロケーションを避ける。
    /// どのカードが掴まれているかは知らないため、除外条件は呼び出し側から渡してもらう
    /// </summary>
    public class UpgradeCardFinder
    {
        /// <summary>
        /// レイ判定で受け取る最大ヒット数。RaycastNonAllocは距離順に詰めてくれないため、
        /// ショップの最大候補数（12枚）に加えてカード以外のコライダーぶんも余裕を持たせる。
        /// これを超えると手前のカードが取りこぼされるので、超えたときは警告を出す
        /// </summary>
        private const int MaxRayHits = 16;

        // List<T> のまま持つ。IReadOnlyList<T> 経由で foreach すると構造体の列挙子がボックス化され、
        // 毎フレームの検索でアロケーションが発生する
        private readonly List<UpgradeCardView> _cards;
        private readonly Predicate<UpgradeCardView> _isExcluded;

        private readonly RaycastHit[] _rayHits = new RaycastHit[MaxRayHits];

        /// <summary>UIのレイ判定の結果を受けるバッファ</summary>
        private readonly List<RaycastResult> _uiRaycastResults = new();

        /// <summary>UIのレイ判定に使うイベントデータ。位置だけ差し替えて使い回す</summary>
        private PointerEventData _uiPointerEventData;

        /// <param name="cards">検索対象のカード一覧（呼び出し側が増減させる同じリストを参照する）</param>
        /// <param name="isExcluded">検索対象から外すカードの条件（もう一方の手が持っているカード等）</param>
        public UpgradeCardFinder(List<UpgradeCardView> cards, Predicate<UpgradeCardView> isExcluded)
        {
            _cards = cards;
            _isExcluded = isExcluded;
        }

        /// <summary>
        /// 掴む対象のカード。手を重ねている（近接）カードを優先し、無ければレイの先のカードを返す
        /// </summary>
        public UpgradeCardView FindGrabTarget(Pose pointingPose, in UpgradeCardGrabSettings settings)
        {
            var nearest = FindNearestCard(pointingPose.position, settings);

            return nearest != null
                ? nearest
                : FindCardByRay(new Ray(pointingPose.position, pointingPose.rotation * Vector3.forward), settings);
        }

        /// <summary>手の位置に最も近いカード。近接で掴める距離の外なら null</summary>
        public UpgradeCardView FindNearestCard(Vector3 handPosition, in UpgradeCardGrabSettings settings)
        {
            UpgradeCardView nearest = null;
            var nearestDistance = settings.DirectGrabDistance;

            foreach (var card in _cards)
            {
                if (_isExcluded(card))
                {
                    continue;
                }

                // コライダーが未設定でも掴めるよう、その場合はカード中心までの距離で判定する
                var closestPoint = card.Collider != null
                    ? card.Collider.ClosestPoint(handPosition)
                    : card.transform.position;
                var distance = Vector3.Distance(handPosition, closestPoint);

                if (distance > nearestDistance)
                {
                    continue;
                }

                nearest = card;
                nearestDistance = distance;
            }

            return nearest;
        }

        /// <summary>レイの先にあるカードのうち最も手前のものを返す（手のレイ・マウスのレイで共通）</summary>
        public UpgradeCardView FindCardByRay(Ray ray, in UpgradeCardGrabSettings settings)
        {
            var hitCount = Physics.RaycastNonAlloc(
                ray,
                _rayHits,
                settings.RayGrabDistance,
                settings.CardLayerMask,
                QueryTriggerInteraction.Collide);

            if (hitCount >= MaxRayHits)
            {
                // レイヤーマスクが広すぎて（既定の ~0 のまま等）カード以外のコライダーで埋まっている。
                // 取りこぼしが起きるので、プレハブ側のマスクを絞るよう促す
                Debug.LogWarning(
                    $"[UpgradeCardFinder] レイ判定のヒット数が上限({MaxRayHits})に達しました。カードのレイヤーマスクを絞ってください");
            }

            UpgradeCardView nearest = null;
            var nearestDistance = float.MaxValue;

            for (var i = 0; i < hitCount; i++)
            {
                var card = _rayHits[i].collider.GetComponentInParent<UpgradeCardView>();

                // 除外対象のカードや、カード以外のコライダーは無視する
                if (card == null || _isExcluded(card) || _rayHits[i].distance >= nearestDistance)
                {
                    continue;
                }

                nearest = card;
                nearestDistance = _rayHits[i].distance;
            }

            return nearest;
        }

        /// <summary>
        /// ポインタが「押せるUI」の上にあるか。
        /// <see cref="EventSystem.IsPointerOverGameObject"/> ではショップの背景パネルにも反応してしまい、
        /// パネルに覆われた範囲のカードが一切クリックできなくなるため、Selectableの有無まで見て判定する
        /// </summary>
        public bool IsPointerOverInteractableUi(Vector2 screenPosition)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            _uiPointerEventData ??= new PointerEventData(EventSystem.current);
            _uiPointerEventData.position = screenPosition;
            _uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(_uiPointerEventData, _uiRaycastResults);

            foreach (var result in _uiRaycastResults)
            {
                var selectable = result.gameObject.GetComponentInParent<Selectable>();

                if (selectable != null && selectable.IsInteractable())
                {
                    return true;
                }
            }

            return false;
        }
    }
}

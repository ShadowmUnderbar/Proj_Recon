using App.Battle.Views;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    /// <summary>
    /// アップグレードカード（3Dモデル）のプレースホルダ一式を生成するエディタ拡張。
    ///
    /// 本番のモデルが用意できるまでの仮モデルとして、板ポリ＋TextMeshProのカードプレハブを作り、
    /// ShopViewプレハブへカードボードを差し込んで参照まで繋ぐ。
    /// 本番モデルに差し替えるときは、生成されたカードプレハブの見た目部分だけを置き換えればよい
    /// （<see cref="UpgradeCardView"/> のテキスト・枠・コライダーの参照に加え、
    /// 左右の持ち手（グリップアンカー）も忘れずに繋ぎ直すこと）
    /// </summary>
    public static class UpgradeCardPrefabCreator
    {
        private const string CardPrefabPath = "Assets/App/Prefub/UI/UpgradeCard.prefab";
        private const string ShopViewPrefabPath = "Assets/App/Prefub/UI/ShopView.prefab";
        private const string CardBoardObjectName = "UpgradeCardBoard";

        /// <summary>カードの掴み判定用レイヤー。他のコライダーがレイ判定に混ざらないよう専用にしている</summary>
        private const string CardLayerName = "UpgradeCard";

        // カードの寸法[m]。VRで手に持って読める大きさにしている
        private const float CardWidth = 0.12f;
        private const float CardHeight = 0.18f;
        private const float CardThickness = 0.006f;
        private const float FrameMargin = 0.008f;

        [MenuItem("Tools/Upgrade/アップグレードカードのプレースホルダを生成")]
        public static void Create()
        {
            var cardPrefab = CreateCardPrefab();

            if (cardPrefab == null)
            {
                return;
            }

            AttachCardBoardToShopView(cardPrefab);
            AssetDatabase.SaveAssets();

            Debug.Log($"アップグレードカードのプレースホルダを生成しました: {CardPrefabPath}");
        }

        /// <summary>カード1枚ぶんのプレハブを生成して保存する</summary>
        private static UpgradeCardView CreateCardPrefab()
        {
            var root = new GameObject("UpgradeCard");

            try
            {
                // 掴み判定用のコライダー。物理挙動はさせず、レイと近接判定にだけ使う
                var collider = root.AddComponent<BoxCollider>();
                collider.size = new Vector3(CardWidth, CardHeight, CardThickness * 2f);
                collider.isTrigger = true;

                var frame = CreateBox(
                    "Frame",
                    root.transform,
                    new Vector3(CardWidth + FrameMargin, CardHeight + FrameMargin, CardThickness),
                    Vector3.zero);

                // カードの表（プレイヤー側＝+Z）。枠より手前に薄く重ねて縁取りに見せる
                CreateBox(
                    "Face",
                    root.transform,
                    new Vector3(CardWidth, CardHeight, CardThickness * 0.5f),
                    new Vector3(0f, 0f, CardThickness * 0.4f));

                var nameText = CreateText(
                    "NameText",
                    root.transform,
                    new Vector2(CardWidth * 0.9f, CardHeight * 0.28f),
                    new Vector3(0f, CardHeight * 0.32f, CardThickness * 0.8f),
                    0.016f);

                var levelText = CreateText(
                    "LevelText",
                    root.transform,
                    new Vector2(CardWidth * 0.9f, CardHeight * 0.14f),
                    new Vector3(0f, CardHeight * 0.1f, CardThickness * 0.8f),
                    0.014f);

                var descriptionText = CreateText(
                    "DescriptionText",
                    root.transform,
                    new Vector2(CardWidth * 0.9f, CardHeight * 0.42f),
                    new Vector3(0f, -CardHeight * 0.16f, CardThickness * 0.8f),
                    0.012f);

                // 持ち手はカードの左右の端に置く。実際の持ち位置・角度はこのTransformを動かして調整する。
                // 表をプレイヤー側へ向ける（アンカーをY180にする）ため、ローカル-Xがプレイヤーから見た右端になる。
                // 手前側の端を持たせて、カードが視界の外側ではなく内側へ伸びるようにしている
                var rightGripAnchor = CreateGripAnchor(
                    "GripAnchorRight", root.transform, new Vector3(-CardWidth * 0.5f, 0f, 0f));
                var leftGripAnchor = CreateGripAnchor(
                    "GripAnchorLeft", root.transform, new Vector3(CardWidth * 0.5f, 0f, 0f));

                SetLayerRecursively(root, GetCardLayer());

                var cardView = root.AddComponent<UpgradeCardView>();
                AssignCardViewReferences(cardView, collider, frame, nameText, levelText, descriptionText);
                AssignGripAnchors(cardView, rightGripAnchor, leftGripAnchor);

                var saved = PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
                return saved != null ? saved.GetComponent<UpgradeCardView>() : null;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>SerializeFieldはprivateのため、SerializedObject経由で参照を差し込む</summary>
        private static void AssignCardViewReferences(
            UpgradeCardView cardView,
            Collider collider,
            GameObject frame,
            TextMeshPro nameText,
            TextMeshPro levelText,
            TextMeshPro descriptionText)
        {
            var serialized = new SerializedObject(cardView);
            serialized.FindProperty("_collider").objectReferenceValue = collider;
            serialized.FindProperty("_frameRenderer").objectReferenceValue = frame.GetComponent<Renderer>();
            serialized.FindProperty("_nameText").objectReferenceValue = nameText;
            serialized.FindProperty("_levelText").objectReferenceValue = levelText;
            serialized.FindProperty("_descriptionText").objectReferenceValue = descriptionText;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignGripAnchors(UpgradeCardView cardView, Transform right, Transform left)
        {
            var serialized = new SerializedObject(cardView);
            serialized.FindProperty("_rightHandGripAnchor").objectReferenceValue = right;
            serialized.FindProperty("_leftHandGripAnchor").objectReferenceValue = left;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 持ち手の基準。カードの表（+Z）がプレイヤー側を向くよう、手の前方とカードの裏を合わせた向きにしておく
        /// </summary>
        private static Transform CreateGripAnchor(string name, Transform parent, Vector3 localPosition)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = localPosition;
            anchor.localRotation = Quaternion.Euler(0f, 180f, 0f);

            return anchor;
        }

        /// <summary>ShopViewプレハブにカードボードを追加し、ShopViewから参照できるようにする</summary>
        private static void AttachCardBoardToShopView(UpgradeCardView cardPrefab)
        {
            var shopViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopViewPrefabPath);

            if (shopViewPrefab == null)
            {
                Debug.LogWarning($"ShopViewプレハブが見つかりませんでした: {ShopViewPrefabPath}");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(ShopViewPrefabPath);

            try
            {
                var shopView = root.GetComponent<ShopView>();

                if (shopView == null)
                {
                    Debug.LogWarning("ShopViewコンポーネントが見つからないため、カードボードの接続をスキップしました");
                    return;
                }

                var boardTransform = root.transform.Find(CardBoardObjectName);

                // 何度実行しても増えないよう、既存のボードがあれば作り直さずに使う
                if (boardTransform == null)
                {
                    var boardObject = new GameObject(CardBoardObjectName);
                    boardObject.transform.SetParent(root.transform, false);
                    boardTransform = boardObject.transform;
                }

                var board = boardTransform.GetComponent<UpgradeCardBoardView>()
                            ?? boardTransform.gameObject.AddComponent<UpgradeCardBoardView>();

                var serializedBoard = new SerializedObject(board);
                serializedBoard.FindProperty("_cardPrefab").objectReferenceValue = cardPrefab;
                serializedBoard.FindProperty("_cardLayerMask").intValue = 1 << GetCardLayer();
                serializedBoard.ApplyModifiedPropertiesWithoutUndo();

                var serializedShopView = new SerializedObject(shopView);
                serializedShopView.FindProperty("_upgradeCardBoardView").objectReferenceValue = board;
                serializedShopView.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, ShopViewPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>カード用レイヤー。未設定の場合はDefaultにフォールバックする</summary>
        private static int GetCardLayer()
        {
            var layer = LayerMask.NameToLayer(CardLayerName);

            if (layer >= 0)
            {
                return layer;
            }

            Debug.LogWarning($"レイヤー「{CardLayerName}」が見つからないため、カードをDefaultレイヤーに置きます");
            return 0;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;

            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>見た目用の直方体。当たり判定はカード側のコライダーに任せるため取り除く</summary>
        private static GameObject CreateBox(string name, Transform parent, Vector3 size, Vector3 localPosition)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = size;

            Object.DestroyImmediate(box.GetComponent<BoxCollider>());

            return box;
        }

        private static TextMeshPro CreateText(
            string name,
            Transform parent,
            Vector2 size,
            Vector3 localPosition,
            float fontSize)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<TextMeshPro>();
            text.rectTransform.sizeDelta = size;
            text.rectTransform.localPosition = localPosition;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.black;
            text.text = string.Empty;

            return text;
        }
    }
}

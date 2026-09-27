using App.Common.Data;
using App.MainMenu.Views;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace App.Editor
{
    /// <summary>
    /// オプションパネル（利き手・VRの移動設定・チュートリアル再表示）のプレースホルダUIを組み立てるエディタ拡張。
    /// <see cref="MainMenuRoomBuilder"/>から呼ばれ、見た目は仮のまま操作だけ通るようにしている。
    /// 設定項目は縦スクロールのリストに並べるため、項目を増やすときは<see cref="CreateRows"/>へ1行足すだけでよい。
    /// 本番のUIに差し替えるときは、生成された階層の見た目を置き換えたうえで、
    /// <see cref="OptionPanelView"/>のボタン・スライダー参照を繋ぎ直すこと。
    /// </summary>
    public static class OptionPanelBuilder
    {
        private const string PrefabPath = "Assets/App/Prefub/UI/OptionPanelView.prefab";

        // パネルの大きさ[px]。WorldSpace Canvasのスケール（1px＝1mm）を掛けた値がメートルになる
        private const float PanelWidth = 1400f;
        private const float PanelHeight = 900f;

        private const float RowLabelWidth = 420f;
        private const float RowHeight = 90f;
        private const float RowSpacing = 50f;
        private const float ButtonWidth = 280f;
        private const float SliderWidth = 520f;
        private const float ValueLabelWidth = 200f;

        private const float TitleFontSize = 64f;
        private const float RowFontSize = 40f;

        private const float TitlePositionY = 340f;

        // スクロール領域のパネル端からの余白[px]。右は手で足した戻るボタンと重ならないよう広めに取る
        private const float ScrollMarginLeft = 50f;
        private const float ScrollMarginRight = 140f;
        private const float ScrollMarginTop = 160f;
        private const float ScrollMarginBottom = 40f;
        private const float ScrollbarWidth = 24f;
        private const float ScrollSensitivity = 60f;

        // 各列の横位置[px]。行の中心が原点。
        // 行はスクロール領域（パネル幅 − 左右余白 − スクロールバー）の中に置かれるため、
        // 両端の要素がその半幅（約590）を越えないようにする。越えるとRectMask2Dで端が切れる
        private const float LabelPositionX = -380f;
        private const float FirstColumnPositionX = 60f;
        private const float SecondColumnPositionX = 380f;
        private const float SliderPositionX = 130f;
        private const float ValueLabelPositionX = 470f;

        /// <summary>手で足した戻るボタンの名前。作り直しでも消さない</summary>
        private const string BackButtonName = "BackButton";

        /// <summary>日本語を表示できるフォント。TMPの既定フォントには日本語のグリフが無い</summary>
        private const string FontAssetPath = "Assets/App/Font/nicokaku_v2-5 SDF.asset";

        private static readonly Color PanelColor = new(0.05f, 0.07f, 0.1f, 0.9f);
        private static readonly Color ButtonColor = new(0.2f, 0.28f, 0.38f, 1f);
        private static readonly Color SliderBackgroundColor = new(0.15f, 0.18f, 0.22f, 1f);
        private static readonly Color SliderFillColor = new(0.35f, 0.6f, 0.9f, 1f);
        private static readonly Color ScrollbarHandleColor = new(0.5f, 0.55f, 0.62f, 1f);

        /// <summary>行を組み立てた結果。<see cref="OptionPanelView"/>へ繋ぐ参照をまとめて返す</summary>
        private sealed class RowReferences
        {
            public Button DominantHandLeft;
            public Button DominantHandRight;
            public Button SmoothLocomotion;
            public Button TeleportLocomotion;
            public Slider MoveSpeedSlider;
            public TMP_Text MoveSpeedLabel;
            public Slider SnapTurnSlider;
            public TMP_Text SnapTurnLabel;
            public Button TutorialReplayOn;
            public Button TutorialReplayOff;
        }

        /// <summary>オプションパネル一式を生成し、参照を繋いだルートを返す</summary>
        public static GameObject CreateOptionPanel()
        {
            var root = new GameObject("OptionPanelView", typeof(RectTransform), typeof(Canvas));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var view = root.AddComponent<OptionPanelView>();
            BuildContents(rootRect, view);

            return root;
        }

        /// <summary>
        /// 既存のオプションパネルプレハブの中身を、縦スクロールのリストで作り直す。
        /// プレハブは一度作ると作り直さない方針（<see cref="MainMenuRoomBuilder"/>参照）のため、
        /// 項目を足したときはプレハブを消さずにこのメニューで中身だけ組み直す。
        /// ルートのコンポーネント（Canvas・近接判定など）と手で足した戻るボタンはそのまま残す
        /// </summary>
        [MenuItem("Tools/MainMenu/オプションパネルの項目を作り直す")]
        public static void RebuildPrefabContents()
        {
            using var scope = new PrefabUtility.EditPrefabContentsScope(PrefabPath);
            var root = scope.prefabContentsRoot;
            var view = root.GetComponent<OptionPanelView>();
            if (view == null)
            {
                Debug.LogError($"{PrefabPath} に OptionPanelView がありません");
                return;
            }

            var rootRect = root.GetComponent<RectTransform>();

            // 戻るボタン以外の子（背景・タイトル・旧レイアウトの行）を消してから組み直す
            for (var i = rootRect.childCount - 1; i >= 0; i--)
            {
                var child = rootRect.GetChild(i);
                if (child.name != BackButtonName)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            BuildContents(rootRect, view);

            // 戻るボタンは最後に描いて、スクロール領域より手前に出す
            var backButton = rootRect.Find(BackButtonName);
            if (backButton != null)
            {
                backButton.SetAsLastSibling();
            }

            Debug.Log($"{PrefabPath} の項目を作り直しました");
        }

        /// <summary>背景・タイトル・スクロールする設定項目を作り、Viewへ参照を繋ぐ</summary>
        private static void BuildContents(RectTransform rootRect, OptionPanelView view)
        {
            CreateBackground(rootRect);
            CreateText("Title", rootRect, new Vector2(PanelWidth, 100f), new Vector2(0f, TitlePositionY),
                "OPTION", TitleFontSize, TextAlignmentOptions.Center);

            var content = CreateScrollView(rootRect);
            var rows = CreateRows(content);

            var serialized = new SerializedObject(view);
            serialized.FindProperty("_dominantHandLeftButton").objectReferenceValue = rows.DominantHandLeft;
            serialized.FindProperty("_dominantHandRightButton").objectReferenceValue = rows.DominantHandRight;
            serialized.FindProperty("_smoothLocomotionButton").objectReferenceValue = rows.SmoothLocomotion;
            serialized.FindProperty("_teleportLocomotionButton").objectReferenceValue = rows.TeleportLocomotion;
            serialized.FindProperty("_moveSpeedSlider").objectReferenceValue = rows.MoveSpeedSlider;
            serialized.FindProperty("_moveSpeedLabel").objectReferenceValue = rows.MoveSpeedLabel;
            serialized.FindProperty("_snapTurnAngleSlider").objectReferenceValue = rows.SnapTurnSlider;
            serialized.FindProperty("_snapTurnAngleLabel").objectReferenceValue = rows.SnapTurnLabel;
            serialized.FindProperty("_tutorialReplayOnButton").objectReferenceValue = rows.TutorialReplayOn;
            serialized.FindProperty("_tutorialReplayOffButton").objectReferenceValue = rows.TutorialReplayOff;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>設定項目の行を上から順に作る。項目を増やすときはここへ足す</summary>
        private static RowReferences CreateRows(RectTransform content)
        {
            var rows = new RowReferences();

            rows.DominantHandLeft = CreateButtonRow(content, "DominantHand", "利き手",
                "左", "右", out rows.DominantHandRight);

            rows.SmoothLocomotion = CreateButtonRow(content, "Locomotion", "移動方式",
                "スムーズ", "テレポート", out rows.TeleportLocomotion);

            rows.MoveSpeedSlider = CreateSliderRow(content, "MoveSpeed", "移動速度", out rows.MoveSpeedLabel);
            rows.SnapTurnSlider = CreateSliderRow(content, "SnapTurnAngle", "ターン角度", out rows.SnapTurnLabel);

            rows.TutorialReplayOn = CreateButtonRow(content, "TutorialReplay", "チュートリアル再表示",
                "オン", "オフ", out rows.TutorialReplayOff);

            // スライダーの範囲はプレハブにも保存しておく。実行時にも組み立て直すが、
            // 既定の0〜1のまま保存されていると、値の反映が先に走ったときにクランプされてしまう
            rows.MoveSpeedSlider.minValue = PlayerSettingRange.MinMoveSpeed;
            rows.MoveSpeedSlider.maxValue = PlayerSettingRange.MaxMoveSpeed;
            rows.MoveSpeedSlider.wholeNumbers = false;
            rows.MoveSpeedSlider.value = PlayerSettingRange.DefaultMoveSpeed;

            rows.SnapTurnSlider.minValue = PlayerSettingRange.MinSnapTurnAngle;
            rows.SnapTurnSlider.maxValue = PlayerSettingRange.MaxSnapTurnAngle;
            rows.SnapTurnSlider.wholeNumbers = true;
            rows.SnapTurnSlider.value = PlayerSettingRange.DefaultSnapTurnAngle;

            return rows;
        }

        private static void CreateBackground(RectTransform parent)
        {
            var background = CreateStretchedRect("Background", parent);

            var image = background.gameObject.AddComponent<Image>();
            image.color = PanelColor;

            // 背景がレイを吸うとボタンの手前で当たり判定が止まるため、素通しにする
            image.raycastTarget = false;
        }

        /// <summary>
        /// 縦スクロールの領域を作り、行を並べるContentを返す。
        /// 行はVerticalLayoutGroupで上から詰めるので、行数が増えても位置調整は要らない
        /// </summary>
        private static RectTransform CreateScrollView(RectTransform parent)
        {
            var scrollRect = CreateStretchedRect("ScrollView", parent);
            scrollRect.offsetMin = new Vector2(ScrollMarginLeft, ScrollMarginBottom);
            scrollRect.offsetMax = new Vector2(-ScrollMarginRight, -ScrollMarginTop);

            var viewport = CreateStretchedRect("Viewport", scrollRect);
            viewport.offsetMax = new Vector2(-ScrollbarWidth, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();

            // 何も無い所をつまんでもスクロールできるよう、透明な当たり判定を敷く。
            // 行のボタンはこの上に描かれるため、押下はボタンが受け取る
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = Color.clear;
            viewportImage.raycastTarget = true;

            var content = CreateStretchedRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = RowSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollbar = CreateScrollbar(scrollRect);

            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = ScrollSensitivity;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            return content;
        }

        private static Scrollbar CreateScrollbar(RectTransform parent)
        {
            var rect = CreateStretchedRect("Scrollbar", parent);
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(-ScrollbarWidth, 0f);
            rect.offsetMax = Vector2.zero;

            var background = rect.gameObject.AddComponent<Image>();
            background.color = SliderBackgroundColor;

            var slidingArea = CreateStretchedRect("Sliding Area", rect);
            var handle = CreateStretchedRect("Handle", slidingArea);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = ScrollbarHandleColor;

            var scrollbar = rect.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            return scrollbar;
        }

        /// <summary>行の枠を作る。横幅はレイアウトが決め、高さだけ固定する</summary>
        private static RectTransform CreateRowRect(RectTransform content, string name)
        {
            var row = CreateRect($"{name}Row", content, new Vector2(0f, RowHeight), Vector2.zero);

            var element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = RowHeight;
            element.minHeight = RowHeight;

            return row;
        }

        /// <summary>「ラベル＋選択ボタン2つ」の1行を作る</summary>
        private static Button CreateButtonRow(
            RectTransform content,
            string name,
            string label,
            string firstLabel,
            string secondLabel,
            out Button secondButton)
        {
            var row = CreateRowRect(content, name);

            CreateText($"{name}Label", row, new Vector2(RowLabelWidth, RowHeight),
                new Vector2(LabelPositionX, 0f), label, RowFontSize, TextAlignmentOptions.Left);

            var firstButton = CreateButton($"{name}First", row,
                new Vector2(ButtonWidth, RowHeight), new Vector2(FirstColumnPositionX, 0f), firstLabel);

            secondButton = CreateButton($"{name}Second", row,
                new Vector2(ButtonWidth, RowHeight), new Vector2(SecondColumnPositionX, 0f), secondLabel);

            return firstButton;
        }

        /// <summary>「ラベル＋スライダー＋数値表示」の1行を作る</summary>
        private static Slider CreateSliderRow(
            RectTransform content,
            string name,
            string label,
            out TMP_Text valueLabel)
        {
            var row = CreateRowRect(content, name);

            CreateText($"{name}Label", row, new Vector2(RowLabelWidth, RowHeight),
                new Vector2(LabelPositionX, 0f), label, RowFontSize, TextAlignmentOptions.Left);

            var slider = CreateSlider($"{name}Slider", row,
                new Vector2(SliderWidth, RowHeight), new Vector2(SliderPositionX, 0f));

            valueLabel = CreateText($"{name}Value", row, new Vector2(ValueLabelWidth, RowHeight),
                new Vector2(ValueLabelPositionX, 0f), "-", RowFontSize, TextAlignmentOptions.Right);

            return slider;
        }

        private static RectTransform CreateRect(string name, RectTransform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            return rect;
        }

        private static TMP_Text CreateText(
            string name,
            RectTransform parent,
            Vector2 size,
            Vector2 position,
            string content,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            var rect = CreateRect(name, parent, size, position);

            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (font != null)
            {
                text.font = font;
            }
            else
            {
                Debug.LogWarning($"{FontAssetPath} が見つかりませんでした。日本語が表示されない可能性があります");
            }

            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.raycastTarget = false;

            return text;
        }

        private static Button CreateButton(
            string name,
            RectTransform parent,
            Vector2 size,
            Vector2 position,
            string label)
        {
            var rect = CreateRect(name, parent, size, position);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText($"{name}Label", rect, size, Vector2.zero, label, RowFontSize,
                TextAlignmentOptions.Center);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            return button;
        }

        private static Slider CreateSlider(string name, RectTransform parent, Vector2 size, Vector2 position)
        {
            var rect = CreateRect(name, parent, size, position);
            var slider = rect.gameObject.AddComponent<Slider>();

            var background = CreateStretchedRect("Background", rect);
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = SliderBackgroundColor;

            var fillArea = CreateStretchedRect("Fill Area", rect);
            var fill = CreateStretchedRect("Fill", fillArea);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = SliderFillColor;

            var handleArea = CreateStretchedRect("Handle Slide Area", rect);
            var handle = CreateRect("Handle", handleArea, new Vector2(RowHeight, RowHeight), Vector2.zero);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Color.white;

            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;

            return slider;
        }

        private static RectTransform CreateStretchedRect(string name, RectTransform parent)
        {
            var rect = CreateRect(name, parent, Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return rect;
        }
    }
}

using App.Common.Views;
using App.MainMenu.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace App.Editor
{
    /// <summary>
    /// メインメニューの「歩き回れる部屋」一式を生成するエディタ拡張。
    ///
    /// 部屋のアートが入るまでのプレースホルダとして、床・壁・天井をプリミティブで組み、
    /// XRリグに移動用のCharacterControllerを付け、壁際にSTART／OPTIONパネルを固定設置する。
    /// 本番のアートに差し替えるときは、生成された MenuRoom 以下の見た目だけを置き換えればよい
    /// （床のコライダーはテレポートの着地判定に使うため残すこと）。
    ///
    /// 何度実行しても同じ結果になるよう、生成物は毎回作り直す。
    /// </summary>
    public static class MainMenuRoomBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string MainMenuViewPrefabPath = "Assets/App/Prefub/UI/MainMenuView.prefab";
        private const string OptionPanelPrefabPath = "Assets/App/Prefub/UI/OptionPanelView.prefab";

        private const string RoomRootName = "MenuRoom";
        private const string RigRootName = "MenuPlayerRig";
        private const string StartPanelName = "StartPanel";
        private const string OptionPanelName = "OptionPanel";

        // 部屋の寸法[m]。歩き回る楽しさと、パネルまでの移動が面倒にならない広さの折衷
        private const float RoomWidth = 10f;
        private const float RoomDepth = 10f;
        private const float RoomHeight = 3f;
        private const float WallThickness = 0.2f;

        // XRリグのカプセル。人の体格に寄せた値
        private const float CapsuleRadius = 0.3f;
        private const float CapsuleHeight = 1.7f;
        private const float CapsuleSkinWidth = 0.02f;

        /// <summary>パネルを壁からどれだけ内側へ出すか[m]。壁にめり込むとレイが通らなくなる</summary>
        private const float PanelWallOffset = 0.05f;

        /// <summary>パネル中心の高さ[m]。立ったときの目線よりやや下</summary>
        private const float PanelHeight = 1.4f;

        /// <summary>WorldSpace Canvasのスケール。1px＝1mmとして扱う</summary>
        private const float CanvasScale = 0.001f;

        /// <summary>テレポート着地マーカーの直径[m]</summary>
        private const float TeleportMarkerDiameter = 0.5f;

        /// <summary>設置位置の検証で許す誤差[m]</summary>
        private const float PositionTolerance = 0.001f;

        /// <summary>テレポート照準のラインの太さ[m]</summary>
        private const float TeleportLineWidth = 0.01f;

        /// <summary>非VRで確認するときのカメラの高さ[m]。立ったときの目線に合わせる</summary>
        private const float NonVrEyeHeight = 1.6f;

        /// <summary>STARTパネルの設置位置。北の壁（部屋の中心から見て正面）</summary>
        private static Vector3 StartPanelPosition =>
            new(0f, PanelHeight, RoomDepth * 0.5f - WallThickness * 0.5f - PanelWallOffset);

        /// <summary>OPTIONパネルの設置位置。東の壁（STARTとは別の壁にして歩く導線を作る）</summary>
        private static Vector3 OptionPanelPosition =>
            new(RoomWidth * 0.5f - WallThickness * 0.5f - PanelWallOffset, PanelHeight, 0f);

        [MenuItem("Tools/MainMenu/メインメニューの部屋を生成")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 配信用カメラなどが増えても頭の基準がぶれないよう、MainCameraタグで取る
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError($"{ScenePath} にMainCameraタグのカメラが見つかりませんでした。Camera.prefabを配置してから実行してください");
                return;
            }

            BuildRoom();
            BuildPlayerRig(camera);
            BuildStartPanel(camera);
            BuildOptionPanel(camera);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            VerifyPanelPlacement();

            Debug.Log("メインメニューの部屋を生成しました");
        }

        /// <summary>
        /// パネルが原点に残っていないかを確かめる。
        /// CanvasのRectTransformは書き込み方を誤ると位置が保存されず、
        /// 見た目には「パネルがスポーン地点に埋まっている」形で現れるため、生成時に気付けるようにしている。
        /// </summary>
        private static void VerifyPanelPlacement()
        {
            VerifySingleInstance<MainMenuView>();
            VerifySingleInstance<OptionPanelView>();

            VerifyPosition(StartPanelName, StartPanelPosition);
            VerifyPosition(OptionPanelName, OptionPanelPosition);
        }

        /// <summary>パネルが狙った位置に置かれているかを確かめる</summary>
        private static void VerifyPosition(string panelName, Vector3 expected)
        {
            var panel = GameObject.Find(panelName);
            if (panel == null)
            {
                Debug.LogError($"{panelName} が生成されていません");
                return;
            }

            var actual = panel.transform.position;
            if (Vector3.Distance(actual, expected) > PositionTolerance)
            {
                Debug.LogError($"{panelName} の位置が {actual} で、狙った {expected} と違います。設置処理を確認してください");
            }
        }

        /// <summary>同じパネルが二重に残っていないかを確かめる</summary>
        private static void VerifySingleInstance<T>() where T : Component
        {
            var found = Object.FindObjectsOfType<T>(true);
            if (found.Length != 1)
            {
                Debug.LogError($"{typeof(T).Name} がシーンに{found.Length}個あります。1個になるよう整理してください");
            }
        }

        #region 部屋

        /// <summary>床・壁・天井のブロックアウトを作る</summary>
        private static void BuildRoom()
        {
            var existing = GameObject.Find(RoomRootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var root = new GameObject(RoomRootName);

            // 床は上面がy=0に来るように置く。テレポートの着地面になる
            CreateBox("Floor", root.transform,
                new Vector3(RoomWidth, WallThickness, RoomDepth),
                new Vector3(0f, -WallThickness * 0.5f, 0f));

            CreateBox("Ceiling", root.transform,
                new Vector3(RoomWidth, WallThickness, RoomDepth),
                new Vector3(0f, RoomHeight + WallThickness * 0.5f, 0f));

            var wallY = RoomHeight * 0.5f;
            var halfWidth = RoomWidth * 0.5f;
            var halfDepth = RoomDepth * 0.5f;

            CreateBox("WallNorth", root.transform,
                new Vector3(RoomWidth, RoomHeight, WallThickness),
                new Vector3(0f, wallY, halfDepth));

            CreateBox("WallSouth", root.transform,
                new Vector3(RoomWidth, RoomHeight, WallThickness),
                new Vector3(0f, wallY, -halfDepth));

            CreateBox("WallEast", root.transform,
                new Vector3(WallThickness, RoomHeight, RoomDepth),
                new Vector3(halfWidth, wallY, 0f));

            CreateBox("WallWest", root.transform,
                new Vector3(WallThickness, RoomHeight, RoomDepth),
                new Vector3(-halfWidth, wallY, 0f));
        }

        private static void CreateBox(string name, Transform parent, Vector3 size, Vector3 position)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
        }

        #endregion

        #region XRリグ

        /// <summary>カメラ一式をリグの下へ入れ、移動できるようにする</summary>
        private static void BuildPlayerRig(Camera camera)
        {
            var cameraRoot = camera.transform.root.gameObject;

            // 既にリグがある場合は作り直す。カメラ一式だけ外へ出してから古いリグを消す
            if (cameraRoot.name == RigRootName)
            {
                var cameraBranch = camera.transform;
                while (cameraBranch.parent != null && cameraBranch.parent != cameraRoot.transform)
                {
                    cameraBranch = cameraBranch.parent;
                }

                cameraBranch.SetParent(null, true);
                Object.DestroyImmediate(cameraRoot);
                cameraRoot = cameraBranch.gameObject;
            }

            var rig = new GameObject(RigRootName);
            rig.transform.position = Vector3.zero;

            // カメラ一式をリグの子にする。位置関係は保ったまま入れ替える
            cameraRoot.transform.SetParent(rig.transform, true);

            // 非VRではHMDの高さが入らずカメラが床に埋まってしまうため、目線の高さへ上げておく。
            // VRではTrackedPoseDriverが毎フレーム上書きするので影響しない
            var headLocalPosition = camera.transform.localPosition;
            camera.transform.localPosition = new Vector3(headLocalPosition.x, NonVrEyeHeight, headLocalPosition.z);

            var characterController = rig.AddComponent<CharacterController>();
            characterController.radius = CapsuleRadius;
            characterController.height = CapsuleHeight;
            characterController.skinWidth = CapsuleSkinWidth;
            characterController.center = new Vector3(0f, CapsuleHeight * 0.5f, 0f);

            // メインメニューはUIしか無いので、ハンドレイは常に出しておく
            rig.AddComponent<VrUiRayAlwaysOnView>();

            var teleportLine = CreateTeleportLine(rig.transform);
            var teleportMarker = CreateTeleportMarker(rig.transform);

            var locomotion = rig.AddComponent<MenuLocomotionView>();
            var serialized = new SerializedObject(locomotion);
            serialized.FindProperty("_head").objectReferenceValue = camera.transform;
            serialized.FindProperty("_teleportRayOrigin").objectReferenceValue = FindRayOrigin(rig.transform);
            serialized.FindProperty("_teleportLine").objectReferenceValue = teleportLine;
            serialized.FindProperty("_teleportMarker").objectReferenceValue = teleportMarker;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>テレポートの照準に使うコントローラのレイ原点を探す。見つからなければHMDから飛ばす</summary>
        private static Transform FindRayOrigin(Transform rig)
        {
            foreach (var child in rig.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "RayOrigin")
                {
                    return child;
                }
            }

            return null;
        }

        private static LineRenderer CreateTeleportLine(Transform parent)
        {
            var go = new GameObject("TeleportLine");
            go.transform.SetParent(parent, false);

            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = TeleportLineWidth;

            // マテリアル未設定だとマゼンタで描かれるため、組み込みのライン用マテリアルを当てておく
            line.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
            line.enabled = false;

            return line;
        }

        private static GameObject CreateTeleportMarker(Transform parent)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "TeleportMarker";
            marker.transform.SetParent(parent, false);

            // 床に置く目印なので薄い円盤にする。当たり判定はレイの邪魔になるため外す
            marker.transform.localScale = new Vector3(TeleportMarkerDiameter, 0.01f, TeleportMarkerDiameter);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.SetActive(false);

            return marker;
        }

        #endregion

        #region パネル

        /// <summary>既存のMainMenuView（STARTボタン）を壁際の固定パネルとして置き直す</summary>
        private static void BuildStartPanel(Camera camera)
        {
            DestroyExisting<MainMenuView>();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuViewPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"{MainMenuViewPrefabPath} が見つかりませんでした");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = StartPanelName;

            // 部屋に固定設置するため、視線へ追従させる仕組みは外す
            var follow = instance.GetComponentInChildren<VrUiFollowCanvasView>(true);
            if (follow != null)
            {
                Object.DestroyImmediate(follow);
            }

            var canvas = instance.GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                Debug.LogError($"{MainMenuViewPrefabPath} にCanvasが見つかりませんでした");
                return;
            }

            SetupFixedPanel(canvas, camera);

            // 設置はプレハブのルートで行う。キャンバスはプレハブ内の位置関係のままにしておく
            canvas.transform.localPosition = Vector3.zero;
            canvas.transform.localRotation = Quaternion.identity;

            PlacePanel(instance, StartPanelPosition, Quaternion.Euler(0f, 180f, 0f));
            RemoveStrays<MainMenuView>(instance);
        }

        /// <summary>オプションパネルのプレハブを作り、壁際へ設置する</summary>
        private static void BuildOptionPanel(Camera camera)
        {
            DestroyExisting<OptionPanelView>();

            // 一度作ったプレハブは作り直さない。見た目を手で差し替えた後に
            // このメニューを再実行しても、その作業が消えないようにする
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OptionPanelPrefabPath);

            if (prefab == null)
            {
                // 組み立て用の一時オブジェクトからプレハブを作り、シーンには残さない。
                // 残すとプレハブから生成したものと二重になり、
                // VContainerが取り残しの方を解決してUIの操作が効かなくなる
                var source = OptionPanelBuilder.CreateOptionPanel();
                SetupFixedPanel(source.GetComponent<Canvas>(), camera);
                prefab = PrefabUtility.SaveAsPrefabAsset(source, OptionPanelPrefabPath);
                Object.DestroyImmediate(source);

                if (prefab == null)
                {
                    Debug.LogError($"{OptionPanelPrefabPath} の保存に失敗しました");
                    return;
                }
            }

            var panel = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            SetupFixedPanel(panel.GetComponent<Canvas>(), camera);
            panel.name = OptionPanelName;

            PlacePanel(panel, OptionPanelPosition, Quaternion.Euler(0f, -90f, 0f));
            RemoveStrays<OptionPanelView>(panel);
        }

        /// <summary>
        /// パネルを指定の位置・向きへ置く。
        /// ルートがRectTransformの場合、位置はanchoredPositionとして持たれるため、
        /// transform.positionへ代入してもシーンに保存されず原点に戻ってしまう。
        /// パネルはシーン直下に置くので、anchoredPosition3Dはワールド座標と一致する。
        /// </summary>
        private static void PlacePanel(GameObject panel, Vector3 position, Quaternion rotation)
        {
            if (panel.transform is RectTransform rect)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition3D = position;
                rect.localRotation = rotation;
                return;
            }

            panel.transform.position = position;
            panel.transform.rotation = rotation;
        }

        /// <summary>
        /// 設置したパネル以外を消す。プレハブの生成・保存の過程で組み立て用のオブジェクトが
        /// シーンに残ることがあり、残るとVContainerがそちらを解決してUIの操作が効かなくなる。
        /// </summary>
        private static void RemoveStrays<T>(GameObject keep) where T : Component
        {
            foreach (var found in Object.FindObjectsOfType<T>(true))
            {
                if (found.gameObject != keep)
                {
                    Object.DestroyImmediate(found.gameObject);
                }
            }
        }

        /// <summary>
        /// 生成済みのパネルを消す。名前ではなくコンポーネントで探すのは、
        /// プレハブ名のまま取り残されたものまで確実に片付けるため
        /// （取り残しがあると、VContainerがそちらを解決してUIの操作が効かなくなる）。
        /// </summary>
        private static void DestroyExisting<T>() where T : Component
        {
            foreach (var existing in Object.FindObjectsOfType<T>(true))
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        /// <summary>WorldSpaceのパネルとして、VRのハンドレイ・非VRのマウスの双方で押せるようにする</summary>
        private static void SetupFixedPanel(Canvas canvas, Camera camera)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;

            canvas.GetComponent<RectTransform>().localScale = Vector3.one * CanvasScale;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }

            if (canvas.GetComponent<CanvasGroup>() == null)
            {
                canvas.gameObject.AddComponent<CanvasGroup>();
            }

            if (canvas.GetComponent<MenuPanelProximityView>() == null)
            {
                canvas.gameObject.AddComponent<MenuPanelProximityView>();
            }
        }

        #endregion
    }
}

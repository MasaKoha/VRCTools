using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Masakoha.VRCTools.BackupBlendShapes.Editor
{
    public sealed class BackupBlendShapesWindow : EditorWindow
    {
        private const int Padding = 8;
        private const int Margin = 4;
        private const float IconSize = 32;
        [SerializeField] private SkinnedMeshData _data = new();
        private SerializedObject _serializedObject;
        private SerializedProperty _rendererProperty;
        private readonly List<BlendShape> _blendShapes = new();

        [MenuItem("Tools/Masakoha/バックアップブレンドシェイプ")]
        private static void Open()
        {
            GetWindow<BackupBlendShapesWindow>("バックアップブレンドシェイプ");
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _rendererProperty = _serializedObject.FindProperty("_data.renderer");
        }

        private void OnGUI()
        {
            _serializedObject.Update();

            EditorGUILayout.LabelField("バックアップブレンドシェイプ　メニュー", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "保存や更新をしたいオブジェクトを入れてください",
                MessageType.Info
            );

            var redTextStyle =
                new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = Color.red },
                    wordWrap = true
                };
            var yellowTextStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.yellow },
                wordWrap = true
            };
            var boxStyle1 = new GUIStyle(GUI.skin.box);
            var errorIconTexture1 = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
            boxStyle1.padding = new RectOffset(Padding, Padding, Padding, Padding);
            boxStyle1.margin = new RectOffset(Margin, Margin, Margin, Margin);
            GUILayout.BeginHorizontal(boxStyle1);
            GUILayout.Label(errorIconTexture1, GUILayout.Width(IconSize), GUILayout.Height(IconSize)); // アイコンを表示
            GUILayout.Label("※保存や保存呼び出しを押したら、「対象のブレンドシェイプのオブジェクト」が空っぽになる仕様にしています。\n" +
                            "データを保存するオブジェクトとデータを保存呼び出しするオブジェクトを間違えないよう確認してください。", yellowTextStyle);
            GUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(_rendererProperty, new GUIContent("対象のブレンドシェイプのオブジェクト"));
            EditorGUILayout.HelpBox(
                "保存 : 指定したオブジェクトに紐づいているブレンドシェイプの" +
                "パラメータを保存できます。保存する際に保存場所とファイル名を決定できます。",
                MessageType.Info
            );
            if (GUILayout.Button("保存 : Save"))
            {
                Save();
            }

            var boxStyle = new GUIStyle(GUI.skin.box);
            var errorIconTexture = EditorGUIUtility.IconContent("console.erroricon").image as Texture2D;
            boxStyle.padding = new RectOffset(Padding, Padding, Padding, Padding);
            boxStyle.margin = new RectOffset(Margin, Margin, Margin, Margin);
            GUILayout.BeginHorizontal(boxStyle);
            GUILayout.Label(errorIconTexture, GUILayout.Width(IconSize), GUILayout.Height(IconSize)); // アイコンを表示
            GUILayout.Label("※保存呼び出しをした場合、ブレンドシェイプのパラメータが上書きされます。\n" +
                            "現状のブレンドシェイプのパラメータを残したい場合、あらかじめ現在の状態を保存してください", redTextStyle);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("保存呼び出し : Update"))
            {
                UpdateBlendShape();
            }

            _serializedObject.ApplyModifiedProperties();
        }

        private void Save()
        {
            var renderer = _data.renderer;
            if (renderer == null)
            {
                Debug.LogError("対象のブレンドシェイプのオブジェクトに何も入っていません。");
                return;
            }

            var mesh = renderer.sharedMesh;
            var blendShapeCount = mesh.blendShapeCount;
            var avatarInfo = new AvatarInfo();
            for (var i = 0; i < blendShapeCount; i++)
            {
                var keyName = mesh.GetBlendShapeName(i);
                var weight = _data.renderer.GetBlendShapeWeight(i);
                var blendShape = new BlendShape
                {
                    key = keyName,
                    weight = weight,
                };
                _blendShapes.Add(blendShape);
            }

            var path = EditorUtility.SaveFilePanel("ブレンドシェイプファイルの保存", "", "BlendShape.json", "json");

            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("保存キャンセル");
                return;
            }

            avatarInfo.blendShapes = _blendShapes;
            var json = JsonUtility.ToJson(avatarInfo, true);
            File.WriteAllText(path, json);
            Debug.Log($"ファイルが保存されました : {path}");
            _blendShapes.Clear();
            _data = new SkinnedMeshData();
            AssetDatabase.Refresh();
        }

        private void UpdateBlendShape()
        {
            var path = EditorUtility.OpenFilePanel("ブレンドシェイプファイルの保存呼び出し", "", "json");
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("保存呼び出しキャンセル");
                return;
            }

            AvatarInfo avatarInfo = null;
            try
            {
                var json = File.ReadAllText(path);
                avatarInfo = JsonUtility.FromJson<AvatarInfo>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"json の読み込みに失敗 : {e.Message}\n 読込ファイル先 : {path}");
                return;
            }

            if (_data.renderer == null)
            {
                Debug.LogError("対象のブレンドシェイプのオブジェクトに何も入っていません。");
                return;
            }

            for (var i = 0; i < _data.renderer.sharedMesh.blendShapeCount; i++)
            {
                var keyName = _data.renderer.sharedMesh.GetBlendShapeName(i);
                var blendShapeItem = avatarInfo.blendShapes.FirstOrDefault(v => v.key == keyName);
                if (blendShapeItem != null)
                {
                    _data.renderer.SetBlendShapeWeight(i, blendShapeItem.weight);
                }

                avatarInfo.blendShapes.Remove(blendShapeItem);
            }

            _data = new SkinnedMeshData();
            Debug.Log($"保存呼び出しからシェイプキーを更新しました : {path}");
        }
    }
}
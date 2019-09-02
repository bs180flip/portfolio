using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class FontReplacer : EditorWindow
{
	private Font font;

	private string excludeStr;

	private string game = "Scenes/Game";

	private string[] splitExcludeArray;

	[MenuItem("Window/FontReplacer")]
	public static void OpenWindow()
	{
		GetWindow(typeof(FontReplacer)).Show();
	}

	void OnGUI()
	{
		font = EditorGUILayout.ObjectField("Font", font, typeof(Font), true) as Font;
		EditorGUILayout.HelpBox("除外するFont名の一部を改行かカンマで区切った文字列を入力してください。", MessageType.Info);
		excludeStr = EditorGUILayout.TextArea(excludeStr, GUILayout.Height(120), GUILayout.ExpandWidth(true));

		if (font != null)
		{
			if (GUILayout.Button("Replace font in all"))
			{
				ReplaceFont(font);
				Debug.Log("Complete Replaced font in all scenes.");

				ReplacePrefabs(font);
				Debug.Log("Complete Replaced font in all prefab.");
			}
		}
	}

	void ReplaceFont(Font font)
	{
		if (excludeStr == null) { excludeStr = ""; }

		//除外文字列
		excludeStr = excludeStr.Trim();

		// 現在開いているシーン群
		var sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
		var scenePath2ActiveMap = new List<string>();
		for (int i = 0; i < sceneCount; i++)
		{
			var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
			scenePath2ActiveMap.Add(scene.name);
		}

		var allScenePaths = AssetDatabase.FindAssets("t:Scene").Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToList();

		var includeScene = new List<string>();

		// 対象のSceneを取得
		foreach (var path in allScenePaths)	
		{
			bool result = Regex.IsMatch(path, game);
			if (!result) { continue; }

			includeScene.Add(path);
		}

		float cnt = 0;
		float includeSceneCnt = includeScene.Count;

		foreach (var scenePath in includeScene)
		{
			EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

			// シーン取得
			var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);

			EditorUtility.DisplayProgressBar("Scene操作", scene.name + "シーンの操作をしています", cnt / includeSceneCnt);

			// Scene上に存在するTextComponentのフォントを取得
			var textComponents = Resources.FindObjectsOfTypeAll(typeof(Text)) as Text[];

			if (excludeStr == null) { excludeStr = ""; }

			// 除外文字列
		    excludeStr = excludeStr.Trim();

			splitExcludeArray = excludeStr.Split(new char[] { '\n', ',' });

			Debug.Log("-------Scene名 : " + scene.name  + "------------");

			ReplaceTextComponents(textComponents);

			// シーンに変更があることをUnity側に通知しないと、シーンを切り替えたときに変更が破棄されてしまうので、↓が必要
			EditorSceneManager.MarkAllScenesDirty();

			EditorSceneManager.SaveScene(scene);

			// 予め開いていたシーン群以外を閉じる
			if (!scenePath2ActiveMap.Contains(scenePath))
			{
				EditorSceneManager.CloseScene(scene, true);
			}

			cnt++;
		}

		EditorUtility.ClearProgressBar();

		Close();
	}

	private void ReplaceTextComponents(Text[] textComponents)
	{
		// TextComponentのフォントを置換
		foreach (var textComponent in textComponents)
		{
			bool result = false;

			if (textComponent.font == null) { continue; }

			foreach (var excStr in splitExcludeArray)
			{
				result = Regex.IsMatch(textComponent.font.name, excStr);

				if (result) { break; }
			}

			if (result) { continue; }

			Debug.Log(textComponent.name);

			textComponent.font = font;
		}
	}

	void ReplacePrefabs(Font font)
	{
		var allPrefabs = AssetDatabase.FindAssets("t:Prefab").Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToList();

		var includePrefabList = new List<string>();

		// 対象のPrefabを取得
		foreach (var path in allPrefabs)
		{
			bool result = Regex.IsMatch(path, game);
			if (!result) { continue; }

			includePrefabList.Add(path);
		}

		float cnt = 0;
		int includePrefabCnt = includePrefabList.Count;

		ExecuteChange(includePrefabList, prefab =>
		{
			EditorUtility.DisplayProgressBar("Prefab操作", prefab.name + "Prefabの操作をしています", cnt / includePrefabCnt);

			var textCompornents  = prefab.GetComponentsInChildren<Text>();

			Debug.Log("-------Prefab名 : " + prefab.name + "------------");

			ReplaceTextComponents(textCompornents);
			cnt++;
		});

		EditorUtility.ClearProgressBar();
	}

	void ExecuteChange(List<string> allPrefabs, System.Action<GameObject> onChange)
	{
		if (onChange == null) return;

		foreach (string path in allPrefabs)
		{
			var prefabAsset = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
			if (prefabAsset == null) continue;

			var prefab = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;

			if (prefab == null) continue;

			onChange(prefab);

			PrefabUtility.SaveAsPrefabAsset(prefab, path);

			DestroyImmediate(prefab);
		}
	}
}

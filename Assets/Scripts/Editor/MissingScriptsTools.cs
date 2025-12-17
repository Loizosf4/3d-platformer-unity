using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissingScriptsTools
{
	private const string MenuRoot = "Tools/Missing Scripts/";

	[MenuItem(MenuRoot + "Report In Selection")]
	private static void ReportInSelection()
	{
		var targets = CollectSelectionTargets();
		ReportMissingScripts(targets, "Selection");
	}

	[MenuItem(MenuRoot + "Remove In Selection")]
	private static void RemoveInSelection()
	{
		var targets = CollectSelectionTargets();
		int removed = RemoveMissingScripts(targets);
		Debug.Log($"[MissingScriptsTools] Removed {removed} missing script component(s) in Selection.");
	}

	[MenuItem(MenuRoot + "Report In Open Scenes")]
	private static void ReportInOpenScenes()
	{
		var roots = CollectOpenSceneRoots();
		ReportMissingScripts(roots, "Open Scenes");
	}

	[MenuItem(MenuRoot + "Remove In Open Scenes")]
	private static void RemoveInOpenScenes()
	{
		var roots = CollectOpenSceneRoots();
		int removed = RemoveMissingScripts(roots);
		if (removed > 0)
		{
			EditorSceneManager.MarkAllScenesDirty();
		}

		Debug.Log($"[MissingScriptsTools] Removed {removed} missing script component(s) in Open Scenes.");
	}

	private static List<GameObject> CollectSelectionTargets()
	{
		var targets = new List<GameObject>();

		// Selected GameObjects in scene hierarchy.
		foreach (var go in Selection.gameObjects)
		{
			if (go != null)
				targets.Add(go);
		}

		// Selected prefab assets in Project window.
		foreach (var obj in Selection.objects)
		{
			if (obj == null)
				continue;

			string path = AssetDatabase.GetAssetPath(obj);
			if (string.IsNullOrEmpty(path))
				continue;

			if (!path.EndsWith(".prefab"))
				continue;

			var prefabRoot = PrefabUtility.LoadPrefabContents(path);
			if (prefabRoot == null)
				continue;

			// Stash the prefab root temporarily for scanning/removal.
			// We will unload it in the caller functions.
			targets.Add(prefabRoot);
		}

		return targets;
	}

	private static List<GameObject> CollectOpenSceneRoots()
	{
		var roots = new List<GameObject>();

		for (int i = 0; i < SceneManager.sceneCount; i++)
		{
			var scene = SceneManager.GetSceneAt(i);
			if (!scene.isLoaded)
				continue;

			foreach (var root in scene.GetRootGameObjects())
				roots.Add(root);
		}

		return roots;
	}

	private static void ReportMissingScripts(List<GameObject> targets, string scopeLabel)
	{
		if (targets == null || targets.Count == 0)
		{
			Debug.LogWarning($"[MissingScriptsTools] No targets found for {scopeLabel}.");
			return;
		}

		int totalMissing = 0;

		foreach (var root in targets)
		{
			if (root == null)
				continue;

			int missing = CountMissingScriptsRecursive(root);
			if (missing > 0)
			{
				totalMissing += missing;
				Debug.LogWarning($"[MissingScriptsTools] {missing} missing script component(s) under: {GetHierarchyPath(root)}", root);
			}
		}

		Debug.Log($"[MissingScriptsTools] Reported {totalMissing} missing script component(s) in {scopeLabel}.");

		// If any targets came from PrefabUtility.LoadPrefabContents, unload them now.
		UnloadAnyPrefabContents(targets);
	}

	private static int RemoveMissingScripts(List<GameObject> targets)
	{
		if (targets == null || targets.Count == 0)
			return 0;

		int removedTotal = 0;

		foreach (var root in targets)
		{
			if (root == null)
				continue;

			removedTotal += RemoveMissingScriptsRecursive(root);
		}

		// If any targets came from PrefabUtility.LoadPrefabContents, save/unload them now.
		SaveAndUnloadAnyPrefabContents(targets);

		return removedTotal;
	}

	private static int CountMissingScriptsRecursive(GameObject root)
	{
		int missingHere = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);

		int missingChildren = 0;
		foreach (Transform child in root.transform)
			missingChildren += CountMissingScriptsRecursive(child.gameObject);

		return missingHere + missingChildren;
	}

	private static int RemoveMissingScriptsRecursive(GameObject root)
	{
		int removedHere = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);

		int removedChildren = 0;
		foreach (Transform child in root.transform)
			removedChildren += RemoveMissingScriptsRecursive(child.gameObject);

		return removedHere + removedChildren;
	}

	private static void UnloadAnyPrefabContents(List<GameObject> targets)
	{
		// Prefab contents roots are not part of a scene and have a valid prefab asset path.
		foreach (var root in targets)
		{
			if (root == null)
				continue;

			string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
			if (string.IsNullOrEmpty(prefabPath))
				continue;

			// Only unload if this is a prefab contents root (not a scene instance).
			if (!root.scene.IsValid() || !root.scene.isLoaded)
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}
	}

	private static void SaveAndUnloadAnyPrefabContents(List<GameObject> targets)
	{
		foreach (var root in targets)
		{
			if (root == null)
				continue;

			string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
			if (string.IsNullOrEmpty(prefabPath))
				continue;

			if (!root.scene.IsValid() || !root.scene.isLoaded)
			{
				PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
				PrefabUtility.UnloadPrefabContents(root);
			}
		}
	}

	private static string GetHierarchyPath(GameObject obj)
	{
		if (obj == null)
			return "<null>";

		var path = obj.name;
		var current = obj.transform.parent;
		while (current != null)
		{
			path = current.name + "/" + path;
			current = current.parent;
		}

		return path;
	}
}

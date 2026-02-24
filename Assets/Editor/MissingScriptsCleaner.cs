using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class MissingScriptsCleaner : EditorWindow
{
    [MenuItem("Tools/Maintenance/Remove All Missing Scripts")]
    public static void ShowWindow()
    {
        RemoveAllMissingScripts();
    }

    public static void RemoveAllMissingScripts()
    {
        if (!EditorUtility.DisplayDialog("Confirmare Curățare", 
            "Ești sigur că vrei să ștergi TOATE scripturile lipsă din PROIECT (Prefab-uri) și din TOATE SCENELE?\n\nAceastă operațiune va modifica fișierele și nu poate fi anulată prin Undo.", 
            "Da, Șterge Tot", "Anulează"))
            return;

        int totalRemoved = 0;
        int filesAffected = 0;

        // 1. Curățare Prefab-uri
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            EditorUtility.DisplayProgressBar("Mentenanță: Prefab-uri", path, (float)i / prefabGuids.Length);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
                if (count > 0)
                {
                    EditorUtility.SetDirty(prefab);
                    totalRemoved += count;
                    filesAffected++;
                    Debug.Log($"<color=orange>[Prefab]</color> Am șters {count} scripturi lipsă din: {path}");
                }
            }
        }

        // 2. Curățare Scene
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        for (int i = 0; i < sceneGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
            EditorUtility.DisplayProgressBar("Mentenanță: Scene", path, (float)i / sceneGuids.Length);

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            int countInScene = 0;
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                countInScene += RemoveMissingScriptsRecursively(go);
            }

            if (countInScene > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                totalRemoved += countInScene;
                filesAffected++;
                Debug.Log($"<color=cyan>[Scenă]</color> Am șters {countInScene} scripturi lipsă din: {path}");
            }
            EditorSceneManager.CloseScene(scene, true);
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        string finalReport = $"Operațiune finalizată.\nS-au șters {totalRemoved} scripturi lipsă din {filesAffected} fișiere.";
        Debug.Log($"<color=green><b>[RAPORT FINAL]</b></color> {finalReport}");
        EditorUtility.DisplayDialog("Raport Final", finalReport, "OK");
    }

    private static int RemoveMissingScriptsRecursively(GameObject go)
    {
        int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        foreach (Transform child in go.transform)
        {
            count += RemoveMissingScriptsRecursively(child.gameObject);
        }
        return count;
    }
}
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfflineRetrievalBenchmarkMenu
{
    private const string ScenePath =
        "Assets/Scenes/OfflineRetrievalBenchmark.unity";

    [MenuItem("FGR/Research/Run Offline Retrieval Benchmark")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning(
                "Stop Play Mode before starting the offline retrieval benchmark.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        SceneAsset existingScene =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        if (existingScene == null)
        {
            CreateBenchmarkScene();
        }
        else
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        EditorApplication.isPlaying = true;
    }

    private static void CreateBenchmarkScene()
    {
        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);

        GameObject memorySystemObject = new GameObject("MemorySystem");
        memorySystemObject.AddComponent<MemorySystem>();

        GameObject runnerObject =
            new GameObject("OfflineRetrievalBenchmarkRunner");
        runnerObject.AddComponent<OfflineRetrievalBenchmarkRunner>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
    }
}
#endif

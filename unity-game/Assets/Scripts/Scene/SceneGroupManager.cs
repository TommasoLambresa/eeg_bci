using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneGroupManager
{
    public event Action<string> OnSceneLoaded = delegate { };
    public event Action<string> OnSceneUnloaded = delegate { };
    public event Action OnSceneGroupLoaded = delegate { };

    private SceneGroup activeSceneGroup;

    public async Task LoadScenes(SceneGroup group, IProgress<float> progress, bool reloadDupScenes = false) 
    {
        activeSceneGroup = group;
        var loadedScenes = new List<string>();

        await UnloadScenes();

        int sceneCount = SceneManager.sceneCount;

        for (int i = 0; i < sceneCount; i++)
            loadedScenes.Add(SceneManager.GetSceneAt(i).name);

        var totalScenesToLoad = activeSceneGroup.data.Count;

        var operationGroup = new AsyncOperationGroup(totalScenesToLoad);

        for (int i = 0; i < totalScenesToLoad; i++)
        {
            var sceneData = group.data[i];
            if (!reloadDupScenes && loadedScenes.Contains(sceneData._name))
                continue;

            var operation = SceneManager.LoadSceneAsync(sceneData.sceneReference.Path, LoadSceneMode.Additive);
            //await Task.Delay(TimeSpan.FromSeconds(2.5f));

            operationGroup.operations.Add(operation);

            OnSceneLoaded.Invoke(sceneData._name);
        }

        while (!operationGroup.isDone)
        {
            progress?.Report(operationGroup.progress);
            await Task.Delay(100);
        }

        string activeSceneName = activeSceneGroup.FindSceneNameByType(SceneType.ActiveScene);
        if (string.IsNullOrEmpty(activeSceneName))
            activeSceneName = activeSceneGroup.FindSceneNameByType(SceneType.MainMenu);
        if (string.IsNullOrEmpty(activeSceneName) && activeSceneGroup.data.Count > 0)
            activeSceneName = activeSceneGroup.data[0]._name;

        Scene activeScene = SceneManager.GetSceneByName(activeSceneName);

        if (activeScene.IsValid())
        {
            SceneManager.SetActiveScene(activeScene);
            Debug.Log($"Active scene set to: {activeScene.name}");
        }
        else
        {
            Debug.LogWarning($"Could not set active scene for group '{activeSceneGroup.groupName}'. Requested scene: '{activeSceneName}'");
        }

        OnSceneGroupLoaded.Invoke();
    }

    public async Task UnloadScenes()
    {
        var scenes = new List<string>();

        int sceneCount = SceneManager.sceneCount;

        for (int i = sceneCount - 1; i > 0; i--)
        {
            var sceneAt = SceneManager.GetSceneAt(i);
            if (!sceneAt.isLoaded)
                continue;

            var sceneName = sceneAt.name;
            if (sceneName.Equals("LoadingScene") || sceneName.Equals("Bootstrapper"))
                continue;

            scenes.Add(sceneName);
        }

        var operationGroup = new AsyncOperationGroup(scenes.Count);

        foreach (var scene in scenes)
        {
            var operation = SceneManager.UnloadSceneAsync(scene);
            if (operation == null)
                continue;

            operationGroup.operations.Add(operation);

            OnSceneUnloaded.Invoke(scene);
        }

        while (!operationGroup.isDone)
            await Task.Delay(100);

        await Resources.UnloadUnusedAssets();
    }
}

public readonly struct AsyncOperationGroup
{
    public readonly List<AsyncOperation> operations;

    public float progress => operations.Count == 0 ? 0 : operations.Average(o => o.progress);
    public bool isDone => operations.All(o => o.isDone);

    public AsyncOperationGroup(int initialCapacity)
    {
        operations = new List<AsyncOperation>(initialCapacity);
    }
}

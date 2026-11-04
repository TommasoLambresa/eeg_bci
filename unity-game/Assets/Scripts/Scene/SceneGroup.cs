using System;
using System.Collections.Generic;
using System.Linq;
using Eflatun.SceneReference;

[Serializable]
public class SceneGroup
{
    public string groupName = "New Scene Group";
    public List<SceneData> data;

    public string FindSceneNameByType(SceneType sceneType)
    {
        return data.FirstOrDefault(scene => scene.sceneType == sceneType)?.sceneReference.Name;
    }
}

[Serializable]
public class SceneData
{
    public SceneReference sceneReference;
    public string _name => sceneReference.Name;

    public SceneType sceneType;
}

public enum SceneType
{
    ActiveScene,
    MainMenu,
    GameUI,
    ExperimentoUm,
    ExperimentoDois,
    Interface,
    HUD,
    Environment,
    Tooling,
    Gameplay
};
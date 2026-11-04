using UnityEngine;

public class LoadScene : MonoBehaviour
{
    async public void NextScene(int group)
    {
        SceneLoader loader = FindAnyObjectByType<SceneLoader>();
        await loader.LoadSceneGroup(group);
    }
}

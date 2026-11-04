using UnityEngine;

/// <summary>
/// This class relegates signals dispatched from the camera flythrough timeline
/// </summary>
public class MediaSceneLoader : MonoBehaviour
{
    [SerializeField] private SceneLoad m_GardenSceneLoader;
    [SerializeField] private SceneLoad m_CockpitSceneLoader;
    [SerializeField] private SceneLoad m_OasisSceneLoader;
    
    private SceneLoad TerminalSceneLoader;

    void Start()
    {
        TerminalSceneLoader = GetComponent<SceneLoad>();
    }
    public void EnableGarden()
    {
        m_GardenSceneLoader.EnableScene();
    }

    public void DisableGarden()
    {
        m_GardenSceneLoader.DisableScene();
    }

    public void EnableCockpit()
    {
        m_CockpitSceneLoader.EnableScene();
    }

    public void DisableCockpit()
    {
        m_CockpitSceneLoader.DisableScene();
    }

    public void EnableTerminal()
    {
        TerminalSceneLoader.EnableScene();
    }

    public void EnableOasis()
    {
        m_OasisSceneLoader.EnableScene();
    }
    
    public void DisableOasis()
    {
        m_OasisSceneLoader.DisableScene();
    }

    public void Transition()
    {
        SceneTransitionManager.StartTransition(this);
    }

    public SceneLoad GetTerminalSceneLoader()
    {
        return TerminalSceneLoader;
    }
    
}

using UnityEngine;

// Script responsável para tirar e colocar a aplicação em tela cheia
public class FullscreenChoice : MonoBehaviour
{
    private void Awake()
    {
        var objs = FindObjectsOfType<FullscreenChoice>();

        if (objs.Length > 1)
        {
            Destroy(this.gameObject);
            return;
        }    
        DontDestroyOnLoad(this);
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        // O comando só irá funcionar no editor
        if (UnityEditor.EditorApplication.isPlaying && ShouldToggleMaximize())
        {
            UnityEditor.EditorWindow.focusedWindow.maximized = !UnityEditor.EditorWindow.focusedWindow.maximized;
        }
#else
        // O comando só irá funcionar na build
        if (ShouldToggleMaximize())
        {
            Screen.fullScreen = !Screen.fullScreen;
        }
#endif
    }

    // Identifica se a tecla foi apertada
    private bool ShouldToggleMaximize()
    {
        return Input.GetKeyDown(KeyCode.F11);
    }
}

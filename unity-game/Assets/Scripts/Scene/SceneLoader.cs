using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [Header("Loading UI")]
    [SerializeField] private Image loadingBar;
    [SerializeField] private float fillSmoothness = 2f;
    [SerializeField] private float minFillDuration = 1f;
    [SerializeField] private Canvas loadingCanvas;
    [SerializeField] private Camera loadingCamera;

    [Space, Header("Scene Groups")]
    [SerializeField] private SceneGroup[] sceneGroups;

    private float targetProgress;
    private float currentProgress;
    private bool isLoading;
    private bool loadingComplete;
    private float loadingStartTime;

    public readonly SceneGroupManager manager = new SceneGroupManager();

    async void Start()
    {
        await LoadSceneGroup(0);
    }

    private void Update()
    {
        if (!isLoading)
            return;

        currentProgress = Mathf.MoveTowards(
            currentProgress,
            targetProgress, 
            fillSmoothness * Time.deltaTime
            );

        loadingBar.fillAmount = currentProgress;

        if (loadingComplete && Mathf.Approximately(currentProgress, 1f))
            FinishLoading();
    }

    public async Task LoadSceneGroup(int index)
    {
        loadingBar.fillAmount = 0f;
        currentProgress = 0f;
        targetProgress = 0f;
        loadingComplete = false;
        loadingStartTime = Time.time;

        if (index < 0 || index >= sceneGroups.Length)
        {
            Debug.LogError($"Invalid scene group index: {index}");
            return;
        }

        EnableLoadingCanvas(true);

        var progress = new LoadingProgress();
        progress.progressed += UpdateTargetProgress;

        try
        {
            await manager.LoadScenes(sceneGroups[index], progress);

            loadingComplete = true;

            float elapsed = Time.time - loadingStartTime;
            if (elapsed < minFillDuration)
                await Task.Delay((int)((minFillDuration - elapsed) * 1000));

            targetProgress = 1f;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading scene group: {e.Message}");
            FinishLoading();
        }
    }

    private void EnableLoadingCanvas(bool enable)
    {
        isLoading = enable;
        loadingCanvas.gameObject.SetActive(enable);
        loadingCamera.gameObject.SetActive(enable);
    }

    private void FinishLoading()
    {
        EnableLoadingCanvas(false);
        loadingComplete = false;
    }

    private void UpdateTargetProgress(float progress)
    {
        targetProgress = Mathf.Clamp01(progress);

        if (targetProgress < currentProgress)
            currentProgress = targetProgress;
    }
}

public class LoadingProgress : IProgress<float>
{
    public event Action<float> progressed;

    public void Report (float value)
    {
        float clampedValue = Mathf.Clamp01(value);
        progressed?.Invoke(clampedValue);
    }
}

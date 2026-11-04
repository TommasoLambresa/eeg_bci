using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Exibe um espaço de representação 1D/2D já calculado no Python.
///
/// O Unity NÃO aplica PCA, alinhamento, Procrustes ou qualquer transformação.
/// Ele apenas:
/// - carrega uma textura estática (densidades do treino);
/// - lê os limites do espaço em um manifesto JSON;
/// - mapeia rep1/rep2 recebidos pelo LSL para coordenadas de UI;
/// - desenha o ponto atual e um rastro curto.
///
/// Isso mantém o feedback genérico: qualquer transformação futura deve acontecer
/// no Python antes de gerar o mapa e antes de publicar rep1/rep2.
/// </summary>
public sealed class RepresentationFeedbackView : MonoBehaviour
{
    [Serializable]
    private sealed class RepresentationManifest
    {
        public int schema_version = 1;
        public string representation_id;
        public string representation_name;
        public int dimensions = 2;
        public float x_min = -1f;
        public float x_max = 1f;
        public float y_min = -1f;
        public float y_max = 1f;
        public string map_image;
        public string transform_type;
        public string[] class_labels;
    }

    [Header("UI")]
    public GameObject root;
    public RectTransform plotRect;
    public RawImage mapImage;
    public Image currentPoint;
    public Sprite dotSprite;
    public TMP_Text titleText;
    public TMP_Text statusText;

    [Header("Ponto")]
    [Range(0.1f, 1f)] public float idlePointAlpha = 0.55f;
    [Range(0.1f, 1f)] public float activePointAlpha = 1f;
    public bool clampPointToMap = true;

    [Header("Rastro")]
    [Min(0)] public int trailDotCount = 24;
    [Min(0.01f)] public float trailSampleInterval = 0.05f;
    [Range(0.01f, 1f)] public float trailMinAlpha = 0.06f;
    [Range(0.01f, 1f)] public float trailMaxAlpha = 0.45f;
    [Range(2f, 20f)] public float trailDotSize = 6f;

    private readonly List<Vector2> trailPositions = new List<Vector2>();
    private readonly List<Image> trailImages = new List<Image>();

    private RepresentationManifest manifest;
    private Texture2D ownedTexture;
    private bool attemptActive;
    private float lastTrailTime = -999f;
    private string loadedManifestPath = string.Empty;

    public bool IsLoaded => manifest != null && mapImage != null && mapImage.texture != null;
    public string LoadedManifestPath => loadedManifestPath;
    public string RepresentationId => manifest != null ? manifest.representation_id : string.Empty;
    public string RepresentationName => manifest != null ? manifest.representation_name : string.Empty;
    public int Dimensions => manifest != null ? Mathf.Clamp(manifest.dimensions, 1, 2) : 0;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        SetAttemptActive(false);
    }

    public void SetVisible(bool visible)
    {
        GameObject target = root != null ? root : gameObject;
        if (target.activeSelf != visible)
            target.SetActive(visible);
    }

    public bool LoadManifest(string manifestPath, out string error)
    {
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            error = "caminho do manifesto vazio";
            return false;
        }

        if (!File.Exists(manifestPath))
        {
            error = $"manifesto não encontrado: {manifestPath}";
            return false;
        }

        try
        {
            string json = File.ReadAllText(manifestPath);
            RepresentationManifest loaded = JsonUtility.FromJson<RepresentationManifest>(json);

            if (loaded == null)
            {
                error = "JSON de representação inválido";
                return false;
            }

            if (loaded.dimensions < 1 || loaded.dimensions > 2)
            {
                error = $"dimensions={loaded.dimensions}; esperado 1 ou 2";
                return false;
            }

            if (!(loaded.x_max > loaded.x_min))
            {
                error = "limites X inválidos";
                return false;
            }

            if (loaded.dimensions >= 2 && !(loaded.y_max > loaded.y_min))
            {
                error = "limites Y inválidos";
                return false;
            }

            string directory = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
            string imagePath = loaded.map_image;

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                error = "manifesto não contém map_image";
                return false;
            }

            if (!Path.IsPathRooted(imagePath))
                imagePath = Path.Combine(directory, imagePath);

            if (!File.Exists(imagePath))
            {
                error = $"imagem do mapa não encontrada: {imagePath}";
                return false;
            }

            byte[] bytes = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.name = $"RepresentationMap_{loaded.representation_id}";

            if (!texture.LoadImage(bytes))
            {
                Destroy(texture);
                error = "não foi possível decodificar o PNG do mapa";
                return false;
            }

            if (ownedTexture != null)
                Destroy(ownedTexture);

            ownedTexture = texture;
            manifest = loaded;
            loadedManifestPath = Path.GetFullPath(manifestPath);

            if (mapImage != null)
            {
                mapImage.texture = ownedTexture;
                mapImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            }

            if (titleText != null)
            {
                string title = string.IsNullOrWhiteSpace(loaded.representation_name)
                    ? "REPRESENTAÇÃO"
                    : loaded.representation_name.ToUpperInvariant();
                titleText.text = title;
            }

            if (statusText != null)
            {
                string tr = string.IsNullOrWhiteSpace(loaded.transform_type)
                    ? string.Empty
                    : $" • {loaded.transform_type}";
                statusText.text = $"{loaded.dimensions}D{tr}";
            }

            Canvas.ForceUpdateCanvases();
            EnsureTrailObjects();
            ClearTrail();
            SetAttemptActive(false);

            return true;
        }
        catch (Exception ex)
        {
            error = $"{ex.GetType().Name}: {ex.Message}";
            return false;
        }
    }

    public void SetAttemptActive(bool active)
    {
        attemptActive = active;

        if (currentPoint != null)
        {
            Color c = currentPoint.color;
            c.a = active ? activePointAlpha : idlePointAlpha;
            currentPoint.color = c;
        }

        if (active)
            lastTrailTime = -999f;
    }

    public void PushSample(float rep1, float rep2)
    {
        if (!IsLoaded || plotRect == null || currentPoint == null)
            return;

        Vector2 local = MapToLocal(rep1, rep2);
        currentPoint.rectTransform.anchoredPosition = local;

        if (attemptActive && trailDotCount > 0 &&
            Time.unscaledTime - lastTrailTime >= trailSampleInterval)
        {
            trailPositions.Add(local);
            if (trailPositions.Count > trailDotCount)
                trailPositions.RemoveAt(0);

            lastTrailTime = Time.unscaledTime;
            RefreshTrail();
        }
    }

    public void ClearTrail()
    {
        trailPositions.Clear();

        for (int i = 0; i < trailImages.Count; i++)
        {
            if (trailImages[i] != null)
                trailImages[i].gameObject.SetActive(false);
        }
    }

    private Vector2 MapToLocal(float rep1, float rep2)
    {
        float u = Mathf.InverseLerp(manifest.x_min, manifest.x_max, rep1);
        float v = manifest.dimensions >= 2
            ? Mathf.InverseLerp(manifest.y_min, manifest.y_max, rep2)
            : 0.5f;

        if (clampPointToMap)
        {
            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);
        }

        Rect rect = plotRect.rect;
        float x = (u - plotRect.pivot.x) * rect.width;
        float y = (v - plotRect.pivot.y) * rect.height;
        return new Vector2(x, y);
    }

    private void EnsureTrailObjects()
    {
        if (plotRect == null)
            return;

        while (trailImages.Count < trailDotCount)
        {
            int index = trailImages.Count;
            GameObject obj = new GameObject(
                $"TraceDot_{index:00}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            obj.layer = gameObject.layer;
            obj.transform.SetParent(plotRect, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(trailDotSize, trailDotSize);

            Image image = obj.GetComponent<Image>();
            image.raycastTarget = false;
            image.sprite = dotSprite;
            image.color = new Color(1f, 1f, 1f, trailMinAlpha);

            obj.SetActive(false);
            trailImages.Add(image);
        }

        for (int i = 0; i < trailImages.Count; i++)
        {
            if (trailImages[i] != null)
                trailImages[i].rectTransform.sizeDelta = new Vector2(trailDotSize, trailDotSize);
        }

        if (currentPoint != null)
            currentPoint.transform.SetAsLastSibling();
    }

    private void RefreshTrail()
    {
        EnsureTrailObjects();

        for (int i = 0; i < trailImages.Count; i++)
        {
            Image image = trailImages[i];
            if (image == null)
                continue;

            if (i >= trailPositions.Count)
            {
                image.gameObject.SetActive(false);
                continue;
            }

            image.gameObject.SetActive(true);
            image.rectTransform.anchoredPosition = trailPositions[i];

            float t = trailPositions.Count <= 1
                ? 1f
                : (float)i / (trailPositions.Count - 1);

            Color c = image.color;
            c.a = Mathf.Lerp(trailMinAlpha, trailMaxAlpha, t);
            image.color = c;
        }

        if (currentPoint != null)
            currentPoint.transform.SetAsLastSibling();
    }

    private void OnDestroy()
    {
        if (ownedTexture != null)
            Destroy(ownedTexture);
    }
}

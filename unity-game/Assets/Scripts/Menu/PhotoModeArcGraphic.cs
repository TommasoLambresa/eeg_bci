using UnityEngine;
using UnityEngine.UI;

public class PhotoModeArcGraphic : MaskableGraphic
{
    [Range(0f, 90f)] public float startAngle = 0f;
    [Range(0f, 90f)] public float angle = 30f;
    public float radius = 48f;
    public float thickness = 5f;
    public int segments = 32;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float start = Mathf.Clamp(startAngle, 0f, 90f);
        float endAngle = Mathf.Clamp(angle, 0f, 90f);
        if (endAngle <= start + 0.1f)
            return;

        int segmentCount = Mathf.Max(2, Mathf.CeilToInt(segments * ((endAngle - start) / 90f)));
        float innerRadius = Mathf.Max(1f, radius - thickness);
        int startIndex = 0;

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            float radians = Mathf.Lerp(start, endAngle, t) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            vh.AddVert(dir * innerRadius, color, Vector2.zero);
            vh.AddVert(dir * radius, color, Vector2.zero);

            if (i < segmentCount)
            {
                vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
                vh.AddTriangle(startIndex + 1, startIndex + 3, startIndex + 2);
                startIndex += 2;
            }
        }
    }

    public void SetAngle(float value)
    {
        startAngle = 0f;
        angle = Mathf.Clamp(value, 0f, 90f);
        SetVerticesDirty();
    }

    public void SetRange(float start, float end)
    {
        startAngle = Mathf.Clamp(start, 0f, 90f);
        angle = Mathf.Clamp(end, 0f, 90f);
        SetVerticesDirty();
    }
}

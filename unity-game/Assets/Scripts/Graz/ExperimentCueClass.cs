using System;
using UnityEngine;

[Serializable]
public class ExperimentCueClass
{
    [SerializeField] private bool enabled = true;
    [SerializeField] private string classId;
    [SerializeField] private string label;
    [SerializeField] private int markerCode = -1;
    [SerializeField] private Sprite cueImage;
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [SerializeField] private float targetAngle;

    public bool Enabled => enabled;
    public string ClassId => classId;
    public string Label => label;
    public int MarkerCode => markerCode;
    public Sprite CueImage => cueImage;
    public string Description => description;
    public float TargetAngle => targetAngle;

    public bool HasCustomMarkerCode => markerCode >= 0;

    public void EnsureDefaults(int index, int baseMarkerCode)
    {
        if (string.IsNullOrWhiteSpace(classId))
        {
            classId = $"class_{index:00}";
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            label = $"Classe {index}";
        }

        if (markerCode < 0)
        {
            markerCode = baseMarkerCode + index;
        }
    }
}

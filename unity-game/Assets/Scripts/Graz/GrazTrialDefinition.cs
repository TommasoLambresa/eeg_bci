using System;
using UnityEngine;

[Serializable]
public class GrazTrialDefinition
{
    [SerializeField] private int trialIndex;
    [SerializeField] private string classId;
    [SerializeField] private string classLabel;
    [SerializeField] private int markerCode;
    [SerializeField] private Sprite cueImage;
    [SerializeField] private string description;
    [SerializeField] private float targetAngle;

    public int TrialIndex => trialIndex;
    public string ClassId => classId;
    public string ClassLabel => classLabel;
    public int MarkerCode => markerCode;
    public Sprite CueImage => cueImage;
    public string Description => description;
    public float TargetAngle => targetAngle;

    public GrazTrialDefinition(int trialIndex, ExperimentCueClass cueClass)
    {
        this.trialIndex = trialIndex;
        classId = cueClass.ClassId;
        classLabel = cueClass.Label;
        markerCode = cueClass.MarkerCode;
        cueImage = cueClass.CueImage;
        description = cueClass.Description;
        targetAngle = cueClass.TargetAngle;
    }
}

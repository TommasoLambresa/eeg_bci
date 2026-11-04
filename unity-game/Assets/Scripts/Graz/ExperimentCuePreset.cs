using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ExperimentCuePreset",
    menuName = "BCI/GRAZ/Experiment Cue Preset",
    order = 10)]
public class ExperimentCuePreset : ScriptableObject
{
    [SerializeField] private string presetName = "Simple: one leg movement at different angles";
    [TextArea(2, 5)]
    [SerializeField] private string notes;
    [SerializeField] private int baseMarkerCode = 1000;
    [Min(1)]
    [SerializeField] private int repetitionsPerClass = 10;
    [SerializeField] private List<ExperimentCueClass> classes = new List<ExperimentCueClass>();

    public string PresetName => presetName;
    public string Notes => notes;
    public int BaseMarkerCode => baseMarkerCode;
    public int RepetitionsPerClass => repetitionsPerClass;
    public IReadOnlyList<ExperimentCueClass> Classes => classes;

    public IEnumerable<ExperimentCueClass> EnabledClasses
    {
        get
        {
            foreach (var cueClass in classes)
            {
                if (cueClass != null && cueClass.Enabled)
                {
                    yield return cueClass;
                }
            }
        }
    }

    private void OnValidate()
    {
        EnsureClassDefaults();
    }

    public void EnsureClassDefaults()
    {
        for (int i = 0; i < classes.Count; i++)
        {
            classes[i]?.EnsureDefaults(i, baseMarkerCode);
        }
    }

    public bool HasDuplicateMarkerCodes()
    {
        var seen = new HashSet<int>();

        foreach (var cueClass in EnabledClasses)
        {
            if (!seen.Add(cueClass.MarkerCode))
            {
                return true;
            }
        }

        return false;
    }
}

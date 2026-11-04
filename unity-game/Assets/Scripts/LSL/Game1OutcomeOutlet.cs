using LSL;
using UnityEngine;

/// <summary>
/// Envia ao PsychoPy o estado do Game 1.
/// 101 = READY, 201 = HIT, 202 = MISS, 299 = FINISHED.
/// </summary>
public sealed class Game1OutcomeOutlet : MonoBehaviour
{
    [SerializeField] private string streamName = "Unity_Game1_Feedback";
    [SerializeField] private string streamType = "Markers";

    private StreamOutlet outlet;
    private readonly float[] sample = new float[1];

    public void Configure(string newName, string newType)
    {
        streamName = string.IsNullOrWhiteSpace(newName) ? "Unity_Game1_Feedback" : newName.Trim();
        streamType = string.IsNullOrWhiteSpace(newType) ? "Markers" : newType.Trim();
        CreateOutlet();
    }

    public void Send(int code)
    {
        if (outlet == null)
            CreateOutlet();

        if (outlet == null)
            return;

        sample[0] = code;
        outlet.push_sample(sample);
    }

    private void CreateOutlet()
    {
        var info = new StreamInfo(
            streamName,
            streamType,
            1,
            LSL.LSL.IRREGULAR_RATE,
            channel_format_t.cf_float32,
            "unity_game1_feedback");

        outlet = new StreamOutlet(info);
    }
}

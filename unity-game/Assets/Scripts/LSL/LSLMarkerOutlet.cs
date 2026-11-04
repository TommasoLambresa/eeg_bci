using LSL;
using UnityEngine;

public class LSLMarkerOutlet : MonoBehaviour
{
    private static LSLMarkerOutlet _instance;

    public static LSLMarkerOutlet Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<LSLMarkerOutlet>();
            return _instance;
        }
    }

    [Header("Marker Outlet")]
    [SerializeField] private string streamName = "Unity_GRAZ_Markers";
    [SerializeField] private string streamType = "Markers";
    [SerializeField] private bool numericOnly = true;

    private StreamOutlet outlet;
    private string[] stringSample;
    private float[] numericSample;
    private string activeStreamName;
    private string activeStreamType;
    private bool activeNumericOnly;

    public bool IsReady => outlet != null;
    public string StreamName => streamName;
    public string StreamType => streamType;
    public bool NumericOnly => numericOnly;

    private void Awake()
    {
        if (_instance == null)
            _instance = this;
    }

    public void Configure(string newStreamName, string newStreamType, bool sendNumericOnly)
    {
        streamName = string.IsNullOrWhiteSpace(newStreamName) ? "Unity_GRAZ_Markers" : newStreamName.Trim();
        streamType = string.IsNullOrWhiteSpace(newStreamType) ? "Markers" : newStreamType.Trim();
        numericOnly = sendNumericOnly;
        EnsureOutlet();
    }

    public void SendClassMarker(int markerCode)
    {
        SendMarker(markerCode, markerCode.ToString());
    }

    public void SendPhaseMarker(int markerCode, string markerLabel)
    {
        SendMarker(markerCode, markerLabel);
    }

    public void SendMarker(int markerCode, string markerLabel)
    {
        EnsureOutlet();

        if (outlet == null)
            return;

        if (numericOnly)
        {
            numericSample[0] = markerCode;
            outlet.push_sample(numericSample);
        }
        else
        {
            stringSample[0] = string.IsNullOrWhiteSpace(markerLabel) ? markerCode.ToString() : markerLabel;
            outlet.push_sample(stringSample);
        }
    }

    private void EnsureOutlet()
    {
        if (outlet != null &&
            activeStreamName == streamName &&
            activeStreamType == streamType &&
            activeNumericOnly == numericOnly)
        {
            return;
        }

        activeStreamName = streamName;
        activeStreamType = streamType;
        activeNumericOnly = numericOnly;

        if (numericOnly)
        {
            numericSample = new float[1];
            var info = new StreamInfo(
                streamName,
                streamType,
                1,
                LSL.LSL.IRREGULAR_RATE,
                channel_format_t.cf_float32,
                "unity_graz_numeric_markers");
            outlet = new StreamOutlet(info);
        }
        else
        {
            stringSample = new string[1];
            var info = new StreamInfo(
                streamName,
                streamType,
                1,
                LSL.LSL.IRREGULAR_RATE,
                channel_format_t.cf_string,
                "unity_graz_string_markers");
            outlet = new StreamOutlet(info);
        }
    }
}

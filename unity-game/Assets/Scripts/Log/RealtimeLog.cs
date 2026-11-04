using UnityEngine;
using System.IO;
using System.Diagnostics;
using System;
using System.Linq;
using System.Collections.Generic;

// Classe para representar dados de log
public class LogData
{
    // Construtor novo: inclui coordenadas da representação e probabilidades do decoder.
    public LogData(
        string eegTime,
        string unityTime,
        string classifier,
        string rep1,
        string rep2,
        string decoderLeft,
        string decoderRest,
        string decoderRight,
        string rightAngle,
        string leftAngle,
        string xPos,
        string yPos,
        string zPos,
        string num,
        string marcador,
        string acertos,
        string classe,
        string tentativa,
        string target)
    {
        TempoEEG = eegTime;
        TempoUNITY = unityTime;
        Classifier = classifier;
        Rep1 = rep1;
        Rep2 = rep2;
        DecoderLeft = decoderLeft;
        DecoderRest = decoderRest;
        DecoderRight = decoderRight;
        AngleLegR = rightAngle;
        AngleLegL = leftAngle;
        xBall = xPos;
        yBall = yPos;
        zBall = zPos;
        Marcadornum = num;
        Marcador = marcador;
        Acertos = acertos;
        Classe = classe;
        Tentativa = tentativa;
        Target = target;
    }

    // Compatibilidade com chamadas antigas do projeto.
    public LogData(
        string eegTime,
        string unityTime,
        string classifier,
        string rightAngle,
        string leftAngle,
        string xPos,
        string yPos,
        string zPos,
        string num,
        string marcador,
        string acertos,
        string classe,
        string tentativa,
        string target)
        : this(
            eegTime, unityTime, classifier,
            string.Empty, string.Empty,
            string.Empty, string.Empty, string.Empty,
            rightAngle, leftAngle,
            xPos, yPos, zPos,
            num, marcador, acertos, classe, tentativa, target)
    {
    }

    public string TempoEEG { get; set; }
    public string TempoUNITY { get; set; }
    public string Classifier { get; set; }

    public string Rep1 { get; set; }
    public string Rep2 { get; set; }
    public string DecoderLeft { get; set; }
    public string DecoderRest { get; set; }
    public string DecoderRight { get; set; }

    public string AngleLegR { get; set; }
    public string AngleLegL { get; set; }

    public string xBall { get; set; }
    public string yBall { get; set; }
    public string zBall { get; set; }

    public string Marcadornum { get; set; }
    public string Marcador { get; set; }

    public string Acertos { get; set; }
    public string Classe { get; set; }
    public string Tentativa { get; set; }
    public string Target { get; set; }

    // Arquivo final continua separado por "|".
    public static implicit operator string(LogData log)
        => $"{log.TempoEEG}|{log.TempoUNITY}|{log.Classifier}|{log.Rep1}|{log.Rep2}|{log.DecoderLeft}|{log.DecoderRest}|{log.DecoderRight}|{log.AngleLegR}|{log.AngleLegL}|{log.xBall}|{log.yBall}|{log.zBall}|{log.Marcadornum}|{log.Marcador}|{log.Acertos}|{log.Classe}|{log.Tentativa}|{log.Target}";

    // Aceita tanto o novo formato de 19 colunas quanto logs antigos de 14 colunas.
    public static implicit operator LogData(string line)
    {
        var data = line.Split('|');

        if (data.Length >= 19)
        {
            return new LogData(
                data[0], data[1], data[2],
                data[3], data[4], data[5], data[6], data[7],
                data[8], data[9],
                data[10], data[11], data[12],
                data[13], data[14], data[15], data[16], data[17], data[18]);
        }

        if (data.Length >= 14)
        {
            return new LogData(
                data[0], data[1], data[2],
                data[3], data[4],
                data[5], data[6], data[7],
                data[8], data[9], data[10], data[11], data[12], data[13]);
        }

        throw new FormatException($"Linha de log inválida: esperado >=14 campos, recebido {data.Length}.");
    }
}

public static class StringExtension
{
    public static DateTime ToDateTime(this string value)
    {
        var data = value.Split('-');
        return new DateTime(int.Parse(data[0]), int.Parse(data[1]), int.Parse(data[2]));
    }
}

// Sistema principal de logging em tempo real
public class RealtimeLog : MonoBehaviour
{
    #region SINGLETON

    private static RealtimeLog _instance;

    public static RealtimeLog instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<RealtimeLog>();
            return _instance;
        }
    }

    #endregion

    #region VARIAVEIS

    [HideInInspector] public float _classifier;

    // Representação final enviada pelo Python.
    // Hoje pode ser PCA1/PCA2; futuramente pode ser o espaço já alinhado.
    [HideInInspector] public float _rep1;
    [HideInInspector] public float _rep2;

    // Saídas contínuas do decoder antes da aplicação do limiar no Unity.
    [HideInInspector] public float _decoderLeft;
    [HideInInspector] public float _decoderRest;
    [HideInInspector] public float _decoderRight;

    public Vector2 _pernaAngles;
    [HideInInspector] public Vector3 _ballPosition;
    [HideInInspector] public int _marcadorNum = 1;
    [HideInInspector] public string _marcador;

    [HideInInspector] public Vector2 _inputPernas;
    [HideInInspector] public string _collisionTime;

    [HideInInspector] public string _acerto;
    [HideInInspector] public int _classe;
    [HideInInspector] public int _tentativa;

    [HideInInspector] public string _target;

    private float _eegTime;
    private float _unityTime;

    #endregion

    public string _filePath;
    private StreamWriter _writer;
    private FileStream _fileStream;

    public float _updateTime = .1f;
    private float _nextUpdateTime;

    #region TAMANHO COLUNAS

    private const int ballPosWidth = 22;
    private const int inputWidth = 15;
    private const int timeColisionWidth = 17;
    private const int acertoWidth = 15;
    private const int timeWidth = 15;

    #endregion

    private Process notepadProcess;
    private bool isStarted;

    [SerializeField] private bool InRealtime;

    public List<LogData> logs = new List<LogData>();

    public enum ExperienciaChoice
    {
        Um,
        Dois
    }

    [HideInInspector] public ExperienciaChoice experienciaChoice;

    private void Start()
    {
        logs.Add(CreateHeader());
    }

    private static LogData CreateHeader()
    {
        return new LogData(
            "TempoEEG",
            "TempoUNITY",
            "Classifier",
            "Rep1",
            "Rep2",
            "DecoderLeft",
            "DecoderRest",
            "DecoderRight",
            "AngleLegR",
            "AngleLegL",
            "xBall",
            "yBall",
            "zBall",
            "Marcadornum",
            "Marcador",
            "Acertos",
            "Classe",
            "Tentativa",
            "Target");
    }

    private void Update()
    {
        _eegTime = Time.time;

        if (!isStarted || _writer == null)
            return;

        if (!InRealtime)
            return;

        _unityTime += Time.deltaTime;

        if (Time.time - _nextUpdateTime <= _updateTime)
            return;

        var log = new LogData(
            _eegTime.ToString(),
            _unityTime.ToString(),
            _inputPernas.ToString(),
            _rep1.ToString(),
            _rep2.ToString(),
            _decoderLeft.ToString(),
            _decoderRest.ToString(),
            _decoderRight.ToString(),
            _pernaAngles.y.ToString(),
            _pernaAngles.x.ToString(),
            _ballPosition.x.ToString(),
            _ballPosition.y.ToString(),
            _ballPosition.z.ToString(),
            _marcadorNum.ToString(),
            _marcador ?? string.Empty,
            _acerto ?? string.Empty,
            _classe.ToString(),
            _tentativa.ToString(),
            _target ?? string.Empty);

        logs.Add(log);

        // Mantém o writer legível durante a sessão.
        string line =
            $"{_eegTime.ToString("F3").PadLeft(timeWidth)} " +
            $"{_unityTime.ToString("F3").PadLeft(timeWidth)} " +
            $"{_inputPernas.ToString("F3").PadLeft(inputWidth)} " +
            $"{_rep1.ToString("F3").PadLeft(timeWidth)} " +
            $"{_rep2.ToString("F3").PadLeft(timeWidth)} " +
            $"{_decoderLeft.ToString("F3").PadLeft(timeWidth)} " +
            $"{_decoderRest.ToString("F3").PadLeft(timeWidth)} " +
            $"{_decoderRight.ToString("F3").PadLeft(timeWidth)} " +
            $"{_pernaAngles.y.ToString("F3").PadLeft(timeWidth)} " +
            $"{_pernaAngles.x.ToString("F3").PadLeft(acertoWidth)} " +
            $"{_ballPosition.x.ToString("F3").PadLeft(acertoWidth)} " +
            $"{_ballPosition.y.ToString("F3").PadLeft(acertoWidth)} " +
            $"{_ballPosition.z.ToString("F3").PadLeft(acertoWidth)} " +
            $"{_marcadorNum.ToString().PadLeft(acertoWidth)} " +
            $"{(_marcador ?? string.Empty).PadLeft(acertoWidth)} " +
            $"{(_acerto ?? string.Empty).PadLeft(acertoWidth)} " +
            $"{_classe.ToString().PadLeft(acertoWidth)} " +
            $"{_tentativa.ToString().PadLeft(acertoWidth)} " +
            $"{(_target ?? string.Empty).PadLeft(acertoWidth)}\n";

        _writer.Write(line);
        _writer.Flush();

        _nextUpdateTime = Time.time;
    }

    public void StartPath(string path)
    {
        if (!InRealtime)
            return;

        _filePath = path;
        _writer = new StreamWriter(_filePath, false);

        string header =
            $"{ "TempoEEG",-ballPosWidth} " +
            $"{ "TempoUNITY",-inputWidth} " +
            $"{ "Classifier",-timeWidth} " +
            $"{ "Rep1",-timeWidth} " +
            $"{ "Rep2",-timeWidth} " +
            $"{ "DecoderLeft",-timeColisionWidth} " +
            $"{ "DecoderRest",-timeColisionWidth} " +
            $"{ "DecoderRight",-timeColisionWidth} " +
            $"{ "AngleLegR",-acertoWidth} " +
            $"{ "AngleLegL",-timeColisionWidth} " +
            $"{ "xBall",-timeColisionWidth} " +
            $"{ "yBall",-timeColisionWidth} " +
            $"{ "zBall",-timeColisionWidth} " +
            $"{ "Marcadornum",-timeColisionWidth} " +
            $"{ "Marcador",-timeColisionWidth} " +
            $"{ "Acertos",-timeColisionWidth} " +
            $"{ "Classe",-timeColisionWidth} " +
            $"{ "Tentativa",-timeColisionWidth} " +
            $"{ "Target",-timeColisionWidth}\n";

        _writer.Write(header);
        _writer.Flush();

        isStarted = true;
        OpenLogFile();
    }

    private void OpenLogFile()
    {
        if (!File.Exists(_filePath))
            return;

        try
        {
            notepadProcess = Process.Start("notepad++.exe", _filePath);
            UnityEngine.Debug.Log("Arquivo de log aberto automaticamente");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Erro ao abrir arquivo: " + e.Message);
        }
    }

    public void EndLog()
    {
        if (!InRealtime)
            return;

        if (_writer != null)
        {
            _writer.Close();
            _writer.Dispose();
            _writer = null;

            File.Delete(_filePath);

            var data = logs.Select(log => (string)log).ToList();
            File.WriteAllLines(_filePath, data);
        }

        _unityTime = 0f;
        isStarted = false;
    }

    private void OnApplicationQuit()
    {
        if (_writer == null)
            return;

        _writer.Close();
        _writer.Dispose();

        File.Delete(_filePath);

        var data = logs.Select(log => (string)log).ToList();
        File.WriteAllLines(_filePath, data);
    }
}

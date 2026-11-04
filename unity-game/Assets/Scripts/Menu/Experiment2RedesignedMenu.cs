using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Experiment2RedesignedMenu : MonoBehaviour
{
    private enum ExperimentMode
    {
        Capture,
        FreeLsl,
        GrazAcquisition,
        GrazFeedback
    }

    private GameUIDois ui;
    private ExperimentMode activeMode = ExperimentMode.GrazFeedback;
    private Vector2 mainScroll;
    private bool stylesReady;
    private GUIStyle pageStyle;
    private GUIStyle panelStyle;
    private GUIStyle darkPanelStyle;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle sectionTitleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle selectedButtonStyle;
    private GUIStyle buttonStyle;
    private GUIStyle fieldLabelStyle;
    private GUIStyle statusStyle;
    private string status = "Pronto para configurar";

    private string trialsText = "25";
    private string streamNameText = "Python_EEG";
    private string streamTypeText = "EEG";
    private string baselineText = "10";
    private string fixationText = "3";
    private string taskText = "3.75";
    private string minPauseText = "1.5";
    private string maxPauseText = "3.5";
    private string gravityText = "9.71";
    private string toleranceText = "1";
    private string holdTimeText = "3";
    private string presetNameText = "Simple: one leg movement at different angles";
    private string subjectIdText = "S01";
    private string sessionIdText = "01";
    private string runText = "1";
    private string repetitionsPerClassText = "10";
    private string markerStreamNameText = "Unity_GRAZ_Markers";
    private string markerStreamTypeText = "Markers";
    private string randomSeedText = "12345";
    private bool shuffleTrials = true;
    private bool numericMarkersOnly = true;
    private List<EditableCueClass> cueClasses = new List<EditableCueClass>();
    private List<string> trialPreview = new List<string>();
    private List<GeneratedTrialRow> generatedTrialPlan = new List<GeneratedTrialRow>();
    private string lastPresetPath = "";
    private string lastTrialPlanPath = "";

    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string CuePresetPrefsKey = "Experiment2.GrazCuePreset";

    public void Initialize(GameUIDois owner)
    {
        ui = owner;
        HideLegacyConfigPanel();
        PullValuesFromLegacyFields();
        LoadCuePresetFromPrefs(false);
        EnsureDefaultCueClasses();
    }

    private void HideLegacyConfigPanel()
    {
        if (ui == null || ui.painelConfigs == null)
            return;

        ui.painelConfigs.alpha = 0f;
        ui.painelConfigs.interactable = false;
        ui.painelConfigs.blocksRaycasts = false;
    }

    public void ShowAfterPhotoMode()
    {
        enabled = true;
        HideLegacyConfigPanel();
        PullValuesFromLegacyFields();
        status = "Modo camera encerrado.";
    }

    public void ShowFromExperimentSelection()
    {
        enabled = true;
        HideLegacyConfigPanel();
        PullValuesFromLegacyFields();
        status = "Pronto para configurar";
    }

    private void PullValuesFromLegacyFields()
    {
        if (ui == null)
            return;

        trialsText = ui.trialsField != null ? ui.trialsField.text : trialsText;
        streamNameText = ui.inputStreamName != null ? ui.inputStreamName.text : streamNameText;
        streamTypeText = ui.inputStreamType != null ? ui.inputStreamType.text : streamTypeText;
        baselineText = ui._baselineField != null ? ui._baselineField.text : baselineText;
        fixationText = ui._fixationField != null ? ui._fixationField.text : fixationText;
        taskText = ui._feedbackField != null ? ui._feedbackField.text : taskText;
        minPauseText = ui._minPauseField != null ? ui._minPauseField.text : minPauseText;
        maxPauseText = ui._maxPauseField != null ? ui._maxPauseField.text : maxPauseText;

        var gravidade = PrivateInput("gravidadeField");
        var margem = PrivateInput("margemField");
        var tempo = PrivateInput("tempoPrecisaoField");
        gravityText = gravidade != null ? gravidade.text : gravityText;
        toleranceText = margem != null ? margem.text : toleranceText;
        holdTimeText = tempo != null ? tempo.text : holdTimeText;
    }

    [Serializable]
    private class EditableCueClass
    {
        public bool enabled = true;
        public string classId;
        public string label;
        public string markerCode;
        public string cueImagePath;
        public string description;
        public string targetAngle;
        public string tolerance;
        public string holdTime;

        public EditableCueClass(string id, string label, int marker, float targetAngle, string description)
        {
            classId = id;
            this.label = label;
            markerCode = marker.ToString();
            cueImagePath = "";
            this.description = description;
            this.targetAngle = targetAngle.ToString("0");
            tolerance = "5";
            holdTime = "3";
        }
    }

    [Serializable]
    private class EditableCuePresetData
    {
        public string presetName;
        public string subjectId;
        public string sessionId;
        public string run;
        public string repetitionsPerClass;
        public string randomSeed;
        public bool shuffle;
        public string markerStreamName;
        public string markerStreamType;
        public bool numericOnly;
        public List<EditableCueClass> classes;
    }

    [Serializable]
    private class GeneratedTrialRow
    {
        public int trialIndex;
        public string subjectId;
        public string sessionId;
        public string run;
        public string classId;
        public string label;
        public int markerCode;
        public string cueImagePath;
        public string description;
        public float targetAngle;
        public float tolerance;
        public float holdTime;
        public int randomSeed;
        public bool shuffled;
    }

    [Serializable]
    private class GeneratedTrialPlanData
    {
        public string presetName;
        public string subjectId;
        public string sessionId;
        public string run;
        public string generatedAt;
        public int repetitionsPerClass;
        public int randomSeed;
        public bool shuffled;
        public List<GeneratedTrialRow> trials;
    }

    private void EnsureDefaultCueClasses()
    {
        if (cueClasses.Count > 0)
            return;

        cueClasses.Add(new EditableCueClass("right_90", "direita 90", 1000, 90, "Perna direita em 90 graus."));
        cueClasses.Add(new EditableCueClass("right_60", "direita 60", 1001, 60, "Perna direita em 60 graus."));
        cueClasses.Add(new EditableCueClass("right_45", "direita 45", 1002, 45, "Perna direita em 45 graus."));
        cueClasses.Add(new EditableCueClass("right_30", "direita 30", 1003, 30, "Perna direita em 30 graus."));
        cueClasses.Add(new EditableCueClass("relaxed", "relaxado", 1004, 0, "Estado relaxado/sem movimento."));
    }

    private void ResetDefaultCueClasses()
    {
        cueClasses.Clear();
        EnsureDefaultCueClasses();
        repetitionsPerClassText = "10";
        randomSeedText = "12345";
        shuffleTrials = true;
        trialPreview.Clear();
        status = "Preset simples restaurado.";
    }

    private void SaveCuePresetToPrefs()
    {
        if (!ValidateCueClasses(out string validationMessage))
        {
            status = validationMessage;
            return;
        }

        PlayerPrefs.SetString(CuePresetPrefsKey, JsonUtility.ToJson(BuildPresetData()));
        PlayerPrefs.Save();
        status = "Preset padrao salvo.";
    }

    private void LoadCuePresetFromPrefs(bool updateStatus)
    {
        if (!PlayerPrefs.HasKey(CuePresetPrefsKey))
        {
            if (updateStatus)
                status = "Nenhum preset salvo ainda.";
            return;
        }

        var json = PlayerPrefs.GetString(CuePresetPrefsKey);
        var data = JsonUtility.FromJson<EditableCuePresetData>(json);
        if (data == null)
        {
            if (updateStatus)
                status = "Preset salvo invalido.";
            return;
        }

        ApplyPresetData(data);
        if (updateStatus)
            status = "Preset padrao carregado.";
    }

    private void OnGUI()
    {
        if (ui == null)
            return;
        if (ShouldHideForPhotoMode())
            return;

        EnsureStyles();

        float margin = Mathf.Max(18f, Screen.width * 0.035f);
        float top = Mathf.Max(16f, Screen.height * 0.035f);
        var page = new Rect(margin, top, Screen.width - margin * 2f, Screen.height - top * 2f);

        GUILayout.BeginArea(page, pageStyle);
        DrawHeader();

        GUILayout.Space(12f);
        GUILayout.BeginHorizontal();
        DrawModeColumn(GUILayout.Width(Mathf.Clamp(page.width * 0.22f, 220f, 310f)), GUILayout.ExpandHeight(true));
        GUILayout.Space(12f);
        DrawMainColumn(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUILayout.Space(12f);
        DrawActionColumn(GUILayout.Width(Mathf.Clamp(page.width * 0.25f, 260f, 340f)), GUILayout.ExpandHeight(true));
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawHeader()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Label("Experimento 2", titleStyle);
        GUILayout.Label("Menu do operador para configurar captura, LSL, GRAZ de aquisicao e GRAZ com feedback.", subtitleStyle);
        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.Label(status, statusStyle, GUILayout.Width(420f));
        GUILayout.EndHorizontal();
    }

    private void DrawModeColumn(params GUILayoutOption[] options)
    {
        GUILayout.BeginVertical(darkPanelStyle, options);
        GUILayout.Label("Fluxo", sectionTitleStyle);
        ModeButton(ExperimentMode.Capture, "1. Captura de imagens", "prints/cues sem LSL");
        ModeButton(ExperimentMode.FreeLsl, "2. Controle livre LSL", "familiarizacao");
        ModeButton(ExperimentMode.GrazAcquisition, "3. GRAZ aquisicao", "treino EEG/EMG");
        ModeButton(ExperimentMode.GrazFeedback, "4. GRAZ feedback", "teste com alvo");

        GUILayout.Space(10f);
        GUILayout.Label("Escolha primeiro o modo. Depois configure os blocos que aparecem no centro.", bodyStyle);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Voltar aos experimentos", buttonStyle, GUILayout.Height(42f)))
        {
            ReturnToExperimentSelection();
            GUIUtility.ExitGUI();
        }

        GUILayout.Space(6f);

        if (GUILayout.Button("Mostrar menu antigo", buttonStyle, GUILayout.Height(42f)))
            RestoreLegacyPanel();
        GUILayout.EndVertical();
    }

    private void ModeButton(ExperimentMode mode, string title, string subtitle)
    {
        var style = activeMode == mode ? selectedButtonStyle : buttonStyle;
        if (GUILayout.Button(title + "\n" + subtitle, style, GUILayout.Height(62f)))
        {
            activeMode = mode;
            mainScroll = Vector2.zero;
            status = "Modo selecionado: " + title;
        }
    }

    private void DrawMainColumn(params GUILayoutOption[] options)
    {
        GUILayout.BeginVertical(panelStyle, options);
        DrawModeSummary();
        GUILayout.Space(8f);
        mainScroll = GUILayout.BeginScrollView(mainScroll);

        if (activeMode == ExperimentMode.Capture)
            DrawCaptureMode();
        else if (activeMode == ExperimentMode.FreeLsl)
            DrawFreeLslMode();
        else if (activeMode == ExperimentMode.GrazAcquisition)
            DrawGrazAcquisitionMode();
        else
            DrawGrazFeedbackMode();

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawModeSummary()
    {
        string title;
        string summary;
        string route;

        if (activeMode == ExperimentMode.Capture)
        {
            title = "Captura de imagens";
            summary = "Ajuste camera, perna, vista lateral e feedback visual para gerar imagens de cue/apresentacao.";
            route = "Rota conectada: PhotoMode";
        }
        else if (activeMode == ExperimentMode.FreeLsl)
        {
            title = "Controle livre LSL";
            summary = "Modo de familiarizacao para o usuario testar controle da protese via classificador.";
            route = "Rota futura";
        }
        else if (activeMode == ExperimentMode.GrazAcquisition)
        {
            title = "GRAZ aquisicao";
            summary = "Protocolo limpo para EEG/EMG: fixation, cue, task, pause, markers, shuffle e log.";
            route = "Rota futura";
        }
        else
        {
            title = "GRAZ feedback/teste";
            summary = "Cue + LSL + feedback da perna, com alvo, tolerancia, tempo no alvo e gravidade.";
            route = "Rota conectada: fluxo atual";
        }

        GUILayout.Label(title, titleStyle);
        GUILayout.Label(summary, subtitleStyle);
        GUILayout.Label(route, statusStyle);
    }

    private void DrawCaptureMode()
    {
        Card("Modo camera", "Ferramenta do experimentador, independente de LSL.");
        GUILayout.Label("Use este modo para gerar imagens das cues. O print nao precisa existir antes do GRAZ; depois a imagem sera escolhida no editor de classes.", bodyStyle);
        if (GUILayout.Button("Abrir modo camera", buttonStyle, GUILayout.Height(40f)))
        {
            var photoMode = ui.GetComponent<PhotoMode>();
            if (photoMode != null)
            {
                photoMode.EntryPhotoMode();
                GUIUtility.ExitGUI();
            }
        }

        Future("Biblioteca de cues", "Listar screenshots salvos, renomear por classe e anexar ao preset.");
        Future("Presets de camera", "Salvar composicoes de camera/perna/feedback por classe.");
    }

    private void DrawFreeLslMode()
    {
        DrawSessionAndLsl(true);
        Card("Controle em tempo real", "Parametros futuros do controle livre.");
        Future("Mapeamento do stream", "Classe discreta, angulo continuo ou comando normalizado.");
        Future("Smoothing/dead zone", "Filtros para reduzir tremor antes de mover a perna.");
    }

    private void DrawGrazAcquisitionMode()
    {
        DrawSessionAndLsl(false);
        DrawGrazTimings();
        DrawCueMarkers(false);
        Card("Tela do sujeito", "Aquisicao limpa: sem painel tecnico e sem feedback de desempenho.");
    }

    private void DrawGrazFeedbackMode()
    {
        DrawSessionAndLsl(true);
        DrawGrazTimings();
        DrawCueMarkers(true);
        DrawFeedbackCriteria();
    }

    private void DrawSessionAndLsl(bool classifier)
    {
        Card("Sessao e LSL", classifier ? "Identificacao, log e stream do classificador." : "Identificacao, log e markers do protocolo.");
        subjectIdText = Field("Sujeito", subjectIdText, "entra no log e markers");
        sessionIdText = Field("Sessao", sessionIdText, "sessao/bloco experimental");
        runText = Field("Run", runText, "indice do run atual");
        trialsText = Field("Trials totais", trialsText, "quantidade total de trials");
        if (classifier)
        {
            streamNameText = Field("Stream entrada", streamNameText, "classificador/angulo");
            streamTypeText = Field("Tipo entrada", streamTypeText, "tipo do stream");
        }

        markerStreamNameText = Field("Stream markers", markerStreamNameText, "outlet enviado pelo Unity");
        markerStreamTypeText = Field("Tipo markers", markerStreamTypeText, "normalmente Markers");
        numericMarkersOnly = GUILayout.Toggle(numericMarkersOnly, "Enviar pelo LSL apenas codigo numerico");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Aplicar campos", buttonStyle, GUILayout.Height(36f)))
            ApplySessionFields();
        if (GUILayout.Button("Escolher log", buttonStyle, GUILayout.Height(36f)))
            ui.SelecionarPath();
        if (GUILayout.Button("Carregar JSON antigo", buttonStyle, GUILayout.Height(36f)))
            ui.CarregarInfos();
        GUILayout.EndHorizontal();
    }

    private void DrawGrazTimings()
    {
        Card("Tempos do GRAZ", "Duracao de cada fase usada pelos markers e pelo log.");
        baselineText = Field("Baseline (s)", baselineText, "inicio da sessao/run");
        fixationText = Field("Fixation (s)", fixationText, "ponto/cruz antes da cue");
        taskText = Field("Task/feedback (s)", taskText, "execucao ou imaginacao");
        minPauseText = Field("Pausa min (s)", minPauseText, "pausa randomizada minima");
        maxPauseText = Field("Pausa max (s)", maxPauseText, "pausa randomizada maxima");
        if (GUILayout.Button("Aplicar tempos", buttonStyle, GUILayout.Height(36f)))
            ApplyTimingFields();
    }

    private void DrawCueMarkers(bool feedback)
    {
        Card("Classes, cues e markers", feedback ? "Cada classe tera imagem, codigo e alvo." : "Cada classe tera imagem e codigo.");
        presetNameText = Field("Nome do preset", presetNameText, "identifica esse desenho");
        repetitionsPerClassText = Field("Repeticoes/classe", repetitionsPerClassText, "define total planejado");
        randomSeedText = Field("Seed", randomSeedText, "ordem reprodutivel");
        shuffleTrials = GUILayout.Toggle(shuffleTrials, "Embaralhar classes antes do run");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Adicionar classe", buttonStyle, GUILayout.Height(34f)))
            AddCueClass();
        if (GUILayout.Button("Autocodigos", buttonStyle, GUILayout.Height(34f)))
            AutoCodeCueClasses();
        if (GUILayout.Button("Preview ordem", buttonStyle, GUILayout.Height(34f)))
            GenerateTrialPreview();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Salvar padrao", buttonStyle, GUILayout.Height(34f)))
            SaveCuePresetToPrefs();
        if (GUILayout.Button("Carregar padrao", buttonStyle, GUILayout.Height(34f)))
            LoadCuePresetFromPrefs(true);
        if (GUILayout.Button("Salvar JSON", buttonStyle, GUILayout.Height(34f)))
            StartCoroutine(SaveCuePresetJsonCoroutine());
        if (GUILayout.Button("Carregar JSON", buttonStyle, GUILayout.Height(34f)))
            StartCoroutine(LoadCuePresetJsonCoroutine());
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Exportar ordem", buttonStyle, GUILayout.Height(34f)))
            StartCoroutine(ExportTrialPlanJsonCoroutine());
        if (GUILayout.Button("Restaurar simples", buttonStyle, GUILayout.Height(34f)))
            ResetDefaultCueClasses();
        GUILayout.EndHorizontal();

        if (!string.IsNullOrWhiteSpace(lastPresetPath))
            GUILayout.Label("Preset: " + lastPresetPath, bodyStyle);
        if (!string.IsNullOrWhiteSpace(lastTrialPlanPath))
            GUILayout.Label("Ordem: " + lastTrialPlanPath, bodyStyle);

        GUILayout.Space(6f);
        for (int i = 0; i < cueClasses.Count; i++)
            DrawCueClassEditor(i, feedback);

        if (trialPreview.Count > 0)
        {
            Card("Preview da ordem", "Primeiros trials gerados a partir das classes ativas.");
            int maxPreview = Mathf.Min(trialPreview.Count, 12);
            for (int i = 0; i < maxPreview; i++)
                GUILayout.Label(trialPreview[i], bodyStyle);
            if (trialPreview.Count > maxPreview)
                GUILayout.Label("... total: " + trialPreview.Count + " trials", bodyStyle);
        }
    }

    private void DrawFeedbackCriteria()
    {
        Card("Criterio de sucesso e dificuldade", "Define o que conta como bom controle da protese.");
        gravityText = Field("Gravidade", gravityText, "quanto maior, mais a perna cai");
        toleranceText = Field("Tolerancia", toleranceText, "desvio aceito em torno do alvo");
        holdTimeText = Field("Tempo no alvo (s)", holdTimeText, "tempo minimo dentro da tolerancia");
        if (GUILayout.Button("Aplicar feedback", buttonStyle, GUILayout.Height(36f)))
            ApplyFeedbackFields();
        Future("Criterio por classe", "Cada cue podera ter alvo, tolerancia, hold time e gravidade proprios.");
        Future("Log de desempenho", "Erro angular, sucesso/falha, tempo dentro do alvo e classificacoes recebidas.");
    }

    private void DrawCueClassEditor(int index, bool feedback)
    {
        var cueClass = cueClasses[index];

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.BeginHorizontal();
        cueClass.enabled = GUILayout.Toggle(cueClass.enabled, "", GUILayout.Width(22f));
        GUILayout.Label("Classe " + index, fieldLabelStyle, GUILayout.Width(70f));
        cueClass.label = GUILayout.TextField(cueClass.label, GUILayout.Height(26f), GUILayout.MinWidth(110f));
        GUILayout.Label("Codigo", fieldLabelStyle, GUILayout.Width(52f));
        cueClass.markerCode = GUILayout.TextField(cueClass.markerCode, GUILayout.Height(26f), GUILayout.Width(62f));
        if (GUILayout.Button("X", buttonStyle, GUILayout.Width(30f), GUILayout.Height(26f)))
        {
            cueClasses.RemoveAt(index);
            return;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("ID", fieldLabelStyle, GUILayout.Width(70f));
        cueClass.classId = GUILayout.TextField(cueClass.classId, GUILayout.Height(26f), GUILayout.MinWidth(110f));
        GUILayout.Label("Alvo", fieldLabelStyle, GUILayout.Width(45f));
        cueClass.targetAngle = GUILayout.TextField(cueClass.targetAngle, GUILayout.Height(26f), GUILayout.Width(54f));
        if (feedback)
        {
            GUILayout.Label("Tol", fieldLabelStyle, GUILayout.Width(32f));
            cueClass.tolerance = GUILayout.TextField(cueClass.tolerance, GUILayout.Height(26f), GUILayout.Width(45f));
            GUILayout.Label("Hold", fieldLabelStyle, GUILayout.Width(42f));
            cueClass.holdTime = GUILayout.TextField(cueClass.holdTime, GUILayout.Height(26f), GUILayout.Width(45f));
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Imagem", fieldLabelStyle, GUILayout.Width(70f));
        cueClass.cueImagePath = GUILayout.TextField(cueClass.cueImagePath, GUILayout.Height(26f));
        if (GUILayout.Button("Escolher", buttonStyle, GUILayout.Width(82f), GUILayout.Height(26f)))
            StartCoroutine(SelectCueImageCoroutine(index));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Descricao", fieldLabelStyle, GUILayout.Width(70f));
        cueClass.description = GUILayout.TextArea(cueClass.description, GUILayout.Height(42f));
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private void AddCueClass()
    {
        int index = cueClasses.Count;
        cueClasses.Add(new EditableCueClass("class_" + index.ToString("00"), "Classe " + index, 1000 + index, 0, ""));
        status = "Classe adicionada.";
    }

    private void AutoCodeCueClasses()
    {
        for (int i = 0; i < cueClasses.Count; i++)
        {
            cueClasses[i].markerCode = (1000 + i).ToString();
            if (string.IsNullOrWhiteSpace(cueClasses[i].classId))
                cueClasses[i].classId = "class_" + i.ToString("00");
        }

        status = "Codigos numericos atualizados.";
    }

    private bool ValidateCueClasses(out string message, bool requireCueImages = false)
    {
        var markerCodes = new HashSet<int>();
        int enabledCount = 0;

        for (int i = 0; i < cueClasses.Count; i++)
        {
            var cueClass = cueClasses[i];
            if (!cueClass.enabled)
                continue;

            enabledCount++;

            if (string.IsNullOrWhiteSpace(cueClass.label))
            {
                message = "Classe " + i + " esta sem label.";
                return false;
            }

            if (!int.TryParse(cueClass.markerCode, out int markerCode))
            {
                message = "Classe " + cueClass.label + " tem codigo invalido.";
                return false;
            }

            if (!markerCodes.Add(markerCode))
            {
                message = "Codigo duplicado: " + markerCode + ".";
                return false;
            }

            if (!float.TryParse(cueClass.targetAngle, out _))
            {
                message = "Classe " + cueClass.label + " tem alvo invalido.";
                return false;
            }

            if (requireCueImages)
            {
                if (string.IsNullOrWhiteSpace(cueClass.cueImagePath))
                {
                    message = "Classe " + cueClass.label + " esta sem imagem de cue.";
                    return false;
                }

                if (!File.Exists(cueClass.cueImagePath))
                {
                    message = "Imagem nao encontrada para " + cueClass.label + ".";
                    return false;
                }
            }
        }

        if (enabledCount == 0)
        {
            message = "Ative pelo menos uma classe.";
            return false;
        }

        message = "Configuracao valida: " + enabledCount + " classes ativas.";
        return true;
    }

    private void GenerateTrialPreview()
    {
        if (!ValidateCueClasses(out string validationMessage))
        {
            status = validationMessage;
            return;
        }

        int repetitions = ParsePositiveInt(repetitionsPerClassText, 1);
        int seed = ParseInt(randomSeedText, Environment.TickCount);
        var planned = new List<EditableCueClass>();

        foreach (var cueClass in cueClasses)
        {
            if (!cueClass.enabled)
                continue;

            for (int i = 0; i < repetitions; i++)
                planned.Add(cueClass);
        }

        if (shuffleTrials)
            Shuffle(planned, seed);

        trialPreview.Clear();
        generatedTrialPlan.Clear();
        for (int i = 0; i < planned.Count; i++)
        {
            var cueClass = planned[i];
            int markerCode = ParseInt(cueClass.markerCode, 1000 + i);
            float targetAngle = ParseFloat(cueClass.targetAngle, 0f);
            float tolerance = ParseFloat(cueClass.tolerance, ParseFloat(toleranceText, 5f));
            float holdTime = ParseFloat(cueClass.holdTime, ParseFloat(holdTimeText, 3f));

            generatedTrialPlan.Add(new GeneratedTrialRow
            {
                trialIndex = i + 1,
                subjectId = subjectIdText,
                sessionId = sessionIdText,
                run = runText,
                classId = cueClass.classId,
                label = cueClass.label,
                markerCode = markerCode,
                cueImagePath = cueClass.cueImagePath,
                description = cueClass.description,
                targetAngle = targetAngle,
                tolerance = tolerance,
                holdTime = holdTime,
                randomSeed = seed,
                shuffled = shuffleTrials
            });

            trialPreview.Add(
                (i + 1).ToString("00") +
                " | " + cueClass.label +
                " | marker " + cueClass.markerCode +
                " | alvo " + cueClass.targetAngle + " graus");
        }

        trialsText = planned.Count.ToString();
        ApplySessionFields();
        status = "Ordem gerada: " + planned.Count + " trials.";
    }

    private void Shuffle<T>(IList<T> items, int seed)
    {
        var random = new System.Random(seed);

        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            T temp = items[i];
            items[i] = items[j];
            items[j] = temp;
        }
    }

    private IEnumerator SelectCueImageCoroutine(int cueIndex)
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".jpg", ".jpeg", ".png"));
        FileBrowser.SetDefaultFilter(".png");
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");

        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Selecione a imagem da cue", "Usar imagem");
        if (!FileBrowser.Success || FileBrowser.Result == null || FileBrowser.Result.Length == 0)
        {
            status = "Selecao de imagem cancelada.";
            yield break;
        }

        if (cueIndex < 0 || cueIndex >= cueClasses.Count)
            yield break;

        cueClasses[cueIndex].cueImagePath = FileBrowser.Result[0];
        status = "Imagem definida para " + cueClasses[cueIndex].label + ".";
    }

    private IEnumerator SaveCuePresetJsonCoroutine()
    {
        var defaultPath = GetPresetDirectory();
        var filename = MakeFileSafe(presetNameText) + ".json";

        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON", ".json"));
        FileBrowser.SetDefaultFilter(".json");
        yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, defaultPath, filename, "Salvar preset GRAZ", "Salvar");

        if (!FileBrowser.Success || FileBrowser.Result == null || FileBrowser.Result.Length == 0)
        {
            status = "Salvamento cancelado.";
            yield break;
        }

        SaveCuePresetJson(FileBrowser.Result[0]);
    }

    private IEnumerator LoadCuePresetJsonCoroutine()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON", ".json"));
        FileBrowser.SetDefaultFilter(".json");
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, GetPresetDirectory(), null, "Carregar preset GRAZ", "Carregar");

        if (!FileBrowser.Success || FileBrowser.Result == null || FileBrowser.Result.Length == 0)
        {
            status = "Carregamento cancelado.";
            yield break;
        }

        LoadCuePresetJson(FileBrowser.Result[0]);
    }

    private IEnumerator ExportTrialPlanJsonCoroutine()
    {
        if (!ValidateCueClasses(out string validationMessage, true))
        {
            status = validationMessage;
            yield break;
        }

        GenerateTrialPreview();

        var defaultPath = GetPresetDirectory();
        var filename = MakeFileSafe(presetNameText) + "_trial_order.json";

        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON", ".json"));
        FileBrowser.SetDefaultFilter(".json");
        yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, defaultPath, filename, "Exportar ordem de trials", "Exportar");

        if (!FileBrowser.Success || FileBrowser.Result == null || FileBrowser.Result.Length == 0)
        {
            status = "Exportacao cancelada.";
            yield break;
        }

        ExportTrialPlanJson(FileBrowser.Result[0]);
    }

    private void SaveCuePresetJson(string path)
    {
        path = EnsureExtension(path, ".json");
        var data = BuildPresetData();
        EnsureParentDirectory(path);
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        lastPresetPath = path;
        status = "Preset salvo em JSON.";
    }

    private void LoadCuePresetJson(string path)
    {
        if (!File.Exists(path))
        {
            status = "Arquivo de preset nao encontrado.";
            return;
        }

        var data = JsonUtility.FromJson<EditableCuePresetData>(File.ReadAllText(path));
        if (data == null)
        {
            status = "Arquivo de preset invalido.";
            return;
        }

        ApplyPresetData(data);
        lastPresetPath = path;
        status = "Preset carregado de JSON.";
    }

    private void ExportTrialPlanJson(string path)
    {
        path = EnsureExtension(path, ".json");
        int repetitions = ParsePositiveInt(repetitionsPerClassText, 1);
        int seed = ParseInt(randomSeedText, Environment.TickCount);
        var data = new GeneratedTrialPlanData
        {
            presetName = presetNameText,
            subjectId = subjectIdText,
            sessionId = sessionIdText,
            run = runText,
            generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            repetitionsPerClass = repetitions,
            randomSeed = seed,
            shuffled = shuffleTrials,
            trials = generatedTrialPlan
        };

        EnsureParentDirectory(path);
        File.WriteAllText(path, JsonUtility.ToJson(data, true));

        var csvPath = Path.ChangeExtension(path, ".csv");
        File.WriteAllText(csvPath, BuildTrialPlanCsv());

        lastTrialPlanPath = path;
        status = "Ordem exportada em JSON e CSV.";
    }

    private EditableCuePresetData BuildPresetData()
    {
        return new EditableCuePresetData
        {
            presetName = presetNameText,
            subjectId = subjectIdText,
            sessionId = sessionIdText,
            run = runText,
            repetitionsPerClass = repetitionsPerClassText,
            randomSeed = randomSeedText,
            shuffle = shuffleTrials,
            markerStreamName = markerStreamNameText,
            markerStreamType = markerStreamTypeText,
            numericOnly = numericMarkersOnly,
            classes = cueClasses
        };
    }

    private void ApplyPresetData(EditableCuePresetData data)
    {
        presetNameText = string.IsNullOrWhiteSpace(data.presetName) ? presetNameText : data.presetName;
        subjectIdText = string.IsNullOrWhiteSpace(data.subjectId) ? subjectIdText : data.subjectId;
        sessionIdText = string.IsNullOrWhiteSpace(data.sessionId) ? sessionIdText : data.sessionId;
        runText = string.IsNullOrWhiteSpace(data.run) ? runText : data.run;
        repetitionsPerClassText = string.IsNullOrWhiteSpace(data.repetitionsPerClass) ? repetitionsPerClassText : data.repetitionsPerClass;
        randomSeedText = string.IsNullOrWhiteSpace(data.randomSeed) ? randomSeedText : data.randomSeed;
        markerStreamNameText = string.IsNullOrWhiteSpace(data.markerStreamName) ? markerStreamNameText : data.markerStreamName;
        markerStreamTypeText = string.IsNullOrWhiteSpace(data.markerStreamType) ? markerStreamTypeText : data.markerStreamType;
        shuffleTrials = data.shuffle;
        numericMarkersOnly = data.numericOnly;

        if (data.classes != null && data.classes.Count > 0)
            cueClasses = data.classes;

        trialPreview.Clear();
        generatedTrialPlan.Clear();
    }

    private string BuildTrialPlanCsv()
    {
        var lines = new List<string>
        {
            "trial_index,subject_id,session_id,run,class_id,label,marker_code,cue_image_path,target_angle,tolerance,hold_time,random_seed,shuffled"
        };

        foreach (var trial in generatedTrialPlan)
        {
            lines.Add(string.Join(",",
                trial.trialIndex,
                EscapeCsv(trial.subjectId),
                EscapeCsv(trial.sessionId),
                EscapeCsv(trial.run),
                EscapeCsv(trial.classId),
                EscapeCsv(trial.label),
                trial.markerCode,
                EscapeCsv(trial.cueImagePath),
                trial.targetAngle.ToString(CultureInfo.InvariantCulture),
                trial.tolerance.ToString(CultureInfo.InvariantCulture),
                trial.holdTime.ToString(CultureInfo.InvariantCulture),
                trial.randomSeed,
                trial.shuffled));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private string EscapeCsv(string value)
    {
        if (value == null)
            return "";

        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            return "\"" + value.Replace("\"", "\"\"") + "\"";

        return value;
    }

    private string GetPresetDirectory()
    {
        var directory = Path.Combine(Application.persistentDataPath, "GrazCuePresets");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private string MakeFileSafe(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "graz_cue_preset";

        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return value.Replace(" ", "_");
    }

    private string EnsureExtension(string path, string extension)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.HasExtension(path))
            return path;

        return path + extension;
    }

    private void EnsureParentDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }

    private float ParseFloat(string value, float fallback)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out float parsed))
            return parsed;

        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            return parsed;

        string normalized = string.IsNullOrWhiteSpace(value) ? "" : value.Replace(',', '.');
        if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            return parsed;

        return fallback;
    }

    private void DrawActionColumn(params GUILayoutOption[] options)
    {
        GUILayout.BeginVertical(darkPanelStyle, options);
        GUILayout.Label("Acoes", sectionTitleStyle);
        GUILayout.Label("Sessao\nSujeito, sessao, trials e log.", bodyStyle);
        GUILayout.Space(4f);
        GUILayout.Label("LSL\nEntrada do classificador e/ou markers.", bodyStyle);
        GUILayout.Space(4f);
        GUILayout.Label("GRAZ\nTempos, classes, cues e shuffle.", bodyStyle);

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Validar configuracao", buttonStyle, GUILayout.Height(42f)))
        {
            ValidateCueClasses(out string validationMessage, true);
            status = validationMessage;
        }
        if (GUILayout.Button("Salvar preset", buttonStyle, GUILayout.Height(42f)))
            SaveCuePresetToPrefs();
        if (GUILayout.Button("Preview da ordem", buttonStyle, GUILayout.Height(42f)))
            GenerateTrialPreview();

        GUILayout.Space(8f);
        if (GUILayout.Button(PrimaryActionLabel(), selectedButtonStyle, GUILayout.Height(54f)))
            StartSelectedMode();
        GUILayout.EndVertical();
    }

    private string PrimaryActionLabel()
    {
        if (activeMode == ExperimentMode.Capture)
            return "Abrir camera";
        if (activeMode == ExperimentMode.GrazFeedback)
            return "Iniciar feedback";
        return "Rota preparada";
    }

    private void Card(string title, string subtitle)
    {
        GUILayout.Space(6f);
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(title, sectionTitleStyle);
        GUILayout.Label(subtitle, bodyStyle);
        GUILayout.EndVertical();
    }

    private void Future(string title, string text)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(title + "  |  futuro", fieldLabelStyle);
        GUILayout.Label(text, bodyStyle);
        GUILayout.EndVertical();
    }

    private string Field(string label, string value, string help)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, fieldLabelStyle, GUILayout.Width(150f));
        var newValue = GUILayout.TextField(value, GUILayout.Height(28f));
        GUILayout.Label(help, bodyStyle, GUILayout.Width(210f));
        GUILayout.EndHorizontal();
        return newValue;
    }

    private void ApplySessionFields()
    {
        if (ui.trialsField != null)
            ui.trialsField.text = trialsText;
        if (ui.inputStreamName != null)
            ui.inputStreamName.text = streamNameText;
        if (ui.inputStreamType != null)
            ui.inputStreamType.text = streamTypeText;
        ui.SetTrials(trialsText);
        ConfigureMarkerOutlet();
        status = "Campos de sessao/LSL aplicados.";
    }

    private void ApplyTimingFields()
    {
        if (ui._baselineField != null)
            ui._baselineField.text = baselineText;
        if (ui._fixationField != null)
            ui._fixationField.text = fixationText;
        if (ui._feedbackField != null)
            ui._feedbackField.text = taskText;
        if (ui._minPauseField != null)
            ui._minPauseField.text = minPauseText;
        if (ui._maxPauseField != null)
            ui._maxPauseField.text = maxPauseText;
        status = "Tempos do GRAZ aplicados.";
    }

    private void ApplyFeedbackFields()
    {
        var gravidade = PrivateInput("gravidadeField");
        var margem = PrivateInput("margemField");
        var tempo = PrivateInput("tempoPrecisaoField");

        if (gravidade != null)
            gravidade.text = gravityText;
        if (margem != null)
            margem.text = toleranceText;
        if (tempo != null)
            tempo.text = holdTimeText;
        ui.SetGravidade(gravityText);
        status = "Criterios de feedback aplicados.";
    }

    private void ConfigureMarkerOutlet()
    {
        var outlet = LSLMarkerOutlet.Instance;
        if (outlet == null && ui != null)
            outlet = ui.gameObject.AddComponent<LSLMarkerOutlet>();
        if (outlet != null)
            outlet.Configure(markerStreamNameText, markerStreamTypeText, numericMarkersOnly);
    }

    private void StartSelectedMode()
    {
        ApplySessionFields();
        ApplyTimingFields();
        ApplyFeedbackFields();

        if (activeMode == ExperimentMode.Capture)
        {
            var photoMode = ui.GetComponent<PhotoMode>();
            if (photoMode != null)
            {
                photoMode.EntryPhotoMode();
                GUIUtility.ExitGUI();
            }
            return;
        }

        if (activeMode == ExperimentMode.GrazFeedback)
        {
            if (!ValidateCueClasses(out string validationMessage))
            {
                status = validationMessage;
                return;
            }

            ConfigureMarkerOutlet();
            ui.SetMode("principal");
            ui.StartGame();
            status = "Fluxo atual iniciado. Aperte Space para iniciar.";
            return;
        }

        status = "Esta rota ja esta desenhada e sera implementada na proxima etapa.";
    }

    private bool ShouldHideForPhotoMode()
    {
        var photoMode = ui.GetComponent<PhotoMode>();
        return photoMode != null && photoMode.modeActive;
    }

    private void RestoreLegacyPanel()
    {
        if (ui == null || ui.painelConfigs == null)
            return;

        ui.painelConfigs.alpha = 1f;
        ui.painelConfigs.interactable = true;
        ui.painelConfigs.blocksRaycasts = true;
        enabled = false;
    }

    private async void ReturnToExperimentSelection()
    {
        HideLegacyConfigPanel();

        if (ui != null)
        {
            if (ui.configsUI != null)
                ui.configsUI.SetActive(false);
            if (ui.spaceBar != null)
                ui.spaceBar.SetActive(false);
            if (ui.startGame != null)
                ui.startGame.Started = false;
        }

        enabled = false;

        var loader = FindAnyObjectByType<SceneLoader>();
        if (loader != null)
        {
            await loader.LoadSceneGroup(0);
            return;
        }

        SceneManager.LoadScene("Menu", LoadSceneMode.Single);
    }

    private TMP_InputField PrivateInput(string fieldName)
    {
        var field = typeof(GameUIDois).GetField(fieldName, PrivateInstance);
        return field != null ? field.GetValue(ui) as TMP_InputField : null;
    }

    private int ParseInt(string value, int fallback)
    {
        return int.TryParse(value, out int parsed) ? parsed : fallback;
    }

    private int ParsePositiveInt(string value, int fallback)
    {
        int parsed = ParseInt(value, fallback);
        return Mathf.Max(1, parsed);
    }

    private void EnsureStyles()
    {
        if (stylesReady)
            return;

        pageStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(18, 18, 16, 16) };
        panelStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 14, 14) };
        darkPanelStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 14, 14) };
        darkPanelStyle.normal.background = Texture2D.grayTexture;

        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, normal = { textColor = Color.black }, wordWrap = true };
        subtitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = new Color(0.18f, 0.20f, 0.21f) }, wordWrap = true };
        sectionTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = Color.white }, wordWrap = true };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = Color.white }, wordWrap = true };
        fieldLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = Color.white }, wordWrap = true };
        statusStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(0.05f, 0.42f, 0.46f) }, wordWrap = true };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold, wordWrap = true };
        selectedButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, wordWrap = true };

        stylesReady = true;
    }
}

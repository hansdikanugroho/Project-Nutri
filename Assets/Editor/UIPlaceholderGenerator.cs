using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class UIPlaceholderGenerator : EditorWindow
{
    [MenuItem("Tools/Generate UI Placeholders")]
    public static void GenerateAll()
    {
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogError("[UI Gen] GameManager not found in scene!");
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            Debug.Log("[UI Gen] Canvas created.");
        }

        Undo.RecordObject(gm, "Generate UI Placeholders");

        // --- Timer Slider ---
        Slider timerSlider = CreateSlider(canvas.transform, "TimerSlider",
            new Vector2(0.25f, 0.92f), new Vector2(0.75f, 0.96f));
        gm.timerSlider = timerSlider;

        // --- Day Report Panel ---
        GameObject dayReportPanel = CreatePanel(canvas.transform, "DayReportPanel",
            new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f));
        gm.dayReportPanel = dayReportPanel;

        TextMeshProUGUI dayTitle = CreateText(dayReportPanel.transform, "DayTitleText",
            new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.97f),
            "Laporan Akhir Hari 1", 28, TextAlignmentOptions.Center);
        gm.dayTitleText = dayTitle;

        TextMeshProUGUI repResult = CreateText(dayReportPanel.transform, "ReputationResultText",
            new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.85f),
            "Perubahan Reputasi: 0\nReputasi Akhir: 100", 18, TextAlignmentOptions.TopLeft);
        gm.reputationResultText = repResult;

        TextMeshProUGUI stampStats = CreateText(dayReportPanel.transform, "StampStatsText",
            new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.50f),
            "Statistik Cap Hari Ini:\n-", 16, TextAlignmentOptions.TopLeft);
        gm.stampStatsText = stampStats;

        Button nextDayBtn = CreateButton(dayReportPanel.transform, "NextDayButton",
            new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.12f),
            "LANJUT", new Color(0.2f, 0.65f, 0.3f));
        gm.nextDayButton = nextDayBtn;

        // --- SP Popup Panel ---
        GameObject spPopup = CreatePanel(canvas.transform, "SpPopupPanel",
            new Vector2(0.25f, 0.35f), new Vector2(0.75f, 0.65f));
        gm.spPopupPanel = spPopup;

        TextMeshProUGUI spTitle = CreateText(spPopup.transform, "SpTitleText",
            new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.95f),
            "SP 1 - Peringatan!", 32, TextAlignmentOptions.Center);
        gm.spTitleText = spTitle;

        TextMeshProUGUI spMsg = CreateText(spPopup.transform, "SpMessageText",
            new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.60f),
            "Reputasi Anda masuk Bar 1. Waspada!", 18, TextAlignmentOptions.Center);
        gm.spMessageText = spMsg;

        // --- Toast Panel ---
        GameObject toastPanel = CreatePanel(canvas.transform, "ToastPanel",
            new Vector2(0.25f, 0.80f), new Vector2(0.75f, 0.90f));
        toastPanel.GetComponent<Image>().color = Color.white;
        gm.toastPanel = toastPanel;

        CanvasGroup toastGroup = toastPanel.AddComponent<CanvasGroup>();
        gm.toastCanvasGroup = toastGroup;
        gm.toastBackgroundImage = toastPanel.GetComponent<Image>();

        Image toastIcon = CreateImage(toastPanel.transform, "ToastIconImage",
            new Vector2(0.02f, 0.15f), new Vector2(0.18f, 0.85f));
        gm.toastIconImage = toastIcon;

        TextMeshProUGUI toastMain = CreateText(toastPanel.transform, "ToastMainText",
            new Vector2(0.20f, 0.50f), new Vector2(0.98f, 0.90f),
            "Produk Disetujui.", 22, TextAlignmentOptions.Left);
        gm.toastMainText = toastMain;

        TextMeshProUGUI toastSub = CreateText(toastPanel.transform, "ToastSubText",
            new Vector2(0.20f, 0.10f), new Vector2(0.98f, 0.50f),
            "Nutri Level A Telah Diberikan.", 16, TextAlignmentOptions.Left);
        gm.toastSubText = toastSub;

        // --- Reputation Slider ---
        Slider repSlider = CreateSlider(canvas.transform, "ReputationSlider",
            new Vector2(0.25f, 0.04f), new Vector2(0.75f, 0.08f));
        gm.reputationSlider = repSlider;
        gm.reputationBarRect = repSlider.GetComponent<RectTransform>();

        // --- Weekly Report Panel ---
        GameObject weeklyPanel = CreatePanel(canvas.transform, "WeeklyReportPanel",
            new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.95f));
        weeklyPanel.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.98f);

        GameObject weeklyBox = new GameObject("ContentBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        weeklyBox.transform.SetParent(weeklyPanel.transform, false);
        RectTransform weeklyBoxRt = weeklyBox.GetComponent<RectTransform>();
        weeklyBoxRt.anchorMin = new Vector2(0.03f, 0.03f);
        weeklyBoxRt.anchorMax = new Vector2(0.97f, 0.97f);
        weeklyBoxRt.offsetMin = Vector2.zero;
        weeklyBoxRt.offsetMax = Vector2.zero;
        weeklyBox.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.22f, 1f);
        weeklyBox.SetActive(true);

        TextMeshProUGUI weeklyTitle = CreateText(weeklyBox.transform, "WeeklyTitleText",
            new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.98f),
            "LAPORAN MINGGUAN 1", 28, TextAlignmentOptions.Center);
        weeklyTitle.fontStyle = FontStyles.Bold;

        TextMeshProUGUI weeklyPeriod = CreateText(weeklyBox.transform, "WeeklyPeriodText",
            new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.90f),
            "Hari 1 - 7", 18, TextAlignmentOptions.Center);
        weeklyPeriod.color = Color.yellow;

        TextMeshProUGUI weeklySummary = CreateText(weeklyBox.transform, "WeeklySummaryText",
            new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.84f),
            "Total Produk Diperiksa: 0\nCap Benar: 0 | Cap Salah: 0\nTolak Benar: 0 | Tolak Salah: 0 | Timeout: 0\nAkurasi Mingguan: 0% (0/0)", 16, TextAlignmentOptions.TopLeft);

        TextMeshProUGUI weeklyZat = CreateText(weeklyBox.transform, "WeeklyZatBerbahayaText",
            new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.67f),
            "Tidak ada zat berbahaya terdeteksi minggu ini.", 15, TextAlignmentOptions.TopLeft);
        weeklyZat.enableWordWrapping = true;

        TextMeshProUGUI weeklyProduk = CreateText(weeklyBox.transform, "WeeklyProdukListText",
            new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.37f),
            "-", 14, TextAlignmentOptions.TopLeft);
        weeklyProduk.enableWordWrapping = true;

        Button weeklyContinueBtn = CreateButton(weeklyBox.transform, "WeeklyContinueButton",
            new Vector2(0.35f, 0.03f), new Vector2(0.65f, 0.11f),
            "LANJUT", new Color(0.2f, 0.65f, 0.3f));

        WeeklyReportPanel weeklyComp = weeklyPanel.AddComponent<WeeklyReportPanel>();
        weeklyComp.weeklyReportPanel = weeklyPanel;
        weeklyComp.weeklyTitleText = weeklyTitle;
        weeklyComp.weeklyPeriodText = weeklyPeriod;
        weeklyComp.weeklySummaryText = weeklySummary;
        weeklyComp.weeklyZatBerbahayaText = weeklyZat;
        weeklyComp.weeklyProdukListText = weeklyProduk;
        weeklyComp.weeklyContinueButton = weeklyContinueBtn.GetComponent<Button>();

        gm.weeklyReportController = weeklyComp;
        weeklyPanel.SetActive(false);

        EditorUtility.SetDirty(gm);
        AssetDatabase.SaveAssets();

        Debug.Log("[UI Gen] All UI placeholders generated and bound to GameManager.");
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = obj.GetComponent<Image>();
        img.color = new Color(0.08f, 0.10f, 0.15f, 0.95f);
        obj.SetActive(false);

        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        return obj;
    }

    private static Slider CreateSlider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = obj.GetComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        RectTransform fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(rt, false);
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(0, 0);
        fillArea.offsetMax = new Vector2(0, 0);

        Image fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        fill.transform.SetParent(fillArea, false);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;
        fill.color = Color.green;

        Slider slider = obj.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = bg;
        slider.direction = Slider.Direction.LeftToRight;

        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        return slider;
    }

    private static Button CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string label, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = obj.GetComponent<Image>();
        img.color = color;

        TextMeshProUGUI txt = CreateText(obj.transform, "Text",
            Vector2.zero, Vector2.one, label, 18, TextAlignmentOptions.Center);

        Button btn = obj.GetComponent<Button>();
        btn.targetGraphic = img;

        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        return btn;
    }

    private static Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        return obj.GetComponent<Image>();
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string content, float fontSize, TextAlignmentOptions align)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI txt = obj.GetComponent<TextMeshProUGUI>();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.alignment = align;
        txt.color = Color.white;
        txt.enableWordWrapping = true;

        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        return txt;
    }
}
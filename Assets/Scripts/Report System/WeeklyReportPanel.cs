using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class WeeklyReportPanel : MonoBehaviour
{
    [Header("Weekly Panel Refs")]
    public GameObject weeklyReportPanel;
    public TextMeshProUGUI weeklyTitleText;
    public TextMeshProUGUI weeklyPeriodText;
    public TextMeshProUGUI weeklySummaryText;
    public TextMeshProUGUI weeklyStatusText;
    public TextMeshProUGUI weeklyZatBerbahayaText;
    public TextMeshProUGUI weeklyProdukListText;
    public TextMeshProUGUI weeklyMotivationText;
    public Button weeklyContinueButton;
    public Button weeklyCloseButton;

    [Header("Rating & SP Visuals")]
    public Image[] starImages = new Image[5];
    public Image[] warningImages = new Image[3];
    public Color filledStarColor = new Color(1f, 0.72f, 0.12f, 1f);
    public Color emptyStarColor = new Color(0.35f, 0.28f, 0.2f, 0.22f);
    public Color warningColor = new Color(0.78f, 0.12f, 0.08f, 1f);

    [Header("Edit Mode Preview")]
    public bool previewInEditMode = true;
    [Range(1, 3)] public int previewWeek = 1;
    [Range(0, 100)] public int previewAccuracy = 82;
    [Range(0, 100)] public int previewReputation = 78;
    [Range(0, 3)] public int previewSPLevel;
    [Min(1)] public int previewTotalProducts = 18;

    [Header("Display Limits")]
    [Min(1)] public int maxDisplayedMistakes = 4;

    private Action onContinue;
    private bool listenersBound;

    private void Awake()
    {
        if (Application.isPlaying)
        {
            BindButtons();
            if (weeklyReportPanel != null) weeklyReportPanel.SetActive(false);
        }
        else
        {
            RefreshEditModePreview();
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying) RefreshEditModePreview();
    }

    private void OnValidate()
    {
        previewWeek = Mathf.Clamp(previewWeek, 1, 3);
        previewAccuracy = Mathf.Clamp(previewAccuracy, 0, 100);
        previewReputation = Mathf.Clamp(previewReputation, 0, 100);
        previewSPLevel = Mathf.Clamp(previewSPLevel, 0, 3);
        previewTotalProducts = Mathf.Max(1, previewTotalProducts);
        maxDisplayedMistakes = Mathf.Max(1, maxDisplayedMistakes);

        if (!Application.isPlaying) RefreshEditModePreview();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    private void BindButtons()
    {
        if (listenersBound) return;
        if (weeklyContinueButton != null) weeklyContinueButton.onClick.AddListener(HandleContinue);
        if (weeklyCloseButton != null) weeklyCloseButton.onClick.AddListener(HandleContinue);
        listenersBound = true;
    }

    private void UnbindButtons()
    {
        if (!listenersBound) return;
        if (weeklyContinueButton != null) weeklyContinueButton.onClick.RemoveListener(HandleContinue);
        if (weeklyCloseButton != null) weeklyCloseButton.onClick.RemoveListener(HandleContinue);
        listenersBound = false;
    }

    private void HandleContinue()
    {
        if (weeklyReportPanel != null) weeklyReportPanel.SetActive(false);
        Action callback = onContinue;
        onContinue = null;
        callback?.Invoke();
    }

    public void Show(
        int weekNumber,
        int daysPerWeek,
        List<StampRecord> weekRecords,
        List<DayStampStats> weekDayStats,
        int reputation,
        int maxReputation,
        int spLevel,
        Action continueCallback)
    {
        BindButtons();
        onContinue = continueCallback;

        if (weeklyReportPanel == null)
        {
            continueCallback?.Invoke();
            return;
        }

        weeklyReportPanel.transform.localScale = Vector3.one;
        weeklyReportPanel.transform.SetAsLastSibling();
        weeklyReportPanel.SetActive(true);

        ApplyRuntimeReport(
            Mathf.Max(1, weekNumber),
            Mathf.Max(1, daysPerWeek),
            weekRecords ?? new List<StampRecord>(),
            weekDayStats ?? new List<DayStampStats>(),
            reputation,
            maxReputation,
            spLevel);
    }

    [ContextMenu("Refresh Edit Mode Preview")]
    public void RefreshEditModePreview()
    {
        if (Application.isPlaying || !previewInEditMode || weeklyReportPanel == null) return;

        int total = Mathf.Max(1, previewTotalProducts);
        int correct = Mathf.Clamp(Mathf.RoundToInt(total * previewAccuracy / 100f), 0, total);
        int wrong = total - correct;
        int startDay = (previewWeek - 1) * 7 + 1;
        int endDay = startDay + 6;

        SetHeader(previewWeek, startDay, endDay);
        SetText(weeklySummaryText,
            $"Produk diperiksa : {total}\n" +
            $"Keputusan benar : {correct}    Salah : {wrong}\n" +
            $"Akurasi mingguan : {previewAccuracy}%\n" +
            "Hari terbaik      : Hari " + endDay + "\n" +
            "Rata-rata harian  : " + previewAccuracy + "%");

        SetStatus(previewAccuracy, previewReputation, previewSPLevel);
        SetText(weeklyZatBerbahayaText,
            "TEMUAN INVESTIGASI\n" +
            "2 produk bermasalah diperiksa\n" +
            "2 berhasil ditolak • 0 lolos");
        SetText(weeklyProdukListText,
            "CATATAN KESALAHAN\n" +
            "• Contoh produk salah cap\n" +
            "• Data ini hanya preview Edit Mode");
        SetText(weeklyMotivationText, BuildMotivation(previewAccuracy, previewSPLevel));
        UpdateRatingVisuals(GetStarRating(previewAccuracy), previewSPLevel);
    }

    private void ApplyRuntimeReport(
        int weekNumber,
        int daysPerWeek,
        List<StampRecord> records,
        List<DayStampStats> dayStats,
        int reputation,
        int maxReputation,
        int spLevel)
    {
        int startDay = (weekNumber - 1) * daysPerWeek + 1;
        int endDay = startDay + daysPerWeek - 1;
        SetHeader(weekNumber, startDay, endDay);

        int totalPelanggan = dayStats.Sum(s => s.totalPelanggan);
        int totalBenar = dayStats.Sum(s => s.TotalBenar);
        int totalSalah = dayStats.Sum(s => s.TotalSalah);
        int stampBenar = dayStats.Sum(s => s.stampBenar);
        int stampSalah = dayStats.Sum(s => s.stampSalah);
        int tolakBenar = dayStats.Sum(s => s.tolakBenar);
        int tolakSalah = dayStats.Sum(s => s.tolakSalah);
        int timeout = dayStats.Sum(s => s.tidakDijawab);

        if (totalPelanggan <= 0 && records.Count > 0)
        {
            totalPelanggan = records.Count;
            totalBenar = records.Count(r => r.isCorrect);
            totalSalah = totalPelanggan - totalBenar;
            stampBenar = records.Count(r => r.wasStamped && r.isCorrect);
            stampSalah = records.Count(r => r.wasStamped && !r.isCorrect);
            tolakBenar = records.Count(r => r.wasRejected && r.isCorrect);
            tolakSalah = records.Count(r => r.wasRejected && !r.isCorrect);
            timeout = records.Count(r => r.isTimeout);
        }

        int accuracy = totalPelanggan > 0
            ? Mathf.RoundToInt(totalBenar * 100f / totalPelanggan)
            : 0;

        List<DayStampStats> activeDays = dayStats.Where(s => s.totalPelanggan > 0).ToList();
        int averageDaily = activeDays.Count > 0
            ? Mathf.RoundToInt((float)activeDays.Average(s => s.AkurasiPersen))
            : accuracy;
        DayStampStats bestDay = activeDays.OrderByDescending(s => s.AkurasiPersen).FirstOrDefault();
        DayStampStats worstDay = activeDays.OrderBy(s => s.AkurasiPersen).FirstOrDefault();

        string bestText = bestDay != null ? $"H{bestDay.day} ({bestDay.AkurasiPersen}%)" : "-";
        string worstText = worstDay != null ? $"H{worstDay.day} ({worstDay.AkurasiPersen}%)" : "-";

        SetText(weeklySummaryText,
            $"Produk diperiksa : {totalPelanggan}\n" +
            $"Cap benar/salah  : {stampBenar} / {stampSalah}\n" +
            $"Tolak benar/salah: {tolakBenar} / {tolakSalah}   Timeout: {timeout}\n" +
            $"Akurasi           : {accuracy}% ({totalBenar}/{Mathf.Max(0, totalPelanggan)})\n" +
            $"Terbaik {bestText} • Terendah {worstText} • Rata-rata {averageDaily}%");

        int reputationPercent = maxReputation > 0
            ? Mathf.RoundToInt(Mathf.Clamp01(reputation / (float)maxReputation) * 100f)
            : 0;
        SetStatus(accuracy, reputationPercent, spLevel);
        SetText(weeklyZatBerbahayaText, BuildHazardText(records));
        SetText(weeklyProdukListText, BuildMistakeText(records));
        SetText(weeklyMotivationText, BuildMotivation(accuracy, spLevel));
        UpdateRatingVisuals(GetStarRating(accuracy), spLevel);
    }

    private void SetHeader(int weekNumber, int startDay, int endDay)
    {
        SetText(weeklyTitleText, $"LAPORAN MINGGUAN {weekNumber}");
        SetText(weeklyPeriodText, $"PERIODE HARI {startDay}–{endDay}");
    }

    private void SetStatus(int accuracy, int reputationPercent, int spLevel)
    {
        int safeSP = Mathf.Clamp(spLevel, 0, 3);
        int stayChance = Mathf.Clamp(Mathf.RoundToInt(accuracy * 0.65f + reputationPercent * 0.35f), 0, 100);
        int remaining = Mathf.Max(0, 3 - safeSP);
        string status = safeSP == 0 ? "STAY / AMAN" : safeSP >= 3 ? "SP 3 / DIBERHENTIKAN" : $"SP {safeSP} / PERINGATAN";

        SetText(weeklyStatusText,
            $"STATUS: {status}\n" +
            $"Peluang bertahan : {stayChance}%\n" +
            $"Reputasi         : {reputationPercent}%\n" +
            $"Kesempatan sebelum SP 3: {remaining}");
    }

    private string BuildHazardText(List<StampRecord> records)
    {
        List<HazardousSubstanceSummary> hazardous = BuildHazardousSummary(records);
        if (hazardous.Count == 0)
        {
            return "TEMUAN INVESTIGASI\nTidak ada produk berbahaya pada pemeriksaan minggu ini.";
        }

        int found = hazardous.Sum(h => h.jumlahDitemukan);
        int rejected = hazardous.Sum(h => h.ditolakBenar);
        int escaped = hazardous.Sum(h => h.lolosSalah);
        string substances = string.Join(", ", hazardous
            .OrderByDescending(h => h.level)
            .Take(3)
            .Select(h => h.zat));

        return "TEMUAN INVESTIGASI\n" +
               $"Ditemukan {found} • Ditolak {rejected} • Lolos {escaped}\n" +
               $"Jenis: {substances}";
    }

    private string BuildMistakeText(List<StampRecord> records)
    {
        List<StampRecord> mistakes = records.Where(r => !r.isCorrect).Take(maxDisplayedMistakes).ToList();
        if (mistakes.Count == 0)
        {
            return "CATATAN KESALAHAN\nTidak ada kesalahan. Pertahankan ketelitian Anda.";
        }

        StringBuilder builder = new StringBuilder("CATATAN KESALAHAN\n");
        foreach (StampRecord record in mistakes)
        {
            string action = record.isTimeout ? "TIMEOUT" : record.wasRejected ? "TOLAK" : $"CAP {record.appliedLevel}";
            builder.Append("• H").Append(record.day).Append(' ')
                .Append(record.namaProduk).Append(" — ").Append(action).AppendLine();
        }

        int remaining = records.Count(r => !r.isCorrect) - mistakes.Count;
        if (remaining > 0) builder.Append("• +").Append(remaining).Append(" kesalahan lainnya");
        return builder.ToString().TrimEnd();
    }

    private string BuildMotivation(int accuracy, int spLevel)
    {
        if (spLevel >= 3)
            return "Evaluasi berakhir di SP 3. Pelajari kembali acuan GGL dan mulai lebih teliti pada percobaan berikutnya.";
        if (spLevel == 2)
            return "Kondisi kritis, tetapi belum terlambat. Periksa GGL, investigasi data mencurigakan, dan jangan terburu-buru memberi cap.";
        if (spLevel == 1)
            return "Anda mendekati batas SP. Tetap tenang—utamakan ketelitian pada produk berisiko dan gunakan investigasi dengan bijak.";
        if (accuracy >= 90)
            return "Kinerja sangat baik tanpa SP. Pertahankan ketelitian dan konsistensi pada minggu berikutnya!";
        if (accuracy >= 75)
            return "Tidak ada SP minggu ini. Kinerja sudah baik; jaga ritme dan periksa kembali keputusan yang meragukan.";
        return "Anda masih aman dari SP. Tingkatkan ketelitian sedikit demi sedikit—setiap keputusan benar menjaga reputasi.";
    }

    private int GetStarRating(int accuracy)
    {
        if (accuracy >= 90) return 5;
        if (accuracy >= 80) return 4;
        if (accuracy >= 70) return 3;
        if (accuracy >= 55) return 2;
        return 1;
    }

    private void UpdateRatingVisuals(int starCount, int spLevel)
    {
        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] == null) continue;
                starImages[i].gameObject.SetActive(true);
                starImages[i].color = i < starCount ? filledStarColor : emptyStarColor;
            }
        }

        if (warningImages != null)
        {
            int warningCount = Mathf.Clamp(spLevel, 0, warningImages.Length);
            for (int i = 0; i < warningImages.Length; i++)
            {
                if (warningImages[i] == null) continue;
                warningImages[i].color = warningColor;
                warningImages[i].gameObject.SetActive(i < warningCount);
            }
        }
    }

    private static void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null) target.text = value;
    }

    private List<HazardousSubstanceSummary> BuildHazardousSummary(List<StampRecord> records)
    {
        Dictionary<string, HazardousSubstanceSummary> map = new Dictionary<string, HazardousSubstanceSummary>();
        foreach (StampRecord record in records)
        {
            if (!record.HasZatBerbahaya) continue;
            string key = string.IsNullOrEmpty(record.kandunganBerbahaya)
                ? (record.adaZatTerlarang ? "Zat Terlarang" : "Pemalsuan Data")
                : record.kandunganBerbahaya.Trim();

            if (!map.TryGetValue(key, out HazardousSubstanceSummary summary))
            {
                summary = new HazardousSubstanceSummary
                {
                    zat = key,
                    level = record.levelZatBerbahaya,
                    hariPertama = record.day,
                    hariTerakhir = record.day
                };
                map[key] = summary;
            }

            summary.jumlahDitemukan++;
            if (record.levelZatBerbahaya > summary.level) summary.level = record.levelZatBerbahaya;
            summary.hariPertama = Mathf.Min(summary.hariPertama, record.day);
            summary.hariTerakhir = Mathf.Max(summary.hariTerakhir, record.day);
            if (record.wasRejected && record.isCorrect) summary.ditolakBenar++;
            if ((!record.wasRejected && !record.isTimeout && !record.isCorrect) ||
                (record.isTimeout && !record.isCorrect))
            {
                summary.lolosSalah++;
            }
        }

        return map.Values.ToList();
    }
}

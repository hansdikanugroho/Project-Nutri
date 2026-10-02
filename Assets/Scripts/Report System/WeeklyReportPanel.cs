using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeeklyReportPanel : MonoBehaviour
{
    [Header("Weekly Panel Refs")]
    public GameObject weeklyReportPanel;
    public TextMeshProUGUI weeklyTitleText;
    public TextMeshProUGUI weeklyPeriodText;
    public TextMeshProUGUI weeklySummaryText;
    public TextMeshProUGUI weeklyZatBerbahayaText;
    public TextMeshProUGUI weeklyProdukListText;
    public Button weeklyContinueButton;
    public Button weeklyCloseButton;

    private System.Action onContinue;

    void Awake()
    {
        if (weeklyContinueButton != null) weeklyContinueButton.onClick.AddListener(HandleContinue);
        if (weeklyCloseButton != null) weeklyCloseButton.onClick.AddListener(HandleContinue);
        if (weeklyReportPanel != null) weeklyReportPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (weeklyContinueButton != null) weeklyContinueButton.onClick.RemoveListener(HandleContinue);
        if (weeklyCloseButton != null) weeklyCloseButton.onClick.RemoveListener(HandleContinue);
    }

    private void HandleContinue()
    {
        if (weeklyReportPanel != null) weeklyReportPanel.SetActive(false);
        onContinue?.Invoke();
    }

    public void Show(int weekNumber, List<StampRecord> weekRecords, List<DayStampStats> weekDayStats, System.Action continueCallback)
    {
        onContinue = continueCallback;
        if (weeklyReportPanel == null) { continueCallback?.Invoke(); return; }

        weeklyReportPanel.transform.localScale = Vector3.one;
        weeklyReportPanel.transform.SetAsLastSibling();
        weeklyReportPanel.SetActive(true);

        int startDay = (weekNumber - 1) * 7 + 1;
        int endDay = startDay + 7 - 1;

        if (weeklyTitleText != null) weeklyTitleText.text = $"LAPORAN MINGGUAN {weekNumber}";
        if (weeklyPeriodText != null) weeklyPeriodText.text = $"Hari {startDay} - {endDay}";

        int totalPelanggan = weekDayStats.Sum(s => s.totalPelanggan);
        int totalBenar = weekDayStats.Sum(s => s.TotalBenar);
        int totalSalah = weekDayStats.Sum(s => s.TotalSalah);
        int stampBenar = weekDayStats.Sum(s => s.stampBenar);
        int stampSalah = weekDayStats.Sum(s => s.stampSalah);
        int tolakBenar = weekDayStats.Sum(s => s.tolakBenar);
        int tolakSalah = weekDayStats.Sum(s => s.tolakSalah);
        int timeout = weekDayStats.Sum(s => s.tidakDijawab);
        int akurasi = totalPelanggan > 0 ? Mathf.RoundToInt(totalBenar * 100f / totalPelanggan) : 0;

        if (weeklySummaryText != null)
        {
            weeklySummaryText.text =
                $"Total Produk Diperiksa: {totalPelanggan}\n" +
                $"Cap Benar: {stampBenar} | Cap Salah: {stampSalah}\n" +
                $"Tolak Benar: {tolakBenar} | Tolak Salah: {tolakSalah} | Timeout: {timeout}\n" +
                $"Akurasi Mingguan: {akurasi}% ({totalBenar}/{totalPelanggan})";
        }

        var hazardous = BuildHazardousSummary(weekRecords);
        if (weeklyZatBerbahayaText != null)
        {
            if (hazardous.Count == 0)
            {
                weeklyZatBerbahayaText.text = "<color=#55FF55>Tidak ada zat berbahaya terdeteksi minggu ini.</color>";
            }
            else
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"<b>{hazardous.Count} Jenis Zat Berbahaya Ditemukan:</b>");
                foreach (var h in hazardous.OrderByDescending(x => x.level).ThenByDescending(x => x.jumlahDitemukan))
                {
                    string color = h.LevelHex();
                    string status = h.lolosSalah > 0 ? $"<color=#FF4444>LOLOS {h.lolosSalah}</color>" : $"<color=#55FF55>Semua Ditolak</color>";
                    sb.AppendLine($"- {h.zat} <color={color}>[{h.LevelText()}]</color> x{h.jumlahDitemukan} | Tolak Benar {h.ditolakBenar} | {status}");
                }
                int lolosTotal = hazardous.Sum(x => x.lolosSalah);
                if (lolosTotal > 0) sb.AppendLine($"\n<color=#FF4444>PERINGATAN: {lolosTotal} produk berbahaya LOLOS cap!</color>");
                weeklyZatBerbahayaText.text = sb.ToString();
            }
        }

        if (weeklyProdukListText != null)
        {
            if (weekRecords.Count == 0)
            {
                weeklyProdukListText.text = "-";
            }
            else
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"<b>Riwayat {weekRecords.Count} Produk Dicapat:</b>");
                foreach (var r in weekRecords)
                {
                    string ikon = r.isCorrect ? "<color=#55FF55>✓</color>" : "<color=#FF4444>✗</color>";
                    string aksi = r.wasRejected ? "TOLAK" : r.isTimeout ? "TIMEOUT" : $"CAP {r.appliedLevel}";
                    string zat = r.HasZatBerbahaya ? $" | Zat: {r.KeteranganZat()} <color={GetLevelHex(r.levelZatBerbahaya)}>({r.levelZatBerbahaya})</color>" : "";
                    sb.AppendLine($"{ikon} H{r.day} {r.namaProduk} [{aksi}] -> {(r.isCorrect ? "BENAR" : "SALAH")}{zat}");
                }
                weeklyProdukListText.text = sb.ToString();
            }
        }
    }

    private List<HazardousSubstanceSummary> BuildHazardousSummary(List<StampRecord> records)
    {
        Dictionary<string, HazardousSubstanceSummary> map = new Dictionary<string, HazardousSubstanceSummary>();
        foreach (var r in records)
        {
            if (!r.HasZatBerbahaya) continue;
            string key = string.IsNullOrEmpty(r.kandunganBerbahaya)
                ? (r.adaZatTerlarang ? "Zat Terlarang" : "Pemalsuan Data")
                : r.kandunganBerbahaya.Trim();
            if (!map.TryGetValue(key, out var h))
            {
                h = new HazardousSubstanceSummary { zat = key, level = r.levelZatBerbahaya, jumlahDitemukan = 0, hariPertama = r.day, hariTerakhir = r.day };
                map[key] = h;
            }
            h.jumlahDitemukan++;
            if (r.levelZatBerbahaya > h.level) h.level = r.levelZatBerbahaya;
            h.hariPertama = Mathf.Min(h.hariPertama, r.day);
            h.hariTerakhir = Mathf.Max(h.hariTerakhir, r.day);
            bool shouldBeRejected = r.HasZatBerbahaya;
            bool wasRejectedCorrectly = r.wasRejected && r.isCorrect;
            bool wasStampedWrongly = !r.wasRejected && !r.isTimeout && shouldBeRejected && !r.isCorrect;
            bool wasTimeoutWrongly = r.isTimeout && shouldBeRejected;
            if (wasRejectedCorrectly) h.ditolakBenar++;
            if (wasStampedWrongly || wasTimeoutWrongly) h.lolosSalah++;
        }
        return map.Values.ToList();
    }

    private string GetLevelHex(DangerLevel level)
    {
        switch (level) { case DangerLevel.Tinggi: return "#FF4444"; case DangerLevel.Sedang: return "#FFA500"; default: return "#CCCCCC"; }
    }
}

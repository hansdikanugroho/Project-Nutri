using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StampRecord
{
    public int day;
    public string namaProduk;
    public bool wasStamped;
    public bool wasRejected;
    public bool isTimeout;
    public NutriLevel appliedLevel;
    public NutriLevel expectedLevel;
    public bool isCorrect;
    public bool wasPemalsuan;
    public string kandunganBerbahaya;
    public DangerLevel levelZatBerbahaya;
    public bool adaZatTerlarang;

    public bool HasZatBerbahaya
    {
        get
        {
            if (adaZatTerlarang) return true;
            if (wasPemalsuan) return true;
            return !string.IsNullOrEmpty(kandunganBerbahaya);
        }
    }

    public string KeteranganZat()
    {
        if (!string.IsNullOrEmpty(kandunganBerbahaya)) return kandunganBerbahaya;
        if (adaZatTerlarang) return "Zat Terlarang";
        if (wasPemalsuan) return "Pemalsuan Data";
        return "-";
    }
}

[Serializable]
public class DayStampStats
{
    public int day;
    public int totalPelanggan;
    public int stampBenar;
    public int stampSalah;
    public int tolakBenar;
    public int tolakSalah;
    public int tidakDijawab;

    public int TotalCap => stampBenar + stampSalah;
    public int TotalBenar => stampBenar + tolakBenar;
    public int TotalSalah => stampSalah + tolakSalah + tidakDijawab;

    public int AkurasiPersen
    {
        get
        {
            if (totalPelanggan <= 0) return 0;
            return Mathf.RoundToInt(TotalBenar * 100f / totalPelanggan);
        }
    }
}

[Serializable]
public class HazardousSubstanceSummary
{
    public string zat = "-";
    public DangerLevel level = DangerLevel.TidakAda;
    public int jumlahDitemukan;
    public int ditolakBenar;
    public int lolosSalah;
    public int hariPertama;
    public int hariTerakhir;

    public string LevelText()
    {
        return level == DangerLevel.TidakAda ? "TIDAK ADA" : level.ToString().ToUpper();
    }

    public string LevelHex()
    {
        switch (level)
        {
            case DangerLevel.Tinggi: return "#FF4444";
            case DangerLevel.Sedang: return "#FFA500";
            default: return "#CCCCCC";
        }
    }
}
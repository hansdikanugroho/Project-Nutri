# Sistem Investigasi (Pemalsuan Produk)

Dokumen ini menjelaskan cara setup UI untuk mekanik investigasi produk palsu
di scene `Assets/Scenes/Main Dev.unity`.

---

## 1. Pakai UI apa?

**UI biasa (uGUI) — Canvas + TextMeshPro + Button.** Jangan pakai UI Toolkit.

Alasannya, semua yang sudah ada di scene ini memang uGUI:

| Komponen | Status di scene |
|---|---|
| Canvas `Customer_Visual` | ada (`m_RenderMode: 0` = Screen Space Overlay) |
| `CanvasScaler` | ada |
| `GraphicRaycaster` | ada → **Button langsung jalan** |
| `EventSystem` + `InputSystemUIInputModule` | ada |
| `CanvasGroup` = `paperUI` | ada, dipakai untuk fade |

Artinya: begitu kamu taruh `Button` di bawah canvas `Customer_Visual`, dia
langsung bisa diklik. Tidak perlu setup apa-apa.

Kalau pakai UI Toolkit, kamu harus bikin asset `PanelSettings` baru, dan
`paperUI` (CanvasGroup) tidak bisa mengelolanya — hasilnya dua sistem
UI jalan bersamaan di satu layar. Tidak sepadan dengan hasilnya.

---

## 2. Script ditaruh di mana?

Script **tidak** perlu file baru. Semua logika investigasi sudah ada di
`CustomerManager.cs`.

```
Assets/Scripts/
├── Customer Manager/
│   ├── CustomerManager.cs      ← OnInvestigateButtonClicked(), RevealTrueData()
│   ├── PaperRevealEffect.cs    ← coret/typewriter + shake
│   └── README.md               ← dokumen ini
├── Product SO/
│   └── ProductData.cs          ← fieldgist data produk
├── GameManager.cs              ← timer, hasilinspection
├── GGLIntroController.cs       ← contoh pola button + listener
└── GGLUIController.cs
```

**Konvensi project:** satu folder per sistem di bawah `Assets/Scripts/`.
Script yang sifatnya global/singleton diletakkan langsung di root
(`GameManager.cs`, `GGLIntroController.cs`, `GGLUIController.cs`).

Kalau nanti investigasi sudah terlalu panjang untuk ditabung di
`CustomerManager.cs`, pecah ke `Assets/Scripts/Customer Manager/InvestigationController.cs`
— tapi **jangan** bikin folder/scripts UI terpisah, karena logicanya masih
ber State's `currentProduct` yang private di `CustomerManager`.

Untuk referensi cara wiring button, lihat `GGLIntroController.cs:19` —
pola `button.onClick.AddListener(...)` di `Awake()`, dilepas lagi di
`OnDestroy()`.

---

## 3. Struktur panel kertas sekarang

Objek `GGL` (ayah dari semua TMP kertas) berisi pasangan **label + value**:

| Posisi Y | Label (kiri, x≈-491) | Value (kanan, x≈-200) |
|---|---|---|
| +16 | `Nama Produk (1)` → "Nama Produk :" | `Nama Produk` → "Produk" |
| -44 | `Jenis Produk` → "Jenis Produk :" | `Jenis Produk (1)` → "Makanan Ringan" |
| -105 | `Produsen` → "Produsen :" | `Produsen (1)` → "Asal Produsen" |
| -216 | `Gula (1)` → "Gula" | `Gula` → "Gula" |
| -265 | `Lemak (1)` → "Garam" | `Lemak` → "Garam" |
| -315 | `Garam  (1)` → "Lemak" | `'Garam '` → "Lemak" |

> ⚠️ **Nama GameObject tidak reliable.** Orang yang bikin UI sudah
> salah ketik dan me-*rename* objeknya, tapi tidak mengganti teksnya.
> **Selalu lihat isi teksnya (`Inspector > Text Mesh Pro UGUI > Text`),
> bukan nama objek.**

---

## 4. Yang harus kamu kerjakan di Inspector

### 4a. Fix bug: `garamText` dan `lemakText` tertukar

Di GameObject `Customer Manager`, dua field ini sekarang tertukar:

```
gulaText    -> Gula       (benar, isi "Gula")
garamText   -> 'Garam '   ❌ field ini berlabel "Lemak"
lemakText   -> Lemak      ❌ field ini berlabel "Garam"
```

Efeknya di game: angka yang muncul di baris "Garam" sebenarnya nilai lemak,
dan sebaliknya. Player bakal salah menilai.

**Cara fix:** di Inspector `Customer Manager`, tukar drag-drop antara
`Garam Text` dan `Lemak Text` supaya:

```
garamText   -> Lemak      (value di baris berlabel "Garam")
lemakText   -> 'Garam '   (value di baris berlabel "Lemak")
```

### 4b. Isi 3 reference yang masih kosong

Di `Customer Manager`, tiga field ini masih `{fileID: 0}` (belum diisi):

| Field Inspector | Taruh ke GameObject |
|---|---|
| `Produsen Text` | `Produsen (1)` (teks "Asal Produsen") |
| `Kategori Text` | `Jenis Produk (1)` (teks "Makanan Ringan") |
| `Kandungan Berbahaya Text` | *(buat baru, lihat 4c)* |

### 4c. Bikin field "Zat Terlarang"

Buat satu TMP baru:

1. Duplicate `Gula`, taruh sebagai anak `GGL`.
2. Ganti nama jadi `Zat Terlarang`.
3. Isi teks placeholder: `"ZAT TERLARANG :"`.
4. Font color → merah, `m_IsActive` → **uncheck** (start hidden).
5. Drag ke `Kandungan Berbahaya Text`.

Positioning: taruh di bawah baris "Lemak" (y ≈ -365), atau di atas kalau
mau lebih menonjol. Yang penting barisnya kelihatan.

Field ini **hanya muncul setelah investigasi** (lihat §5).

### 4d. Bikin tombol Investigasi

Tombol ini **belum ada sama sekali** di scene. Butuh:

1. GameObject baru `Investigate` di bawah canvas `Customer_Visual`.
2. Komponen: `Image` + `Button` (atau `TextMeshProUGUI` + `Button`).
3. Hook ke `Customer Manager`:

```
Button (Investigate) > On Click ()
  └─ Customer Manager.OnInvestigateButtonClicked()
```

> Formulir kertas (`GGL`) **tidak** ikut ter-fade kalau `paperUI` di-fade,
> karena `GGL` ternyata bukan anak dari `paperContainer` (`kertas`).
> Kalau kamu mau tombolnya ikut hilang saat transisi, taruh `Investigate`
> sebagai anak dari objek yang sama dengan `GGL`.

---

## 5. Alur runtime

```
CustomerManager.SpawnSequence()
  └─ produk punya fungusIntroMessage? → broadcast Fungus, stop
  └─ tidak → ShowPaperAndStartTimer()
       ├─ isi namaProduk / produsen / kategori / gula / garam / lemak
       └─ kandunganBerbahayaText → teks "" + SetActive(false)

tombol Investigasi diklik
  └─ OnInvestigateButtonClicked()
       ├─ sudah pernah investigasi ATAU sudah dicap? → return
       ├─ isPemalsuan == true  ATAU  kandunganBerbahaya terisi
       │    ├─ RevealTrueData()   ← efek coret + typewriter + shake + SFX
       │    └─ Fungus broadcast (kalau ada fungusFakeRevealMessage)
       └─ keduanya kosong
            ├─ sprite customer jadi marah
            └─ timer -5 detik
```

`RevealTrueData()` dipanggil **sebelum** broadcast Fungus, jadi dialog tetap
jalan walau `fungusFakeRevealMessage` kosong.

Field `kandunganBerbahayaText` di-`SetActive(false)` lagi di `SetPaperTextVisible(false)`
supaya tidak nyangkut dari pelanggan sebelumnya.

### Kenapa angka Gula/Garam/Lemak tidak berubah untuk produk asli?

Untuk produk yang **tidak dipalsukan** tapi mengandung zat berbahaya,
`gulaAsli` / `garamAsli` / `lemakAsli` masih `0` (tidak diisi di Inspector).
Jadi animasi typewriter hanya jalan kalau `isPemalsuan == true`:

```csharp
if (currentProduct.isPemalsuan)   // hanya kalau data memang dipalsukan
{
    SpawnRevealField(gulaText, currentProduct.gula + "g", currentProduct.gulaAsli + "g");
    ...
}
```

Kalau tidak dijaga, angka yang sudah benar akan tertimpa `0g` saat produk
asli diinvestigasi.

---

## 6. Efek reveal (coret + typewriter + shake)

| File | Isi |
|---|---|
| `Customer Manager/PaperRevealEffect.cs` | `RevealField()` coret lama → ketik baru, `Shake()` getar kertas |
| `Customer Manager/CustomerManager.cs` | orkestrasi + field config |

Field baru di Inspector `Customer Manager` (header **Reveal Effect**):

| Field | Default | Fungsi |
|---|---|---|
| `Shake Target` | *(wajib)* | RectTransform yang digetar. **Harus** `GGL` atau panel pembungkus |
| `Reveal SFX` | *(opsional)* | AudioClip, diputar sekali saat reveal |
| `Reveal Strike Delay` | `0.35` | jeda sebelum angka lama dihapus |
| `Reveal Chars Per Second` | `18` | kecepatan ketik angka baru |
| `Shake Duration` | `0.4` | lama getar (detik) |
| `Shake Magnitude` | `12` | besar getar (piksel), `0` = matikan |

Semua opsional — kalau `Shake Target` dan `Reveal SFX` kosong, reveal tetap jalan
tanpa getar dan tanpa suara.

> ⚠️ `Shake Target` **wajib diisi**. Kalau dibiarkan `None`, efeknya hanya
> strike-through + typewriter.
>
> Arahkan ke `GGL` — lalu efeknya konsisten dengan isi kertas, dan
> `anchoredPosition` `GGL` dikembalikan lagi setelah getar selesai.

Kalau player sudah cap sebelum animasi selesai, animasi dihentikan bersih
di `EndCustomerSequence()` → `StopRevealRoutine()`, dan posisi `Shake Target`
dikembalikan ke posisi awal supaya tidak tertinggal geser.

### Rich text

Baris "ZAT TERLARANG" memakai tag warna TMP:

```
ZAT TERLARANG: Boraks  <color=#FF4444>TINGGI</color> — Gangguan saluran pencernaan
```

`#FF4444` = Tinggi, `#FFA500` = Sedang. Kalau tag-nya tampil mentah
(`<color=...>`) artinya **Rich Text** belum dicentang di
`Inspector > Text Mesh Pro UGUI > Text Settings`.

---

## 7. Isi data di Inspector produk

`ProductData` (`Assets/Scripts/Product SO/`):

- **Product Information** → `Asal Produsen`, `Kategori Produk`
- **Data Pemalsuan (Investigasi)** → centang `Is Pemalsuan`, isi:
  - `Kandungan Berbahaya` — nama zat, mis. `"Boraks"`
  - `Dampak Zat Berbahaya` — mis. `"Gangguan saluran pencernaan"`
  - `Level Zat Berbahaya` — `TidakAda` / `Sedang` / `Tinggi`
- **Data Asli (Jika Pemalsuan)** → isi `Gula Asli` / `Garam Asli` / `Lemak Asli`
  (hanya diisi kalau `Is Pemalsuan` dicentang)

Kalau `Kandungan Berbahaya` kosong, baris "ZAT TERLARANG" tidak muncul
(wajib pakai `!string.IsNullOrEmpty` — sudah ditangani di `ShowHazardousSubstance()`).

### Dua jenis produk "(tidak) berbahaya"

| Setelan | Customer | Dampak investigasi |
|---|---|---|
| `isPemalsuan` ✅ | cemas | angka diubah ke data asli + efek coret/typewriter |
| `isPemalsuan` ❌ + `kandunganBerbahaya` ✅ | cemas | angka tetap, baris ZAT TERLARANG muncul |
| keduanya ❌ | marah | timer −5 detik |

Jadi penalti −5 detik sekarang berarti "kamu investigasi tapi tidak menemukan
apa-apa" — bukan "produk ini bersih".

---

## 8. Checklist sebelum test

- [ ] `garamText` / `lemakText` ditukar (§4a)
- [ ] `produsenText`, `kategoriText`, `kandunganBerbahayaText` diisi (§4b)
- [ ] GameObject `Zat Terlarang` dibuat, merah, start hidden (§4c)
- [ ] Tombol `Investigate` dibuat + hook ke `OnInvestigateButtonClicked()` (§4d)
- [ ] `Shake Target` diisi (opsional tapi disarankan) + cek **Rich Text** on di
      `kandunganBerbahayaText` (§6)
- [ ] SO 1: `isPemalsuan = true`, semua field terisi → harusnya angka berubah + shake
- [ ] SO 2: `isPemalsuan = false`, `kandunganBerbahaya = "Formalin"`, `Dampak` +
      `Level = Tinggi` → angka **tidak** berubah, baris merah muncul
- [ ] SO 3: semua kosong → customer marah, timer −5 detik
- [ ] Uji SO 1 **dengan** dan **tanpa** `fungusFakeRevealMessage` — dua-duanya
      harus tetap reveal
- [ ] Cap di tengah animasi → tidak boleh ada angka yang tertinggal `0g` atau
      kertas yang geser


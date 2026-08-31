# 🚀 Work Tracker Pro (TrackerKerja)

> **Platform Manajemen Tugas Kerja, Pelacakan Timesheet, Dokumentasi Teknis, & Analitik Kinerja Tim Terintegrasi**

![ASP.NET Core 8.0](https://img.shields.io/badge/ASP.NET%20Core-8.0-indigo.svg)
![Entity Framework Core](https://img.shields.io/badge/EF%20Core-SQLite-blue.svg)
![ClosedXML](https://img.shields.io/badge/ClosedXML-Excel%20Engine-emerald.svg)
![Multi-Device](https://img.shields.io/badge/Responsive-Mobile%20%26%20Tablet-purple.svg)

---

## 🌟 Fitur Utama

### 📱 1. Antarmuka Multi-Device & Mobile-Friendly
- **Off-Canvas Sidebar Drawer**: Sidebar desktop otomatis beralih menjadi drawer elegan dengan *backdrop blur* pada perangkat seluler & tablet.
- **Glassmorphic Bottom Navigation**: Navigasi bawah melayang khusus smartphone dengan 5 tombol aksi cepat (*Home*, *Tugas*, *Elevated Glowing Add +*, *Proyek*, *Menu*).
- **Mobile Segmented Kanban Switcher**: Tombol tab pil interaktif (`📋 Todo`, `🔄 In Progress`, `✅ Done`) pada smartphone untuk beralih kolom kanban tanpa scrolling vertikal panjang.
- **Formulir & Filter Responsif**: Grid adaptif 1-kolom di HP dan 2-kolom di tablet/desktop dengan *touch targets* yang lapang.

### 📊 2. Timesheet & Laporan Personal Excel (.xlsx)
- **Pelacakan Jam Kerja**: Pencatatan durasi kerja per tugas harian dan mingguan.
- **Laporan Timesheet Personal (ClosedXML)**:
  - Generate rekapitulasi jam kerja ke format Excel multi-sheet (.xlsx).
  - **Sheet 1 ("Timesheet Personal")**: Informasi metadata karyawan, tabel detail sesi, formula grand total otomatis `=SUM(...)`.
  - **Sheet 2 ("Rekap per Proyek")**: Ringkasan alokasi waktu kerja dan persentase kontribusi per proyek.
  - **Privasi Terisolasi**: Pengguna hanya dapat mengekstrak data pencatatan waktu miliknya sendiri.

### 🎨 3. Sistem Tema Dinamis (10 Tema)
- Pilihan 10 palet warna modern (*Indigo Violet*, *Oceanic Cyan*, *Emerald Forest*, *Sunset Orange*, *Rose Pink*, *Midnight Dark*, *Cyberpunk Neon*, *Royal Amethyst*, *Slate Minimal*, *Warm Amber*) yang dapat diganti secara instan dari topbar.

### 📋 4. Manajemen Tugas & Kanban Board
- **Hierarki Parenting**: Dukungan relasi tugas induk (*Parent Task*) dan sub-tugas (*Child Task*).
- **Kolom Kendala & Solusi**: Pencatatan hambatan operasional dan solusi pemecahan masalah teknis.
- **Progress 0–100%**: Slider interaktif dengan tombol preset cepat serta sinkronisasi otomatis status *Done*.
- **Drag & Drop Kanban**: Pembaruan status kartu secara real-time.

### 📁 5. Dokumentasi Kerja & Multi-File Storage
- **Rich Text Editor**: Editor Quill.js dengan format visual.
- **Struktur Folder Pengguna**: Lampiran multi-file disimpan rapi dan terisolasi di direktori `wwwroot/uploads/notes/{username}/`.

### 📑 6. Import & Export Excel Enterprise
- **Format Standar (9 Kolom)**: Dilengkapi pratinjau interaktif dan fitur penugasan PIC dinamis (*bulk reassign*).
- **Format ARMS Enterprise (21 Kolom)**: Ekspor dan impor tugas format spesifikasi ARMS.

### 🛡️ 7. Keamanan & Audit Trail
- **Role-Based Access Control (RBAC)**: Pemisahan hak akses antara Administrator dan Anggota Tim (*User*).
- **Global Audit Trail**: Pencatatan otomatis setiap aksi controller ke dalam database SQLite dengan visualisasi grafik aktivitas multi-series.

---

## 🛠️ Teknologi & Arsitektur

- **Framework**: ASP.NET Core 8.0 MVC (C#)
- **Database**: SQLite melalui Entity Framework Core 8.0
- **Autentikasi**: ASP.NET Core Identity
- **Styling**: Tailwind CSS & Custom CSS Token System (`themes.css`, `site.css`)
- **Library Client**: SortableJS, FullCalendar, Chart.js, Quill.js, FontAwesome 6
- **Engine Excel**: ClosedXML 0.104.2

---

## 🚀 Panduan Menjalankan Proyek

### 🐳 1. Menjalankan Menggunakan Docker (Rekomendasi)
Aplikasi telah dikemas siap pakai dengan Docker & Docker Compose:

```bash
# Jalankan container di background (auto-build & auto-migrate DB)
docker compose up -d --build

# Atau gunakan script helper di Windows PowerShell:
.\docker-run.ps1 up
```
Buka browser di:
- **Web App**: **`http://localhost:5000`**
- **Swagger REST API**: **`http://localhost:5000/swagger`**
- **Kredensial Default**: `admin@trackerkerja.com` / `Admin123!`

> 📖 Untuk panduan lengkap Docker, volume data, backup, dan troubleshooting, lihat **[DOCKER_GUIDE.md](file:///c:/TEMP/VSCODE/TrackerKerja/DOCKER_GUIDE.md)**.

---

### 💻 2. Menjalankan Mode Development (.NET SDK)
Pastikan [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) telah terinstal pada sistem.

```bash
# Jalankan aplikasi
dotnet run --urls=http://localhost:5000
```

---

### 📦 3. Mempublikasikan ke Release Build Standalone
```bash
# Publish aplikasi ke folder ./publish
dotnet publish -c Release -o ./publish

# Jalankan executable hasil publish
cd publish
dotnet TrackerKerja.dll --urls=http://localhost:5000
```

---

## 📚 Dokumentasi
- **Panduan Pengoperasian Docker**: 👉 **[DOCKER_GUIDE.md](file:///c:/TEMP/VSCODE/TrackerKerja/DOCKER_GUIDE.md)**
- **User Guide & Workflow**: 👉 **[USER_GUIDE.md](file:///c:/TEMP/VSCODE/TrackerKerja/USER_GUIDE.md)**
- **Spesifikasi Fungsional (FSD)**: 👉 **[FSD_WORK_TRACKER_PRO.md](file:///c:/TEMP/VSCODE/TrackerKerja/FSD_WORK_TRACKER_PRO.md)**


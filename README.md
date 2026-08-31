# 🚀 Work Tracker Pro (TrackerKerja)

> **Enterprise Work Task Management, Multi-Timer Timesheet Tracking, Technical Documentation, & Team Performance Analytics Platform**

[![ASP.NET Core 8.0](https://img.shields.io/badge/ASP.NET%20Core-8.0%20MVC-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-SQLite-blue?logo=sqlite&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![ClosedXML](https://img.shields.io/badge/ClosedXML-0.104.2-emerald?logo=microsoft-excel&logoColor=white)](https://github.com/ClosedXML/ClosedXML)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![REST API](https://img.shields.io/badge/REST%20API-OpenAPI%20%2F%20Swagger-85EA2D?logo=swagger&logoColor=black)](http://localhost:5000/swagger)
[![GitHub](https://img.shields.io/badge/GitHub-zhavick%2FTrackerKerja-181717?logo=github&logoColor=white)](https://github.com/zhavick/TrackerKerja.git)
[![Responsive UI](https://img.shields.io/badge/Responsive-Mobile%20%26%20Desktop-purple)](http://localhost:5000)

---

## 🌟 Fitur Utama Sistem

### 📱 1. Antarmuka Multi-Device & Mobile-Friendly
- **Off-Canvas Sidebar Drawer**: Sidebar desktop otomatis bertransformasi menjadi drawer elegan dengan *backdrop blur* pada layar smartphone & tablet.
- **Glassmorphic Bottom Navigation**: Navigasi bawah melayang khusus smartphone dengan 5 tombol aksi cepat (*Home*, *Tugas*, *Elevated Glowing Add +*, *Proyek*, *Menu*).
- **Mobile Segmented Kanban Switcher**: Tab pil interaktif (`📋 Todo`, `🔄 In Progress`, `✅ Done`) untuk kemudahan switching kolom tanpa scrolling vertikal panjang.
- **Formulir & Filter Responsif**: Grid adaptif 1-kolom di mobile dan 2-kolom di desktop dengan *touch target* yang nyaman.

### ⏱️ 2. Timesheet, Multi-Timer Serentak & Laporan Excel Personal
- **Multi-Timer Serentak per Pengguna**: Setiap pengguna dapat menjalankan timer pada beberapa tugas sekaligus tanpa saling menimpa (*active timer sync*).
- **Pencatatan Waktu Fleksibel**: Mendukung timer otomatis detik/menit dan input sesi manual.
- **Laporan Timesheet Personal (ClosedXML .xlsx)**:
  - **Sheet 1 ("Timesheet Personal")**: Metadata karyawan, tabel detail sesi waktu kerja, dan formula grand total otomatis `=SUM(...)`.
  - **Sheet 2 ("Rekap per Proyek")**: Alokasi jam kerja dan persentase kontribusi per proyek.
  - **Proteksi Privasi**: Pengguna hanya dapat mengakses dan mengunduh rekaman waktu miliknya sendiri.

### 📋 3. Manajemen Tugas, Parenting & Kanban Board
- **Hierarki Tugas**: Relasi tugas induk (*Parent Task*) dan sub-tugas (*Child Task*).
- **Kendala & Solusi**: Kolom khusus pencatatan hambatan operasional dan solusi pemecahan teknis.
- **Progress Interaktif 0–100%**: Slider interaktif dengan preset cepat (0%, 25%, 50%, 75%, 100%) serta sinkronisasi otomatis status *Done*.
- **Drag & Drop Kanban**: Pembaruan status kartu secara real-time bertenaga SortableJS.

### 📁 4. Dokumentasi Kerja & Penyimpanan Multi-File
- **Rich Text Editor**: Editor Quill.js untuk notula meeting, spesifikasi teknis, dan catatan tugas.
- **Struktur Folder Pengguna**: Lampiran multi-file diisolasi rapi di direktori `wwwroot/uploads/notes/{username}/`.
- **Note Pinning**: Kemampuan menyematkan catatan penting di bagian atas dashboard.

### 📑 5. Ekspor & Impor Excel Enterprise
- **Format Standar (9 Kolom)**: Ekspor/impor dengan fitur penugasan PIC dinamis (*bulk reassign*) dan filter periode/proyek.
- **Format ARMS Enterprise (21 Kolom)**: Integrasi penuh dengan template Waterfall SDLC Milestone (*Requirement Analysis*, *System Design*, *Implementation*, *Testing & QA*, *Deployment*, *Maintenance*).

### 👥 6. Manajemen Tim & Admin Password Reset
- **Direktori Anggota**: Kartu profil tim, statistik penyelesaian tugas, dan total jam kerja.
- **Admin Password Reset**: Administrator dapat mereset kata sandi anggota tim secara langsung dari antarmuka Web UI maupun REST API tanpa memerlukan konfirmasi email lama.

### 🛡️ 7. Keamanan, RBAC & Global Audit Trail
- **Role-Based Access Control (RBAC)**: Pemisahan hak akses antara Administrator dan Anggota Tim (*User*).
- **Global Audit Trail Filter**: Pencatatan otomatis setiap aksi controller ke database SQLite lengkap dengan visualisasi grafik aktivitas multi-series.

### 🌐 8. RESTful API & Swagger OpenAPI Documentation
- **70+ Endpoint RESTful**: Terintegrasi penuh untuk modul Auth, Tasks, Projects, Notes, Timesheet, Members, Calendar, Reports, Master Data, System Settings, dan Notifications.
- **Interactive Swagger UI**: Dokumentasi OpenAPI interaktif yang dapat langsung diuji pada [http://localhost:5000/swagger](http://localhost:5000/swagger).
- **Postman Collection & Environment**: Dilengkapi berkas siap pakai `TrackerKerja_Postman_Collection.json` dan `TrackerKerja_Postman_Environment.json`.

### 🎨 9. Sistem Tema Dinamis (16 Tema)
- **10 Tema Terang**: *Indigo Violet*, *Oceanic Cyan*, *Emerald Forest*, *Sunset Orange*, *Rose Pink*, *Midnight Dark*, *Cyberpunk Neon*, *Royal Amethyst*, *Slate Minimal*, *Warm Amber*.
- **6 Tema Gelap**: Terintegrasi instan dengan CSS custom tokens.

---

## 🛠️ Teknologi & Arsitektur

| Komponen | Teknologi | Keterangan |
| :--- | :--- | :--- |
| **Framework Backend** | ASP.NET Core 8.0 MVC & Web API | C# 12, Kestrel Web Server |
| **Database & ORM** | SQLite + Entity Framework Core 8.0 | Auto-migration & database seeder |
| **Containerization** | Docker & Docker Compose | Multi-stage build .NET 8, persistent volumes |
| **Engine Spreadsheet** | ClosedXML 0.104.2 | Format ARMS 21-kolom, Timesheet multi-sheet, Standard 9-kolom |
| **Dokumentasi API** | Swashbuckle OpenAPI (Swagger v3.1) | Interactive REST API explorer |
| **Styling & Theme** | Tailwind CSS + Custom CSS Variables | Dynamic token system (`themes.css`, `site.css`) |
| **Client Libraries** | SortableJS, FullCalendar, Chart.js, Quill.js | Interaktivitas UI modern |
| **Keamanan** | ASP.NET Core Identity & RBAC | Cookie authentication, Password hashing, Audit Filter |

---

## 🚀 Panduan Menjalankan Aplikasi

### 🐳 1. Menjalankan Menggunakan Docker (Sangat Disarankan)

Aplikasi telah dikemas siap pakai dengan Docker & Docker Compose. Database dan file upload tetap tersimpan secara persisten pada host machine.

```bash
# Menjalankan container di background (auto-build & auto-migrate DB)
docker compose up -d --build
```

#### Atau Gunakan Script Helper (PowerShell Windows):
```powershell
# Inisialisasi data lokal ke volume Docker
.\docker-run.ps1 init-data

# Jalankan container
.\docker-run.ps1 up

# Cek streaming log container
.\docker-run.ps1 logs

# Cek status
.\docker-run.ps1 status

# Hentikan container
.\docker-run.ps1 down
```

> 📖 Untuk panduan lengkap Docker, volume data, backup, dan troubleshooting, lihat **[DOCKER_GUIDE.md](file:///c:/TEMP/VSCODE/TrackerKerja/DOCKER_GUIDE.md)**.

---

### 💻 2. Menjalankan Mode Development (.NET SDK)
Pastikan [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) telah terinstal.

```bash
# Clone repository
git clone https://github.com/zhavick/TrackerKerja.git
cd TrackerKerja

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

## 🐙 Repositori GitHub & Push Helper

Repositori proyek: 👉 **[https://github.com/zhavick/TrackerKerja.git](https://github.com/zhavick/TrackerKerja.git)**

Untuk mengunggah kode terbaru ke GitHub menggunakan Personal Access Token (PAT):
```powershell
# Jalankan script helper push
.\git-push.ps1
```

---

## 🔐 Kredensial Akun Default

Saat pertama kali aplikasi dijalankan, database SQLite akan otomatis dibuat dan di-seed dengan akun default:

| Role | Email | Password Default | Akses |
| :--- | :--- | :--- | :--- |
| **Administrator** | `admin@trackerkerja.com` | `Admin123!` | Akses Penuh: Konfigurasi, Reset Password, Audit Trail, Master Data |
| **Project Lead** | `glenn.hakim@elistec.com` | `Password123!` | Manajemen Proyek, Tugas, Timesheet, Catatan |
| **QA Specialist** | `heni.rahayu@elistec.com` | `Password123!` | Pengujian, Kanban, Timesheet, Catatan |
| **Frontend Dev** | `haviz.indra@elistec.com` | `Password123!` | Tugas Frontend, Timesheet, Catatan |
| **Backend Dev** | `Iqbal.ali@elistec.com` | `Password123!` | Tugas Backend, Timesheet, Catatan |
| **DevOps Engineer** | `mohammad.danang@elistec.com` | `Password123!` | Tugas DevOps, Timesheet, Catatan |
| **System Analyst** | `syafix.said@elistec.com` | `Password123!` | Analisis Kebutuhan, Timesheet, Catatan |
| **Technical Writer** | `nanda.putri@elistec.com` | `Password123!` | Dokumentasi Teknis, Timesheet, Catatan |
| **Fullstack Dev** | `athallah.bariq@elistec.com` | `Password123!` | Fitur End-to-End, Timesheet, Catatan |

---

## 🌐 Endpoint & Akses Cepat

- **Web Dashboard**: [http://localhost:5000](http://localhost:5000)
- **Swagger REST API Documentation**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **OpenAPI JSON Spec**: [http://localhost:5000/swagger/v1/swagger.json](http://localhost:5000/swagger/v1/swagger.json)

---

## 📚 Referensi Dokumentasi Lengkap

- 🐳 **[DOCKER_GUIDE.md](file:///c:/TEMP/VSCODE/TrackerKerja/DOCKER_GUIDE.md)**: Panduan lengkap Docker, volume data, backup, dan perintah maintenance.
- 📖 **[USER_GUIDE.md](file:///c:/TEMP/VSCODE/TrackerKerja/USER_GUIDE.md)**: Panduan pengguna menyeluruh dengan alur kerja seluruh fitur dan modul.
- 📐 **[FSD_WORK_TRACKER_PRO.md](file:///c:/TEMP/VSCODE/TrackerKerja/FSD_WORK_TRACKER_PRO.md)**: Dokumen Spesifikasi Fungsional (FSD), arsitektur modul, diagram Mermaid, dan skema database ERD.

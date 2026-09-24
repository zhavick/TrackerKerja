# 🚀 Work Tracker Pro (TrackerKerja)

> **Enterprise Work Task Management, Multi-Timer Timesheet Tracking, Attendance Management, Technical Documentation, & Team Performance Analytics Platform (v3.6 Enterprise Security Edition)**

[![ASP.NET Core 8.0](https://img.shields.io/badge/ASP.NET%20Core-8.0%20MVC-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-SQLite-blue?logo=sqlite&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![ClosedXML](https://img.shields.io/badge/ClosedXML-0.104.2-emerald?logo=microsoft-excel&logoColor=white)](https://github.com/ClosedXML/ClosedXML)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![REST API](https://img.shields.io/badge/REST%20API-100%2B%20Endpoints-85EA2D?logo=swagger&logoColor=black)](http://localhost:5000/swagger)
[![JWT Bearer](https://img.shields.io/badge/Auth-Cookie%20%2B%20JWT%20Bearer-orange?logo=jsonwebtokens&logoColor=white)](http://localhost:5000/swagger)
[![Themes & Fonts](https://img.shields.io/badge/Themes%20%26%20Fonts-40%20Themes%20%7C%205%20Fonts-pink)](http://localhost:5000)
[![GitHub](https://img.shields.io/badge/GitHub-zhavick%2FTrackerKerja-181717?logo=github&logoColor=white)](https://github.com/zhavick/TrackerKerja.git)

---

## 🌟 Fitur Unggulan Sistem (v3.6)

### 🔐 1. Keamanan Enterprise & Autentikasi Ganda (Dual Auth)
- **Dual Authentication Pipeline**: Integrasi Cookie Session terenkripsi untuk peramban web dan **JWT Bearer Token** untuk RESTful API dan integrasi eksternal.
- **Strict Swagger JWT Authorization**: Dokumentasi OpenAPI/Swagger UI di `/swagger` dilengkapi tombol modal **Authorize** untuk menguji endpoint berotentikasi Bearer JWT.
- **Keamanan Sesi & Auto-Logout Inaktivitas 1 Jam (`session-manager.js`)**: Pemantauan idle real-time, dialog peringatan interaktif 5 menit dengan hitung mundur detik, dan proteksi redirect otomatis ke `/Account/Login?reason=timeout`.
- **Halaman Login Lottie Modern**: Redesain visual modern dengan animasi Lottie, toggle mode gelap/terang instan tanpa reload, dan dropdown perusahaan Select2.

### 🎨 2. 40 Tema Eye-Friendly & 5 Google Fonts Switcher
- **40 Tema Tampilan Dinamis**: 22 Tema Terang + 18 Tema Gelap ramah mata (*eye-friendly* dengan kontras seimbang) bertenaga CSS custom tokens (`themes.css`).
- **Global Font Switcher**: 5 opsi Google Fonts pilihan (*Inter, Plus Jakarta Sans, Outfit, Poppins, Roboto*) yang dapat diganti secara instan tanpa reload halaman dan tersimpan di `localStorage` (*Anti-FOUC*).
- **Tur Interaktif Layar (*Interactive Onboarding Tour*)**: 6 spotlight interaktif yang memandu pengguna baru memahami alur operasional aplikasi (`onboarding-tour.js`).
- **Paginasi Grid Tabel AJAX (*Zero Reload*)**: Navigasi tabel Tugas, Anggota Tim, Presensi, Audit Trail, dan Timesheet instan tanpa reload halaman (`ajax-grid-manager.js`).

### 👥 3. Direktori Anggota Tim, Banner Cover Profil & Hapus Akun Permanen
- **Pure Grid Card Layout**: Kartu anggota tim berstruktur grid responsif dengan proteksi anti-overflow (*text-ellipsis* dan tooltip hover).
- **Kustomisasi Banner Sampul Profil (`CoverPictureUrl`)**: Unggah gambar cover banner profil atau gunakan template vektor SVG default (`default-profile-cover.svg`).
- **Fitur Hapus Permanen Akun (*Permanent User Deletion*)**: Khusus peran Administrator dengan proteksi konfirmasi ganda verifikasi nama target dan kata sandi admin.
- **Admin Password Reset**: Fasilitas reset kata sandi langsung dari kartu anggota disertai notifikasi email otomatis.

### ⏱️ 4. Timesheet, Multi-Timer & Penyatuan Form Edit Tugas
- **Penyatuan Formulir Edit Tugas & Timesheet Manual (`SaveTaskAndSession`)**: Satu tombol simpan terpadu untuk memperbarui detail tugas dan mencatat sesi jam kerja manual baru sekaligus.
- **Multi-Timer Serentak**: Menjalankan beberapa timer tugas bersamaan tanpa saling mengganggu antar pengguna.
- **Laporan Excel Timesheet Resmi (ClosedXML)**: Ekspor multi-sheet dengan rincian harian, rekapitulasi per proyek, konversi Man-Days, dan formula otomatis.

### 🔄 5. Sinkronisasi Multi-Instance Host Induk & File Attachments
- **Online Push & Pull Sync dengan File Streaming**: Sinkronisasi transaksi database dan seluruh berkas fisik lampiran (`uploads/notes/*`, `uploads/avatars/*`, `uploads/covers/*`) menggunakan Base64 streaming melalui REST API.
- **Paket ZIP Offline Mandiri**: Ekspor dan impor paket arsip lengkap (`manifest.json`, `sync_data.sql`, folder `uploads/`) untuk instalasi jaringan tertutup (*air-gapped*).

### 📧 6. Integrasi Server Email (SMTP) & 7 Template Event
- **Konfigurasi SMTP Dinamis**: Pengaturan host mail, port, kredensial, dan SSL/TLS tersimpan di database tanpa perlu restart aplikasi.
- **Uji Koneksi Mandiri**: Live diagnostik handshake SMTP dan pengukuran latensi koneksi.
- **7 Template Email Event**: Template pendaftaran, persetujuan akun, penolakan, reset password, penugasan tugas, dan perubahan status dengan live HTML rendering preview.

### 📋 7. Manajemen Tugas, Proyek, Presensi & Developer Tools
- **Hierarki Parent-Child Tasks & Kanban Board SortableJS**.
- **Pencatatan Kendala (Obstacle) & Solusi (Solution)** untuk evaluasi sprint.
- **Presensi Terintegrasi (Check In/Out, WFH, Sakit, Izin, Cuti)** & Rekonsiliasi Tim.
- **Developer Tools Terpadu**: SQL Beautifier/Formatter mendukung 15+ dialek database dan JSON Payload Tools.
- **Ekspor/Impor Excel Ganda**: Format Standar 9-kolom dan Format ARMS 21-kolom.

---

## 🛠️ Teknologi & Arsitektur

| Komponen | Teknologi | Keterangan |
| :--- | :--- | :--- |
| **Framework Backend** | ASP.NET Core 8.0 MVC & Web API | C# 12, Kestrel Web Server, .NET 8 LTS |
| **Database & ORM** | SQLite + Entity Framework Core 8.0 | Auto-migration & Database Seeder |
| **Autentikasi & Keamanan** | Dual Auth: Cookie + JWT Bearer | Identity PBKDF2, Strict Swagger JWT, Inactivity Guard (60 min) |
| **Containerization** | Docker & Docker Compose | Multi-stage build, persistent data volumes (`./db_data`, `./uploads`) |
| **Engine Spreadsheet** | ClosedXML 0.104.2 | Format ARMS 21-kolom, Timesheet multi-sheet, Standard 9-kolom |
| **Dokumentasi API** | Swashbuckle OpenAPI (Swagger v3.1) | 100+ Endpoints terproteksi dengan Authorize Bearer Modal |
| **Styling & Theme** | Tailwind CSS + CSS Custom Tokens | 40 Dynamic Themes (22 Light + 18 Dark), 5 Google Fonts |
| **Client Libraries** | SortableJS, FullCalendar, Chart.js, Quill.js, Lottie | Interaktivitas UI modern dan responsif |

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

## 🌐 Endpoint & Akses Cepat

- **Web Dashboard**: [http://localhost:5000](http://localhost:5000)
- **Swagger REST API Documentation**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **OpenAPI JSON Spec**: [http://localhost:5000/swagger/v1/swagger.json](http://localhost:5000/swagger/v1/swagger.json)

---

## 📚 Referensi Dokumentasi Lengkap

### Format Markdown (.md)
- 📐 **[FSD_WORK_TRACKER_PRO.md](file:///c:/TEMP/VSCODE/TrackerKerja/FSD_WORK_TRACKER_PRO.md)**: Dokumen Spesifikasi Fungsional (FSD), arsitektur modul, diagram Mermaid, dan alur bisnis.
- 📘 **[TSD_WORK_TRACKER_PRO.md](file:///c:/TEMP/VSCODE/TrackerKerja/TSD_WORK_TRACKER_PRO.md)**: Dokumen Spesifikasi Teknis (TSD), controller & API retrieval procedures, arsitektur basis data, ERD, dan sample data.
- 📖 **[USER_GUIDE.md](file:///c:/TEMP/VSCODE/TrackerKerja/USER_GUIDE.md)**: Panduan pengguna menyeluruh dengan alur kerja seluruh fitur dan modul.
- 🐳 **[DOCKER_GUIDE.md](file:///c:/TEMP/VSCODE/TrackerKerja/DOCKER_GUIDE.md)**: Panduan lengkap Docker, volume data, backup, dan perintah maintenance.

### Format Microsoft Word (.docx - Tampilan Eksekutif & Profesional)
- 📄 **[FSD_WORK_TRACKER_PRO.docx](file:///c:/TEMP/VSCODE/TrackerKerja/FSD_WORK_TRACKER_PRO.docx)**: Dokumen FSD resmi berformat Microsoft Word dengan Cover Page, Running Header/Footer, dan Tabel Terformat.
- 📄 **[TSD_WORK_TRACKER_PRO.docx](file:///c:/TEMP/VSCODE/TrackerKerja/TSD_WORK_TRACKER_PRO.docx)**: Dokumen TSD resmi berformat Microsoft Word mencakup seluruh spesifikasi API, controller, prosedur, skema 21 tabel, dan sample data.
- 📄 **[USER_GUIDE_WORK_TRACKER_PRO.docx](file:///c:/TEMP/VSCODE/TrackerKerja/USER_GUIDE_WORK_TRACKER_PRO.docx)** (atau **[USER_GUIDE.docx](file:///c:/TEMP/VSCODE/TrackerKerja/USER_GUIDE.docx)**): Buku panduan operasional pengguna lengkap siap cetak/distribusi.

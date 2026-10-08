# QUALITY ASSURANCE (QA) - TRACKERKERJA (WORK TRACKER PRO v3.7)

Folder ini berisi dokumen dan artefak pengujian mutu (Quality Assurance), pelacakan eksekusi pengujian (*Test Execution Tracking*), repositori Test Case End-to-End (E2E), dan pencatatan temuan bug (*Defect Log*) untuk aplikasi **Work Tracker Pro (TrackerKerja)** versi 3.7.

---

## 📁 Berkas Utama
- **[`Tracking_Test_Case_E2E_TrackerKerja.xlsx`](file:///c:/TEMP/VSCODE/TrackerKerja/QA/Tracking_Test_Case_E2E_TrackerKerja.xlsx)**: Workbook spreadsheet pelacakan pengujian E2E interaktif dengan formula otomatis, styling enterprise, data validation, dan conditional formatting.
- **[`README.md`](file:///c:/TEMP/VSCODE/TrackerKerja/QA/README.md)**: Panduan operasional, konvensi ID, dan standar siklus pengujian QA.

---

## 📊 Struktur Lembar Kerja (*Workbook Sheets*)

Workbook **`Tracking_Test_Case_E2E_TrackerKerja.xlsx`** terdiri dari 4 sheet utama yang saling terhubung:

### 1. `Dashboard & Metrics` (Executive Summary)
- **KPI Summary Cards**:
  - `TOTAL TEST CASES`: Dihitung otomatis menggunakan formula `=COUNTA(...)`.
  - `PASSED`: Dihitung otomatis menggunakan formula `=COUNTIF(..., "Passed")`.
  - `FAILED`: Dihitung otomatis menggunakan formula `=COUNTIF(..., "Failed")`.
  - `IN PROGRESS`: Dihitung otomatis menggunakan formula `=COUNTIF(..., "In Progress")`.
  - `BLOCKED`: Dihitung otomatis menggunakan formula `=COUNTIF(..., "Blocked")`.
  - `COMPLETION %` & `PASS RATE %`: Terkalkulasi otomatis secara dinamis.
- **Tabel Ringkasan Per Modul (19 Modul)**:
  - Menyajikan rincian status per modul aplikasi dari Autentikasi hingga RESTful API & Swagger.

### 2. `E2E Test Cases Master` (Repositori Skenario Uji)
Sheet ini berisi skenario pengujian komprehensif dari awal hingga akhir (*End-to-End*) yang mencakup:
- **Kolom Standar**:
  1. `TC ID` (Format: `TC-[MODUL]-[NO]`)
  2. `Modul Utama`
  3. `Sub-Fitur / Komponen`
  4. `Tipe Pengujian` (*E2E Flow, Functional, Security/RBAC, UI/UX, Negative*)
  5. `Skenario Pengujian`
  6. `Prasyarat (Preconditions)`
  7. `Langkah-Langkah Pengujian (Test Steps)`
  8. `Data Pengujian (Test Data)`
  9. `Status` (*Passed, Failed, In Progress, Blocked, Ready, Draft* - dengan Dropdown Validation & Pewarnaan Otomatis)
  10. `Prioritas` (*Critical, High, Medium, Low*)
  11. `Hasil yang Diharapkan (Expected Result)`
  12. `Hasil Aktual (Actual Result)`
  13. `Tipe Eksekusi` (*Manual UI, API Postman, E2E Automated*)
  14. `Tester (PIC)`
  15. `Tgl Eksekusi`
  16. `Defect ID` (Link referensi ke Sheet Defect Log jika ada kegagalan)
  17. `Catatan / Bukti (Evidence)`

### 3. `Execution Cycles` (Siklus Rilis Pengujian)
Memantau progres pelaksanaan pengujian berdasarkan tahapan rilis:
- **CYCLE-01**: *Smoke / Sanity Test v3.7* (Jalur kritis sistem)
- **CYCLE-02**: *Multi-Company & Isolation Regression*
- **CYCLE-03**: *ClosedXML & ARMS Data Interoperability*
- **CYCLE-04**: *Full E2E Regression Sprint 12*
- **CYCLE-05**: *User Acceptance Testing (UAT)*

### 4. `Defect Log` (Pelacakan Bug & Kendala)
Mencatat seluruh kendala atau cacat fungsional yang ditemukan selama pengujian:
- **Atribut Defect**: `Defect ID`, `Ref TC ID`, `Modul`, `Ringkasan Masalah`, `Severity`, `Priority`, `Status`, `Dilaporkan Oleh`, `Tgl Lapor`, `Assigned Dev`, `Solusi / Resolusi`.

---

## 🏷️ Standar Konvensi Test Case ID

| Prefiks ID | Modul Terkait | Contoh |
|---|---|---|
| `TC-AUTH` | Autentikasi, Dual Auth (Cookie & JWT), Registrasi, Reset Sandi | `TC-AUTH-001` |
| `TC-COMP` | Multi-Company, Corporate Code Data Isolation | `TC-COMP-001` |
| `TC-PROJ` | Manajemen Proyek, Anggaran & Kategori | `TC-PROJ-001` |
| `TC-TASK` | Manajemen Tugas, Hierarki Parent-Subtask, Obstacle & Solution | `TC-TASK-001` |
| `TC-KANB` | Kanban Board Interaktif, Drag-and-Drop, Mobile View | `TC-KANB-001` |
| `TC-TIME` | Timesheet, Multi-Timer Serentak, Ekspor Excel ClosedXML | `TC-TIME-001` |
| `TC-ATTD` | Presensi Harian (Check-In/Out), WFH, Rekonsiliasi Tim | `TC-ATTD-001` |
| `TC-NOTE` | Catatan Quill.js, Multi-File Upload Terisolasi `/uploads/notes/{user}/` | `TC-NOTE-001` |
| `TC-EXCL` | Ekspor & Impor Excel Format Standar (9-Kolom) & ARMS (21-Kolom) | `TC-EXCL-001` |
| `TC-MEMB` | Anggota Tim, Pure Grid Card, Profil Banner, Hapus Akun | `TC-MEMB-001` |
| `TC-CAL`  | Kalender Tugas FullCalendar, Scope Filter Pribadi vs Tim | `TC-CAL-001` |
| `TC-DEV`  | Developer Tools (SQL Beautifier 15+ Dialek, Query Runner, JSON) | `TC-DEV-001` |
| `TC-AUD`  | Audit Trail & Action Logger | `TC-AUD-001` |
| `TC-MST`  | Master Data (Prioritas, Status Workflow, Milestone SDLC) | `TC-MST-001` |
| `TC-GAM`  | Gamifikasi, Daily Check-In Streak, Badges Gaul & Leaderboard | `TC-GAM-001` |
| `TC-NOTIF`| Notifikasi Lonceng In-App & Integrasi SMTP Email (7 Template) | `TC-NOTIF-001` |
| `TC-SYNC` | Multi-Instance Node Synchronization ke Host Induk | `TC-SYNC-001` |
| `TC-THEME`| Sistem Tema Tampilan Dinamis (40 Tema) & 5 Google Fonts | `TC-THEME-001` |
| `TC-API`  | RESTful API (100+ Endpoints), Swagger UI Bearer JWT & Postman | `TC-API-001` |

---

## 🚦 Klasifikasi Status & Tingkat Keparahan (Severity)

### Status Test Case
- 🟢 **Passed**: Skenario berhasil dieksekusi dan hasil aktual sesuai ekspektasi.
- 🔴 **Failed**: Terdapat ketidaksesuaian hasil atau muncul error/bug sistem.
- 🔵 **In Progress**: Pengujian sedang berlangsung oleh tester.
- 🟡 **Blocked**: Pengujian terhambat oleh ketergantungan fitur lain atau bug pemblokir.
- ⚪ **Ready**: Test case siap dieksekusi.
- ⚪ **Draft**: Test case masih dalam tahap penyusunan.

### Defect Severity
- **Critical / Blocker**: Sistem mengalami crash, kehilangan data, atau celah keamanan fatal.
- **Major**: Fungsi utama tidak berjalan dan tidak ada solusi alternatif (*workaround*).
- **Moderate**: Fungsi tidak berjalan normal namun tersedia *workaround*.
- **Minor**: Cacat visual, typo teks, atau kendala kosmetik minor pada antarmuka.

---

## 🔄 Pembaruan Tracking
Saat mengeksekusi pengujian:
1. Buka sheet **`E2E Test Cases Master`**.
2. Ubah kolom **Status (Kolom I)** menggunakan dropdown yang tersedia.
3. Masukkan **Hasil Aktual (Kolom L)**, **Tester (Kolom N)**, dan **Tanggal Eksekusi (Kolom O)**.
4. Jika status diubah menjadi **Failed**, catat nomor defect baru di sheet **`Defect Log`** dan cantumkan Defect ID pada kolom P.
5. Sheet **`Dashboard & Metrics`** akan otomatis memperbarui seluruh ringkasan statistik dan persentase kelulusan.

# Buku Panduan Pengguna (User Guide)
# Work Tracker Pro (TrackerKerja)

> **Versi Aplikasi**: 3.3 (Enterprise Multi-Instance Edition)  
> **Target Pengguna**: Seluruh Karyawan, System Analyst, Developer, QA, Technical Writer, Project Lead, dan Administrator  
> **Terakhir Diperbarui**: September 2026  

---

## Daftar Isi Panduan

1. [Pengenalan & Memulai Aplikasi](#1-pengenalan--memulai-aplikasi)
   - 1.1 [Halaman Masuk (Login)](#11-halaman-masuk-login)
   - 1.2 [Tata Letak Antarmuka & Navigasi](#12-tata-letak-antarmuka--navigasi)
   - 1.3 [Kustomisasi Tema & Tampilan (16 Pilihan Tema)](#13-kustomisasi-tema--tampilan-16-pilihan-tema)
2. [Dashboard & Ringkasan Kinerja](#2-dashboard--ringkasan-kinerja)
   - 2.1 [Kartu Metrik & Statistik Pribadi](#21-kartu-metrik--statistik-pribadi)
   - 2.2 [Pemberitahuan & Notifikasi Lonceng](#22-pemberitahuan--notifikasi-lonceng)
   - 2.3 [Distribusi Tugas & Beban Kerja Proyek](#23-distribusi-tugas--beban-kerja-proyek)
3. [Modul Task (Manajemen Tugas Kerja)](#3-modul-task-manajemen-tugas-kerja)
   - 3.1 [Membuat Tugas Baru](#31-membuat-tugas-baru)
   - 3.2 [Daftar Tugas & Filter Pencarian Cepat](#32-daftar-tugas--filter-pencarian-cepat)
   - 3.3 [Memperbarui Status & Slider Kemajuan (0–100%)](#33-memperbarui-status--slider-kemajuan-0100)
   - 3.4 [Sub-Task (Hierarki Tugas Induk & Anak)](#34-sub-task-hierarki-tugas-induk--anak)
   - 3.5 [Mencatat Kendala (Obstacle) & Solusi Teknis](#35-mencatat-kendala-obstacle--solusi-teknis)
   - 3.6 [Papan Kanban Interaktif (Geser & Letakkan)](#36-papan-kanban-interaktif-geser--letakkan)
   - 3.7 [Import Data Tugas dari Excel (Format Standar 22 Kolom)](#37-import-data-tugas-dari-excel-format-standar-22-kolom)
4. [Modul Project (Manajemen Proyek)](#4-modul-project-manajemen-proyek)
   - 4.1 [Membuat & Mengelola Proyek](#41-membuat--mengelola-proyek)
   - 4.2 [Memantau Linimasa, Tenggat Waktu & Progres Proyek](#42-memantau-linimasa-tenggat-waktu--progres-proyek)
5. [Modul Timesheet & Pelacakan Jam Kerja](#5-modul-timesheet--pelacakan-jam-kerja)
   - 5.1 [Pencatatan Jam Otomatis (Live Timer / Clock In & Clock Out)](#51-pencatatan-jam-otomatis-live-timer--clock-in--clock-out)
   - 5.2 [Penggunaan Multi-Timer Bersamaan](#52-penggunaan-multi-timer-bersamaan)
   - 5.3 [Pencatatan Jam Kerja Manual](#53-pencatatan-jam-kerja-manual)
   - 5.4 [Notifikasi Pengingat Pengisian Timesheet (Cut-Off Tanggal 25)](#54-notifikasi-pengingat-pengisian-timesheet-cut-off-tanggal-25)
   - 5.5 [Export Laporan Excel Timesheet Resmi (Format Standar Elistec)](#55-export-laporan-excel-timesheet-resmi-format-standar-elistec)
6. [Modul Absensi & Presensi Kerja (Attendance)](#6-modul-absensi--presensi-kerja-attendance)
   - 6.1 [Pencatatan Check-In & Check-Out Harian](#61-pencatatan-check-in--check-out-harian)
   - 6.2 [Status Kehadiran Fleksibel (Hadir, WFH, Sakit, Izin, Cuti, Libur)](#62-status-kehadiran-fleksibel-hadir-wfh-sakit-izin-cuti-libur)
   - 6.3 [Monitoring Rekapitulasi Presensi Tim](#63-monitoring-rekapitulasi-presensi-tim)
7. [Modul Kalender Kerja & Jadwal (Calendar)](#7-modul-kalender-kerja--jadwal-calendar)
   - 7.1 [Tampilan Kalender Berbasis Peran (RBAC Filter)](#71-tampilan-kalender-berbasis-peran-rbac-filter)
   - 7.2 [Fitur Khusus Administrator (Dropdown Semua Tugas vs Tugas Saya)](#72-fitur-khusus-administrator-dropdown-semua-tugas-vs-tugas-saya)
   - 7.3 [Deteksi Irisan Jadwal & Detail PIC](#73-deteksi-irisan-jadwal--detail-pic)
8. [Modul Catatan Kerja & Dokumentasi (Notes)](#8-modul-catatan-kerja--dokumentasi-notes)
   - 8.1 [Membuat Catatan dengan Rich-Text Editor](#81-membuat-catatan-dengan-rich-text-editor)
   - 8.2 [Menyematkan Catatan Penting (Pin Note)](#82-menyematkan-catatan-penting-pin-note)
   - 8.3 [Menghubungkan Catatan ke Tugas atau Proyek](#83-menghubungkan-catatan-ke-tugas-atau-proyek)
   - 8.4 [Mengunggah Berkas Lampiran](#84-mengunggah-berkas-lampiran)
9. [Modul JSON Payload Tools](#9-modul-json-payload-tools)
   - 9.1 [Merapikan Format JSON (Beautify)](#91-merapikan-format-json-beautify)
   - 9.2 [Validasi Sintaks & Minify JSON](#92-validasi-sintaks--minify-json)
   - 9.3 [Penyimpanan Template Payload Pengujian](#93-penyimpanan-template-payload-pengujian)
10. [Modul SQL Beautifier & Formatter Tools](#10-modul-sql-beautifier--formatter-tools)
    - 10.1 [Format & Beautify 15+ Dialek SQL Engine](#101-format--beautify-15-dialek-sql-engine)
    - 10.2 [Validasi Sintaks & Minifikasi Kueri](#102-validasi-sintaks--minifikasi-kueri)
    - 10.3 [Penyimpanan Snippet Kueri SQL Terkait Tugas](#103-penyimpanan-snippet-kueri-sql-terkait-tugas)
11. [Modul Laporan & Analitik Kinerja](#11-modul-laporan--analitik-kinerja)
12. [Modul Anggota Tim & Gamifikasi Prestasi](#12-modul-anggota-tim--gamifikasi-prestasi)
13. [Modul Master Data, Sinkronisasi Multi-Instance & Backup](#13-modul-master-data-sinkronisasi-multi-instance--backup)
    - 13.1 [Master Kategori, Prioritas, dan Status](#131-master-kategori-prioritas-dan-status)
    - 13.2 [Master Milestone SDLC Waterfall](#132-master-milestone-sdlc-waterfall)
    - 13.3 [Master Hari Libur Nasional](#133-master-hari-libur-nasional)
    - 13.4 [Sinkronisasi Multi-Instance ke Host Induk (Online Push & SQL Import/Export)](#134-sinkronisasi-multi-instance-ke-host-induk-online-push--sql-importexport)
    - 13.5 [Backup & Export Database (.db & .sql)](#135-backup--export-database-db--sql)
14. [Tips & Pertanyaan Umum (FAQ)](#14-tips--pertanyaan-umum-faq)

---

## 1. Pengenalan & Memulai Aplikasi

**Work Tracker Pro (TrackerKerja)** adalah aplikasi manajemen pekerjaan terpadu yang dirancang untuk mempermudah tim dalam merencanakan tugas, mencatat waktu kerja secara akurat (*timesheet*), mengelola absensi kehadiran, mendokumentasikan kendala dan solusi teknis, mengolah payload data/SQL, serta menghasilkan laporan kerja siap pakai.

### 1.1 Halaman Masuk (Login)
1. Buka peramban web dan akses alamat aplikasi TrackerKerja.
2. Masukkan **Alamat Email** dan **Kata Sandi (Password)** Anda yang telah didaftarkan.
3. Beri tanda centang pada opsi **Ingat Saya (Remember Me)** jika Anda ingin sesi login tetap tersimpan pada perangkat pribadi.
4. Klik tombol **Masuk (Login)** untuk masuk ke Dashboard utama.

### 1.2 Tata Letak Antarmuka & Navigasi
- **Bilah Samping (Sidebar)**: Berisi menu navigasi utama aplikasi (Dashboard, Tugas, Kanban, Proyek, Timesheet, Absensi, Kalender, Catatan, JSON Tools, SQL Tools, Laporan, Anggota Tim, dan Master Data).
- **Bilah Atas (Topbar)**: Berisi judul halaman aktif, kotak pencarian global, tombol aksi cepat (Tugas Baru, Import Excel), tombol **Panduan Pengguna**, lonceng notifikasi cerdas, serta menu profil akun.
- **Panel Timer Samping**: Menampilkan daftar sesi kerja yang sedang berjalan secara *real-time* dan dapat dikontrol kapan saja.

### 1.3 Kustomisasi Tema & Tampilan (16 Pilihan Tema)
Aplikasi menyediakan 16 pilihan tema warna yang dapat dipilih sesuai preferensi kenyamanan mata Anda:
1. Klik avatar profil Anda di pojok kanan atas atau buka menu **Profil Akun**.
2. Pilih palet tema yang diinginkan (tersedia tema bernuansa *Light Mode* seperti Indigo Modern, Emerald Green, Rose Coral, Amber Sunset, Ocean Breeze, hingga tema *Dark Mode* seperti Dark OLED, Matrix Cyber, Midnight Purple, dan Slate Pro).
3. Tampilan aplikasi akan berubah secara instan tanpa perlu memuat ulang halaman.

---

## 2. Dashboard & Ringkasan Kinerja

Halaman Dashboard merupakan pusat informasi terpadu yang menyajikan ikhtisar aktivitas dan produktivitas Anda.

### 2.1 Kartu Metrik & Statistik Pribadi
- **Total Tugas**: Menampilkan jumlah seluruh tugas yang ditugaskan kepada Anda.
- **Tugas Sedang Dikerjakan (In Progress)**: Jumlah tugas yang saat ini aktif dalam proses pengerjaan.
- **Tugas Selesai (Done)**: Jumlah tugas yang telah tuntas dikerjakan.
- **Tenggat Terlewat (Overdue)**: Peringatan visual untuk tugas yang melewati batas tanggal selesai namun belum berstatus *Done*.
- **Jam Kerja Hari Ini**: Akumulasi durasi waktu kerja yang telah Anda catat pada hari ini.

### 2.2 Pemberitahuan & Notifikasi Lonceng
Klik ikon **Lonceng Notifikasi** pada bilah atas untuk melihat panel pemberitahuan cerdas:
- **Tab Semua**: Seluruh pemberitahuan sistem.
- **Tab Overdue**: Tugas-tugas yang telah melewati tenggat waktu agar segera diselesaikan.
- **Tab Mendekati Deadline**: Tugas yang memiliki batas waktu dalam 2–3 hari ke depan.
- **Tab Timesheet (Pengingat Cut-Off)**: Menampilkan daftar tugas aktif yang belum memiliki catatan jam kerja (*timesheet*), terutama saat mendekati periode cut-off bulanan tanggal 25.

### 2.3 Distribusi Tugas & Beban Kerja Proyek
Menampilkan diagram lingkaran (*doughnut chart*) dan grafik batang interaktif untuk melihat sebaran tugas per proyek, persentase penyelesaian, serta ringkasan aktivitas terbaru tim.

---

## 3. Modul Task (Manajemen Tugas Kerja)

Modul Tugas adalah inti pengelolaan pekerjaan sehari-hari.

### 3.1 Membuat Tugas Baru
1. Klik tombol **+ Tugas Baru** pada bilah atas atau di halaman Daftar Tugas (`/Task`).
2. Isi formulir pembuatan tugas:
   - **Judul Tugas**: Nama ringkas aktivitas pekerjaan (contoh: *Pembuatan Dokumen FSD Integrasi API*).
   - **Kode Tugas (Task Code)**: Dihasilkan otomatis oleh sistem (contoh: `TSK-0732`).
   - **Proyek**: Pilih proyek yang menaungi tugas ini.
   - **Kategori**: Pilih kategori kerja (misal: *System Analysis*, *Frontend*, *Backend*, *Quality Assurance*, dll).
   - **Tingkat Prioritas**: Pilih *Low*, *Medium*, *High*, atau *Critical*.
   - **Milestone SDLC**: Pilih tahapan siklus kerja (misal: *Requirement Analysis*, *System Design*, *Implementation*, *Testing & QA*, *Deployment*, *Maintenance*).
   - **Penanggung Jawab (PIC / Assigned To)**: Tentukan anggota tim yang bertugas.
   - **Tanggal Mulai & Tenggat Waktu (Due Date)**: Tentukan batas waktu pengerjaan.
   - **Deskripsi Detail**: Uraikan ruang lingkup dan instruksi tugas.
3. Klik tombol **Simpan Tugas**.

### 3.2 Daftar Tugas & Filter Pencarian Cepat
Pada halaman `/Task`, Anda dapat memfilter tugas berdasarkan:
- Pencarian kata kunci pada judul atau kode tugas.
- Filter berdasarkan **Proyek**.
- Filter berdasarkan **Status** (*Todo*, *In Progress*, *Done*).
- Filter berdasarkan **Prioritas** dan **Penanggung Jawab (PIC)**.
- Opsi sorting berdasarkan tanggal terbaru, prioritas tertinggi, atau tenggat waktu terdekat.

### 3.3 Memperbarui Status & Slider Kemajuan (0–100%)
Setiap tugas dilengkapi dengan slider persentase kemajuan (*progress bar*):
- Menggeser progress ke angka **1–99%** secara otomatis mengubah status tugas menjadi **In Progress**.
- Menggeser progress ke angka **100%** secara otomatis mengubah status tugas menjadi **Done (Selesai)**.
- Sebaliknya, mengubah status langsung ke *Done* akan otomatis mengisi progress menjadi 100%.

### 3.4 Sub-Task (Hierarki Tugas Induk & Anak)
Untuk memecah tugas besar menjadi bagian-bagian kecil:
1. Buka detail tugas utama (tugas induk / *parent task*).
2. Pada bagian **Sub-Tasks**, klik **Tambah Sub-Task**.
3. Masukkan judul dan PIC sub-task.
4. Kemajuan tugas induk akan mencerminkan rata-rata penyelesaian seluruh sub-task di bawahnya.

### 3.5 Mencatat Kendala (Obstacle) & Solusi Teknis
Fitur ini sangat berguna untuk mencatat hambatan (*blocker*) selama pengerjaan:
1. Buka form Edit Tugas atau Detail Tugas.
2. Isi kolom **Kendala / Hambatan (Obstacle)** (contoh: *Menunggu akses credential database server development*).
3. Isi kolom **Solusi / Tindak Lanjut (Solution)** (contoh: *Koordinasi dengan tim IT Infra via tiket permintaan akses*).
4. Catatan kendala dan solusi akan tampil dengan kartu informasi khusus berwarna peringatan agar mudah ditinjau saat rapat berkala.

### 3.6 Papan Kanban Interaktif (Geser & Letakkan)
Buka menu **Kanban** (`/Kanban`) untuk visualisasi alur kerja bergaya kartu:
- Tiga kolom utama: **To Do (Belum Dimulai)**, **In Progress (Sedang Dikerjakan)**, dan **Done (Selesai)**.
- **Drag & Drop**: Cukup klik dan tahan kartu tugas, lalu geser ke kolom status yang diinginkan. Status tugas di database akan otomatis diperbarui.
- **Tampilan Mobile**: Pada layar ponsel, tersedia tombol tab pintar di bagian atas untuk berpindah antar kolom secara cepat dan rapi.

### 3.7 Import Data Tugas dari Excel (Format Standar 22 Kolom)
Aplikasi mendukung impor banyak tugas sekaligus melalui berkas spreadsheet Excel `.xlsx` menggunakan format standar 22 kolom (*Proposed Tracker / Enterprise Format*):
1. Buka menu **Import Task** (`/Import`).
2. Klik tombol **Download Template Excel (22 Kolom)** untuk mengunduh berkas template siap pakai.
3. Isi data tugas pada lembar kerja Excel dan unggah berkas yang telah diisi pada area *Drag & Drop*.
4. Pada halaman **Preview Data**, sistem akan otomatis mencocokkan PIC ke akun pengguna terdaftar.
5. Klik **Konfirmasi Import Task** untuk menyimpan seluruh data tugas secara instan.

---

## 4. Modul Project (Manajemen Proyek)

### 4.1 Membuat & Mengelola Proyek
1. Buka menu **Proyek** (`/Project`).
2. Klik tombol **+ Proyek Baru**.
3. Masukkan **Nama Proyek**, **Deskripsi**, **Warna Identitas Proyek** (digunakan sebagai label pada kartu tugas), dan **Batas Akhir Proyek (Deadline)**.
4. Klik **Simpan Proyek**.

### 4.2 Memantau Linimasa, Tenggat Waktu & Progres Proyek
- Setiap kartu proyek menampilkan persentase penyelesaian keseluruhan tugas, rasio tugas selesai vs total tugas, dan status ketercapaian target waktu (*On Track* atau *At Risk*).
- Mengklik salah satu proyek akan membuka halaman khusus proyek yang menyajikan seluruh tugas, timesheet, catatan, dan linimasa yang berkaitan langsung dengan proyek tersebut.

---

## 5. Modul Timesheet & Pelacakan Jam Kerja

Modul Timesheet mencatat waktu aktual yang dihabiskan untuk menyelesaikan setiap tugas secara transparan dan akurat.

### 5.1 Pencatatan Jam Otomatis (Live Timer / Clock In & Clock Out)
1. Buka menu **Timesheet** atau buka kartu tugas apa saja.
2. Klik tombol **Mulai Timer (Clock In)** berwarna hijau pada tugas yang akan dikerjakan.
3. Timer akan berjalan secara *real-time*. Durasi waktu yang berjalan dapat dilihat di panel samping maupun di bagian atas layar.
4. Setelah selesai bekerja, klik tombol **Hentikan Timer (Clock Out)** berwarna merah.
5. Anda dapat menambahkan catatan ringkas mengenai pekerjaan yang telah diselesaikan pada sesi tersebut.

### 5.2 Penggunaan Multi-Timer Bersamaan
Aplikasi mendukung **Multi-Timer Aktif** per pengguna:
- Jika Anda sedang mengerjakan tugas analisis sekaligus melakukan pemantauan deployment tugas lain, Anda dapat menjalankan timer untuk masing-masing tugas tersebut.
- Seluruh timer yang aktif akan tercatat secara independen dan dapat dihentikan satu per satu.

### 5.3 Pencatatan Jam Kerja Manual
Jika Anda lupa menyalakan timer saat bekerja:
1. Buka menu **Timesheet** (`/Timesheet`).
2. Klik tombol **+ Tambah Jam Kerja Manual**.
3. Pilih **Tugas**, **Tanggal Kerja**, **Jam Mulai**, **Jam Selesai**, serta isi **Catatan Aktivitas**.
4. Klik **Simpan Sesi**.

### 5.4 Notifikasi Pengingat Pengisian Timesheet (Cut-Off Tanggal 25)
- **Periode Cut-Off**: Jatuh pada **tanggal 25 setiap bulannya**.
- **Jendela Pengingat**: Mulai tanggal **18 hingga 25 setiap bulan**, sistem otomatis memindai seluruh tugas aktif yang ditugaskan kepada Anda namun **belum memiliki catatan jam kerja (0 jam)**.
- **Pemberitahuan**: Lonceng notifikasi pada topbar dan banner peringatan di halaman Timesheet akan menyala, menampilkan daftar tugas yang belum diisi jam kerjanya.

### 5.5 Export Laporan Excel Timesheet Resmi (Format Standar Elistec)
1. Buka menu **Timesheet** (`/Timesheet`).
2. Klik tombol **Export Timesheet Personal (Excel)**.
3. Pilih rentang tanggal (tersedia preset *Bulan Ini*, *Bulan Lalu*, *Minggu Ini*, atau rentang tanggal kustom).
4. Klik **Download Excel**.
- Format laporan mencakup konversi otomatis ke *Man-Days (MD)*, penandaan hari libur nasional, akhir pekan, dan formula kalkulasi grand total otomatis.

---

## 6. Modul Absensi & Presensi Kerja (Attendance)

Modul Presensi (`/Attendance`) memudahkan pencatatan kehadiran kerja harian seluruh anggota tim:

### 6.1 Pencatatan Check-In & Check-Out Harian
1. Buka menu **Absensi** pada bilah navigasi.
2. Klik tombol **Check In** saat memulai hari kerja. Waktu masuk akan tercatat secara presisi.
3. Klik tombol **Check Out** saat mengakhiri hari kerja untuk menyelesaikan durasi kerja harian.

### 6.2 Status Kehadiran Fleksibel
Pengguna dapat memilih status kehadiran sesuai kondisi kerja:
- **Hadir (WFO)**: Bekerja di kantor.
- **WFH (Work From Home)**: Bekerja dari rumah / jarak jauh.
- **Sakit**: Kehadiran izin sakit.
- **Izin / Cuti**: Mengambil jatah cuti atau izin keperluan pribadi.
- **Libur**: Hari libur resmi.

### 6.3 Monitoring Rekapitulasi Presensi Tim
- Administrator dapat melihat rekapitulasi kehadiran seluruh anggota tim dalam format tabel bulanan yang rapi.
- Membantu bagian operasional dan manajemen dalam memverifikasi kedisiplinan dan ketersediaan tim.

---

## 7. Modul Kalender Kerja & Jadwal (Calendar)

Buka menu **Kalender** (`/Calendar`) untuk visualisasi jadwal tugas dan deadline interaktif:

### 7.1 Tampilan Kalender Berbasis Peran (RBAC Filter)
- **Member Reguler**: Kalender secara otomatis menyaring dan **hanya menampilkan tugas milik member yang sedang login** (*Tugas Saya*). Terdapat penanda badge *👤 Tugas Saya* di bagian atas.
- **Pilihan Tampilan**: Mendukung tampilan Bulanan (*Month*), Mingguan (*Week*), Harian (*Day*), dan Daftar Agenda (*List View*).

### 7.2 Fitur Khusus Administrator
- Pengguna dengan peran **Administrator** memiliki dropdown filter di header kalender:
  - **🌐 Semua Tugas Tim**: Menampilkan persebaran seluruh tugas dari semua anggota tim.
  - **👤 Tugas Saya Sendiri**: Menyaring kalender agar hanya menampilkan tugas yang ditugaskan ke Admin.
- Mengubah pilihan dropdown akan langsung memperbarui kalender (*AJAX refetch*) tanpa me-refresh seluruh halaman.

### 7.3 Deteksi Irisan Jadwal & Detail PIC
- **Peringatan Irisan (Overlap Warning)**: Sistem otomatis mendeteksi jika terdapat tugas-tugas yang memiliki rentang tanggal yang berbenturan dan menampilkan banner peringatan.
- **Task Detail Modal & Tooltip**: Mengklik salah satu event pada kalender akan membuka popup modal yang menampilkan informasi lengkap: Judul, Status, Prioritas, Proyek, Deadline, serta **Nama & Avatar PIC (Penanggung Jawab)**.

---

## 8. Modul Catatan Kerja & Dokumentasi (Notes)

Modul Catatan (`/Note`) berfungsi sebagai repositori dokumentasi teknis, notula rapat, dan referensi harian.

### 8.1 Membuat Catatan dengan Rich-Text Editor
1. Buka menu **Catatan** dan klik **+ Buat Catatan Baru**.
2. Masukkan **Judul Catatan**.
3. Gunakan editor (*Quill.js Rich-Text*) untuk teks berformat, heading, tabel, dan blok kode.
4. Pilih warna kartu catatan untuk mempermudah identifikasi visual.

### 8.2 Menyematkan Catatan Penting (Pin Note)
- Klik ikon pin pada kartu catatan agar catatan tersebut selalu berada di posisi paling atas halaman.

### 8.3 Menghubungkan Catatan ke Tugas atau Proyek
- Anda dapat mengaitkan catatan dengan tugas atau proyek tertentu agar otomatis tampil pada tab dokumentasi tugas/proyek tersebut.

### 8.4 Mengunggah Berkas Lampiran
- Lampirkan berkas dokumen (PDF, Word, Excel, gambar) ke dalam catatan. Berkas tersimpan rapi dan dapat diunduh kapan saja.

---

## 9. Modul JSON Payload Tools

Modul JSON Tools (`/JsonTools`) disediakan khusus untuk System Analyst, Developer, dan QA dalam mengolah data payload JSON saat pengujian integrasi API.

### 9.1 Merapikan Format JSON (Beautify)
- Tempel teks JSON ke dalam editor dan klik **Format JSON** untuk merapikan indentasi baris secara terstruktur.

### 9.2 Validasi Sintaks & Minify JSON
- **Validasi**: Mendeteksi kesalahan penulisan kurung, koma, kutip, atau struktur data JSON.
- **Minify**: Menghapus whitespace berlebih untuk menghasilkan payload ringkas siap pakai.

### 9.3 Penyimpanan Template Payload Pengujian
- Simpan snippet JSON pengujian yang sering digunakan ke dalam daftar template untuk dimuat ulang secara instan.

---

## 10. Modul SQL Beautifier & Formatter Tools

Modul SQL Beautifier (`/SqlTools`) dirancang untuk merapikan, memformat, memvalidasi, dan mengompres kueri SQL dengan standar engine database modern.

### 10.1 Format & Beautify 15+ Dialek SQL Engine
- Mendukung dialek SQL: Standard ANSI, PostgreSQL, MySQL, MariaDB, SQLite, Transact-SQL (SQL Server), Oracle (PL/SQL), BigQuery, Snowflake, Redshift, Spark SQL, DB2, Trino, Couchbase, dan SingleStore.
- Kustomisasi indentasi (2/4/8 spasi atau Tab) dan pengubahan kata kunci keyword menjadi `UPPERCASE` atau `lowercase`.
- Shortcut cepat: Tekan **Ctrl + Enter** untuk memformat kueri secara instan.

### 10.2 Validasi Sintaks & Minifikasi Kueri
- Memeriksa keseimbangan tanda kurung dan kutip secara otomatis.
- Menghapus komentar dan whitespace berlebih untuk kompresi kueri (*Minify*).

### 10.3 Penyimpanan Snippet Kueri SQL Terkait Tugas
- Simpan kueri ke database lokal dan hubungkan ke tugas kerja tertentu untuk dokumentasi tim.

---

## 11. Modul Laporan & Analitik Kinerja

Buka menu **Laporan** (`/Report`) untuk evaluasi produktivitas:
- **Grafik Distribusi Jam Kerja**: Rekapitulasi perbandingan total jam kerja per proyek.
- **Matriks Beban Kerja Tim (Workload Matrix)**: Memantau jumlah tugas aktif per anggota tim.
- **Linimasa Gantt**: Menampilkan urutan pengerjaan tugas dalam bentuk diagram batang linimasa.
- **Ekspor Laporan**: Fasilitas ekspor data rekapitulasi ke format Excel.

---

## 12. Modul Anggota Tim & Gamifikasi Prestasi

Buka menu **Anggota Tim** (`/Member`):
- **Kartu Profil & Level Kontribusi**: Statistik penyelesaian tugas dan akumulasi jam kerja.
- **Badge Prestasi**: Penghargaan otomatis dan manual atas pencapaian kinerja tim.
- **Manajemen Pengguna (Khusus Admin)**: Menambah anggota tim baru, mereset kata sandi akun, atau menonaktifkan akun yang sudah tidak bertugas.

---

## 13. Modul Master Data, Sinkronisasi Multi-Instance & Backup

Khusus untuk peran **Administrator**, menu **Master Data** (`/MasterData`) dan **Konfigurasi** (`/Configuration`):

### 13.1 Master Kategori, Prioritas, dan Status
- Mengelola kategori tugas, prioritas (*Critical, High, Medium, Low*), dan status alur kerja.

### 13.2 Master Milestone SDLC Waterfall
- Mengatur tahapan siklus pengembangan perangkat lunak (Requirement, Design, Development, Testing, Deployment).

### 13.3 Master Hari Libur Nasional
- Mengelola daftar hari libur resmi yang terintegrasi otomatis dengan laporan Timesheet Excel.

### 13.4 Sinkronisasi Multi-Instance ke Host Induk
- **Online API Push**: Mengirimkan data pembaruan tugas dan sesi jam kerja dari server cabang/lokal ke Host Induk terpusat.
- **Export/Import SQL Script**: Mendukung sinkronisasi manual antar instance melalui skrip dump SQL.

### 13.5 Backup & Export Database (.db & .sql)
- **Export File Database (.db)**: Mengunduh berkas biner SQLite `.db` utuh untuk pencadangan offline (*full binary backup*).
- **Export Script SQL (.sql)**: Mengunduh skrip DDL dan DML lengkap yang siap direstore ke database manapun.
- **Kompresi Database (VACUUM)**: Mengoptimalkan dan mengklaim kembali ruang kosong file SQLite.

---

## 14. Tips & Pertanyaan Umum (FAQ)

### Q1: Bagaimana cara mencetak atau menyimpan panduan ini ke format PDF?
> **Jawaban**: Klik tombol **📖 Panduan** pada bilah atas aplikasi, lalu klik tombol **🖨️ Cetak / Simpan PDF**. Pada jendela print peramban, pilih tujuan printer sebagai **Save as PDF (Simpan sebagai PDF)** dan klik **Save**.

### Q2: Mengapa di Kalender saya hanya melihat tugas milik saya sendiri?
> **Jawaban**: Untuk anggota tim reguler, kalender secara default disaring khusus untuk tugas yang ditugaskan kepada Anda agar Anda dapat fokus pada jadwal kerja pribadi. Administrator memiliki opsi dropdown untuk melihat tugas seluruh tim.

### Q3: Mengapa saya mendapatkan notifikasi pengingat Timesheet menjelang tanggal 25?
> **Jawaban**: Notifikasi tersebut merupakan pengingat otomatis bagi anggota tim yang memiliki tugas aktif namun belum mengisi catatan jam kerja (*timesheet*) agar seluruh jam kerja bulan berjalan terekam lengkap sebelum batas cut-off bulanan tanggal 25.

### Q4: Apakah saya bisa menjalankan timer untuk lebih dari satu tugas sekaligus?
> **Jawaban**: Ya. TrackerKerja mendukung multi-timer serentak. Anda dapat menekan tombol *Clock In* pada beberapa tugas berbeda dan seluruh sesi waktu akan dicatat secara akurat.

### Q5: Bagaimana cara mencatat absensi jika saya bekerja dari rumah (WFH)?
> **Jawaban**: Buka menu **Absensi** (`/Attendance`), pilih status **WFH (Work From Home)**, lalu klik tombol **Check In**.

---

*(Buku Panduan Pengguna Work Tracker Pro — Diterbitkan untuk Efisiensi & Transparansi Kerja Tim)*

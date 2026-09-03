# Buku Panduan Pengguna (User Guide)
# Work Tracker Pro (TrackerKerja)

> **Versi Aplikasi**: 3.2 (Enterprise Edition)  
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
   - 3.7 [Import Data Tugas dari Excel (Format Standar 21 Kolom)](#37-import-data-tugas-dari-excel-format-standar-21-kolom)
4. [Modul Project (Manajemen Proyek)](#4-modul-project-manajemen-proyek)
   - 4.1 [Membuat & Mengelola Proyek](#41-membuat--mengelola-proyek)
   - 4.2 [Memantau Linimasa, Tenggat Waktu & Progres Proyek](#42-memantau-linimasa-tenggat-waktu--progres-proyek)
5. [Modul Timesheet & Pelacakan Jam Kerja](#5-modul-timesheet--pelacakan-jam-kerja)
   - 5.1 [Pencatatan Jam Otomatis (Live Timer / Clock In & Clock Out)](#51-pencatatan-jam-otomatis-live-timer--clock-in--clock-out)
   - 5.2 [Penggunaan Multi-Timer Bersamaan](#52-penggunaan-multi-timer-bersamaan)
   - 5.3 [Pencatatan Jam Kerja Manual](#53-pencatatan-jam-kerja-manual)
   - 5.4 [Notifikasi Pengingat Pengisian Timesheet (Cut-Off Tanggal 25)](#54-notifikasi-pengingat-pengisian-timesheet-cut-off-tanggal-25)
   - 5.5 [Export Laporan Excel Timesheet Resmi (Format Standar Elistec)](#55-export-laporan-excel-timesheet-resmi-format-standar-elistec)
6. [Modul JSON Payload & Alat Bantu Pengujian API](#6-modul-json-payload--alat-bantu-pengujian-api)
   - 6.1 [Merapikan Format JSON (Beautify / Pretty-Print)](#61-merapikan-format-json-beautify--pretty-print)
   - 6.2 [Pemeriksaan & Validasi Sintaks JSON](#62-pemeriksaan--validasi-sintaks-json)
   - 6.3 [Memampatkan JSON (Minify)](#63-memampatkan-json-minify)
   - 6.4 [Menyimpan & Mengelola Template Payload Pengujian](#64-menyimpan--mengelola-template-payload-pengujian)
7. [Modul Catatan Kerja & Dokumentasi (Notes)](#7-modul-catatan-kerja--dokumentasi-notes)
   - 7.1 [Membuat Catatan dengan Rich-Text Editor](#71-membuat-catatan-dengan-rich-text-editor)
   - 7.2 [Menyematkan Catatan Penting (Pin Note)](#72-menyematkan-catatan-penting-pin-note)
   - 7.3 [Menghubungkan Catatan ke Tugas atau Proyek](#73-menghubungkan-catatan-ke-tugas-atau-proyek)
   - 7.4 [Mengunggah Berkas Lampiran](#74-mengunggah-berkas-lampiran)
8. [Modul Kalender Kerja & Jadwal](#8-modul-kalender-kerja--jadwal)
9. [Modul Laporan & Analitik Kinerja](#9-modul-laporan--analitik-kinerja)
10. [Modul Anggota Tim & Gamifikasi Prestasi](#10-modul-anggota-tim--gamifikasi-prestasi)
    - 10.1 [Profil Pengguna & Level Pengalaman](#101-profil-pengguna--level-pengalaman)
    - 10.2 [Koleksi Badge Penghargaan](#102-koleksi-badge-penghargaan)
    - 10.3 [Manajemen Pengguna oleh Administrator](#103-manajemen-pengguna-oleh-administrator)
11. [Modul Master Data](#11-modul-master-data)
    - 11.1 [Kategori, Prioritas, dan Status](#111-kategori-prioritas-dan-status)
    - 11.2 [Milestone SDLC Waterfall](#112-milestone-sdlc-waterfall)
    - 11.3 [Master Hari Libur Nasional & Cuti Bersama](#113-master-hari-libur-nasional--cuti-bersama)
    - 11.4 [Konfigurasi Sistem & Backup/Export Database (.db & .sql)](#114-konfigurasi-sistem--backupexport-database-db--sql)
12. [Tips & Pertanyaan Umum (FAQ)](#12-tips--pertanyaan-umum-faq)

---

## 1. Pengenalan & Memulai Aplikasi

**Work Tracker Pro (TrackerKerja)** adalah aplikasi manajemen pekerjaan terpadu yang dirancang untuk mempermudah tim dalam merencanakan tugas, mencatat waktu kerja secara akurat (*timesheet*), mendokumentasikan kendala dan solusi teknis, mengelola pengujian payload data, serta menghasilkan laporan kerja siap pakai untuk kebutuhan operasional maupun pelaporan manajemen.

### 1.1 Halaman Masuk (Login)
1. Buka peramban web dan akses alamat aplikasi TrackerKerja.
2. Masukkan **Alamat Email** dan **Kata Sandi (Password)** Anda yang telah terdaftar.
3. Beri tanda centang pada opsi **Ingat Saya (Remember Me)** jika Anda ingin sesi login tetap tersimpan hingga 7 hari pada perangkat pribadi Anda.
4. Klik tombol **Masuk (Login)** untuk masuk ke Dashboard utama.

> 💡 **Informasi Akun Default**:
> - Akun Admin: `admin@trackerkerja.com`
> - Kata sandi standar pengguna baru dapat diatur oleh Administrator atau diubah secara mandiri pada menu Profil.

### 1.2 Tata Letak Antarmuka & Navigasi
- **Bilah Samping (Sidebar)**: Berisi menu navigasi utama aplikasi (Dashboard, Tugas, Kanban, Proyek, Timesheet, JSON Tools, Catatan, Kalender, Laporan, Anggota Tim, dan Master Data).
- **Bilah Atas (Topbar)**: Berisi judul halaman aktif, kotak pencarian global, tombol aksi cepat (Tugas Baru, Import Excel), tombol **Panduan Pengguna**, lonceng notifikasi, serta menu profil akun.
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
Menampilkan grafik diagram lingkaran (*doughnut chart*) dan grafik batang interaktif untuk melihat sebaran tugas per proyek, persentase penyelesaian, serta ringkasan aktivitas terbaru tim.

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
   - **Milestone SDLC**: Pilih tahapan siklus kerja (misal: *Requirement & BRD*, *FSD & TSD*, *Development*, *Testing & QA*, *UAT & Deployment*).
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

### 3.7 Import Data Tugas dari Excel (Format Standar 21 Kolom)
Aplikasi mendukung impor banyak tugas sekaligus melalui berkas spreadsheet Excel `.xlsx` menggunakan format standar 21 kolom (*Proposed Tracker / Enterprise Format*):
1. Buka menu **Import Task** (`/Import`).
2. Klik tombol **Download Template Excel (21 Kolom)** untuk mengunduh berkas template siap pakai.
3. Isi data tugas pada lembar kerja Excel mulai dari baris ke-2:
   - `project_name`: Nama proyek terkait (dibuat otomatis jika proyek belum ada).
   - `requirement_code`: Kode dokumen requirement / BRD / TSD.
   - `title`: Judul nama tugas kerja (**Wajib diisi**).
   - `status`: `TODO`, `IN_PROGRESS`, `DONE`, atau `TESTING`.
   - `priority`: `LOW`, `MEDIUM`, `HIGH`, atau `CRITICAL`.
   - `jenis_task`: Kategori pengerjaan (`NEW_APP`, `ENHANCEMENT`, `BUGFIX`, dll).
   - `module_name`: Nama modul sistem / milestone.
   - `bug_type`: Klasifikasi isu (`Feature`, `Bug`, `Task`, dll).
   - `progress`: Persentase kemajuan (0–100%).
   - `start_date` & `due_date`: Tanggal mulai dan tenggat waktu tugas.
   - `developer_emails`: Alamat email PIC pengembang (dapat dipisah tanda `;` jika lebih dari 1 orang).
   - `ba_emails`, `infra_emails`, `master_data_emails`, `tester_emails`, `tw_emails`: Alamat email pemangku kepentingan terkait.
   - `kendala` & `solusi`: Catatan hambatan dan rekomendasi penanganan teknis.
   - `Notes Tracker`: Catatan aktivitas atau deskripsi tugas.
4. Unggah berkas yang telah diisi pada area *Drag & Drop*.
5. Pada halaman **Preview Data**, periksa validitas baris data dan Anda dapat mengatur ulang PIC penugasan secara interaktif jika diperlukan.
6. Klik **Konfirmasi & Simpan ke Database** untuk menyimpan seluruh data tugas secara instan.

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
5. Anda dapat menambahkan catatan ringkas mengenai pekerjaan yang telah diselesaikan pada sesi tersebut. Sistem akan otomatis menghitung total durasi dalam jam dan detik.

### 5.2 Penggunaan Multi-Timer Bersamaan
Aplikasi mendukung **Multi-Timer Aktif** per pengguna:
- Jika Anda sedang mengerjakan tugas analisis sekaligus melakukan pemantauan deployment tugas lain, Anda dapat menjalankan timer untuk masing-masing tugas tersebut.
- Seluruh timer yang aktif akan tercatat secara independen dan dapat dihentikan satu per satu.

### 5.3 Pencatatan Jam Kerja Manual
Jika Anda lupa menyalakan timer saat bekerja:
1. Buka menu **Timesheet** (`/Timesheet`).
2. Klik tombol **+ Tambah Jam Kerja Manual**.
3. Pilih **Tugas**, **Tanggal Kerja**, **Jam Mulai**, **Jam Selesai** (atau input langsung durasi dalam jam/menit), serta isi **Catatan Aktivitas**.
4. Klik **Simpan Sesi**.

### 5.4 Notifikasi Pengingat Pengisian Timesheet (Cut-Off Tanggal 25)
Untuk memastikan seluruh data jam kerja bulanan lengkap sebelum batas pelaporan:
- **Periode Cut-Off**: Jatuh pada **tanggal 25 setiap bulannya**.
- **Jendela Pengingat**: Mulai tanggal **18 hingga 25 setiap bulan**, sistem otomatis memindai seluruh tugas aktif yang ditugaskan kepada Anda namun **belum memiliki catatan jam kerja (0 jam)**.
- **Pemberitahuan**: Lonceng notifikasi pada topbar dan banner peringatan di halaman Timesheet akan menyala, menampilkan daftar tugas yang belum diisi jam kerjanya beserta tombol cepat *Isi Timesheet*.

### 5.5 Export Laporan Excel Timesheet Resmi (Format Standar Elistec)
Aplikasi dilengkapi fitur ekspor berkas spreadsheet Excel profesional yang dirancang khusus sesuai format resmi laporan timesheet:
1. Buka menu **Timesheet** (`/Timesheet`).
2. Klik tombol **Export Timesheet Personal (Excel)**.
3. Pilih rentang tanggal (tersedia preset *Bulan Ini*, *Bulan Lalu*, *Minggu Ini*, atau rentang tanggal kustom).
4. Klik **Download Excel**.

> 📄 **Karakteristik Format Laporan Timesheet Excel**:
> - **Header Perusahaan**: Menampilkan judul *ELISTEC - Timesheet*, nama karyawan, total durasi dalam *Man-Days (MD)*, nama klien, dan nama proyek.
> - **Struktur Kolom Standar (A–O)**:
>   `Issue Key` | `Issue Summary` | `Hours` | `MD (=Hours/8)` | `Work Date` | `Username` | `Full Name` | `Period` | `Project Name` | `Project Name` | `Client` | `Activity Type` | `Working Place (WFO/WFH)` | `Clock In` | `Clock Out`
> - **Aturan Tanggal Lengkap (Tidak Ada Tanggal yang Di-skip)**: Seluruh hari dalam rentang periode (dari tanggal 1 hingga akhir periode) tetap dicantumkan secara runtut.
> - **Penandaan Hari Libur Otomatis**: Hari libur nasional yang terdaftar di Master Data akan otomatis ditandai dengan keterangan (contoh: *Hari Libur Nasional : Kemerdekaan RI*).
> - **Penandaan Akhir Pekan**: Hari Sabtu dan Minggu ditampilkan dengan baris penanda hari.
> - **Formula Excel Aktif**: Menggunakan formula dinamis `=C{baris}/8` untuk konversi Man-Days, serta `=SUM(...)` pada baris Total di bagian bawah.
> - **Blok Tanda Tangan**: Dilengkapi area pengesahan *Mengetahui* lengkap dengan nama karyawan dan lead/atasan.

---

## 6. Modul JSON Payload & Alat Bantu Pengujian API

Modul JSON Tools (`/JsonTools`) disediakan khusus untuk memudahkan tim (khususnya System Analyst, Backend Developer, dan QA) dalam mengolah data payload JSON saat pengujian integrasi sistem.

### 6.1 Merapikan Format JSON (Beautify / Pretty-Print)
- Tempel teks JSON yang panjang atau tidak beraturan ke dalam kotak editor input.
- Klik tombol **Format JSON**.
- Sistem akan merapikan indentasi baris menjadi struktur bertingkat yang sangat mudah dibaca.

### 6.2 Pemeriksaan & Validasi Sintaks JSON
- Klik tombol **Validasi JSON**.
- Sistem akan memverifikasi apakah format tanda kurung, koma, tanda kutip, dan tipe data sudah sesuai dengan standar JSON.
- Jika terdapat kesalahan, sistem akan menandai lokasi baris dan karakter yang tidak valid beserta penjelasan perbaikannya.

### 6.3 Memampatkan JSON (Minify)
- Klik tombol **Minify JSON**.
- Sistem akan menghapus seluruh spasi dan karakter baris baru (*newline*) yang tidak diperlukan sehingga ukuran teks payload menjadi sekecil mungkin, siap ditempelkan ke dalam parameter pengujian API.

### 6.4 Menyimpan & Mengelola Template Payload Pengujian
- Anda dapat memberi nama dan menyimpan potongan (*snippet*) JSON pengujian yang sering digunakan ke dalam daftar template.
- Template yang tersimpan dapat dimuat kembali kapan saja hanya dengan 1 kali klik.

---

## 7. Modul SQL Beautifier & Formatter

Modul SQL Beautifier (`/SqlTools`) dirancang untuk membantu System Analyst, Database Administrator, dan Software Engineer dalam merapikan, memformat, memvalidasi, dan mengompres kueri SQL dengan standar 15+ database engine modern.

### 7.1 Fitur & Kemampuan Format SQL
- **Dukungan 15+ Dialek Database Engine**: Standard SQL (ANSI), PostgreSQL, MySQL, MariaDB, SQLite, Transact-SQL (T-SQL / SQL Server), Oracle (PL/SQL), Google BigQuery, Snowflake, Amazon Redshift, Spark SQL, IBM DB2, Trino / Presto, Couchbase (N1QL), dan SingleStoreDB.
- **Kustomisasi Indentasi**: Mendukung 2 spasi, 4 spasi, 8 spasi, atau Tab (`\t`).
- **Kustomisasi Casing Huruf Keyword**: Ubah kata kunci SQL menjadi `UPPERCASE` (`SELECT`, `FROM`, `WHERE`), `lowercase` (`select`, `where`), atau pertahankan format asli (*Preserve*).
- **Format Instan Cepat**: Cukup tekan tombol pintas **Ctrl + Enter** pada editor untuk merapikan kueri secara instan.

### 7.2 Minify / Kompresi Kueri SQL
- Menghapus komentar baris (`--`) dan komentar blok (`/* ... */`), serta mereduksi whitespace berlebih menjadi single-line kueri padat berkecepatan transfer optimal.
- Menampilkan metrik rasio kompresi data (*Compression Ratio*) dan perbandingan ukuran byte.

### 7.3 Validasi Sintaks & Inspeksi Struktur
- Memeriksa keseimbangan tanda kurung `()`, tanda kutip tunggal `'`, dan tanda kutip ganda `"` secara otomatis.
- Mendeteksi letak nomor baris jika terdapat kesalahan penulisan struktur SQL.

### 7.4 Ekspor & Manajemen Snippet Kueri
- **Salin ke Clipboard**: Menyalin hasil kueri terformat dalam 1 klik.
- **Download File `.sql`**: Mengunduh file skrip SQL langsung ke komputer.
- **Simpan Snippet & Riwayat**: Simpan kueri ke database lokal dan dapat dihubungkan ke referensi Tugas Kerja (*Task*) tertentu untuk dokumentasi tim.

---

## 8. Modul Catatan Kerja & Dokumentasi (Notes)

Modul Catatan (`/Note`) berfungsi sebagai repositori dokumentasi teknis, rangkuman rapat (*minutes of meeting*), hasil analisis, dan catatan referensi harian.

### 7.1 Membuat Catatan dengan Rich-Text Editor
1. Buka menu **Catatan** dan klik **+ Buat Catatan Baru**.
2. Masukkan **Judul Catatan**.
3. Gunakan bilah alat editor (*Quill.js Rich-Text*) untuk memformat tulisan:
   - Teks tebal (*Bold*), miring (*Italic*), garis bawah (*Underline*), dan coret (*Strikethrough*).
   - Judul tingkatan (*Heading 1, Heading 2, Heading 3*).
   - Daftar berpoin (*Bullet list*) dan bernomor (*Numbered list*).
   - Blok kutipan (*Blockquote*) dan blok kode program (*Code Block*).
   - Penyisipan tautan link web dan tabel data.
4. Pilih warna latar belakang kartu catatan untuk mempermudah identifikasi visual.

### 7.2 Menyematkan Catatan Penting (Pin Note)
- Beri tanda centang pada opsi **Sematkan Catatan (Pin Note)** atau klik ikon jarum pin pada kartu catatan.
- Catatan yang disematkan akan selalu berada di posisi paling atas halaman sehingga tidak terlewatkan.

### 7.3 Menghubungkan Catatan ke Tugas atau Proyek
- Anda dapat mengaitkan catatan dengan **Tugas Tertentu** atau **Proyek Tertentu**.
- Catatan yang terhubung akan otomatis tampil pada tab dokumentasi di halaman rincian tugas dan proyek tersebut.

### 7.4 Mengunggah Berkas Lampiran
- Anda dapat melampirkan berkas dokumen (PDF, Word, Excel, gambar PNG/JPG, diagram arsitektur) ke dalam catatan.
- Berkas yang diunggah tersimpan secara rapi dan dapat diunduh kembali oleh rekan tim yang memiliki akses.

---

## 8. Modul Kalender Kerja & Jadwal

Buka menu **Kalender** (`/Calendar`) untuk melihat persebaran jadwal kerja:
- **Tampilan Fleksibel**: Pilihan tampilan Bulanan (*Month*), Mingguan (*Week*), Harian (*Day*), atau Daftar Agenda (*List View*).
- **Warna Indikator**: Setiap tugas diwarnai sesuai dengan warna proyek atau statusnya.
- **Interaksi Klik**: Mengklik salah satu jadwal pada kalender akan langsung membuka jendela ringkasan detail tugas dan tautan untuk mengedit.

---

## 9. Modul Laporan & Analitik Kinerja

Buka menu **Laporan** (`/Report`) untuk evaluasi produktivitas berkala:
- **Grafik Distribusi Jam Kerja**: Rekapitulasi perbandingan total jam kerja yang dicurahkan pada setiap proyek.
- **Matriks Beban Kerja Tim (Workload Matrix)**: Memantau jumlah tugas aktif per anggota tim untuk memastikan pembagian kerja seimbang.
- **Linimasa Gantt**: Menampilkan durasi dan urutan pengerjaan tugas dari awal hingga selesai dalam bentuk diagram batang linimasa.
- **Ekspor Laporan**: Fasilitas ekspor data rekapitulasi ke format Excel untuk kebutuhan laporan pertanggungjawaban mingguan/bulanan.

---

## 10. Modul Anggota Tim & Gamifikasi Prestasi

Buka menu **Anggota Tim** (`/Member`) untuk melihat rekan kerja dan kontribusinya.

### 10.1 Profil Pengguna & Level Pengalaman
- Setiap anggota tim memiliki kartu profil yang mencantumkan nama, jabatan (*Job Title*), total tugas yang diselesaikan, akumulasi jam kerja, dan level kontribusi (*Experience Level*).

### 10.2 Koleksi Badge Penghargaan
Aplikasi mengapresiasi pencapaian kerja anggota tim melalui lencana (*Badge*) prestasi:
- **Badge Otomatis**: Diberikan oleh sistem secara otomatis saat pengguna mencapai tonggak tertentu (misal: *First Task Master*, *Centurion 100 Jam Kerja*, *Top Contributor*, *Master Documenter*, dll).
- **Badge Khusus / Manual**: Administrator dapat memberikan lencana penghargaan khusus atas kinerja istimewa anggota tim.
- Anggota dapat memilih salah satu badge favorit untuk ditampilkan sebagai lencana utama (*Featured Badge*) pada profilnya.

### 10.3 Manajemen Pengguna oleh Administrator
Khusus untuk pengguna dengan peran **Admin**:
- **Menambah Anggota Baru**: Mengisi form pendaftaran anggota tim baru dengan email dan jabatan.
- **Ubah Kata Sandi Instan (Direct Password Reset)**: Admin dapat membantu mereset kata sandi anggota tim yang lupa password secara langsung tanpa memerlukan tautan email.
- **Menonaktifkan Pengguna (Inactivate)**: Menonaktifkan akun anggota yang sudah tidak bertugas. Akun yang berstatus *Inactive* otomatis disembunyikan dari statistik aktif Dashboard agar data analitik tetap relevan dan rapi.
- **Menghapus Pengguna Tanpa Tugas**: Admin dapat menghapus data akun pengguna jika akun tersebut belum memiliki riwayat tugas atau catatan jam kerja sama sekali (0 tasks & 0 sessions). Jika akun telah memiliki tugas terkait, sistem akan menyarankan tindakan nonaktifkan (*inactivate*) untuk menjaga keutuhan riwayat data.

---

## 11. Modul Master Data

Khusus untuk peran **Administrator**, menu **Master Data** (`/MasterData`) digunakan untuk mengatur konfigurasi dasar aplikasi:

### 11.1 Kategori, Prioritas, dan Status
- Menambah, mengubah, dan menghapus daftar kategori tugas beserta warna labelnya.
- Menyesuaikan tingkatan prioritas (*Critical, High, Medium, Low*) dan urutan tampilannya.
- Mengatur alur status kerja (*To Do, In Progress, Done, Testing, On Hold*).

### 11.2 Milestone SDLC Waterfall
- Mengatur tahapan siklus pengembangan perangkat lunak (misal: *BRD, FSD, TSD, Development, QA Testing, UAT, Deployment*).

### 11.3 Master Hari Libur Nasional & Cuti Bersama
- Mencatat daftar tanggal hari libur nasional dan cuti bersama resmi.
- Data hari libur ini terintegrasi secara otomatis dengan modul **Export Timesheet Excel**, sehingga pada saat laporan diekspor, sistem akan mengenali dan menandai hari libur tersebut secara akurat.
- Tersedia tombol cepat **Seed Hari Libur Nasional** untuk memuat daftar hari libur resmi tahun berjalan secara otomatis.

### 11.4 Konfigurasi Sistem & Backup/Export Database (.db & .sql)
Menu **Konfigurasi Sistem & Database** (`/Configuration`) menyediakan kontrol penuh bagi Administrator untuk:
- **Global Base URL**: Mengatur URL domain aktif aplikasi untuk integrasi REST API, Swagger UI, dan Webhook.
- **Export File Database (.db)**: Mengunduh berkas biner SQLite (`.db`) utuh untuk pencadangan offline (*full binary backup*) atau pemindahan (*migration*) ke server lain.
- **Export Script SQL (DDL & Data .sql)**: Mengunduh skrip SQL komprehensif yang berisi pernyataan pembuatan skema tabel (`CREATE TABLE`), indeks, dan seluruh baris kueri data (`INSERT INTO`) yang siap diimpor/direstore ke sistem database manapun.
- **Live Preview Script SQL**: Meninjau sintaks DDL skema database secara langsung melalui jendela pratinjau interaktif lengkap dengan fitur salin ke clipboard (*Copy*).
- **Shrink / Kompresi Database (VACUUM)**: Mengoptimalkan dan mengklaim kembali ruang kosong file SQLite setelah penghapusan data.
- **Reset Database**: Fitur pemeliharaan untuk mereset log transaksi atau melakukan *factory reset* data dengan pengamanan kode konfirmasi.

---

## 12. Tips & Pertanyaan Umum (FAQ)

### Q1: Bagaimana cara mencetak atau menyimpan panduan ini ke format PDF?
> **Jawaban**: Klik tombol **📖 Panduan** pada bilah atas aplikasi, lalu klik tombol **🖨️ Cetak / Simpan PDF**. Pada jendela print peramban, pilih tujuan printer sebagai **Save as PDF (Simpan sebagai PDF)** dan klik **Save**.

### Q2: Mengapa saya mendapatkan notifikasi pengingat Timesheet menjelang tanggal 25?
> **Jawaban**: Notifikasi tersebut merupakan pengingat otomatis bagi Anda yang memiliki tugas aktif namun belum mengisi catatan jam kerja (*timesheet*). Hal ini bertujuan agar seluruh jam kerja bulan berjalan terekam lengkap sebelum batas cut-off bulanan tanggal 25.

### Q3: Apakah saya bisa menjalankan timer untuk lebih dari satu tugas sekaligus?
> **Jawaban**: Ya. TrackerKerja mendukung multi-timer. Anda dapat menekan tombol *Clock In* pada beberapa tugas berbeda dan seluruh sesi waktu akan dicatat secara akurat.

### Q4: Apa yang harus dilakukan jika saya lupa mencatat jam kerja kemarin?
> **Jawaban**: Buka menu Timesheet, klik tombol **+ Tambah Jam Kerja Manual**, pilih tugas yang Anda kerjakan, lalu masukkan tanggal kemarin beserta jam mulai dan selesainya.

### Q5: Bagaimana cara melampirkan berkas dokumen pada catatan kerja?
> **Jawaban**: Buka menu Catatan (`/Note`), buat catatan baru atau edit catatan yang ada, lalu pada bagian *Lampiran Berkas*, pilih dokumen (PDF, gambar, spreadsheet) yang ingin diunggah dari komputer Anda.

---

*(Buku Panduan Pengguna Work Tracker Pro — Diterbitkan untuk Efisiensi & Transparansi Kerja Tim)*

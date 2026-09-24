# Buku Panduan Pengguna (User Guide)
# Work Tracker Pro (TrackerKerja)

> **Versi Aplikasi**: 3.6 (Enterprise Security, Dual Auth & Multi-Instance Edition)  
> **Target Pengguna**: Seluruh Karyawan, System Analyst, Developer, QA, Technical Writer, Project Lead, dan Administrator  
> **Terakhir Diperbarui**: 24 September 2026  

---

## Daftar Isi Panduan

1. [Pengenalan & Memulai Aplikasi](#1-pengenalan--memulai-aplikasi)
   - 1.1 [Halaman Masuk (Login Modern Lottie & Admin Approval)](#11-halaman-masuk-login-modern-lottie--admin-approval)
   - 1.2 [Tata Letak Antarmuka, Topbar Minimalis, Tur Layar & Paginasi Grid AJAX](#12-tata-letak-antarmuka-topbar-minimalis-tur-layar--paginasi-grid-ajax)
   - 1.3 [Kustomisasi 40 Tema Tampilan & 5 Google Fonts Switcher](#13-kustomisasi-40-tema-tampilan--5-google-fonts-switcher)
   - 1.4 [Keamanan Sesi, Peringatan 5 Menit & Auto-Logout Inaktivitas 1 Jam](#14-keamanan-sesi-peringatan-5-menit--auto-logout-inaktivitas-1-jam)
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
   - 3.7 [Import Data Tugas dari Excel (Format Standar & Format ARMS)](#37-import-data-tugas-dari-excel-format-standar--format-arms)
   - 3.8 [Penyatuan Formulir Edit Tugas & Pengisian Jam Kerja Manual (Satu Tombol Simpan)](#38-penyatuan-formulir-edit-tugas--pengisian-jam-kerja-manual-satu-tombol-simpan)
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
12. [Modul Anggota Tim (Member), Pure Grid Card, Banner Cover & Hapus Permanen Akun](#12-modul-anggota-tim-member-pure-grid-card-banner-cover--hapus-permanen-akun)
13. [Modul Master Data, Sinkronisasi Multi-Instance, Email & Backup](#13-modul-master-data-sinkronisasi-multi-instance-email--backup)
    - 13.1 [Master Kategori, Prioritas, dan Status](#131-master-kategori-prioritas-dan-status)
    - 13.2 [Master Milestone SDLC Waterfall](#132-master-milestone-sdlc-waterfall)
    - 13.3 [Master Hari Libur Nasional](#133-master-hari-libur-nasional)
    - 13.4 [Sinkronisasi Multi-Instance ke Host Induk (Online Streaming Base64 & Paket ZIP)](#134-sinkronisasi-multi-instance-ke-host-induk-online-streaming-base64--paket-zip)
    - 13.5 [Backup & Export Database (.db & .sql)](#135-backup--export-database-db--sql)
    - 13.6 [Integrasi Server Email (SMTP), Diagnostik Koneksi & 7 Template Event](#136-integrasi-server-email-smtp-diagnostik-koneksi--7-template-event)
14. [Tips & Pertanyaan Umum (FAQ)](#14-tips--pertanyaan-umum-faq)

---

## 1. Pengenalan & Memulai Aplikasi

**Work Tracker Pro (TrackerKerja)** adalah aplikasi manajemen pekerjaan terpadu yang dirancang untuk mempermudah tim dalam merencanakan tugas, mencatat waktu kerja secara akurat (*timesheet*), mengelola absensi kehadiran, mendokumentasikan kendala dan solusi teknis, mengolah payload data/SQL, serta menghasilkan laporan kerja siap pakai.

### 1.1 Halaman Masuk (Login Modern Lottie & Admin Approval)
1. **Masuk ke Aplikasi (Login)**:
   - Buka peramban web dan akses alamat aplikasi TrackerKerja.
   - Halaman login dilengkapi animasi visual **Lottie modern**, tombol sakelar instan **Mode Gelap / Terang** di pojok kanan atas tanpa reload, dan dropdown perusahaan bertenaga **Select2**.
   - Masukkan **Alamat Email** dan **Kata Sandi (Password)** Anda yang telah terverifikasi.
   - Beri tanda centang pada opsi **Ingat Saya (Remember Me)** jika Anda ingin sesi login tetap tersimpan pada perangkat pribadi.
   - Klik tombol **Masuk (Login)** untuk masuk ke Dashboard utama.
   - *Catatan Keamanan*: Jika sesi Anda kedaluwarsa karena tidak ada aktivitas selama 1 jam, sistem otomatis mengalihkan Anda kembali ke halaman ini dengan notifikasi kuning: *"Sesi Anda telah berakhir karena tidak ada aktivitas selama 1 jam. Silakan masuk kembali."*

2. **Pendaftaran Pengguna Baru (Registrasi Akun Mandiri)**:
   - Pada halaman login, klik tombol **Daftar Akun Baru**.
   - Isi formulir pendaftaran:
     - **Nama Lengkap**: Nama lengkap Anda.
     - **Alamat Email**: Email aktif kantor atau pribadi.
     - **Jabatan / Posisi**: Peran kerja Anda (misal: *Lead Developer*, *Quality Assurance*, *System Analyst*).
     - **Organisasi / Perusahaan**: Pilih nama perusahaan yang sudah terdaftar atau pilih *Buat Perusahaan Baru* untuk mendaftarkan nama/kode perusahaan tim Anda.
     - **Kata Sandi & Konfirmasi Kata Sandi**: Minimal 6 karakter dengan kombinasi huruf dan angka.
   - Klik **Daftar Sekarang**.

3. **Alur Persetujuan Administrator (Admin Approval)**:
   - Setelah formulir registrasi dikirimkan, akun baru Anda akan berstatus **Menunggu Persetujuan (Pending Approval)**.
   - Anda **belum dapat login** ke aplikasi sampai Administrator meninjau dan menyetujui akun Anda.
   - Sistem akan secara otomatis mengirimkan notifikasi email ke Administrator.
   - Begitu Administrator menyetujui akun Anda, Anda akan menerima email pemberitahuan resmi bahwa akun telah aktif dan siap digunakan untuk login.
   - *(Bagi Administrator)*: Untuk meninjau pendaftaran baru, buka menu **Anggota Tim (`/Member`)**, buka tab **Menunggu Persetujuan**, lalu klik tombol hijau **Setujui (Approve)** atau tombol merah **Tolak (Reject)** jika pendaftaran tidak valid.

### 1.2 Tata Letak Antarmuka, Topbar Minimalis, Tur Layar & Paginasi Grid AJAX
Aplikasi TrackerKerja dirancang dengan antarmuka yang bersih, modern, dan ergonomis:
- **Bilah Samping (Sidebar)**: Pusat navigasi seluruh modul kerja (Dashboard, Tugas, Kanban, Proyek, Timesheet, Absensi, Kalender, Catatan, JSON Tools, SQL Tools, Laporan, Anggota Tim, Master Data, Konfigurasi Sistem, **Swagger API**, dan **Panduan Pengguna PDF**).
- **Bilah Atas (Topbar)**: Menampilkan judul halaman aktif, badge nama perusahaan/tim Anda, kotak pencarian cepat global, tombol aksi cepat *Import Excel*, lonceng notifikasi tugas cerdas, pemilih 40 tema dinamis & 5 font, serta kartu avatar profil akun dengan tombol dropdown navigasi lengkap.
- **Tur Interaktif Layar (*Interactive Onboarding Tour*)**:
  - Saat pertama kali menggunakan aplikasi atau kapan saja dibutuhkan, Anda dapat memulai tur interaktif 6 langkah yang menyoroti modul-modul esensial.
  - Untuk memulai tur, klik menu profil Anda lalu pilih **Mulai Tur Aplikasi**, atau klik tombol bantuan di halaman panduan pengguna.
- **Paginasi Grid Tabel AJAX (*Zero Reload* - `ajax-grid-manager.js`)**:
  - Seluruh tabel utama (Tugas, Anggota Tim, Presensi, Audit Trail, dan Timesheet) mendukung navigasi halaman instan tanpa perlu memuat ulang seluruh halaman peramban web (*zero page reload*).
  - Pilihan sorting kolom, pencarian, dan perpindahan nomor halaman terjadi secara mulus dengan tetap mempertahankan posisi scroll dan status filter.

### 1.3 Kustomisasi 40 Tema Tampilan & 5 Google Fonts Switcher
Aplikasi menyediakan 40 pilihan tema warna eye-friendly dan 5 opsi jenis huruf Google Fonts:
1. **Pemilih Tema Cepat (40 Tema)**:
   - Klik tombol **Tema** di bilah atas (Topbar) atau melalui menu profil.
   - **22 Tema Terang (Light Themes)**: Indigo Nebula, Emerald Forest, Ocean Azure, Sunset Crimson, Cyberpunk Neon, Royal Amethyst, Amber Gold, Slate Minimalist, Nordic Teal, Midnight Titanium, dan 12 tema eye-friendly lainnya.
   - **18 Tema Gelap (Dark Themes)**: Nordic Frost, Midnight OLED, Cyberpunk Synthwave, Emerald Matrix, Dracula Eclipse, Abyssal Ocean, Solar Ember, dan tema ramah mata malam hari lainnya.
   - Seluruh elemen warna berganti seketika (*instant CSS transition*).
2. **Global Font Switcher (5 Pilihan Google Fonts)**:
   - Pada panel tema, pilih jenis font yang paling nyaman untuk dibaca:
     - **Inter** (Default): Seimbang, tajam, dan sangat mudah dibaca di layar kerja.
     - **Plus Jakarta Sans**: Tipografi geometris modern bergaya enterprise SaaS.
     - **Outfit**: Sans-serif kontemporer dengan kurva visual halus berkelas.
     - **Poppins**: Rounded energik yang ramah dan nyaman dipindai mata.
     - **Roboto**: Presisi tinggi dengan keterbacaan data yang rapat dan rapi.
   - Pilihan tema dan font Anda otomatis disimpan di penyimpanan lokal peramban (*localStorage*) dan langsung aktif saat membuka halaman berikutnya tanpa kedipan layar (*Anti-FOUC*).

### 1.4 Keamanan Sesi, Peringatan 5 Menit & Auto-Logout Inaktivitas 1 Jam
Untuk melindungi kerahasiaan data proyek dan jam kerja dari akses tanpa izin pada perangkat yang ditinggalkan:
- **Deteksi Inaktivitas Cerdas (`session-manager.js`)**: Sistem secara konstan mendeteksi interaksi pengguna (pergerakan mouse, ketikan keyboard, scroll layar, dan sentuhan).
- **Peringatan 5 Menit Sebelum Logout**: Jika tidak terdeteksi aktivitas selama **55 menit**, sebuah dialog modal peringatan akan muncul di tengah layar menampilkan hitung mundur waktu tersisa (300 detik) dan tombol **"Lanjutkan Sesi"**.
- **Perpanjangan Sesi**: Cukup gerakkan kursor atau klik tombol "Lanjutkan Sesi", timer inaktivitas akan di-reset kembali ke awal (60 menit).
- **Auto-Logout Otomatis**: Jika pengguna tetap tidak merespons hingga menit ke-60, sesi kerja akan dikunci secara otomatis demi keamanan dan diarahkan ke halaman login dengan informasi sesi berakhir.

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

### 3.7 Import Data Tugas dari Excel (Format Standar & Format ARMS)
Aplikasi mendukung impor banyak tugas sekaligus melalui berkas spreadsheet Excel `.xlsx` menggunakan dua standar format:
1. **Format Standar (9 Kolom)**: Sangat cocok untuk migrasi cepat daftar backlog tugas, dilengkapi wizard preview interaktif dan tombol penugasan massal (*Bulk Assign* PIC).
2. **Format ARMS Enterprise (21 Kolom)**: Format terstruktur enterprise yang memetakan kode tugas, modul, prioritas, status, PIC penugasan, stakeholder, kendala (*obstacle*), solusi (*solution*), catatan, serta tahapan siklus Waterfall SDLC Milestone.
3. **Cara Melakukan Impor**:
   - Buka menu **Import Task** (`/Import`).
   - Unduh template Excel yang diinginkan (*Template 9-Kolom* atau *Template ARMS 21-Kolom*).
   - Unggah berkas yang telah diisi, tinjau tabel pratinjau data, lalu klik **Konfirmasi Import Task**.

### 3.8 Penyatuan Formulir Edit Tugas & Pengisian Jam Kerja Manual (Satu Tombol Simpan)
Untuk mempercepat alur kerja harian pengembang dan analis sistem:
- Pada halaman **Ubah Tugas** (`/Task/Edit/{id}`), selain memperbarui rincian tugas (judul, status, progress, kendala, solusi), formulir kini menyediakan seksi terintegrasi **Catat Jam Kerja Sesi Ini (Manual Timesheet)**.
- Anda dapat langsung mengisi:
  - **Durasi Jam & Menit**: Misal *2 Jam 30 Menit*.
  - **Tanggal Sesi Kerja**: Tanggal saat pekerjaan dilakukan (default: hari ini).
  - **Catatan Aktivitas Sesi**: Rincian teknis pekerjaan yang baru saja diselesaikan.
- **Tombol Aksi Tunggal ("Simpan Perubahan & Sesi Kerja Manual")**:
  - Cukup satu kali klik, sistem secara atomik akan memperbarui informasi tugas di tabel `WorkTasks` dan sekaligus membuat entri log sesi kerja baru di tabel `WorkSessions`.
  - Anda tidak perlu lagi berpindah bolak-balik antara menu Tugas dan menu Timesheet terpisah.

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

## 12. Modul Anggota Tim (Member), Pure Grid Card, Banner Cover & Hapus Permanen Akun

Buka menu **Anggota Tim** (`/Member`):
- **Direktori Pure Grid Card Layout**:
  - Seluruh anggota tim disajikan dalam format kartu grid modern dan rapi (`grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4`).
  - Dilengkapi sistem *Anti-Overflow*: Teks nama panjang, email, dan jabatan otomatis dipotong secara estetis (*ellipsis*) dan dapat dilihat lengkap melalui tooltip saat kursor diarahkan (*hover*).
  - Menampilkan ringkasan metrik beban kerja, tugas selesai, dan total jam kerja yang terekam.
- **Kustomisasi Banner Cover Profil Karyawan**:
  - Setiap pengguna dapat mempercantik tampilan profilnya melalui menu **Profil Akun** (`/Account/Profile`).
  - Unggah berkas gambar foto sampul (*Cover Banner*) dengan format JPG/PNG/WebP.
  - Tersedia opsi untuk menghapus cover kustom dan kembali ke banner vektor SVG default (`default-profile-cover.svg`).
- **Badge Prestasi & Gamifikasi**:
  - Anggota tim dapat meraih lencana prestasi (*badges*) otomatis berdasarkan produktivitas kerja dan pencapaian milestone.
- **Admin Direct Password Reset**:
  - Administrator dapat mereset kata sandi akun anggota secara langsung melalui antarmuka kartu anggota tanpa menunggu proses pemulihan email.
- **Hapus Permanen Akun (*Permanent User Deletion*)**:
  - Disediakan khusus bagi peran **Administrator** untuk membersihkan akun uji coba atau akun yang sudah tidak relevan.
  - **Mekanisme Proteksi Ganda (Double Verification)**:
    1. Klik tombol merah **Hapus Permanen** pada kartu atau detail anggota.
    2. Jendela modal konfirmasi keamanan akan terbuka dan mewajibkan Admin mengetikkan nama lengkap pengguna target secara persis.
    3. Masukkan kata sandi Administrator untuk mengesahkan tindakan.
    4. Sistem akan menghapus rekaman pengguna dari basis data ASP.NET Identity secara aman dan membebaskan penugasan tugas yang terkait.

---

## 13. Modul Master Data, Sinkronisasi Multi-Instance, Email & Backup

Khusus untuk peran **Administrator**, menu **Master Data** (`/MasterData`) dan **Konfigurasi** (`/Configuration`):

### 13.1 Master Kategori, Prioritas, dan Status
- Mengelola kategori tugas, prioritas (*Critical, High, Medium, Low*), dan status alur kerja.

### 13.2 Master Milestone SDLC Waterfall
- Mengatur tahapan siklus pengembangan perangkat lunak (Requirement, Design, Development, Testing, Deployment).

### 13.3 Master Hari Libur Nasional
- Mengelola daftar hari libur resmi yang terintegrasi otomatis dengan laporan Timesheet Excel.

### 13.4 Sinkronisasi Multi-Instance ke Host Induk (Online Streaming Base64 & Paket ZIP)
Fitur ini memungkinkan instance lokal atau laptop cabang melakukan sinkronisasi data pekerjaan dan seluruh berkas lampiran ke atau dari Server Host Induk terpusat:
- **1. Online Push Sync (Kirim ke Host)**:
  - Masukkan URL Host Induk (contoh: `https://host-tracker.perusahaan.com`) dan **API Secret Key** (`X-Sync-Key`) atau token JWT Bearer.
  - Aktifkan tombol toggle **"Sertakan Berkas Uploads & Lampiran"** agar seluruh berkas lampiran catatan (`uploads/notes/*`), avatar profil (`uploads/avatars/*`), dan banner sampul (`uploads/covers/*`) ikut dikemas (Base64 streaming) dan dikirimkan ke server induk.
  - Klik **"Test Koneksi"** untuk memverifikasi kesiapan host dan melihat statistik data/file di server tujuan.
  - Klik **"Mulai Push Sync ke Host"** untuk menjalankan sinkronisasi data dan berkas secara otomatis.
- **2. Online Pull Sync (Tarik dari Host)**:
  - Digunakan pada server child untuk menarik pembaruan tugas dan lampiran terbaru dari server induk.
  - Klik tombol **"Tarik Data dari Host (Pull Sync)"**.
- **3. Ekspor Paket Lengkap (.zip Offline)**:
  - Klik tombol **"Unduh Paket Sinkronisasi (.zip)"** pada tab *Ekspor Paket*.
  - Menghasilkan file `.zip` terstruktur berisi `manifest.json`, `sync_data.sql` DML terurut, dan seluruh folder berkas `uploads/` (catatan, avatar, cover).
- **4. Impor Paket (.zip / .sql)**:
  - Buka tab *Impor Data / Paket*, lalu unggah file `.zip` (Full Package) atau `.sql` (Dump SQL).
  - Sistem akan mengekstrak berkas lampiran ke folder target secara aman serta mengeksekusi script transaksi database secara otomatis.

### 13.5 Backup & Export Database (.db & .sql)
- **Export File Database (.db)**: Mengunduh berkas biner SQLite `.db` utuh untuk pencadangan offline (*full binary backup*).
- **Export Script SQL (.sql)**: Mengunduh skrip DDL dan DML lengkap yang siap direstore ke database manapun.
- **Kompresi Database (VACUUM)**: Mengoptimalkan dan mengklaim kembali ruang kosong file SQLite.

### 13.6 Integrasi Server Email (SMTP) & Uji Koneksi
Modul ini memungkinkan sistem TrackerKerja mengirimkan email notifikasi otomatis kepada pengguna dan administrator untuk event-event krusial:
1. **Mengonfigurasi Server SMTP**:
   - Buka menu **Konfigurasi Sistem (`/Configuration`)**.
   - Pada kartu **Server Mail (SMTP) & Pengiriman Email**, masukkan parameter koneksi server email:
     - **Host SMTP / Mail Server**: Alamat host mail provider Anda (contoh: `smtp.gmail.com`, `smtp.office365.com`, atau `smtp.mailtrap.io`).
     - **Port SMTP**: Port koneksi (contoh: `587` untuk STARTTLS, `465` untuk SSL/TLS murni, atau `2525`).
     - **Email Pengirim**: Alamat email yang digunakan untuk mengirim pesan (contoh: `notifications@perusahaan.com`).
     - **Nama Pengirim**: Nama identitas pengirim email (contoh: `Work Tracker Notification Engine`).
     - **Kata Sandi Email (App Password)**: Password akun atau App Password khusus aplikasi. Kata sandi yang tersimpan akan disamarkan (`••••••••`).
     - **Aktifkan Enkripsi SSL / TLS**: Centang opsi ini untuk keamanan enkripsi data transmisi (sangat disarankan).
     - **Status Integrasi**: Pastikan sakelar aktif untuk mengizinkan sistem mengirimkan email secara otomatis.
   - Klik **Simpan Pengaturan SMTP**.

2. **Melakukan Uji Koneksi Email (Test Connection & Diagnostics)**:
   - Pada kartu sebelah kanan **Uji Koneksi & Diagnostik SMTP**:
     - Masukkan alamat email tujuan pengujian pada kolom **Email Penerima Uji Coba**.
     - Klik tombol **Kirim Email Percobaan**.
     - Sistem akan melakukan *handshake* langsung ke server mail dan mengukur latensi pengiriman dalam milidetik (*latency ms*).
     - Hasil uji koneksi dan log diagnostik teknis (*Diagnostics Box*) akan ditampilkan langsung di layar (misal respon `250 OK` dan status enkripsi TLS).

### 13.7 Manajemen Template Email Event & Live Preview
Sistem menyediakan sub-modul khusus untuk mengatur format dan isi pesan email untuk setiap peristiwa (*event*) yang terjadi:
1. **Memilih & Memfilter Template**:
   - Pada kartu **Template Email Notifikasi & Pengingat Event**, pilih kategori template:
     - **Semua Event**: Menampilkan seluruh template.
     - **Autentikasi**: Template pendaftaran akun, approval, penolakan, dan reset password.
     - **Admin Alert**: Template pemberitahuan pendaftaran baru khusus untuk Administrator.
     - **Manajemen Tugas**: Template penugasan PIC tugas baru dan pembaruan status tugas.
2. **Menyesuaikan Template (Subjek & Isi Pesan)**:
   - Klik tombol **Edit Template** pada kartu event yang ingin diubah.
   - Pada modal editor:
     - Ubah **Subjek Email** sesuai kebutuhan format tim Anda.
     - Ubah **Isi Pesan (Format HTML)**. Anda dapat mengatur tata letak, warna tombol, dan susunan paragraf.
   - **Menggunakan Variabel Placeholder**:
     - Klik pada chip variabel yang tersedia (misal `{FullName}`, `{TaskTitle}`, `{ProjectName}`, `{ActionUrl}`, `{DueDate}`) untuk menyalinnya secara instan, lalu tempelkan ke dalam subjek atau isi pesan. Saat email dikirim, sistem akan otomatis mengganti tag ini dengan data yang sebenarnya.
   - Klik tombol **Simpan Template**.
3. **Pratinjau Langsung (Live Preview Modal)**:
   - Klik tombol **Pratinjau (Preview)** pada kartu template.
   - Sistem akan me-render template lengkap dengan data contoh secara real-time sehingga Anda dapat memastikan tampilan email tampak rapi, profesional, dan responsif.
4. **Memulihkan Template Bawaan (Reset ke Default)**:
   - Jika Anda ingin mengembalikan format template ke rancangan default pabrik, klik tombol **Reset ke Default** di pojok kanan atas sub-modul template.

---

## 14. Tips & Pertanyaan Umum (FAQ)

### Q1: Bagaimana cara mencetak atau menyimpan panduan ini ke format PDF?
> **Jawaban**: Klik menu **📖 Panduan Pengguna** pada bilah samping (Sidebar) navigasi aplikasi di bagian bawah (*Akun & Bantuan*). Pada jendela modal panduan yang terbuka, klik tombol **🖨️ Cetak / Simpan PDF**. Pada jendela print peramban, pilih tujuan printer sebagai **Save as PDF (Simpan sebagai PDF)** dan klik **Save**.

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

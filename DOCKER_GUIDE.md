# 🐳 Panduan Lengkap Docker - Work Tracker Pro

Panduan resmi untuk menjalankan aplikasi **Work Tracker Pro** menggunakan Docker dan Docker Compose di lingkungan Production maupun Development.

---

## 📋 Daftar Isi
1. [Prasyarat](#-prasyarat)
2. [Menjalankan dengan Docker Compose (Sangat Disarankan)](#-menjalankan-dengan-docker-compose-sangat-disarankan)
3. [Menggunakan Script Helper (PowerShell & Bash)](#-menggunakan-script-helper-powershell--bash)
4. [Menjalankan Manual dengan Docker CLI](#-menjalankan-manual-dengan-docker-cli)
5. [Persistensi Data & Migrasi Database](#-persistensi-data--migrasi-database)
6. [Konfigurasi Environment Variables](#-konfigurasi-environment-variables)
7. [Akses Aplikasi & Endpoint Penting](#-akses-aplikasi--endpoint-penting)
8. [Perawatan & Troubleshooting](#-perawatan--troubleshooting)

---

## 🛠 Prasyarat
Pastikan Anda telah menginstal:
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows / macOS) atau **Docker Engine + Docker Compose v2** (Linux).
- Port **`5000`** tersedia di host machine (bisa diubah di `docker-compose.yml` jika diperlukan).

---

## 🚀 Menjalankan dengan Docker Compose (Sangat Disarankan)

Docker Compose adalah cara termudah dan tercepat untuk menjalankan seluruh sistem dengan konfigurasi persistensi volume yang otomatis.

### 1. Jalankan Aplikasi
```bash
docker compose up -d --build
```

### 2. Cek Status Container
```bash
docker compose ps
```

### 3. Lihat Log Real-time
```bash
docker compose logs -f trackerkerja
```

### 4. Hentikan Aplikasi
```bash
docker compose down
```

---

## ⚡ Menggunakan Script Helper (PowerShell & Bash)

Kami menyediakan skrip otomatisasi untuk memudahkan pengelolaan container:

### Di Windows (PowerShell):
```powershell
# Jalankan container (default)
.\docker-run.ps1 up

# Salin database lokal & uploads yang ada ke volume Docker
.\docker-run.ps1 init-data

# Lihat live streaming log
.\docker-run.ps1 logs

# Restart container
.\docker-run.ps1 restart

# Cek status
.\docker-run.ps1 status

# Matikan container
.\docker-run.ps1 down
```

### Di Linux / macOS / Git Bash:
```bash
chmod +x ./docker-run.sh

./docker-run.sh up
./docker-run.sh init-data
./docker-run.sh logs
./docker-run.sh status
./docker-run.sh down
```

---

## 📦 Menjalankan Manual dengan Docker CLI

Jika Anda ingin mem-build dan menjalankan image secara mandiri tanpa docker-compose:

### 1. Build Docker Image
```bash
docker build -t trackerkerja:latest .
```

### 2. Buat Folder Penyimpanan Host
```bash
mkdir -p ./db_data
mkdir -p ./uploads
```

### 3. Run Container
```bash
docker run -d \
  --name trackerkerja_app \
  -p 5000:5000 \
  -v "$(pwd)/db_data:/app/data" \
  -v "$(pwd)/uploads:/app/wwwroot/uploads" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:5000 \
  -e ConnectionStrings__DefaultConnection="Data Source=data/trackerkerja.db" \
  -e GlobalBaseUrl=http://localhost:5000 \
  --restart unless-stopped \
  trackerkerja:latest
```

---

## 💾 Persistensi Data & Migrasi Database

Aplikasi menggunakan **SQLite** dan file upload lokal. Agar data tidak hilang saat container diperbarui:

| Direktori Host | Direktori Container | Fungsi |
| :--- | :--- | :--- |
| `./db_data/` | `/app/data/` | Menyimpan file database `trackerkerja.db` |
| `./uploads/` | `/app/wwwroot/uploads/` | Menyimpan foto avatar dan lampiran berkas catatan |

### Inisialisasi Data dari Lingkungan Lokal:
Jika Anda sudah memiliki database `trackerkerja.db` dan folder `wwwroot/uploads` di lokal dan ingin langsung menggunakannya di Docker:
```bash
# Otomatis via PowerShell
.\docker-run.ps1 init-data

# Atau manual:
cp trackerkerja.db ./db_data/trackerkerja.db
cp -r wwwroot/uploads/* ./uploads/
```

> **Catatan:** Jika `./db_data/trackerkerja.db` belum ada saat pertama kali container berjalan, aplikasi akan secara otomatis membuat database baru, menjalankan migrasi EF Core, dan men-seed akun admin default serta master data awal!

---

## ⚙ Konfigurasi Environment Variables

Anda dapat mengatur variabel lingkungan di file `docker-compose.yml` atau parameter `-e`:

| Variabel | Default | Keterangan |
| :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | `Production` | `Production` (optimized) atau `Development` (debug error details) |
| `ASPNETCORE_URLS` | `http://+:5000` | Port binding aplikasi di dalam container |
| `ConnectionStrings__DefaultConnection` | `Data Source=data/trackerkerja.db` | Path file SQLite |
| `GlobalBaseUrl` | `http://localhost:5000` | URL publik untuk Swagger, webhook, dan ekspor |

---

## 🌐 Akses Aplikasi & Endpoint Penting

Setelah container berjalan:

- **Web Dashboard**: [http://localhost:5000](http://localhost:5000)
- **Swagger REST API Docs**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **Postman API Spec**: Endpoint `/api/v1/...`
- **Kredensial Default**:
  - **Email**: `admin@trackerkerja.com`
  - **Password**: `Admin123!`

---

## 🔧 Perawatan & Troubleshooting

### 1. Port 5000 Sudah Terpakai di Host?
Ganti mapping port di `docker-compose.yml`, misalnya menggunakan port `8080` atau `5050`:
```yaml
ports:
  - "5050:5000"
```
Akses aplikasi melalui `http://localhost:5050`.

### 2. Backup Database
Cukup copy file `./db_data/trackerkerja.db` ke tempat aman:
```powershell
Copy-Item ./db_data/trackerkerja.db ./backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').db
```

### 3. Masuk ke Shell Container untuk Debug
```bash
docker exec -it trackerkerja_app /bin/bash
# atau
docker exec -it trackerkerja_app /bin/sh
```

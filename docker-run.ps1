# ==============================================================================
# Work Tracker Pro - Docker Management Helper Script (PowerShell)
# ==============================================================================
param (
    [Parameter(Position=0)]
    [ValidateSet("up", "down", "restart", "build", "logs", "status", "init-data", "help")]
    [string]$Action = "up"
)

Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  Work Tracker Pro - Docker Assistant" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan

function Ensure-Directories {
    if (-not (Test-Path -Path "./db_data")) {
        New-Item -ItemType Directory -Path "./db_data" -Force | Out-Null
        Write-Host "[+] Created ./db_data directory for SQLite persistence." -ForegroundColor Green
    }
    if (-not (Test-Path -Path "./uploads")) {
        New-Item -ItemType Directory -Path "./uploads" -Force | Out-Null
        New-Item -ItemType Directory -Path "./uploads/notes" -Force | Out-Null
        New-Item -ItemType Directory -Path "./uploads/avatars" -Force | Out-Null
        Write-Host "[+] Created ./uploads directories for file storage persistence." -ForegroundColor Green
    }
}

switch ($Action) {
    "up" {
        Ensure-Directories
        Write-Host "`n[*] Starting TrackerKerja container in background..." -ForegroundColor Yellow
        docker compose up -d --build
        if ($LASTEXITCODE -eq 0) {
            Write-Host "`n[SUCCESS] TrackerKerja is running!" -ForegroundColor Green
            Write-Host " - Web App URL : http://localhost:5000" -ForegroundColor Cyan
            Write-Host " - Swagger API : http://localhost:5000/swagger" -ForegroundColor Cyan
            Write-Host " - Default Login: admin@trackerkerja.com / Admin123!" -ForegroundColor White
        }
    }
    "down" {
        Write-Host "`n[*] Stopping and removing TrackerKerja container..." -ForegroundColor Yellow
        docker compose down
        Write-Host "[SUCCESS] Container stopped." -ForegroundColor Green
    }
    "restart" {
        Write-Host "`n[*] Restarting TrackerKerja container..." -ForegroundColor Yellow
        docker compose restart
        Write-Host "[SUCCESS] Container restarted." -ForegroundColor Green
    }
    "build" {
        Write-Host "`n[*] Building Docker image (trackerkerja:latest)..." -ForegroundColor Yellow
        docker compose build --no-cache
        Write-Host "[SUCCESS] Docker image built successfully." -ForegroundColor Green
    }
    "logs" {
        Write-Host "`n[*] Streaming container logs (Ctrl+C to exit)..." -ForegroundColor Yellow
        docker compose logs -f trackerkerja
    }
    "status" {
        Write-Host "`n[*] Container status:" -ForegroundColor Yellow
        docker compose ps
    }
    "init-data" {
        Ensure-Directories
        Write-Host "`n[*] Copying existing local database and uploads to persistent Docker volumes..." -ForegroundColor Yellow
        if (Test-Path "./trackerkerja.db") {
            Copy-Item -Path "./trackerkerja.db" -Destination "./db_data/trackerkerja.db" -Force
            Write-Host "[+] Copied trackerkerja.db -> ./db_data/trackerkerja.db" -ForegroundColor Green
        } else {
            Write-Host "[-] trackerkerja.db not found in root. It will be auto-migrated on first boot." -ForegroundColor DarkGray
        }

        if (Test-Path "./wwwroot/uploads") {
            Copy-Item -Path "./wwwroot/uploads/*" -Destination "./uploads" -Recurse -Force
            Write-Host "[+] Copied wwwroot/uploads -> ./uploads/" -ForegroundColor Green
        }
        Write-Host "[SUCCESS] Persistent data initialization complete!" -ForegroundColor Green
    }
    "help" {
        Write-Host "`nUsage: .\docker-run.ps1 [action]" -ForegroundColor White
        Write-Host "Actions:" -ForegroundColor White
        Write-Host "  up         : Build and start the container in the background (default)" -ForegroundColor Cyan
        Write-Host "  down       : Stop and remove running containers" -ForegroundColor Cyan
        Write-Host "  restart    : Restart the container" -ForegroundColor Cyan
        Write-Host "  build      : Rebuild the Docker image without cache" -ForegroundColor Cyan
        Write-Host "  logs       : View live streaming logs" -ForegroundColor Cyan
        Write-Host "  status     : Check container running status" -ForegroundColor Cyan
        Write-Host "  init-data  : Copy local trackerkerja.db and uploads into ./db_data and ./uploads" -ForegroundColor Cyan
        Write-Host "  help       : Show this help message" -ForegroundColor Cyan
    }
}

#!/usr/bin/env bash
# ==============================================================================
# Work Tracker Pro - Docker Management Helper Script (Bash)
# ==============================================================================

ACTION="${1:-up}"

echo "======================================================"
echo "  Work Tracker Pro - Docker Assistant (Bash)"
echo "======================================================"

ensure_directories() {
    mkdir -p ./db_data
    mkdir -p ./uploads/notes
    mkdir -p ./uploads/avatars
}

case "$ACTION" in
    up)
        ensure_directories
        echo "[*] Starting TrackerKerja container in background..."
        docker compose up -d --build
        echo ""
        echo "[SUCCESS] TrackerKerja is running!"
        echo " - Web App URL : http://localhost:5000"
        echo " - Swagger API : http://localhost:5000/swagger"
        echo " - Default Login: admin@trackerkerja.com / Admin123!"
        ;;
    down)
        echo "[*] Stopping TrackerKerja container..."
        docker compose down
        echo "[SUCCESS] Container stopped."
        ;;
    restart)
        echo "[*] Restarting TrackerKerja container..."
        docker compose restart
        echo "[SUCCESS] Container restarted."
        ;;
    build)
        echo "[*] Building Docker image..."
        docker compose build --no-cache
        echo "[SUCCESS] Image built."
        ;;
    logs)
        echo "[*] Streaming logs (Ctrl+C to exit)..."
        docker compose logs -f trackerkerja
        ;;
    status)
        echo "[*] Container status:"
        docker compose ps
        ;;
    init-data)
        ensure_directories
        echo "[*] Copying local database and uploads to Docker volumes..."
        if [ -f "./trackerkerja.db" ]; then
            cp -f ./trackerkerja.db ./db_data/trackerkerja.db
            echo "[+] Copied trackerkerja.db -> ./db_data/trackerkerja.db"
        fi
        if [ -d "./wwwroot/uploads" ]; then
            cp -rf ./wwwroot/uploads/* ./uploads/ 2>/dev/null || true
            echo "[+] Copied wwwroot/uploads -> ./uploads/"
        fi
        echo "[SUCCESS] Persistent data initialization complete!"
        ;;
    *)
        echo "Usage: ./docker-run.sh [up|down|restart|build|logs|status|init-data]"
        ;;
esac

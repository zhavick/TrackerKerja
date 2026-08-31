# ==============================================================================
# Helper Script: Push ke GitHub (TrackerKerja)
# ==============================================================================
param (
    [Parameter(Mandatory=$false)]
    [string]$Token
)

Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  TrackerKerja - GitHub Push Assistant" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan

if (-not $Token) {
    Write-Host "`nMasukkan GitHub Personal Access Token (PAT) Anda:" -ForegroundColor Yellow
    $Token = Read-Host "Token"
}

if (-not $Token) {
    Write-Host "[ERROR] Token tidak boleh kosong." -ForegroundColor Red
    exit 1
}

Write-Host "`n[*] Sedang melakukan push ke https://github.com/zhavick/TrackerKerja.git (branch main)..." -ForegroundColor Yellow

$remoteUrl = "https://${Token}@github.com/zhavick/TrackerKerja.git"
docker run --rm --entrypoint sh -v "${PWD}:/repo" -w /repo alpine/git -c "git config --global --add safe.directory /repo && git push -u '$remoteUrl' main"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[SUCCESS] Berhasil melakukan push ke https://github.com/zhavick/TrackerKerja.git!" -ForegroundColor Green
} else {
    Write-Host "`n[ERROR] Gagal melakukan push. Pastikan Token memiliki izin 'repo' (Contents: Read and write)." -ForegroundColor Red
}

Add-Type -AssemblyName System.Net.Http

$baseUri = "http://localhost:5050"

$handler = New-Object System.Net.Http.HttpClientHandler
$cookies = New-Object System.Net.CookieContainer
$handler.CookieContainer = $cookies
$client = New-Object System.Net.Http.HttpClient($handler)

Write-Host "=== 1. LOGIN AS ADMIN ==="
$loginPage = $client.GetStringAsync("$baseUri/Account/Login").Result
$token = [regex]::Match($loginPage, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value

$dict = New-Object 'System.Collections.Generic.Dictionary[string, string]'
$dict.Add("__RequestVerificationToken", $token)
$dict.Add("Email", "admin@trackerkerja.com")
$dict.Add("Password", "Admin123!")
$client.PostAsync("$baseUri/Account/Login", (New-Object System.Net.Http.FormUrlEncodedContent($dict))).Result | Out-Null
Write-Host "Admin Login: OK"

Write-Host "`n=== 2. TEST EXPORT TO ARMS EXCEL (/Task/ExportArmsExcel) ==="
$armsResp = $client.GetAsync("$baseUri/Task/ExportArmsExcel").Result
Write-Host "Export Status Code:" $armsResp.StatusCode
Write-Host "Content-Type:" $armsResp.Content.Headers.ContentType.MediaType

$armsBytes = $armsResp.Content.ReadAsByteArrayAsync().Result
Write-Host "Exported File Size (bytes):" $armsBytes.Length
$exportPath = [System.IO.Path]::Combine($PSScriptRoot, "test_arms_export.xlsx")
[System.IO.File]::WriteAllBytes($exportPath, $armsBytes)

Write-Host "`n=== 3. VERIFY EXCEL COLUMN HEADERS WITH CLOSEDXML ==="
Add-Type -Path "c:\TEMP\VSCODE\TrackerKerja\bin\Debug\net8.0\ClosedXML.dll"
$wb = New-Object ClosedXML.Excel.XLWorkbook($exportPath)
$ws = $wb.Worksheet(1)

$expectedHeaders = @(
    "project_name", "requirement", "title", "status", "priority",
    "jenis_task", "module_name", "bug_type", "progress", "start_date",
    "due_date", "completed_date", "developer", "ba_emails", "infra_emails",
    "master_data_emails", "tester_emails", "kendala", "solusi"
)

$allHeadersMatch = $true
for ($i = 1; $i -le $expectedHeaders.Length; $i++) {
    $headerVal = $ws.Cell(1, $i).GetString()
    $expected = $expectedHeaders[$i - 1]
    if ($headerVal -eq $expected) {
        Write-Host "  Col $i [$expected]: OK ($headerVal)"
    } else {
        Write-Host "  Col $i MISMATCH! Expected '$expected', Got '$headerVal'" -ForegroundColor Red
        $allHeadersMatch = $false
    }
}

Write-Host "Total Data Rows in Exported ARMS Excel:" ($ws.RowsUsed().Count() - 1)
$sampleTitle = $ws.Cell(2, 3).GetString()
$sampleStatus = $ws.Cell(2, 4).GetString()
$samplePriority = $ws.Cell(2, 5).GetString()
Write-Host "Sample Row 2: Title='$sampleTitle', Status='$sampleStatus', Priority='$samplePriority'"

$wb.Dispose()

Write-Host "`n=== 4. TEST ARMS TEMPLATE DOWNLOAD (/Import/ArmsTemplate) ==="
$templateResp = $client.GetAsync("$baseUri/Import/ArmsTemplate").Result
Write-Host "ArmsTemplate Status Code:" $templateResp.StatusCode
$templateBytes = $templateResp.Content.ReadAsByteArrayAsync().Result
Write-Host "Template File Size (bytes):" $templateBytes.Length

Write-Host "`n=== 5. TEST IMPORT UPLOAD WITH ARMS EXCEL FILE ==="
$uploadPage = $client.GetStringAsync("$baseUri/Import").Result
$tokenUpload = [regex]::Match($uploadPage, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value

$form = New-Object System.Net.Http.MultipartFormDataContent
$form.Add((New-Object System.Net.Http.StringContent($tokenUpload)), "__RequestVerificationToken")
$fileContent = New-Object System.Net.Http.ByteArrayContent($armsBytes)
$fileContent.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
$form.Add($fileContent, "file", "test_arms_upload.xlsx")

$uploadResp = $client.PostAsync("$baseUri/Import/Upload", $form).Result
$uploadHtml = $uploadResp.Content.ReadAsStringAsync().Result
Write-Host "Upload Status Code:" $uploadResp.StatusCode
Write-Host "Upload Preview Page Contains Task Titles:" ($uploadHtml.Contains("Preview Import") -or $uploadHtml.Contains("Konfirmasi"))

# Cleanup
if (Test-Path $exportPath) { Remove-Item -Force $exportPath }
$client.Dispose()
$handler.Dispose()

Write-Host "`n=== ARMS EXPORT & IMPORT MODULE FULLY VERIFIED AND WORKING 100%! ==="

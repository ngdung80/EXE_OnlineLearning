$connStringMaster = "Server=tcp:plo.database.windows.net,1433;Initial Catalog=master;Persist Security Info=False;User ID=adminplo;Password=PloNgDu@4002;Encrypt=True;TrustServerCertificate=False;"
$connMaster = New-Object System.Data.SqlClient.SqlConnection($connStringMaster)
try {
    $connMaster.Open()
    Write-Host "Master Database Connection: SUCCESS"
    $connMaster.Close()
} catch {
    Write-Host "Master Database Connection: FAILED"
    Write-Host $_.Exception.Message
}

$connStringDB = "Server=tcp:plo.database.windows.net,1433;Initial Catalog=PLO_System;Persist Security Info=False;User ID=adminplo;Password=PloNgDu@4002;Encrypt=True;TrustServerCertificate=False;"
$connDB = New-Object System.Data.SqlClient.SqlConnection($connStringDB)
try {
    $connDB.Open()
    Write-Host "PLO_System Database Connection: SUCCESS"
    $connDB.Close()
} catch {
    Write-Host "PLO_System Database Connection: FAILED"
    Write-Host $_.Exception.Message
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipPath = "danhgiabaocao.docx"
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
$entry = $zip.GetEntry("word/document.xml")
if ($entry) {
    $stream = $entry.Open()
    $reader = New-Object System.IO.StreamReader($stream)
    $xmlText = $reader.ReadToEnd()
    $reader.Close()
    $stream.Close()
    
    # Simple XML regex parsing to clean up XML tags and print text readable
    $text = [regex]::Replace($xmlText, "<[^>]+>", " ")
    $text = [regex]::Replace($text, "\s+", " ")
    Write-Output $text
} else {
    Write-Output "Could not find word/document.xml"
}
$zip.Dispose()

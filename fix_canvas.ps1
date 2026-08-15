$scenesDir = "Assets\Scenes"
$files = Get-ChildItem -Path $scenesDir -Filter "*.unity" -Recurse

foreach ($file in $files) {
    Write-Host "Processing $($file.FullName)..."
    $content = Get-Content -Path $file.FullName -Raw
    
    $newContent = $content -replace 'm_MatchWidthOrHeight: 0\b', 'm_MatchWidthOrHeight: 0.5'
    
    if ($newContent -cne $content) {
        Set-Content -Path $file.FullName -Value $newContent -NoNewline
        Write-Host "Fixed $($file.FullName)"
    }
}

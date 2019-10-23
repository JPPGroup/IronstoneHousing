Get-ChildItem -Include *IronstoneHousing.dll -Exclude *Tests.dll -Recurse | Compress-Archive -Update -DestinationPath ($PSScriptRoot + "\IronstoneHousing")
Write-Host "Zip file created"
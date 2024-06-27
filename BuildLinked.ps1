# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/RabbitSteelModManager/*" -Force -Recurse
dotnet publish "./RabbitSteelModManager.csproj" -c Release -o "$env:RELOADEDIIMODS/RabbitSteelModManager" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location
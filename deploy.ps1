# 1. Limpiar versiones anteriores para evitar carpetas anidadas
Write-Host "--- Limpiando carpetas viejas... ---" -ForegroundColor Cyan
if (Test-Path "dist") { Remove-Item -Recurse -Force dist }

# 2. Compilar el proyecto Blazor WASM
Write-Host "--- Compilando Adsum Pater... ---" -ForegroundColor Cyan
dotnet publish -c Release -o dist

# 3. Subir a Firebase
Write-Host "--- Subiendo a Firebase Hosting... ---" -ForegroundColor Cyan
firebase deploy

Write-Host "--- ¡Misión cumplida! Sitio actualizado ---" -ForegroundColor Green
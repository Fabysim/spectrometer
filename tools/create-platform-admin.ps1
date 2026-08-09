# Crée (ou met à jour) un compte PlatformAdmin sur la base locale spectrometre_v2.
# Usage :
#   powershell -File tools/create-platform-admin.ps1
#   powershell -File tools/create-platform-admin.ps1 -Email moi@test.local -Password 'MonMdp!'
#   powershell -File tools/create-platform-admin.ps1 -PgPassword 'autre-mdp-postgres'

param(
    [string]$Email = "platformadmin@local.test",
    [string]$Password = "Str0ng!Passw0rd",
    [string]$PgHost = "localhost",
    [int]$PgPort = 5432,
    [string]$Database = "spectrometre_v2",
    [string]$PgUser = "postgres",
    [string]$PgPassword = "Pil@tes2025"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$connection = "Host=$PgHost;Port=$PgPort;Database=$Database;Username=$PgUser;Password=$PgPassword"

Write-Host "Création / promotion PlatformAdmin : $Email"
dotnet run --project tools/Spectrometre.AdminBootstrap -- `
    --ConnectionStrings:DefaultConnection=$connection `
    $Email $Password

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Connexion UI :"
Write-Host "  Email    : $Email"
Write-Host "  Password : $Password"

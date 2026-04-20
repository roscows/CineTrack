param(
    [string]$ArchivePath = "..\\movietracker.archive",
    [string]$ContainerName = "movietracker-mongodb",
    [string]$DatabaseName = "MovieTrackerDb",
    [string]$MongoUser = "admin",
    [string]$MongoPassword = "admin123"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$resolvedArchive = Resolve-Path -Path (Join-Path $scriptDir $ArchivePath) -ErrorAction SilentlyContinue

if (-not $resolvedArchive) {
    throw "Archive fajl nije pronadjen. Ocekivana putanja: $ArchivePath"
}

$archiveOnHost = $resolvedArchive.Path
$archiveInContainer = "/tmp/movietracker.archive"

$containerRunning = docker ps --format "{{.Names}}" | Select-String -Pattern "^$ContainerName$" -Quiet
if (-not $containerRunning) {
    throw "Kontejner '$ContainerName' nije pokrenut. Pokreni: docker compose up -d"
}

Write-Host "Kopiram archive u kontejner..."
docker cp $archiveOnHost "${ContainerName}:$archiveInContainer" | Out-Null

Write-Host "Pokrecem restore baze '$DatabaseName'..."
docker exec $ContainerName mongorestore `
    --authenticationDatabase admin `
    -u $MongoUser `
    -p $MongoPassword `
    --nsInclude "$DatabaseName.*" `
    --drop `
    --archive=$archiveInContainer `
    --gzip

Write-Host "Restore je zavrsen."

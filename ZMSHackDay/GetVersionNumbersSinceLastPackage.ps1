
# Right-click this file -> "Run with PowerShell"
# On-prem Azure DevOps Server/TFS ONLY. Uses Windows auth via your current user.
# Compatible with Windows PowerShell 5.1 and PowerShell 7+
$ErrorActionPreference = 'Stop'

# =========================
# CONFIG — EDIT THESE
# =========================
# Collection URL and Project (on-prem)
$CollectionUrl         = 'http://tfsserver:8080/tfs/Iho'
$Project               = 'Iho'

# Release pipeline (definition) and stage
$ReleaseDefinitionId   = 4
$EnvironmentId         = 447

# Your current package version (from your packaging step) - this is intended to be passed in as a variable, but set manually for now 
$CurrentVersion        = '3.1.776'

# API versions (pinned)
$ApiVersionRelease     = '6.0'
$ApiVersionBuild       = '6.0'

# How many recent deployments to scan while collecting versions
$DeploymentsTop        = 20

# =========================
# Helpers
# =========================
function Section([string]$text) { Write-Host "`n=== $text ===" -ForegroundColor Cyan }

function Invoke-ADOGet {
  param([string]$Url)
  Write-Host ("GET {0}" -f $Url) -ForegroundColor Yellow
  return Invoke-RestMethod -Uri $Url -Method Get -UseDefaultCredentials -ErrorAction Stop
}

function Compare-Version([string]$A, [string]$B) {
  # Numeric dotted compare (up to 4 parts). Non-numeric suffixes ignored.
  $rx = '^\s*([0-9]+)(?:\.([0-9]+))?(?:\.([0-9]+))?(?:\.([0-9]+))?'
  $ma = [regex]::Match($A, $rx); $mb = [regex]::Match($B, $rx)
  if (-not $ma.Success -or -not $mb.Success) { return [string]::Compare($A,$B,$true) }
  $pa = 0..3 | ForEach-Object { if ($ma.Groups[$_+1].Success) { [int]$ma.Groups[$_+1].Value } else { 0 } }
  $pb = 0..3 | ForEach-Object { if ($mb.Groups[$_+1].Success) { [int]$mb.Groups[$_+1].Value } else { 0 } }
  for ($i=0; $i -lt 4; $i++) { if ($pa[$i] -lt $pb[$i]) { return -1 }; if ($pa[$i] -gt $pb[$i]) { return 1 } }
  return 0
}

function Get-LastSuccessfulDeployment([int]$DefId, [int]$EnvId) {
  $url = ("{0}/{1}/_apis/release/deployments?api-version={2}&definitionId={3}&definitionEnvironmentId={4}&deploymentStatus=succeeded&`$top=1&queryOrder=descending" -f $CollectionUrl, $Project, $ApiVersionRelease, $DefId, $EnvId)
  $d = Invoke-ADOGet $url
  return $d.value | Select-Object -First 1
}

function Get-Release([int]$ReleaseId) {
  if (-not $ReleaseId) { throw "Get-Release called with null/zero ReleaseId." }
  $url = ("{0}/{1}/_apis/release/releases/{2}?api-version={3}" -f $CollectionUrl, $Project, $ReleaseId, $ApiVersionRelease)
  return Invoke-ADOGet $url
}

function Get-BuildNumberFromArtifact($Artifact) {
  if ($Artifact.type -ne 'Build') { return $null }
  $verName = $Artifact.definitionReference.version.name
  if ($verName) { return $verName }
  $buildId = $Artifact.definitionReference.version.id
  if (-not $buildId) { return $null }
  $bUrl = ("{0}/{1}/_apis/build/builds/{2}?api-version={3}" -f $CollectionUrl, $Project, $buildId, $ApiVersionBuild)
  $b = Invoke-ADOGet $bUrl
  return $b.buildNumber
}

function Get-PrimaryArtifactVersion($Release) {
  # Use the first Build-type artifact
  $candidate = $Release.artifacts | Where-Object { $_.type -eq 'Build' } | Select-Object -First 1
  if (-not $candidate) { return $null }
  return (Get-BuildNumberFromArtifact -Artifact $candidate)
}

function Get-DeploymentVersionsSince([int]$DefId, [int]$EnvId, [datetime]$Since, [int]$Top) {
  $url = ("{0}/{1}/_apis/release/deployments?api-version={2}&definitionId={3}&definitionEnvironmentId={4}&`$top={5}&queryOrder=ascending" -f $CollectionUrl, $Project, $ApiVersionRelease, $DefId, $EnvId, $Top)
  $all = Invoke-ADOGet $url
  $deploys = $all.value | Where-Object { $_.completedOn -and ([datetime]$_.completedOn) -ge $Since } | Sort-Object { [datetime]$_.completedOn }

  $versions = New-Object System.Collections.Generic.List[string]
  foreach ($d in $deploys) {
    $rid = [int]$d.release.id
    Write-Host ("Inspecting deployment -> releaseId={0} completedOn={1}" -f $rid, [datetime]$d.completedOn)
    if ($rid -le 0) { continue }
    $rel = Get-Release -ReleaseId $rid
    $v = Get-PrimaryArtifactVersion -Release $rel
    if ($v) { $versions.Add($v) }
  }

  # Return in order (no de-dupe)
  return ,$versions
}

# =========================
# Main
# =========================
try {
  Section 'Finding last successful deployment'
  $last = Get-LastSuccessfulDeployment -DefId $ReleaseDefinitionId -EnvId $EnvironmentId
  if (-not $last) { throw ("No successful deployment found for environment {0} on definition {1}." -f $EnvironmentId, $ReleaseDefinitionId) }

  $lastCompleted = [datetime]$last.completedOn
  $lastReleaseId = 0
  if ($last.release -and $last.release.id) {
    try { $lastReleaseId = [int]$last.release.id } catch {}
  }

  if ($lastReleaseId -le 0) {
    Write-Host "Could not resolve last.release.id - dumping object:" -ForegroundColor Yellow
    $last | ConvertTo-Json -Depth 8 | Write-Host
    throw "Could not resolve a numeric releaseId from the last successful deployment."
  }

  $releaseUrl = ("{0}/{1}/_apis/release/releases/{2}?api-version={3}" -f $CollectionUrl, $Project, $lastReleaseId, $ApiVersionRelease)
  Write-Host ("About to GET release: {0}" -f $releaseUrl)

  $lastRelease = Get-Release -ReleaseId $lastReleaseId
  $lastVersion = Get-PrimaryArtifactVersion -Release $lastRelease

  $displayLast = if ($lastVersion) { $lastVersion } else { '<not found>' }
  Write-Host ("Last success -> ReleaseId={0}  CompletedOn={1:yyyy-MM-dd HH:mm:ss}" -f $lastReleaseId, $lastCompleted)
  Write-Host ("Last success version (artifact build number): {0}" -f $displayLast) -ForegroundColor Green
  if (-not $lastVersion) { throw 'Could not resolve version (build number) from the last success.' }

  Section 'Collecting versions since last success (bounded by CurrentVersion)'
  Write-Host ("Current version: {0}" -f $CurrentVersion)
  $since = $lastCompleted.AddSeconds(1)
  $versionsSince = Get-DeploymentVersionsSince -DefId $ReleaseDefinitionId -EnvId $EnvironmentId -Since $since -Top $DeploymentsTop

  # Filter: > lastVersion AND <= CurrentVersion
  $filtered = @($versionsSince | Where-Object { (Compare-Version $_ $lastVersion) -gt 0 -and (Compare-Version $_ $CurrentVersion) -le 0 })

  # If nothing found but CurrentVersion is >= lastVersion, include CurrentVersion
  if (($filtered.Count -eq 0) -and ((Compare-Version $CurrentVersion $lastVersion) -ge 0)) {
    $filtered = @($CurrentVersion)
  }

  Section 'Results'
  Write-Host ("Env: #{0}" -f $EnvironmentId)
  Write-Host ("Last successful version: {0}" -f $lastVersion)
  if ($filtered.Count -gt 0) {
    Write-Host 'Versions to include (oldest -> newest):' -ForegroundColor Green
    $filtered | ForEach-Object { Write-Host (" - {0}" -f $_) }
  } else {
    Write-Host 'No versions found between last success and current version.' -ForegroundColor Yellow
  }

  exit 0
}
catch {
  $e = $_
  Write-Host ""
  Write-Host ("ERROR: {0}" -f $e.Exception.Message) -ForegroundColor Red
  Write-Host ("Type : {0}" -f $e.Exception.GetType().FullName)
  if ($e.InvocationInfo) {
    Write-Host ("Where: {0}:{1}" -f $e.InvocationInfo.ScriptName, $e.InvocationInfo.ScriptLineNumber)
    Write-Host ("Code : {0}" -f $e.InvocationInfo.Line.Trim())
  }
  if ($e.ScriptStackTrace) { Write-Host ("Stack:`n{0}" -f $e.ScriptStackTrace) }

  # If it was an HTTP error (Invoke-RestMethod), dump the response body too
  $resp = $e.Exception.Response
  if ($resp) {
    try {
      $reader = New-Object IO.StreamReader($resp.GetResponseStream())
      $body   = $reader.ReadToEnd()
      if ($body) {
        Write-Host "`n--- Server response body ---"
        Write-Host $body
        Write-Host "-----------------------------"
      }
    } catch {}
  }

  exit 1
}
finally {
  if ($Host.Name -eq 'ConsoleHost') { Read-Host 'Press Enter to close this window' | Out-Null }
}
 
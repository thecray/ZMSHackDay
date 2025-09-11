
# Right-click this file -> "Run with PowerShell"
# On-prem Azure DevOps Server/TFS ONLY. Uses Windows auth via your current user.
# Compatible with Windows PowerShell 5.1 and PowerShell 7+
$ErrorActionPreference = 'Stop'

$FromVersion           = '3.1.695.0'
$ToVersion             = '3.1.778.0'

# =========================
# CONFIG — EDIT THESE
# =========================
# Collection URL and Project (on-prem)
$CollectionUrl         = 'http://tfsserver:8080/tfs/Iho'
$Project               = 'Iho'

# Release pipeline (definition) and stage
$ReleaseDefinitionId   = 4
$EnvironmentId         = 447

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
  #Write-Host ("GET {0}" -f $Url) -ForegroundColor Yellow
  return Invoke-RestMethod -Uri $Url -Method Get -UseDefaultCredentials -ErrorAction Stop
}

function Invoke-ADOPost {
  param([string]$Url)
  #Write-Host ("POST {0}" -f $Url) -ForegroundColor Yellow

  $headers = @{
      "Content-Type" = "application/json"
  }

  $query = ("SELECT [System.Id] FROM workitems WHERE [System.TeamProject] = @project AND [System.WorkItemType] IN ('Task', 'bug') AND ([DT.ResolvedIn] >= '{0}' AND [DT.ResolvedIn] <= '{1}')" -f $FromVersion, $ToVersion)

  $body = @{
    "query" = $query
  } | ConvertTo-Json

  return Invoke-RestMethod -Uri $Url -Method Post -Body $body -Headers $headers -UseDefaultCredentials -ErrorAction Stop
}

function Post-ReleaseNotes() {
  $url = ("{0}/{1}/_apis/wit/wiql?api-version={2}" -f $CollectionUrl, $Project, $ApiVersionRelease)
  return Invoke-ADOPost $url
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

function CreateVersionObject([string]$version)
{
    $parts = $version.split(".")

    return @{
        major = [int]$parts[0]
        minor = [int]$parts[1]
        build = [int]$parts[2]
        revision = [int]$parts[3]
    }
}

function CompareVersion($version1, $version2)
{
    if ($version1.major -lt $version2.major)
    {
        return -1;
    }
    elseif ($version1.major -gt $version2.major)
    {
        return 1;
    }
    elseif ($version1.major -eq $version2.major)
    {
        if ($version1.minor -lt $version2.minor)
        {
            return -1;
        }
        elseif ($version1.minor -gt $version2.minor)
        {
            return 1;
        }
        elseif ($version1.minor -eq $version2.minor)
        {
            if ($version1.build -lt $version2.build)
            {
                return -1;
            }
            elseif ($version1.build -gt $version2.build)
            {
                return 1;
            }
            elseif ($version1.build -eq $version2.build)
            {
                return 0;
            }
        }
    }

    return 0;
}

function VersionToString($version)
{
    return ("{0}.{1}.{2}.{3}" -f $version.major, $version.minor, $version.build, $version.revision)
}

# =========================
# Main
# =========================
try {
  Section 'WIQL query'

  $postResults = Post-ReleaseNotes

  $fromVersionObject = CreateVersionObject $FromVersion
  $toVersionObject = CreateVersionObject $ToVersion

  Write-host ("From: {0} - To: {1}" -f $FromVersion, $ToVersion)
  Write-host "Gathering data... please wait..."

  $output = @()

  $postResults.workItems | ForEach-Object {
      $result = Invoke-ADOGet $_.url

      $version = CreateVersionObject $result.fields."DT.ResolvedIn"

      $comparisonFrom = CompareVersion $version $fromVersionObject
      $comparisonTo = CompareVersion $version $toVersionObject

      if($comparisonFrom -ge 0 -and $comparisonTo -le 0)
      {
          $output += @{
              id = $result.id
              title = $result.fields."System.Title"
              version = VersionToString $version
              versionObject = $version
              developerNotes = $result.fields."DT.DeveloperNotes"
              functionalNotes = $result.fields."DT.FunctionalNotes"
          }
      }
  }

  $filename = ("release-notes-{0}-{1}.json" -f $FromVersion, $ToVersion)

  Write-Host ("Writing to {0}" -f $filename)
  Set-Content -Path $filename -Value ($output | ConvertTo-Json)

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
 
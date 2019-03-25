function ConvertTo-BasicAuthHeader {
    param([string]$authtoken)

    $ba = (':{0}' -f $authtoken)
    $ba = [System.Text.Encoding]::UTF8.GetBytes($ba)
    $ba = [System.Convert]::ToBase64String($ba)
    $header = @{Authorization = ('Basic{0}' -f $ba); ContentType = 'application/json'}
    return $header
}

$branchRef = $env:BUILD_SOURCEBRANCH
$authHeader = ConvertTo-BasicAuthHeader $env:SYSTEM_ACCESSTOKEN

$projectId = $env:SYSTEM_TEAMPROJECTID

$uri = $env:TCMADDRESS + '/' + $projectId + '/_apis/testresults/CodeCoverage/?buildId=' + $env:BUILD_BUILDID + '&api-version=5.0-preview.1'

Write-Host 'Calling patch via ' $uri

$patchRequest = Invoke-WebRequest -Uri $uri -Headers $authHeader -Method Patch -ContentType 'application/json'

try
{
    Invoke-WebRequest -Uri $uri -Headers $authHeader -Method Patch -ContentType 'application/json'
}
catch
{
    if($_.Exception.Response.StatusCode -ne 425) {
        Write-Host 'Conflict response didnt match, ' + $response
        exit 1;
    }
}

$patchResult = $patchRequest.Content | ConvertFrom-Json

if($patchResult.status -ne 'pending') {
    Write-Host 'status is not pending, ' + $patchResult.status
    exit 1;
}

$getRequest = ''

do
{
    Start-Sleep -s 1
    $getRequest = Invoke-RestMethod -Uri $uri -Headers $authHeader -Method Get -ContentType 'application/json'
} while ($getRequest.status -eq 'pending')


if($getRequest.status -ne 'completed') {
    Write-Host 'status is not completed, ' + $getRequest.status
    exit 1;
}
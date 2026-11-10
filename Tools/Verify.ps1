param(
    [string]$Unity = 'D:\Unity\Editors\6000.3.28f1\Editor\Unity.exe',
    [switch]$Build
)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$results=Join-Path $project 'TestResults'
New-Item -ItemType Directory -Force $results | Out-Null
function Run-Unity([string]$arguments) {
    $process=Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    if($process.ExitCode -ne 0){throw "Unity failed with exit code $($process.ExitCode); inspect $results"}
}
Run-Unity "-batchmode -nographics -projectPath `"$project`" -runTests -testPlatform EditMode -testResults `"$results\rules.xml`" -logFile `"$results\tests.log`""
[xml]$report=Get-Content (Join-Path $results 'rules.xml')
if($report.'test-run'.result -ne 'Passed'){throw 'Rules tests did not pass'}
Write-Output "Passed $($report.'test-run'.passed) rules tests."
foreach($name in @('PerformanceTestRunInfo.json','PerformanceTestRunInfo.json.meta','PerformanceTestRunSettings.json','PerformanceTestRunSettings.json.meta')) {
    $generated=Join-Path $project "Assets\Resources\$name"
    if(Test-Path -LiteralPath $generated){Remove-Item -LiteralPath $generated}
}
if($Build){Run-Unity "-batchmode -nographics -quit -projectPath `"$project`" -executeMethod GrayboxBuild.Build -logFile `"$results\build.log`""}

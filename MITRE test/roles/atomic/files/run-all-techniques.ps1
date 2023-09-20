$techniques = gci C:\tests\atomics\* -Recurse -Include T*.yaml | Get-AtomicTechnique
$counter = 0
foreach ($technique in $techniques) {
    foreach ($atomic in $technique.atomic_tests) {
        if ($atomic.supported_platforms.contains("windows") -and ($atomic.executor -ne "manual")) {
            $counter++
            # Invoke
            Invoke-AtomicTest $technique.attack_technique -TestGuids $atomic.auto_generated_guid -ExecutionLogPath 'C:\tools\evaluation.csv'
            # Sleep then cleanup
            Start-Sleep 3
            Invoke-AtomicTest  $technique.attack_technique -TestGuids $atomic.auto_generated_guid -Cleanup
        }
    }
}

Write.Output "Executed Techniques: $($counter)" 
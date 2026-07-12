# Prints pipeline and activity level run details between two dates. 
# You can go back 45 days only. You can change output to whatever format (csv) as
# you like with custom printing. This prints same run hour/corehour values as what you see in UX.

$startTime = "5/5/2021 4:00:00"
$endTime = "5/5/2021 7:00:00"
 
$pipelineRuns = Get-AzDataFactoryV2PipelineRun -ResourceGroupName ADF -DataFactoryName adfpupcanary -LastUpdatedAfter $startTime -LastUpdatedBefore $endTime
 
foreach($pipelineRun in $pipelineRuns) {
    $activtiyRuns = Get-AzDataFactoryV2ActivityRun -ResourceGroupName ADF -DataFactoryName adfpupcanary -pipelineRunId $pipelineRun.RunId -RunStartedAfter $startTime -RunStartedBefore $endTime
    foreach($activtiyRun in $activtiyRuns) {
        if ($activtiyRun.Output -ne $null -and
                $activtiyRun.Output.SelectToken("billingReference.billableDuration") -ne $null) {
            Write-Output $activtiyRun.Output.SelectToken("billingReference.billableDuration").ToString() for $activtiyRun.Output.SelectToken("billingReference.activityType").ToString()
        }
        else {
            Write-Output "Not Availble" for $activtiyRun.ActivityType
        }
    }
}
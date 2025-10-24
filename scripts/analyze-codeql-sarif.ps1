# CodeQL SARIF Analysis Script
# Parses CodeQL SARIF results to identify and categorize quality improvement opportunities

param(
    [Parameter(Mandatory=$true)]
    [string]$SarifFile,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("all", "warning", "recommendation", "note")]
    [string]$Severity = "all",
    
    [Parameter(Mandatory=$false)]
    [int]$Top = 20,
    
    [Parameter(Mandatory=$false)]
    [switch]$GroupByRule,
    
    [Parameter(Mandatory=$false)]
    [switch]$ShowFiles
)

Write-Host "🔍 Analyzing CodeQL SARIF Results..." -ForegroundColor Cyan
Write-Host "File: $SarifFile" -ForegroundColor Yellow

if (-not (Test-Path $SarifFile)) {
    Write-Error "SARIF file not found: $SarifFile"
    exit 1
}

try {
    $sarif = Get-Content $SarifFile | ConvertFrom-Json
    $results = $sarif.runs[0].results
    $rules = $sarif.runs[0].tool.driver.rules
    
    if ($results.Count -eq 0) {
        Write-Host "🎉 No issues found in the SARIF file!" -ForegroundColor Green
        return
    }
    
    Write-Host "📊 Total findings: $($results.Count)" -ForegroundColor Yellow
    
    # Create detailed findings with rule information
    $detailedFindings = @()
    foreach ($result in $results) {
        $rule = $rules | Where-Object { $_.id -eq $result.ruleId }
        $severity = if ($rule.properties.'problem.severity') { 
            $rule.properties.'problem.severity' 
        } else { 
            "info" 
        }
        
        $location = $result.locations[0].physicalLocation
        $file = $location.artifactLocation.uri -replace '^file:///', '' -replace '%5C', '\'
        $line = $location.region.startLine
        
        $detailedFindings += [PSCustomObject]@{
            RuleId = $result.ruleId
            RuleName = $rule.name
            Severity = $severity
            Message = $result.message.text
            File = $file
            Line = $line
            Description = $rule.shortDescription.text
            HelpUri = $rule.helpUri
        }
    }
    
    # Filter by severity if specified
    if ($Severity -ne "all") {
        $filteredFindings = $detailedFindings | Where-Object { $_.Severity -eq $Severity }
        Write-Host "🔍 Filtered to $Severity findings: $($filteredFindings.Count)" -ForegroundColor Cyan
        $detailedFindings = $filteredFindings
    }
    
    # Group by rule if requested
    if ($GroupByRule) {
        Write-Host "`n📋 Findings grouped by rule:" -ForegroundColor Cyan
        $groupedFindings = $detailedFindings | Group-Object RuleId | Sort-Object Count -Descending
        
        foreach ($group in $groupedFindings | Select-Object -First $Top) {
            $rule = $group.Group[0]
            $color = switch ($rule.Severity) {
                "error" { "Red" }
                "warning" { "Yellow" }
                "recommendation" { "Cyan" }
                "note" { "Gray" }
                default { "White" }
            }
            
            Write-Host "`n🔸 $($rule.RuleId) ($($group.Count) occurrences)" -ForegroundColor $color
            Write-Host "   $($rule.Description)" -ForegroundColor Gray
            Write-Host "   Severity: $($rule.Severity)" -ForegroundColor Gray
            
            if ($ShowFiles) {
                $files = $group.Group | Select-Object File, Line | Sort-Object File, Line
                foreach ($file in $files | Select-Object -First 5) {
                    Write-Host "   - $($file.File):$($file.Line)" -ForegroundColor DarkGray
                }
                if ($files.Count -gt 5) {
                    Write-Host "   ... and $($files.Count - 5) more" -ForegroundColor DarkGray
                }
            }
        }
    }
    
    # Show top findings by file
    if ($ShowFiles -and -not $GroupByRule) {
        Write-Host "`n📁 Top files with issues:" -ForegroundColor Cyan
        $fileGroups = $detailedFindings | Group-Object File | Sort-Object Count -Descending
        
        foreach ($fileGroup in $fileGroups | Select-Object -First $Top) {
            $color = "Yellow"
            Write-Host "`n📄 $($fileGroup.Name) ($($fileGroup.Count) issues)" -ForegroundColor $color
            
            $severityGroups = $fileGroup.Group | Group-Object Severity
            foreach ($severityGroup in $severityGroups) {
                $severityColor = switch ($severityGroup.Name) {
                    "error" { "Red" }
                    "warning" { "Yellow" }
                    "recommendation" { "Cyan" }
                    "note" { "Gray" }
                    default { "White" }
                }
                Write-Host "   $($severityGroup.Name): $($severityGroup.Count)" -ForegroundColor $severityColor
            }
        }
    }
    
    # Show severity distribution
    Write-Host "`n📊 Severity distribution:" -ForegroundColor Cyan
    $severityGroups = $detailedFindings | Group-Object Severity | Sort-Object Count -Descending
    foreach ($severityGroup in $severityGroups) {
        $color = switch ($severityGroup.Name) {
            "error" { "Red" }
            "warning" { "Yellow" }
            "recommendation" { "Cyan" }
            "note" { "Gray" }
            default { "White" }
        }
        Write-Host "   $($severityGroup.Name): $($severityGroup.Count)" -ForegroundColor $color
    }
    
    # Show actionable recommendations
    Write-Host "`n🎯 Most common issues to fix:" -ForegroundColor Green
    $commonIssues = $detailedFindings | Group-Object RuleId | Sort-Object Count -Descending | Select-Object -First 10
    
    $priority = 1
    foreach ($issue in $commonIssues) {
        $rule = $issue.Group[0]
        $color = switch ($rule.Severity) {
            "error" { "Red" }
            "warning" { "Yellow" }
            "recommendation" { "Cyan" }
            "note" { "Gray" }
            default { "White" }
        }
        
        Write-Host "`n$priority. $($rule.RuleId) - $($issue.Count) occurrences" -ForegroundColor $color
        Write-Host "   Issue: $($rule.Description)" -ForegroundColor Gray
        Write-Host "   Example: $($rule.Message)" -ForegroundColor Gray
        if ($rule.HelpUri) {
            Write-Host "   Help: $($rule.HelpUri)" -ForegroundColor Blue
        }
        $priority++
    }
    
    Write-Host "`n✅ Analysis complete! Focus on the most common issues first." -ForegroundColor Green
    
} catch {
    Write-Error "Failed to parse SARIF file: $($_.Exception.Message)"
    exit 1
}
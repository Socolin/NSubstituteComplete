
<#
    .SYNOPSIS
        Taken from: https://github.com/ForNeVeR/AvaloniaRider/blob/master/scripts/Unpack-Distribution.ps1
        The purpose of this script is to unpack the compressed plugin artifact.
        It is used during CI builds to generate the layout for uploading.
    .PARAMETER DistributionsLocation
        Path to the directory containing compressed plugin distribution.
#>
param (
	[string] $DistributionsLocation = "$PSScriptRoot/../build/distributions"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$file = "$DistributionsLocation/ApplicationInsights-Debug-Log-Viewer-*.zip"

Expand-Archive -Path $file -DestinationPath $DistributionsLocation/unpacked
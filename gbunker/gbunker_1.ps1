# Copyright (c) 2019, Robert J. Hansen <rob@hansen.engineering>
#
# Permission to use, copy, modify, and/or distribute this software for any
# purpose with or without fee is hereby granted, provided that the above
# copyright notice and this permission notice appear in all copies.
# 
# THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
# WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
# MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
# ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
# WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
# ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
# OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

# Please start this from a PowerShell session, not a Windows CMD session.
#
# gbunker_1.ps1 -PollRate 60 -RunFor 1 -SourceDir . -DestinationDir dst
#
# This will make the script poll the current directory (".") every sixty
# seconds, moving the appropriate files to the subdirectory "dst", and
# will auto-terminate after one hour.
#
# Most of these command-line parameters may be omitted.  The only one you
# absolutely must give is -DestinationDir.  The others can all be
# configured to sensible defaults.

# This script is extensively commented.  Please read the comments
# carefully.  User-serviceable parts are marked with CHANGEME.

# Command-line arguments for the PowerShell script.  This script takes 
# four:
#
# -PollRate [integer], default of 20.  The script polls the directory
#     every this-many seconds.  CHANGEME if you need it to go faster or
#     slower by default.  If you just want it to go faster or slower
#     on a specific run, just set it at the command line.
# -RunFor [decimal], default of 16.0.  The script automatically ceases
#     running after this-many hours.  CHANGEME if you need it to by
#     default abort sooner or later.
# -DestinationDir, default of "\path\somewhere".  The script drops the
#     matching files here.  CHANGEME because you really want the default
#     to be a valid path.  Note: this can be a relative or an absolute
#     path.
# -SourceDir, no default.  The script looks here for matching files.
#     This is not a user-serviceable component.  Note: this can be a 
#     relative or an absolute path.
param(
    [int]$PollRate = 20,
    [double]$RunFor = 16.0,
    [string]$DestinationDir = "\path\somewhere",
    [Parameter(Mandatory=$true)][string]$SourceDir
)

# Basic sanity checking to ensure both the source and destination
# directories exist.  If either one doesn't, the script will abort
# with an error message.
if (!(Test-Path -Path $DestinationDir -PathType Container)) {
    Write-Host "Error: $DestinationDir does not appear to be " + `
        "a directory."
    Exit-PSHostProcess
}

if (!(Test-Path -Path $SourceDir -PathType Container)) {
    Write-Host "Error: $SourceDir does not appear to be a directory."
    Exit-PSHostProcess
}

# Use the user-specified RunFor parameter to determine our drop-dead
# time.
$endAt = (Get-Date).AddHours($RunFor)

while ((Get-Date) -le $endAt) {
    # Get the current local timestamp in 24-hour format and convert
    # it to an integer.
    [string]$tempTimestamp = Get-Date -Format "HHmm"
    [int]$timestamp = [convert]::ToInt32($tempTimestamp, 10)

    # For each file in the source directory, check to see if it
    # ought be moved.
    Get-ChildItem -Name -File $SourceDir | ForEach-Object -Process {
        # If you don't know regular expressions, learn them before you
        # mess around with this next line.
        if ($_ -match "^.*(\d{4}).*\..*$") {
            # Convert the 24-hour timestamp to an integer.
            [string]$fileTimestamp = $Matches[1]
            [int]$thisTimestamp = [convert]::ToInt32($fileTimestamp, 10)

            # And if this file's timestamp is prior to current time,
            # move it into the destination directory.
            if ($thisTimestamp -le $timestamp) {
                $sourcePath = Join-Path -Path $SourceDir -ChildPath $_
                $destinationPath = Join-Path -Path $DestinationDir `
                    -ChildPath $_
                Move-Item -Path $sourcePath `
                    -Destination $destinationPath
            }
        }
    }

    # Finally, sleep as many seconds as was specified on the command
    # line.
    Start-Sleep -Seconds (1.0 * $PollRate)
}

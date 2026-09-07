param(
    [Parameter(Mandatory = $true)]
    [string] $PackageDirectory,

    [string] $Version = "0.1.0"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Get-PackageEntries([string] $Path) {
    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        return @($archive.Entries | ForEach-Object { $_.FullName })
    }
    finally {
        $archive.Dispose()
    }
}

function Get-NuspecText([string] $Path, [string] $NuspecName) {
    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entry = $archive.GetEntry($NuspecName)
        if ($null -eq $entry) {
            throw "Package '$Path' does not contain '$NuspecName'."
        }

        $reader = [System.IO.StreamReader]::new($entry.Open())
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}

$codeGenPackage = Join-Path $PackageDirectory "Multicaster.CodeGen.Source.$Version.nupkg"
$generatorPackage = Join-Path $PackageDirectory "Multicaster.SourceGenerator.$Version.nupkg"

$codeGenEntries = Get-PackageEntries $codeGenPackage
$expectedSources = @(
    "MethodIdCalculator.cs",
    "ProxyEmitter.cs",
    "ReceiverModel.cs",
    "ReceiverValidator.cs"
)
foreach ($source in $expectedSources) {
    $expectedPath = "contentFiles/cs/any/Cysharp.Runtime.Multicast.CodeGen/$source"
    if ($codeGenEntries -notcontains $expectedPath) {
        throw "Source-only package is missing '$expectedPath'."
    }
}

$generatorEntries = Get-PackageEntries $generatorPackage
if ($generatorEntries -notcontains "analyzers/dotnet/cs/Multicaster.SourceGenerator.dll") {
    throw "Source generator package does not contain its analyzer assembly."
}

$codeGenNuspec = Get-NuspecText $codeGenPackage "Multicaster.CodeGen.Source.nuspec"
$generatorNuspec = Get-NuspecText $generatorPackage "Multicaster.SourceGenerator.nuspec"
if ($codeGenNuspec -match "<dependencies" -or $generatorNuspec -match "<dependencies") {
    throw "Development-only packages must not expose dependency groups."
}

Write-Host "Multicaster package layout verification passed."

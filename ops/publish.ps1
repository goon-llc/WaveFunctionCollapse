param (
  [string]$apikey
)

$version = Get-Content .\WFC\WFC.csproj | Select-String "^\s*<Version>(.*)<\/Version>\s*$" | ForEach-Object {
  $_.Matches.Groups[1].value
}

$semverPattern = "^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$"

if ($version -match $semverPattern)
{
  dotnet nuget push "./WFC/bin/Release/Go-On.WaveFunctionCollapse.$version.nupkg" --source "https://nuget.pkg.github.com/goon-llc/index.json" --api-key $apikey --skip-duplicate
}
else
{
  throw "package version is not valid semver: see https://semver.org/ for details"
}

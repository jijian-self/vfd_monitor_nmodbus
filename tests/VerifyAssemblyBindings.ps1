param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\bin\Debug')
)

$configPath = Join-Path $OutputDirectory 'vfd_monitor_nmodbus.exe.config'
if (-not (Test-Path -LiteralPath $configPath)) {
    throw "Missing application config: $configPath"
}

[xml]$config = Get-Content -LiteralPath $configPath
$namespace = New-Object System.Xml.XmlNamespaceManager($config.NameTable)
$namespace.AddNamespace('asm', 'urn:schemas-microsoft-com:asm.v1')

foreach ($name in @('System.Memory', 'System.Buffers')) {
    $dllPath = Join-Path $OutputDirectory "$name.dll"
    if (-not (Test-Path -LiteralPath $dllPath)) {
        throw "Missing assembly: $dllPath"
    }

    $assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($dllPath).Version.ToString()
    $redirect = $config.SelectSingleNode(
        "/configuration/runtime/asm:assemblyBinding/asm:dependentAssembly[asm:assemblyIdentity/@name='$name']/asm:bindingRedirect",
        $namespace
    )
    if ($null -eq $redirect) {
        throw "Missing binding redirect for $name"
    }
    if ($redirect.newVersion -ne $assemblyVersion) {
        throw "$name redirects to $($redirect.newVersion), but $dllPath has assembly version $assemblyVersion"
    }

    Write-Output "$name binding redirect matches assembly version $assemblyVersion"
}

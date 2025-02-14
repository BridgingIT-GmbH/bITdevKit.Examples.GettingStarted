function Get-FilteredTree {
    param (
        [string]$Path = ".",
        [string[]]$Extensions = @("sln", "csproj", "cs", "json", "xml", "config", "props")
    )

    $items = Get-ChildItem -Path $Path -Recurse -File | 
        Where-Object { $_.Extension -match ($Extensions -join "|") } |
        Sort-Object FullName

    $items | ForEach-Object {
        $relativePath = $_.FullName -replace ([regex]::Escape((Get-Location).Path) + "\\"), ""
        $depth = ($relativePath -split "\\").Count - 1
        (" " * ($depth * 3)) + "|-- " + ($_ | Split-Path -Leaf)
    } | Out-File solution-structure.txt
}

Get-FilteredTree
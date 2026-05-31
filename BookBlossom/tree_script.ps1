$excludeDirs = @('.vs', '.vscode', '.git', 'bin', 'obj', 'lib', 'node_modules', 'TestResults')

function Get-Tree {
    param (
        [string]$Path,
        [string]$Indent = ""
    )
    $items = Get-ChildItem -Path $Path -Force
    
    $dirs = $items | Where-Object { $_.PSIsContainer } | Where-Object { $excludeDirs -notcontains $_.Name }
    $files = $items | Where-Object { -not $_.PSIsContainer } | Where-Object { $_.Name -ne "tree_output.txt" -and $_.Name -ne "tree_script.ps1" -and $_.Name -ne "clean_tree.txt" }

    $all = @($dirs) + @($files)
    $count = $all.Count

    for ($i = 0; $i -lt $count; $i++) {
        $item = $all[$i]
        $isLast = ($i -eq $count - 1)
        
        $marker = if ($isLast) { "\--- " } else { "+--- " }
        $childIndent = if ($isLast) { "    " } else { "|   " }

        if ($item.PSIsContainer) {
            $subItems = Get-ChildItem -Path $item.FullName -Force
            $subDirs = $subItems | Where-Object { $_.PSIsContainer } | Where-Object { $excludeDirs -notcontains $_.Name }
            $subFiles = $subItems | Where-Object { -not $_.PSIsContainer }
            if ($subDirs.Count -eq 0 -and $subFiles.Count -eq 0) {
                Write-Output "$Indent$marker$($item.Name) (Tr?ng)"
            } else {
                Write-Output "$Indent$marker$($item.Name)"
                Get-Tree -Path $item.FullName -Indent ($Indent + $childIndent)
            }
        } else {
            Write-Output "$Indent$marker$($item.Name)"
        }
    }
}

Write-Output "BookBlossom" > clean_tree.txt
Get-Tree -Path "d:\VisualStudio\PBL3_BookBlossom\BookBlossom" >> clean_tree.txt

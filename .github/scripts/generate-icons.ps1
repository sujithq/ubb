# Generates favicon.png (64x64) and icon-192.png (192x192) for the UBB Simulator.
# Design: Copilot-style blue→purple gradient rounded tile, "UBB" wordmark,
# and the three flow-state dots (pass/warn/block) from the app's flow diagram.
Add-Type -AssemblyName System.Drawing

function New-UbbIcon {
    param([int]$Size, [string]$OutPath, [bool]$WithText)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.Clear([System.Drawing.Color]::Transparent)

    # Rounded-rect tile
    $radius = [int]($Size * 0.22)
    $rect = New-Object System.Drawing.Rectangle(0, 0, $Size, $Size)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($Size - $d, 0, $d, $d, 270, 90)
    $path.AddArc($Size - $d, $Size - $d, $d, $d, 0, 90)
    $path.AddArc(0, $Size - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    # Diagonal gradient: GitHub blue → Copilot purple
    $c1 = [System.Drawing.Color]::FromArgb(255, 31, 111, 235)   # #1f6feb
    $c2 = [System.Drawing.Color]::FromArgb(255, 130, 80, 223)   # #8250df
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $c1, $c2, 45.0)
    $g.FillPath($brush, $path)

    if ($WithText) {
        # "UBB" wordmark
        $fontSize = [float]($Size * 0.30)
        $font = New-Object System.Drawing.Font("Segoe UI", $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $white = [System.Drawing.Brushes]::White
        $fmt = New-Object System.Drawing.StringFormat
        $fmt.Alignment = [System.Drawing.StringAlignment]::Center
        $fmt.LineAlignment = [System.Drawing.StringAlignment]::Center
        $textRect = New-Object System.Drawing.RectangleF(0, [float]($Size * 0.08), [float]$Size, [float]($Size * 0.62))
        $g.DrawString("UBB", $font, $white, $textRect, $fmt)
        $font.Dispose()
        $dotY = $Size * 0.70
    }
    else {
        $dotY = $Size * 0.42
    }

    # Three flow-state dots: pass (green) / warn (amber) / block (red)
    $dotR = $Size * 0.085
    $gap = $Size * 0.10
    $cx = $Size / 2.0
    $colors = @(
        [System.Drawing.Color]::FromArgb(255, 63, 185, 80),    # #3fb950 pass
        [System.Drawing.Color]::FromArgb(255, 210, 153, 34),   # #d29922 warn
        [System.Drawing.Color]::FromArgb(255, 248, 81, 73)     # #f85149 block
    )
    for ($i = 0; $i -lt 3; $i++) {
        $x = $cx + ($i - 1) * ($dotR * 2 + $gap) - $dotR
        $b = New-Object System.Drawing.SolidBrush($colors[$i])
        $g.FillEllipse($b, [float]$x, [float]$dotY, [float]($dotR * 2), [float]($dotR * 2))
        $b.Dispose()
    }

    $g.Dispose()
    $bmp.Save($OutPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Wrote $OutPath ($Size x $Size)"
}

$wwwroot = Join-Path $PSScriptRoot "..\..\src\UBB\wwwroot"
New-UbbIcon -Size 192 -OutPath (Join-Path $wwwroot "icon-192.png") -WithText $true
New-UbbIcon -Size 64  -OutPath (Join-Path $wwwroot "favicon.png")  -WithText $false

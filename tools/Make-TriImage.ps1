# Ve lai plugin\Images\Tri.png (hinh tab "Tri stiffener") theo mau cac tab khac: nen 240,242,239 - member xanh - tam vang.
# Sua toa do o day roi chay lai; sau do Import lai hinh vao picTri trong Designer.
Add-Type -AssemblyName System.Drawing
$W = 478; $H = 342
$bmp = New-Object System.Drawing.Bitmap $W, $H
$bmp.SetResolution(96, 96)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(240, 242, 239))

$blue = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(102, 154, 204))
$yel = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 204, 102))
$blk = New-Object System.Drawing.Pen ([System.Drawing.Color]::Black), 1
$wht = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(235, 240, 245)), 1
$txt = [System.Drawing.Brushes]::Black

function Pts($a) { $r = @(); for ($i = 0; $i -lt $a.Count; $i += 2) { $r += New-Object System.Drawing.Point $a[$i], $a[$i + 1] }; , [System.Drawing.Point[]]$r }
function Poly($a) { $p = Pts $a; $g.SmoothingMode = 'AntiAlias'; $g.FillPolygon($yel, $p); $g.DrawPolygon($blk, $p); $g.SmoothingMode = 'None' }
function Arrow($x, $y, $dx, $dy) { $g.SmoothingMode = 'AntiAlias'; $g.FillPolygon($txt, (Pts @($x, $y, ($x + 6 * $dx + 2 * $dy), ($y + 6 * $dy + 2 * $dx), ($x + 6 * $dx - 2 * $dy), ($y + 6 * $dy - 2 * $dx)))); $g.SmoothingMode = 'None' }

$yT = 100; $yB = 240          # mat ngoai top / bottom flange
$x1 = 222; $xm = 239; $x2 = 256   # mat endplate 1 | mat splice | mat endplate 2
$eT = 40; $eB = 300           # mep tren / duoi endplate
$L = 75; $c = 6

# member 1, 2
foreach ($r in @(@(0, $x1), @($x2, $W))) {
    $g.FillRectangle($blue, $r[0], $yT, $r[1] - $r[0], $yB - $yT)
    $g.DrawLine($wht, $r[0], $yT + 8, $r[1] - 1, $yT + 8); $g.DrawLine($wht, $r[0], $yB - 8, $r[1] - 1, $yB - 8)
    $g.DrawLine($blk, $r[0], $yT, $r[1] - 1, $yT); $g.DrawLine($blk, $r[0], $yB, $r[1] - 1, $yB)
}
# tri stiffener (goc trong co chamfer)
Poly @(($x1 - $c), $yT, ($x1 - $L), $yT, $x1, $eT, $x1, ($yT - $c))
Poly @(($x2 + $c), $yT, ($x2 + $L), $yT, $x2, $eT, $x2, ($yT - $c))
Poly @(($x1 - $c), $yB, ($x1 - $L), $yB, $x1, $eB, $x1, ($yB + $c))
Poly @(($x2 + $c), $yB, ($x2 + $L), $yB, $x2, $eB, $x2, ($yB + $c))
# endplate
foreach ($x in @($x1, $xm)) { $g.FillRectangle($yel, $x, $eT, 17, $eB - $eT); $g.DrawRectangle($blk, $x, $eT, 17, $eB - $eT) }
# bolt
foreach ($y in @(70, 135, 205, 270)) {
    foreach ($b in @(@(($x1 - 9), 9), @(($x2 + 1), 9))) { $g.FillRectangle($yel, $b[0], $y - 5, $b[1], 10); $g.DrawRectangle($blk, $b[0], $y - 5, $b[1], 10) }
    $g.DrawLine($blk, $x1 - 13, $y, $x2 + 14, $y)
}
# so member
$f = New-Object System.Drawing.Font 'Arial', 11, ([System.Drawing.FontStyle]::Bold)
$g.TextRenderingHint = 'AntiAliasGridFit'
$g.DrawString('1', $f, $txt, 92, 161); $g.DrawString('2', $f, $txt, 374, 161)

# kich thuoc L (ngang, phia BU 1) va B (dung, phia BU 2) - top roi bottom
foreach ($s in @(@(22, ($yT - 4), ($eT - 4), -1), @(320, ($yB + 4), ($eB + 4), 1))) {
    $y = $s[0]
    $g.DrawLine($blk, $x1 - $L, $s[1], $x1 - $L, $y + 5 * $s[3]); $g.DrawLine($blk, $x1, $s[2], $x1, $y + 5 * $s[3])
    $g.DrawLine($blk, $x1 - $L, $y, $x1, $y); Arrow ($x1 - $L) $y 1 0; Arrow $x1 $y -1 0
}
$xd = 352
foreach ($s in @(@($eT, $yT), @($yB, $eB))) {
    $g.DrawLine($blk, $x2 + 4, $(if ($s[0] -eq $eT) { $eT } else { $eB }), $xd + 5, $(if ($s[0] -eq $eT) { $eT } else { $eB }))
    $g.DrawLine($blk, $x2 + $L + 4, $(if ($s[0] -eq $eT) { $yT } else { $yB }), $xd + 5, $(if ($s[0] -eq $eT) { $yT } else { $yB }))
    $g.DrawLine($blk, $xd, $s[0], $xd, $s[1]); Arrow $xd $s[0] 0 1; Arrow $xd $s[1] 0 -1
}

$out = Join-Path $PSScriptRoot '..\plugin\Images\Tri.png'
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
"Saved $out"

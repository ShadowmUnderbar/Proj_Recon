Add-Type -AssemblyName System.Drawing
$root='D:\UnityProj\Proj_Recon\Art\ElseIf_Blockout_20260922\MasterLocalQuality_20260925'
$files=@('Front','Side','Back','ThreeQuarter','BackQuarter','HighAngle','NaturalPose_Front','NaturalPose_ThreeQuarter','NaturalPose_HighAngle')
$bmp=New-Object Drawing.Bitmap 990,1344
$g=[Drawing.Graphics]::FromImage($bmp)
$g.Clear([Drawing.Color]::FromArgb(24,29,34))
$font=New-Object Drawing.Font 'Segoe UI',13
for($idx=0;$idx -lt $files.Count;$idx++) {
 $x=($idx%3)*330; $y=[math]::Floor($idx/3)*448
 $im=[Drawing.Image]::FromFile((Join-Path $root ($files[$idx]+'.png')))
 $g.DrawImage($im,$x,$y+28,330,420)
 $g.DrawString($files[$idx],$font,[Drawing.Brushes]::White,$x+8,$y+4)
 $im.Dispose()
}
$bmp.Save((Join-Path $root 'Review_Grid.jpg'),[Drawing.Imaging.ImageFormat]::Jpeg)
$g.Dispose(); $bmp.Dispose(); $font.Dispose()

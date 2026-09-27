Add-Type -AssemblyName System.Drawing
$root='D:\UnityProj\Proj_Recon\Art\ElseIf_Blockout_20260922\HeadPart01_20260926'
function Make-Sheet($names,$width,$height,$output) {
 $sheet=New-Object Drawing.Bitmap ($width*$names.Count),($height+32)
 $g=[Drawing.Graphics]::FromImage($sheet);$g.Clear([Drawing.Color]::FromArgb(24,29,34));$font=New-Object Drawing.Font 'Segoe UI',11
 for($i=0;$i -lt $names.Count;$i++) {
  $im=[Drawing.Image]::FromFile((Join-Path $root ($names[$i]+'.png')))
  $scale=[Math]::Min($width/$im.Width,$height/$im.Height);$w=[int]($im.Width*$scale);$h=[int]($im.Height*$scale)
  $g.DrawString($names[$i],$font,[Drawing.Brushes]::White,($i*$width+8),5)
  $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $g.DrawImage($im,[int]($i*$width+($width-$w)/2),[int](32+($height-$h)/2),$w,$h);$im.Dispose()
 }
 $sheet.Save((Join-Path $root $output),[Drawing.Imaging.ImageFormat]::Jpeg);$g.Dispose();$font.Dispose();$sheet.Dispose()
}
Make-Sheet @('Head_Front','Head_Side','Head_Back','Head_ThreeQuarter','Head_HighAngle') 256 256 'Head_Views.jpg'
Make-Sheet @('Assembly_Front','Assembly_ThreeQuarter','Assembly_BackQuarter','Assembly_HighAngle') 256 326 'Assembly_Views.jpg'
Make-Sheet @('Head_Front','Expression_Joy','Expression_Cry') 320 320 'Expressions.jpg'
Make-Sheet @('Head_ThreeQuarter','Assembly_HighAngle') 480 560 'Head01_Overview.jpg'
$r=Get-Content (Join-Path $root 'Final_Statistics.json') -Raw | ConvertFrom-Json
$c=$r.highangle_crop;$im=[Drawing.Image]::FromFile((Join-Path $root 'Assembly_HighAngle.png'));$scale=256.0/[Math]::Max($c[2],$c[3]);$w=[int]($c[2]*$scale);$h=[int]($c[3]*$scale);$bm=New-Object Drawing.Bitmap $w,$h;$g=[Drawing.Graphics]::FromImage($bm);$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$dest=New-Object Drawing.Rectangle 0,0,$w,$h;$src=New-Object Drawing.Rectangle $c[0],$c[1],$c[2],$c[3];$g.DrawImage($im,$dest,$src,[Drawing.GraphicsUnit]::Pixel);$bm.Save((Join-Path $root 'Assembly_HighAngle_256.jpg'),[Drawing.Imaging.ImageFormat]::Jpeg);$g.Dispose();$bm.Dispose();$im.Dispose()

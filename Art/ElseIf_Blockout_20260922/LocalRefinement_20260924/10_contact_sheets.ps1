Add-Type -AssemblyName System.Drawing
$dir=Join-Path $PSScriptRoot '.'
function New-Sheet($filename,$title,$names) {
 $bmp=New-Object Drawing.Bitmap 1440,1376
 $g=[Drawing.Graphics]::FromImage($bmp)
 $g.Clear([Drawing.Color]::FromArgb(22,29,37))
 $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 $font=New-Object Drawing.Font 'Segoe UI',23,([Drawing.FontStyle]::Bold)
 $label=New-Object Drawing.Font 'Segoe UI',15
 $g.DrawString($title,$font,[Drawing.Brushes]::White,24,14)
 $g.DrawString('ElseIf / Local refinement / Shoulder, armhole and shoes',$label,[Drawing.Brushes]::LightGray,27,53)
 for($i=0;$i -lt 6;$i++) {
  $x=($i%3)*480;$y=88+[math]::Floor($i/3)*636
  $g.DrawString($names[$i],$label,[Drawing.Brushes]::White,($x+16),$y)
  $im=[Drawing.Image]::FromFile((Join-Path $dir ($names[$i]+'.png')))
  try {$g.DrawImage($im,[Drawing.Rectangle]::new(($x+8),($y+28),464,591))} finally {$im.Dispose()}
 }
 $bmp.Save((Join-Path $dir $filename),[Drawing.Imaging.ImageFormat]::Jpeg)
 $font.Dispose();$label.Dispose();$g.Dispose();$bmp.Dispose()
}
New-Sheet 'Basis_Overview.jpg' 'BASE POSE / SIX VIEWS' @('Front','Side','Back','ThreeQuarter','BackQuarter','HighAngle')
New-Sheet 'Pose_Comparison.jpg' 'BASE POSE / NATURAL POSE' @('Front','ThreeQuarter','HighAngle','NaturalPose_Front','NaturalPose_ThreeQuarter','NaturalPose_HighAngle')

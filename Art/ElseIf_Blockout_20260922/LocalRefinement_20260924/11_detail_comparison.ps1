Add-Type -AssemblyName System.Drawing
$before=[Drawing.Image]::FromFile((Join-Path $PSScriptRoot '..\DesignTransfer_20260924\ThreeQuarter.png'))
$after=[Drawing.Image]::FromFile((Join-Path $PSScriptRoot 'ThreeQuarter.png'))
$bmp=New-Object Drawing.Bitmap 1200,1060
$g=[Drawing.Graphics]::FromImage($bmp);$g.Clear([Drawing.Color]::FromArgb(22,29,37));$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$font=New-Object Drawing.Font 'Segoe UI',22,([Drawing.FontStyle]::Bold)
$g.DrawString('BEFORE',$font,[Drawing.Brushes]::White,20,15);$g.DrawString('AFTER',$font,[Drawing.Brushes]::White,620,15)
$rects=@([Drawing.RectangleF]::new(330,145,300,250),[Drawing.RectangleF]::new(315,865,240,215))
for($row=0;$row -lt 2;$row++) {
 $rect=$rects[$row];$scale=[math]::Min(560/$rect.Width,450/$rect.Height);$w=$rect.Width*$scale;$h=$rect.Height*$scale;$y=100+$row*490
 $g.DrawString(@('Shoulder / Armhole','Toe / Instep / Collar / Sole')[$row],$font,[Drawing.Brushes]::LightGray,20,($y-40))
 $g.DrawImage($before,[Drawing.RectangleF]::new(20,$y,$w,$h),$rect,[Drawing.GraphicsUnit]::Pixel)
 $g.DrawImage($after,[Drawing.RectangleF]::new(620,$y,$w,$h),$rect,[Drawing.GraphicsUnit]::Pixel)
}
$bmp.Save((Join-Path $PSScriptRoot 'Local_Before_After.jpg'),[Drawing.Imaging.ImageFormat]::Jpeg)
$before.Dispose();$after.Dispose();$font.Dispose();$g.Dispose();$bmp.Dispose()

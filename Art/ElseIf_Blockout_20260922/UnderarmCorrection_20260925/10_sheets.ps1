Add-Type -AssemblyName System.Drawing
function Sheet($name,$title,$files,$labels,$cols,$cw,$ch) {
 $bmp=[Drawing.Bitmap]::new(($cols*$cw),(60+[math]::Ceiling($files.Count/$cols)*$ch));$g=[Drawing.Graphics]::FromImage($bmp);$g.Clear([Drawing.Color]::FromArgb(24,30,37));$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$font=[Drawing.Font]::new('Segoe UI',18,[Drawing.FontStyle]::Bold);$small=[Drawing.Font]::new('Segoe UI',12)
 try {$g.DrawString($title,$font,[Drawing.Brushes]::White,14,15)
 for($i=0;$i -lt $files.Count;$i++) {$x=($i%$cols)*$cw;$y=60+[math]::Floor($i/$cols)*$ch;$g.DrawString($labels[$i],$small,[Drawing.Brushes]::White,($x+8),$y);$im=[Drawing.Image]::FromFile((Join-Path $PSScriptRoot ($files[$i]+'.png')));try {$g.DrawImage($im,[Drawing.Rectangle]::new(($x+8),($y+28),($cw-16),($ch-44)))}finally{$im.Dispose()}}
 $bmp.Save((Join-Path $PSScriptRoot $name),[Drawing.Imaging.ImageFormat]::Jpeg)
 }finally{$small.Dispose();$font.Dispose();$g.Dispose();$bmp.Dispose()}
}
Sheet 'Wide_Comparison.jpg' 'WIDE / SAME CAMERA AND LIGHTING' @('Current_Wide','Optimized_Wide','Corrected_Wide') @('Current Game','Optimized / before','Optimized / corrected') 3 440 584
Sheet 'Wide_Small_256.jpg' 'WIDE / 256px IMAGE HEIGHT' @('Current_Wide','Optimized_Wide','Corrected_Wide') @('Current','Optimized','Corrected') 3 217 300
Sheet 'Pose_Review.jpg' 'CORRECTED / SIX TEST POSES' @('Corrected_Base','Corrected_Arms45','Corrected_Arms90','Corrected_Wide','Corrected_AsymmetricAim','Corrected_OneArmBack') @('Base','Arms 45','Arms 90','Wide','Asymmetric Aim','One Arm Back') 3 360 482

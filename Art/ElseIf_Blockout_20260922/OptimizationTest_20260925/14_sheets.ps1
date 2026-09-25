Add-Type -AssemblyName System.Drawing
$dir=$PSScriptRoot
function Sheet($filename,$title,$files,$labels,$cols=2,$cellw=440,$cellh=584) {
 $rows=[math]::Ceiling($files.Count/$cols);$bmp=[Drawing.Bitmap]::new(($cols*$cellw),(60+$rows*$cellh));$g=[Drawing.Graphics]::FromImage($bmp);$g.Clear([Drawing.Color]::FromArgb(24,30,37));$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$font=[Drawing.Font]::new('Segoe UI',19,[Drawing.FontStyle]::Bold);$small=[Drawing.Font]::new('Segoe UI',12)
 try {
 $g.DrawString($title,$font,[Drawing.Brushes]::White,16,15)
 for($i=0;$i -lt $files.Count;$i++) {$x=($i%$cols)*$cellw;$y=60+[math]::Floor($i/$cols)*$cellh;$g.DrawString($labels[$i],$small,[Drawing.Brushes]::White,($x+8),$y);$im=[Drawing.Image]::FromFile((Join-Path $dir ($files[$i]+'.png')));try {$g.DrawImage($im,[Drawing.Rectangle]::new(($x+8),($y+28),($cellw-16),($cellh-44)))} finally {$im.Dispose()}}
 $bmp.Save((Join-Path $dir $filename),[Drawing.Imaging.ImageFormat]::Jpeg)
 }finally {$small.Dispose();$font.Dispose();$g.Dispose();$bmp.Dispose()}
}
$names=@('Base_HighAngle','Natural_HighAngle','AsymmetricAim','ArmsWide','Elbow90','OneArmBack','WristTwist80','Arms90');$files=@();$labels=@()
foreach($n in $names) {foreach($m in @('Current','Optimized')) {$files+=($m+'_'+$n);$labels+=($m+' / '+$n)}}
Sheet 'Comparison_All.jpg' 'CURRENT / OPTIMIZED - SAME CAMERA AND LIGHT' $files $labels 4 360 482
foreach($n in $names) {Sheet ('Compare_'+$n+'.jpg') $n @(('Current_'+$n),('Optimized_'+$n)) @('CURRENT - 311,109 triangles','OPTIMIZED - 288,752 triangles')}
Sheet 'Compare_Back.jpg' 'BACK GRAPHIC / SAME MATERIALS' @('Current_Back','Optimized_Back') @('CURRENT','OPTIMIZED')
Sheet 'HighAngle_Small.jpg' 'SMALL VIEW CHECK / 256px image height' @('Current_Base_HighAngle','Optimized_Base_HighAngle','Current_ArmsWide','Optimized_ArmsWide') @('Current Base','Optimized Base','Current Wide','Optimized Wide') 4 217 300


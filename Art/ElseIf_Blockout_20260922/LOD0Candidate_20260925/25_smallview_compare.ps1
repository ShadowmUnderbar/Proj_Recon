Add-Type -AssemblyName System.Drawing
$root='D:\UnityProj\Proj_Recon\Art\ElseIf_Blockout_20260922\LOD0Candidate_20260925'
$crops=Get-Content (Join-Path $root 'SmallView_Crops.json') -Raw | ConvertFrom-Json
$names=@('Base_HighAngle','Natural_HighAngle','ArmsWide','AsymmetricAim','Elbow90','OneArmBack','WristTwist','Base_BackQuarter')
$sheet=New-Object Drawing.Bitmap 1088,1280
$sg=[Drawing.Graphics]::FromImage($sheet); $sg.Clear([Drawing.Color]::FromArgb(24,29,34))
$font=New-Object Drawing.Font 'Segoe UI',12
$metrics=@()
for($k=0;$k -lt $names.Count;$k++) {
 $name=$names[$k]; $c=$crops.$name; $scale=256.0/[Math]::Max($c[2],$c[3]); $w=[int][Math]::Round($c[2]*$scale); $h=[int][Math]::Round($c[3]*$scale)
 $pair=New-Object Drawing.Bitmap 544,320; $pg=[Drawing.Graphics]::FromImage($pair); $pg.Clear([Drawing.Color]::FromArgb(24,29,34)); $pg.DrawString($name,$font,[Drawing.Brushes]::White,8,3)
 $small=@()
 for($side=0;$side -lt 2;$side++) {
  $label=@('Current','Optimized')[$side]; $im=[Drawing.Image]::FromFile((Join-Path $root ($label+'_'+$name+'.png'))); $bm=New-Object Drawing.Bitmap $w,$h; $g=[Drawing.Graphics]::FromImage($bm); $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $dest=New-Object Drawing.Rectangle 0,0,$w,$h; $src=New-Object Drawing.Rectangle $c[0],$c[1],$c[2],$c[3]; $g.DrawImage($im,$dest,$src,[Drawing.GraphicsUnit]::Pixel); $g.Dispose(); $im.Dispose(); $small+=,$bm
  $pg.DrawString($label,$font,[Drawing.Brushes]::White,($side*272+8),25); $pg.DrawImage($bm,[int]($side*272+(272-$w)/2),[int](52+(256-$h)/2),$w,$h)
 }
 $sum=0.0; $sq=0.0; $changed=0; $maximum=0
 for($y=0;$y -lt $h;$y++) { for($x=0;$x -lt $w;$x++) { $a=$small[0].GetPixel($x,$y);$b=$small[1].GetPixel($x,$y); $ds=@([Math]::Abs([int]$a.R-[int]$b.R),[Math]::Abs([int]$a.G-[int]$b.G),[Math]::Abs([int]$a.B-[int]$b.B)); $local=0; foreach($d in $ds) {$sum+=$d; $sq+=$d*$d; $local=[Math]::Max($local,$d)}; if($local -gt 10){$changed++};$maximum=[Math]::Max($maximum,$local) } }
 $metrics += [PSCustomObject]@{pose=$name;image_size=@($w,$h);mean_absolute_rgb_255=$sum/($w*$h*3);rmse_rgb_255=[Math]::Sqrt($sq/($w*$h*3));percent_pixels_over_10=100.0*$changed/($w*$h);max_channel_difference=$maximum}
 $pair.Save((Join-Path $root ('Small_'+$name+'.jpg')),[Drawing.Imaging.ImageFormat]::Jpeg); $sg.DrawImage($pair,($k%2)*544,[int][Math]::Floor($k/2)*320,544,320)
 foreach($im in $small){$im.Dispose()};$pg.Dispose();$pair.Dispose()
}
$sheet.Save((Join-Path $root 'SmallView_256_Comparison.jpg'),[Drawing.Imaging.ImageFormat]::Jpeg);$sg.Dispose();$sheet.Dispose();$font.Dispose();$metrics | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $root 'SmallView_Metrics.json') -Encoding utf8
$metrics | Format-Table -AutoSize

Add-Type -AssemblyName System.Drawing
function New-Sheet($filename,$title,$names,$labels,$cols=3) {
 $rows=[math]::Ceiling($names.Count/$cols);$w=$cols*440;$h=64+$rows*584
 $bmp=[Drawing.Bitmap]::new($w,$h);$g=[Drawing.Graphics]::FromImage($bmp)
 $g.Clear([Drawing.Color]::FromArgb(22,29,37));$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 $font=[Drawing.Font]::new('Segoe UI',20,[Drawing.FontStyle]::Bold);$label=[Drawing.Font]::new('Segoe UI',13)
 try {
  $g.DrawString($title,$font,[Drawing.Brushes]::White,18,14)
  for($i=0;$i -lt $names.Count;$i++) {
   $x=($i%$cols)*440;$y=64+[math]::Floor($i/$cols)*584
   $g.DrawString($labels[$i],$label,[Drawing.Brushes]::White,($x+12),$y)
   $im=[Drawing.Image]::FromFile((Join-Path $PSScriptRoot ($names[$i]+'.png')))
   try {$g.DrawImage($im,[Drawing.Rectangle]::new(($x+8),($y+28),424,540))} finally {$im.Dispose()}
  }
  $bmp.Save((Join-Path $PSScriptRoot $filename),[Drawing.Imaging.ImageFormat]::Jpeg)
 } finally {$font.Dispose();$label.Dispose();$g.Dispose();$bmp.Dispose()}
}
New-Sheet 'Master_Game_Comparison.jpg' 'SAME CAMERA / LIGHTING - BASE HIGH ANGLE' @('Master_BasePose_HighAngle','Game_BeforeReduction_HighAngle','Game_BasePose_HighAngle') @('MASTER - 665,037 triangles','GAME before reduction','GAME - 311,109 triangles')
New-Sheet 'Stress_Overview.jpg' 'GAME / A-I STRESS POSES' @('Game_NaturalPose_HighAngle','Game_B_Side90_HighAngle','Game_C_Forward_HighAngle','Game_D_AsymmetricAim_HighAngle','Game_E_Elbow90_HighAngle','Game_F_OneArmBack_HighAngle','Game_G_WideOpen_HighAngle','Game_H_FrontNear_HighAngle','Game_I_FrontBack_HighAngle') @('A Natural','B Side 90','C Forward','D Asymmetric Aim','E Elbow 90','F One Arm Back','G Wide Open','H Front Near','I Front / Back')
New-Sheet 'Required_Views.jpg' 'GAME / DEFORMATION REVIEW' @('Game_BasePose_HighAngle','Game_NaturalPose_HighAngle','Game_D_AsymmetricAim_ThreeQuarter','Game_E_Elbow90_ThreeQuarter','Game_F_OneArmBack_ThreeQuarter','Game_J_WristTwist_HighAngle') @('Base HighAngle','Natural HighAngle','Asymmetric Aim / ThreeQuarter','Elbow 90 / ThreeQuarter','One Arm Back / ThreeQuarter','Additional Wrist Twist 80')

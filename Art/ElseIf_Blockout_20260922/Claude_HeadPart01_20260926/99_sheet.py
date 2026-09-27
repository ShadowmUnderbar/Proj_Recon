# レンダー画像を1枚のシートにまとめる（ローカルPython/PIL）
import sys
from PIL import Image,ImageDraw
prefix=sys.argv[1];out=sys.argv[2];ns=sys.argv[3].split(',');cols=int(sys.argv[4]) if len(sys.argv)>4 else 3;S=int(sys.argv[5]) if len(sys.argv)>5 else 360
ims=[Image.open(f'Renders/{prefix}{n}.png').convert('RGB') for n in ns]
ims=[im.resize((S,int(S*im.height/im.width))) for im in ims];H=ims[0].height
sh=Image.new('RGB',(S*cols,(H+22)*((len(ims)+cols-1)//cols)),(20,22,26));d=ImageDraw.Draw(sh)
for i,(n,im) in enumerate(zip(ns,ims)):
    x=(i%cols)*S;y=(i//cols)*(H+22);d.text((x+6,y+5),n,fill=(230,230,230));sh.paste(im,(x,y+22))
sh.save(out,quality=90)

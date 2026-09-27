# Current（ケーブルなし）とCable Addedを上下2段に並べた比較シートを作る
import sys
from PIL import Image,ImageDraw
out,prefix,ns,S=sys.argv[1],sys.argv[2],sys.argv[3].split(','),int(sys.argv[4])
rows=[('Current Head','Renders/Current_NoCable/'),('Cable Added Head','Renders/')]
ims=[[Image.open(f'{d}{prefix}{n}.png').convert('RGB') for n in ns] for _,d in rows]
H=int(S*ims[0][0].height/ims[0][0].width);L=22
sh=Image.new('RGB',(S*len(ns),(H+L)*2),(20,22,26));dr=ImageDraw.Draw(sh)
for r,(label,_) in enumerate(rows):
    for c,(n,im) in enumerate(zip(ns,ims[r])):
        x=c*S;y=r*(H+L);dr.text((x+6,y+5),f'{label} / {n}',fill=(230,230,230));sh.paste(im.resize((S,H)),(x,y+L))
sh.save(out,quality=90)

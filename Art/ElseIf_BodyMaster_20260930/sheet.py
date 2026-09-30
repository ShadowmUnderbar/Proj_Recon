"""usage: python sheet.py out.png crop(l,t,r,b as fractions or 'none') img1 img2 ..."""
import sys
from PIL import Image
out = sys.argv[1]
crop = sys.argv[2]
ims = [Image.open(p).convert("RGB") for p in sys.argv[3:]]
if crop != "none":
    l, t, r, b = map(float, crop.split(","))
    ims = [im.crop((int(im.width * l), int(im.height * t), int(im.width * r), int(im.height * b))) for im in ims]
w = sum(i.width for i in ims)
h = max(i.height for i in ims)
c = Image.new("RGB", (w, h), (255, 255, 255))
x = 0
for i in ims:
    c.paste(i, (x, 0))
    x += i.width
c.save(out)

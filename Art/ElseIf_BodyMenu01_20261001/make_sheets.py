# 比較シート・ポーズテストシートを作る（Blender外の python で実行）
from PIL import Image, ImageDraw, ImageFont
import os
os.chdir(os.path.join(os.path.dirname(os.path.abspath(__file__)), "Renders"))
f = ImageFont.truetype("C:/Windows/Fonts/meiryo.ttc", 22)


def pair_sheet(views, pm, pg, labels, out, crop=(0.14, 0.06, 0.86, 0.97), w=300):
    cols = []
    for v, lb in zip(views, labels):
        ims = []
        for pre in (pm, pg):
            im = Image.open(f"Compare/{pre}{v}.png").convert("RGB")
            W, H = im.size
            im = im.crop((int(W * crop[0]), int(H * crop[1]), int(W * crop[2]), int(H * crop[3])))
            ims.append(im.resize((w, int(im.height * w / im.width))))
        cols.append((lb, ims))
    h = cols[0][1][0].height
    c = Image.new("RGB", (w * 2 * len(cols), h + 70), (250, 250, 250))
    d = ImageDraw.Draw(c)
    for k, (lb, ims) in enumerate(cols):
        x = k * 2 * w
        d.text((x + 8, 6), lb, fill=(20, 20, 20), font=f)
        d.text((x + 8, 36), "Master", fill=(90, 90, 90), font=f)
        d.text((x + w + 8, 36), "Menu01", fill=(160, 40, 40), font=f)
        c.paste(ims[0], (x, 70))
        c.paste(ims[1], (x + w, 70))
    c.save(out)


pair_sheet(["Front", "Side", "Back"], "Master_", "Menu01_", ["Front", "Side", "Back"], "Compare/Sheet_A_FrontSideBack.png")
pair_sheet(["ThreeQuarter", "BackQuarter", "HighAngle"], "Master_", "Menu01_", ["ThreeQuarter", "BackQuarter", "HighAngle"], "Compare/Sheet_B_Quarter_High.png")
pair_sheet(["Front", "ThreeQuarter", "HighAngle"], "Master_NaturalPose_", "Menu01_NaturalPose_",
           ["NaturalPose Front", "NaturalPose ThreeQuarter", "NaturalPose HighAngle"], "Compare/Sheet_C_NaturalPose.png")
ks = ["ArmsWide", "ArmForward", "ArmBack", "Elbow90", "Asymmetric", "Seated"]
vs = ["ThreeQuarter", "BackQuarter", "HighAngle"]
w = 260
rows = []
for k in ks:
    ims = []
    for v in vs:
        im = Image.open(f"PoseTest/{k}_{v}.png").convert("RGB")
        W, H = im.size
        im = im.crop((int(W * .05), int(H * .08), int(W * .95), int(H * .75)))
        ims.append(im.resize((w, int(im.height * w / im.width))))
    rows.append(ims)
h = rows[0][0].height
c = Image.new("RGB", (w * 3 + 170, (h + 6) * len(ks)), (250, 250, 250))
d = ImageDraw.Draw(c)
for i, (k, ims) in enumerate(zip(ks, rows)):
    y = i * (h + 6)
    d.text((8, y + h // 2 - 12), k, fill=(20, 20, 20), font=f)
    for j, im in enumerate(ims):
        c.paste(im, (170 + j * w, y))
c.save("PoseTest/Sheet_PoseTest.png")

# 近接ショット（Master / Menu01）
names = ["Shoulder", "ShoulderBack", "Collar", "CuffInside", "Elbow", "Hem", "ThighBorder", "Shoe"]
w = 300
tiles = []
for n in names:
    ims = []
    for pre in ("Master_", "Menu01_"):
        im = Image.open(f"Close/{pre}{n}.png").convert("RGB")
        W, H = im.size
        im = im.crop((int(W * .15), int(H * .2), int(W * .85), int(H * .8)))
        ims.append(im.resize((w, int(im.height * w / im.width))))
    tiles.append((n, ims))
h = tiles[0][1][0].height
cols = 2
rows_ = (len(tiles) + cols - 1) // cols
c = Image.new("RGB", (w * 2 * cols, (h + 40) * rows_), (250, 250, 250))
d = ImageDraw.Draw(c)
for k, (n, ims) in enumerate(tiles):
    x = (k % cols) * 2 * w
    y = (k // cols) * (h + 40)
    d.text((x + 8, y + 6), n + "  (左 Master / 右 Menu01)", fill=(20, 20, 20), font=f)
    c.paste(ims[0], (x, y + 40))
    c.paste(ims[1], (x + w, y + 40))
c.save("Close/Sheet_Close.png")
hs = [Image.open(f"Close/Menu01_HandTopology{x}.png").convert("RGB") for x in ("", "_Palm")]
hs = [im.crop((int(im.width * .2), int(im.height * .2), int(im.width * .8), int(im.height * .8))) for im in hs]
c = Image.new("RGB", (sum(i.width for i in hs), hs[0].height), (250, 250, 250))
x = 0
for im in hs:
    c.paste(im, (x, 0))
    x += im.width
c.save("Close/Sheet_HandTopology.png")

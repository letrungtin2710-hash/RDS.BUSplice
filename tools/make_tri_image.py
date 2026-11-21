"""
plugin\\Images\\Tri.png (tab "Tri stiffener") in the SAME drawing style as the neighbouring tabs:

  - bolts and the member numbers 1 / 2 are copied pixel for pixel from plugin\\Images\\Endplate.png
  - colours measured on Endplate.png / Flange.png: background 240,242,239 - steel 102,154,204 - plate 255,204,102
  - every part has the Tekla relief: light edge top / left, grey + black edge bottom / right

    python tools/make_tri_image.py

Writes the PNG and puts the same bytes into plugin\\MainForm.resx (picTri.Image). The input boxes of
MainForm.Designer.cs sit on the dimension lines drawn here: change a coordinate here -> move the box there.
"""
import base64
import io
import os
import re
from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
BACK = (240, 242, 239)
STEEL, STEEL_HI, STEEL_LO = (102, 154, 204), (255, 255, 255), (120, 130, 143)
PLATE, PLATE_HI, PLATE_LO = (255, 204, 102), (255, 255, 222), (139, 128, 100)
BLACK = (0, 0, 0)

W, H = 660, 342
X1, XM, X2 = 314, 330, 346          # face of endplate 1 | splice face | face of endplate 2
YT, YB = 110, 232                   # outer face of top / bottom flange
ET, EB = 50, 292                    # top / bottom edge of the endplates
L, CH = 80, 6                       # triangle leg along the flange, inner corner chamfer
MX1, MX2 = 20, 640                  # far ends of member 1 / 2
BOLT_Y = (80, 150, 192, 262)
DIM_L_Y = (30, 312)                 # y of the L dimension line (top, bottom) - on member 1 side
DIM_B_X = 530                       # x of the B dimension line - on member 2 side


def relief(im, pts, fill, hi, lo):
    """Filled polygon with the Tekla relief: 1 px light edge where the outside is above / left,
    1 px black edge where it is below / right, 1 px 'lo' line just inside the black edge."""
    mask = Image.new('L', im.size, 0)
    ImageDraw.Draw(mask).polygon(pts, fill=255)
    m, px = mask.load(), im.load()
    inside = lambda x, y: 0 <= x < W and 0 <= y < H and m[x, y] > 0
    x1, y1 = max(0, min(p[0] for p in pts)), max(0, min(p[1] for p in pts))
    x2, y2 = min(W - 1, max(p[0] for p in pts)), min(H - 1, max(p[1] for p in pts))
    black = set()
    for y in range(y1, y2 + 1):
        for x in range(x1, x2 + 1):
            if inside(x, y) and (not inside(x + 1, y) or not inside(x, y + 1)):
                black.add((x, y))
    for y in range(y1, y2 + 1):
        for x in range(x1, x2 + 1):
            if not inside(x, y):
                continue
            if (x, y) in black:
                px[x, y] = BLACK
            elif (x + 1, y) in black or (x, y + 1) in black:
                px[x, y] = lo
            elif not inside(x - 1, y) or not inside(x, y - 1):
                px[x, y] = hi
            else:
                px[x, y] = fill


def sprite(src, box, back, tol=60):
    """Pixels of 'box' that are not background 'back' -> [(dx, dy, colour)] relative to the box centre row."""
    px = src.load()
    out = []
    for y in range(box[1], box[3]):
        for x in range(box[0], box[2]):
            c = px[x, y]
            if sum(abs(c[i] - back[i]) for i in range(3)) > tol:
                out.append((x - box[0], y - box[1], c))
    return out


def put(im, sp, x, y):
    px = im.load()
    for dx, dy, c in sp:
        px[x + dx, y + dy] = c


def arrow(d, x, y, dx, dy):
    """Solid 4 x 7 arrow head, tip at (x, y), pointing in direction (dx, dy)."""
    for i in range(7):
        h = (i + 1) // 2
        if dx:
            d.line([(x - dx * i, y - h), (x - dx * i, y + h)], fill=BLACK)
        else:
            d.line([(x - h, y - dy * i), (x + h, y - dy * i)], fill=BLACK)


def dim_h(d, xa, xb, y, ya, yb):
    """Horizontal dimension xa..xb at height y; extension lines come from ya (at xa) and yb (at xb)."""
    s = 1 if y > ya else -1
    d.line([(xa, ya), (xa, y + 4 * s)], fill=BLACK)
    d.line([(xb, yb), (xb, y + 4 * s)], fill=BLACK)
    d.line([(xa, y), (xb, y)], fill=BLACK)
    arrow(d, xa, y, -1, 0)
    arrow(d, xb, y, 1, 0)


def dim_v(d, ya, yb, x, xa, xb):
    d.line([(xa, ya), (x + 4, ya)], fill=BLACK)
    d.line([(xb, yb), (x + 4, yb)], fill=BLACK)
    d.line([(x, ya), (x, yb)], fill=BLACK)
    arrow(d, x, ya, 0, -1)
    arrow(d, x, yb, 0, 1)


def draw():
    ref = Image.open(os.path.join(ROOT, 'plugin', 'Images', 'Endplate.png')).convert('RGB')
    im = Image.new('RGB', (W, H), BACK)
    d = ImageDraw.Draw(im)

    for xa, xb in ((MX1, X1 - 1), (X2 + 1, MX2)):                       # member 1, 2 (web + 2 flanges)
        relief(im, [(xa, YT), (xb, YT), (xb, YB), (xa, YB)], STEEL, STEEL_HI, STEEL_LO)
        d.line([(xa + 1, YT + 9), (xb - 1, YT + 9)], fill=STEEL_LO)      # inner face of the top flange
        d.line([(xa + 1, YT + 10), (xb - 1, YT + 10)], fill=BLACK)
        d.line([(xa + 1, YB - 11), (xb - 1, YB - 11)], fill=STEEL_HI)    # inner face of the bottom flange
        d.line([(xa + 1, YB - 10), (xb - 1, YB - 10)], fill=STEEL_HI)

    for s, y, e in ((-1, YT, ET), (1, YB, EB)):                          # 4 triangles, chamfer at the inner corner
        y0 = y - 1 if s < 0 else y + 1                                   # sits on the flange face
        relief(im, [(X1 - 1 - CH, y0), (X1 - 1 - L, y0), (X1 - 1, e), (X1 - 1, y0 + s * CH)], PLATE, PLATE_HI, PLATE_LO)
        relief(im, [(X2 + 1 + CH, y0), (X2 + 1 + L, y0), (X2 + 1, e), (X2 + 1, y0 + s * CH)], PLATE, PLATE_HI, PLATE_LO)

    for x in (X1, XM):                                                   # 2 endplates
        relief(im, [(x, ET), (x + 15, ET), (x + 15, EB), (x, EB)], PLATE, PLATE_HI, PLATE_LO)

    # bolts: nut (left) and head (right) of the Endplate tab, row y = 88 (over steel) and y = 33 (over background)
    for y in BOLT_Y:
        on_steel = YT < y < YB
        ry, back = (88, STEEL) if on_steel else (33, BACK)
        left = sprite(ref, (100, ry - 9, 112, ry + 9), back)
        right = sprite(ref, (143, ry - 9, 153, ry + 9), back)
        put(im, left, X1 - 12, y - 9)
        put(im, right, X2 + 1, y - 9)

    for box, x in (((60, 128, 74, 148), 150), ((172, 128, 190, 148), 492)):      # member numbers 1 / 2
        put(im, sprite(ref, box, STEEL, 90), x, 161)

    for y, yy, e in ((DIM_L_Y[0], YT, ET), (DIM_L_Y[1], YB, EB)):        # L: along the flange, from the endplate face
        s = -1 if y < yy else 1
        dim_h(d, X1 - 1 - L, X1 - 1, y, yy + 4 * s, e + 4 * s)
    for ya, yb in ((ET, YT), (YB, EB)):                                  # B: along the endplate
        far = ya if ya in (ET, EB) else yb
        dim_v(d, ya, yb, DIM_B_X, X2 + 6 if ya == far else X2 + L + 6, X2 + 6 if yb == far else X2 + L + 6)
    return im


def main():
    im = draw()
    out = os.path.join(ROOT, 'plugin', 'Images', 'Tri.png')
    im.save(out)
    b = io.BytesIO()
    im.save(b, 'PNG')
    b64 = base64.b64encode(b.getvalue()).decode()
    lines = '\r\n'.join('        ' + b64[i:i + 80] for i in range(0, len(b64), 80))
    resx_path = os.path.join(ROOT, 'plugin', 'MainForm.resx')
    resx = open(resx_path, encoding='utf-8-sig', newline='').read()
    pat = re.compile(r'(<data name="picTri\.Image"[^>]*>\s*<value>)(.*?)(</value>)', re.S)
    resx, n = pat.subn(lambda m: m.group(1) + '\r\n' + lines + '\r\n' + m.group(3), resx)
    open(resx_path, 'w', encoding='utf-8', newline='').write(resx)
    print('Tri.png', im.size, 'resx entries replaced:', n)


if __name__ == '__main__':
    main()

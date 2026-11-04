"""Generate original bottle-label artwork as SVG with outlined, portable lettering.

Development dependency: fonttools. The game has no Python or font dependency.
Crimson Text (SIL OFL 1.1) is fetched into the ignored .tools directory.
"""
from pathlib import Path
import math
import sys
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / ".tools" / "python-packages"))
from fontTools.ttLib import TTFont
from fontTools.pens.svgPathPen import SVGPathPen

FONT = ROOT / ".tools" / "CrimsonText-Regular.ttf"
if not FONT.exists():
    FONT.parent.mkdir(parents=True, exist_ok=True)
    FONT.write_bytes(urllib.request.urlopen("https://raw.githubusercontent.com/google/fonts/main/ofl/crimsontext/CrimsonText-Regular.ttf").read())
font = TTFont(FONT)
glyphs = font.getGlyphSet()
cmap = font.getBestCmap()
units = font['head'].unitsPerEm


def lettering(text, y, size, color, tracking=0, x=768):
    scale = size / units
    names = [cmap.get(ord(c), '.notdef') for c in text]
    width = sum(glyphs[n].width * scale for n in names) + max(0, len(names)-1)*tracking
    cursor = x - width/2
    result = []
    for name in names:
        pen = SVGPathPen(glyphs)
        glyphs[name].draw(pen)
        result.append(f'<path fill="{color}" transform="translate({cursor:.2f},{y}) scale({scale:.6f},{-scale:.6f})" d="{pen.getCommands()}"/>')
        cursor += glyphs[name].width * scale + tracking
    return ''.join(result)


LABELS = [
    ('patron', ['PATRÓN'], 'TEQUILA', '#eee8cf', '#254334', '#b08b43', 'AGAVE BLANCO'),
    ('grey-goose', ['GREY GOOSE'], 'VODKA', '#e9edef', '#2d415b', '#98a6b4', 'DISTILLED SPIRIT'),
    ('tanqueray', ['TANQUERAY'], 'LONDON DRY GIN', '#e7dfcb', '#264533', '#b59c64', 'BOTANICAL SELECTION'),
    ('woodford', ['WOODFORD', 'RESERVE'], 'KENTUCKY BOURBON', '#e2d4b2', '#40362c', '#a17c42', 'OAK MATURED'),
    ('zacapa', ['ZACAPA', '23'], 'AGED RUM', '#40342d', '#e6d2a7', '#bca06b', 'RESERVA SELECTION'),
    ('cuervo', ['JOSE CUERVO'], 'TEQUILA', '#ead8a5', '#573b26', '#9f7543', 'AGAVE SELECTION'),
    ('smirnoff', ['SMIRNOFF'], 'VODKA', '#922b29', '#f1e6ce', '#d6b885', 'TRADITIONAL DISTILLATION'),
    ('gordons', ['GORDON’S'], 'LONDON DRY GIN', '#e1dfb6', '#3f482f', '#a48e51', 'JUNIPER & BOTANICALS'),
    ('jack-daniels', ['JACK DANIEL’S'], 'TENNESSEE WHISKEY', '#252721', '#e9e0c7', '#b8a37a', 'OAK BARREL SELECTION'),
    ('bacardi', ['BACARDÍ'], 'WHITE RUM', '#e9e2cf', '#423c32', '#b9995e', 'CARTA BLANCA'),
    ('well-tequila', ['BLANCO'], 'TEQUILA', '#e8dec3', '#3a5140', '#af9258', 'MARGARITA STATION'),
    ('triple-sec', ['TRIPLE SEC'], 'ORANGE LIQUEUR', '#d9bc80', '#4b3827', '#9e7441', 'CITRUS SELECTION'),
]


def label(slug, title, spirit, paper, ink, foil, subtitle):
    svg = [f'<svg xmlns="http://www.w3.org/2000/svg" width="1536" height="2048" viewBox="0 0 1536 2048">',
           f'<rect width="1536" height="2048" fill="{paper}"/>',
           f'<rect x="38" y="38" width="1460" height="1972" rx="26" fill="none" stroke="{foil}" stroke-width="14"/>',
           f'<rect x="65" y="65" width="1406" height="1918" rx="12" fill="none" stroke="{ink}" stroke-width="3"/>',
           f'<rect x="90" y="90" width="1356" height="1868" fill="none" stroke="{foil}" stroke-width="2"/>']
    # Original guilloche medallion and botanical border, independent of official brand artwork.
    for radius, width in [(190, 6), (174, 2), (155, 3)]:
        svg.append(f'<circle cx="768" cy="410" r="{radius}" fill="none" stroke="{foil}" stroke-width="{width}"/>')
    for i in range(48):
        a=i*math.tau/48
        x,y=768+183*math.cos(a),410+183*math.sin(a)
        svg.append(f'<circle cx="{x:.2f}" cy="{y:.2f}" r="3" fill="{foil}"/>')
    svg.append(lettering(title[0][0], 474, 190, ink))
    for side in (-1,1):
        for i in range(11):
            x=768+side*(235+i*28); y=380+math.sin(i*.23)*130
            svg.append(f'<path d="M {x} {y} q {side*35} -50 {side*45} -8 q {-side*12} 30 {-side*45} 8" fill="none" stroke="{foil}" stroke-width="4"/>')
        svg.append(f'<path d="M {768+side*245} 390 Q {768+side*450} 590 {768+side*615} 410" fill="none" stroke="{foil}" stroke-width="5"/>')
    svg.append(lettering('THE NEIGHBORHOOD COLLECTION', 154, 39, ink, 3))
    start=870 if len(title)==1 else 805
    for i,line in enumerate(title):
        size=min(165,1220/max(1,len(line))/.59)
        svg.append(lettering(line,start+i*155,size,ink,1.5))
    for y in (1110,1360):
        svg.append(f'<path d="M 245 {y} H 650 m 235 0 H 1291" stroke="{foil}" stroke-width="4"/>')
        svg.append(f'<path d="M 720 {y} l 48 -16 48 16 -48 16 Z" fill="{foil}"/>')
    svg.append(lettering(spirit,1252,min(85,1100/max(len(spirit),1)/.57),ink,2))
    svg.append(lettering(subtitle,1480,46,ink,2))
    svg.append(lettering('750 mL   ·   BAR SELECTION',1675,49,ink,1))
    svg.append(lettering('SELECTED FOR THE NEIGHBORHOOD BAR',1835,33,ink,2))
    # Fine lines remain visible when approaching a bottle rather than turning into blank labels.
    for i in range(5):
        svg.append(f'<path d="M {410+i*22} {1520+i*13} H {1126-i*22}" stroke="{foil}" opacity="0.55" stroke-width="2"/>')
    for x in (153,1383):
        for y in (180,1868):
            svg.append(f'<path d="M {x-35} {y} l 35 -35 35 35 -35 35 Z" fill="none" stroke="{foil}" stroke-width="4"/>')
    svg.append('</svg>')
    return '\n'.join(svg)


if __name__ == '__main__':
    output=ROOT/'Assets'/'Labels'
    output.mkdir(parents=True,exist_ok=True)
    for spec in LABELS:
        (output/(spec[0]+'.svg')).write_text(label(*spec))
    print(f'Generated {len(LABELS)} original outlined SVG labels.')

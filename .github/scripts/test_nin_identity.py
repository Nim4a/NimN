"""User-visible identity and decoded tray palette regression checks."""
import unittest
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
COLORS = [(51, 153, 204), (220, 38, 38), (147, 51, 234), (22, 128, 61)]

class NiNIdentityTests(unittest.TestCase):
    def test_display_brand_preserves_update_identity(self):
        self.assertTrue('AppName = "NiN"' in (ROOT / 'v2rayN/ServiceLib/Global.cs').read_text(encoding='utf-8-sig'))
        for project in ['v2rayN/v2rayN/v2rayN.csproj', 'v2rayN/v2rayN.Desktop/v2rayN.Desktop.csproj']:
            text = (ROOT / project).read_text(encoding='utf-8-sig')
            self.assertTrue('<Product>NiN</Product>' in text)
            self.assertTrue('<AssemblyName>NimN</AssemblyName>' in text)

    def test_tray_colors_visible_at_all_icon_sizes(self):
        for folder in ['v2rayN/v2rayN/Resources', 'v2rayN/v2rayN.Desktop/Assets']:
            for number, rgb in enumerate(COLORS, 1):
                with Image.open(ROOT / folder / f'NotifyIcon{number}.ico') as icon:
                    for size in sorted(icon.ico.sizes()):
                        with self.subTest(folder=folder, mode=number, size=size):
                            im = icon.ico.getimage(size).convert('RGBA')
                            # Small ICO frames are resampled; allow 8/255 antialiasing error.
                            hits = sum(im.getpixel((x, y))[3] > 240 and
                                       max(abs(im.getpixel((x, y))[i] - c) for i, c in enumerate(rgb)) <= 8
                                       for x in range(im.width) for y in range(im.height))
                            self.assertGreater(hits, size[0] * size[1] * 0.20)

if __name__ == '__main__':
    unittest.main()

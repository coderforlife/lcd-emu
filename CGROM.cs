using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace LCD.Emulator
{
    public class CGROM
    {
        private const int charWidth = 6, charHeight = 10;
        private readonly byte[] cgrom_raw = new byte[256 * charHeight];
        private readonly Region[] cgrom = new Region[256];

        public CGROM(string file)
        {
            using (Bitmap b = new Bitmap(file))
            {
                BitmapData d = b.LockBits(new Rectangle(new Point(0, 0), b.Size), ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);
                int max = Math.Min(d.Height, this.cgrom_raw.Length);
                for (int i = 0; i < max; ++i)
                    this.cgrom_raw[i] = (byte)~Marshal.ReadByte(d.Scan0, i * d.Stride);
                b.UnlockBits(d);
            }
        }

        public Region this[int c]
        { get {
            if (this.cgrom[c] == null)
            {
                int C = c * charHeight;
                Region r = new Region();
                r.MakeEmpty();
                for (int y = 0; y < charHeight; ++y)
                {
                    byte row = this.cgrom_raw[C + y];
                    for (int x = 0; x < charWidth && row != 0; ++x)
                    {
                        if ((row & 0x80) == 0x80)
                            r.Union(new Rectangle(x, y, 1, 1));
                        row <<= 1;
                    }
                }
                this.cgrom[c] = r;
            }
            return this.cgrom[c];
        } }

        public byte[] GetCustomChar(int i)
        {
            if (i < 0 || i >= 8) { throw new ArgumentOutOfRangeException(); }
            byte[] b = new byte[8];
            for (int j = 0; j < 8; ++j)
                b[j] = (byte)(this.cgrom_raw[i * charHeight + j] >> 3);
            return b;
        }
        public void SetCustomChar(int i, byte[] b)
        {
            if (i < 0 || i >= 8) { throw new ArgumentOutOfRangeException(); }
            if (b.Length != 8) { throw new ArgumentException(); }
            for (int j = 0; j < 8; ++j)
                this.cgrom_raw[i * charHeight + j] = (byte)(b[j] << 3);
            this.cgrom[i] = null;
        }
    }

    public static class RegionExtensions
    {
        public static Region TranslatedAndClipped(this Region r, int dx, int dy, Rectangle clip)
        {
            Region R = new Region();
            R.Intersect(r);
            R.Intersect(clip);
            R.Translate(dx, dy);
            return R;
        }
    }
}

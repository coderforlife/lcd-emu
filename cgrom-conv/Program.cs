using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace cgrom_conv
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] data = new byte[10 * 16 * 16];
            StreamReader r = new StreamReader(File.OpenRead("cgrom.txt"));
            string line;
            int x = 0;
            while ((line = r.ReadLine()) != null)
            {
                line = line.PadRight(6, ' ').Substring(0, 6);
                byte row = 0;
                foreach (char c in line)
                {
                    row <<= 1;
                    if (c != ' ')
                        row |= 0x04;
                }
                data[x++] = (byte)~row;
            }

            Bitmap b = new Bitmap(6, data.Length, PixelFormat.Format1bppIndexed);
            BitmapData d = b.LockBits(new Rectangle(0, 0, 6, data.Length), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);
            for (int i = 0; i < data.Length; ++i)
                Marshal.WriteByte(d.Scan0, i*d.Stride, data[i]);
            b.UnlockBits(d);
            b.Save("cgrom.png");
        }
    }
}

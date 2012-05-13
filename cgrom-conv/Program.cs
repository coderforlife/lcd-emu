using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace cgrom_conv
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename = args.Length > 0 ? args[0] : "cgrom.txt";
            StreamReader r = new StreamReader(File.OpenRead(filename));
            byte[] data = new byte[10 * 16 * 16];
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
            b.Save(Path.ChangeExtension(filename, ".png"));
        }
    }
}

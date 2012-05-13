using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;

namespace LCD.Emulator
{
    public class LCDDisplay : Form
    {
        private delegate void Action();

        private const int LINE_LEN = 40;

        private readonly LCDChip chip;
        private byte backlight = 0xFF, contrast = 0xFF / 2;
        private readonly Color background, foreground;
        private readonly SolidBrush fgBrush;
        private readonly Pen borderTopPen, borderLeftPen;
        private readonly CGROM cgrom;
        private readonly Size size, pixelSize, charSize, gapSize, borderSize, charFullSize, charFullPixelSize, tlOffset;
        private readonly Rectangle character;
        private bool display = true, cursor = true;
        private int cursor_pos = 0;
        private readonly byte[] text = new byte[4 * LINE_LEN];
        private Timer blinker = new Timer();
        private bool blink_on = false;

        private static Size ReadSize(string sz)
        {
            int x = sz.IndexOf("x");
            if (x < 0)
            {
                int n = Convert.ToInt32(sz);
                return new Size(n, n);
            }
            else
            {
                return new Size(Convert.ToInt32(sz.Substring(0, x)), Convert.ToInt32(sz.Substring(x + 1)));
            }
        }

        public LCDDisplay(XmlNode settings, LCDChip chip)
        {
            this.chip = chip;

            this.InitializeComponent();
            try { this.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location); }
            catch (Exception) { }
            this.background = ColorTranslator.FromHtml(settings["Background"].InnerText);
            this.foreground = ColorTranslator.FromHtml(settings["Foreground"].InnerText);
            this.fgBrush = new SolidBrush(this.foreground);
            this.cgrom = new CGROM(settings["CGROM"].InnerText);
            this.size = ReadSize(settings["Size"].InnerText);
            this.charSize = ReadSize(settings["CharacterSize"].InnerText);
            this.gapSize = ReadSize(settings["GapSize"].InnerText);
            this.pixelSize = ReadSize(settings["PixelSize"].InnerText);
            this.borderSize = ReadSize(settings["BorderSize"].InnerText);
            this.charFullSize = new Size(this.charSize.Width + this.gapSize.Width, this.charSize.Height + this.gapSize.Height);
            this.charFullPixelSize = new Size(this.charFullSize.Width * this.pixelSize.Width, this.charFullSize.Height * this.pixelSize.Height);
            this.tlOffset = new Size(this.gapSize.Width * this.pixelSize.Width + borderSize.Width, this.gapSize.Height * this.pixelSize.Height + borderSize.Height);
            this.character = new Rectangle(0, 0, this.charSize.Width, this.charSize.Height);

            this.borderTopPen  = new Pen(this.fgBrush, this.borderSize.Height);
            this.borderLeftPen = new Pen(this.fgBrush, this.borderSize.Width );

            this.Size = new Size(
                this.size.Width  * this.charFullPixelSize.Width  + this.tlOffset.Width  + borderSize.Width,
                this.size.Height * this.charFullPixelSize.Height + this.tlOffset.Height + borderSize.Height
            );
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;

            this.blinker.Enabled = true;
            this.blinker.Interval = 400;
            this.blinker.Tick += this.BlinkIt;

            this.Clear();
            this.UpdateColors();
        }
        private void InitializeComponent()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.SuspendLayout();
            menu.Items.AddRange(new ToolStripItem[]{
                new ToolStripMenuItem("Button 1", null,
                    new ToolStripMenuItem("Click", null, delegate(object s, EventArgs e) { this.chip.ButtonClick(0); }),
                    new ToolStripMenuItem("Down",  null, delegate(object s, EventArgs e) { this.chip.ButtonDown (0); }),
                    new ToolStripMenuItem("Up",    null, delegate(object s, EventArgs e) { this.chip.ButtonUp   (0); })
                ),
                new ToolStripMenuItem("Button 2", null,
                    new ToolStripMenuItem("Click", null, delegate(object s, EventArgs e) { this.chip.ButtonClick(1); }),
                    new ToolStripMenuItem("Down",  null, delegate(object s, EventArgs e) { this.chip.ButtonDown (1); }),
                    new ToolStripMenuItem("Up",    null, delegate(object s, EventArgs e) { this.chip.ButtonUp   (1); })
                ),
                new ToolStripMenuItem("Button 3", null,
                    new ToolStripMenuItem("Click", null, delegate(object s, EventArgs e) { this.chip.ButtonClick(2); }),
                    new ToolStripMenuItem("Down",  null, delegate(object s, EventArgs e) { this.chip.ButtonDown (2); }),
                    new ToolStripMenuItem("Up",    null, delegate(object s, EventArgs e) { this.chip.ButtonUp   (2); })
                ),
                new ToolStripMenuItem("Button 4", null,
                    new ToolStripMenuItem("Click", null, delegate(object s, EventArgs e) { this.chip.ButtonClick(3); }),
                    new ToolStripMenuItem("Down",  null, delegate(object s, EventArgs e) { this.chip.ButtonDown (3); }),
                    new ToolStripMenuItem("Up",    null, delegate(object s, EventArgs e) { this.chip.ButtonUp   (3); })
                ),
                new ToolStripMenuItem("Button 5", null,
                    new ToolStripMenuItem("Click", null, delegate(object s, EventArgs e) { this.chip.ButtonClick(4); }),
                    new ToolStripMenuItem("Down",  null, delegate(object s, EventArgs e) { this.chip.ButtonDown (4); }),
                    new ToolStripMenuItem("Up",    null, delegate(object s, EventArgs e) { this.chip.ButtonUp   (4); })
                ),
                new ToolStripMenuItem("Exit", null, this.exit),
            });
            menu.ResumeLayout(false);

            this.SuspendLayout();
            this.ContextMenuStrip = menu;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.Text = "LCD Display";
            this.ResumeLayout(false);
        }
        private void exit(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
                this.Invoke(new Action(this.Close));
            else
                this.Close();
        }

        private void SetBlinker(bool Enabled)
        {
            if (this.InvokeRequired)
                this.Invoke(new Action<bool>(this.SetBlinker), Enabled);
            else
            {
                this.blinker.Enabled = Enabled;
                this.blink_on = true;
                this.InvalidateCurCharacter();
            }
        }
        private void BlinkIt(object sender, EventArgs e)
        {
            this.blink_on = !this.blink_on;
            this.InvalidateCurCharacter();
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;
        [DllImport("user32")] public static extern bool ReleaseCapture();
        [DllImport("user32")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0); } base.OnMouseDown(e); }

        private void DrawCharacter(Graphics g, int x, int y, byte c)
        {
            g.FillRegion(this.fgBrush, this.cgrom[c].TranslatedAndClipped(x * this.charFullSize.Width, y * this.charFullSize.Height, this.character));
        }

        private void DrawUnderline(Graphics g, int x, int y)
        {
            g.FillRectangle(this.fgBrush, x * this.charFullSize.Width, y * this.charFullSize.Height + 8, this.charSize.Width, 1);
        }

        private void DrawBox(Graphics g, int x, int y)
        {
            g.FillRectangle(this.fgBrush, x * this.charFullSize.Width, y * this.charFullSize.Height, this.charSize.Width, this.charSize.Height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.Clear(this.BackColor);
            float w = this.Size.Width, h = this.Size.Height, lh = this.borderSize.Height / 2f, lw = this.borderSize.Width / 2f;
            g.DrawLine(this.borderTopPen,  0, lh,     w, lh    ); // top
            g.DrawLine(this.borderTopPen,  0, h - lh, w, h - lh); // bottom
            g.DrawLine(this.borderLeftPen, lw,     0, lw,     h); // left
            g.DrawLine(this.borderLeftPen, w - lw, 0, w - lw, h); // right
            if (this.display)
            {
                bool blink_off = this.blinker.Enabled && !this.blink_on;
                g.TranslateTransform(this.tlOffset.Width, this.tlOffset.Height);
                g.ScaleTransform(this.pixelSize.Width, this.pixelSize.Height);
                for (int i = 0; i < this.text.Length; ++i)
                    if (i != cursor_pos || !blink_off)
                        DrawCharacter(g, i % LINE_LEN, i / LINE_LEN, (byte)this.text[i]);
                if (this.cursor && !blink_off) DrawUnderline(g, this.cursor_pos % LINE_LEN, this.cursor_pos / LINE_LEN);
                //if (this.blinker.Enabled && this.blink_on) DrawBox(g, this.cursor_pos % LINE_LEN, this.cursor_pos / LINE_LEN); 
            }
        }

        private void InvalidateCurCharacter()
        {
            if (this.InvokeRequired)
                this.Invoke(new Action(this.InvalidateCurCharacter));
            else
                this.Invalidate(new Rectangle(
                    (this.cursor_pos % LINE_LEN) * this.charFullPixelSize.Width  + this.tlOffset.Width,
                    (this.cursor_pos / LINE_LEN) * this.charFullPixelSize.Height + this.tlOffset.Height,
                    this.charFullPixelSize.Width, this.charFullPixelSize.Height));
        }
        private void InvalidateAll()
        {
            if (this.InvokeRequired)
                this.Invoke(new Action(this.Invalidate));
            else
                this.Invalidate();
        }

        private void Goto(int c_pos)
        {
            if (this.InvokeRequired)
                this.Invoke(new Action<int>(this.Goto), c_pos);
            else
            {
                this.InvalidateCurCharacter();
                this.cursor_pos = c_pos % this.text.Length;
                this.InvalidateCurCharacter();
            }
        }

        private int ApplyBacklight(byte c) { return c * this.backlight / 255; }
        private Color ApplyBacklight(Color c) { return Color.FromArgb(ApplyBacklight(c.R), ApplyBacklight(c.G), ApplyBacklight(c.B)); }
        private static int Mix(byte a, byte b, byte amount) { return a * (255 - amount) / 255 + b * amount / 255; } // amount == 0 => all a, amount == 255 => all b
        private static Color Mix(Color a, Color b, byte amount) { return Color.FromArgb(Mix(a.R, b.R, amount), Mix(a.G, b.G, amount), Mix(a.B, b.B, amount)); }
        private void UpdateColors()
        {
            if (this.InvokeRequired)
                this.Invoke(new Action(this.UpdateColors));
            else
            {
                this.BackColor = this.ApplyBacklight(this.background);
                this.ForeColor = this.ApplyBacklight(this.foreground);
                if (this.contrast <= 127) { this.ForeColor = Mix(this.ForeColor, this.BackColor, (byte)((127 - this.contrast) * 2)); } // this.contrast == 0   => entirely this.BackColor
                if (this.contrast >= 128) { this.BackColor = Mix(this.BackColor, this.ForeColor, (byte)((this.contrast - 128) * 2)); } // this.contrast == 255 => entirely this.ForeColor
                this.fgBrush.Color = this.ForeColor;
                this.Invalidate();
            }
        }

        public void Write(byte c)   { this.text[this.cursor_pos] = c; this.CursorRight(); }
        public void Write(byte[] c) { this.Write(c, 0, c.Length); }
        public void Write(byte[] c, int off, int len)
        {
            while (len > 0)
            {
                int avail = LINE_LEN - this.cursor_pos % LINE_LEN, copy = Math.Min(avail, len);
                Array.Copy(c, off, this.text, this.cursor_pos, copy);
                off += copy; len -= copy; this.cursor_pos += copy - 1;
                this.CursorRight();
                this.InvalidateAll();
            }
        }
        public void Clear() { for (int i = 0; i < this.text.Length; ++i) { this.text[i] = (byte)' '; } this.InvalidateAll(); }
        public void Home()  { this.Goto(0); }
        public bool Display { get { return this.display; } set { this.display = value; this.InvalidateAll(); } }
        public bool Blink   { get { return this.blinker.Enabled; } set { /*this.blinker.Enabled = value;*/ this.SetBlinker(value); } }
        public bool ShowCursor { get { return this.cursor; } set { this.cursor = value; this.InvalidateCurCharacter(); } }
        public void Goto(byte col, byte row) { this.Goto(LINE_LEN * (row - 1) + (col - 1)); }
        public void CursorLeft()  { this.Goto(this.cursor_pos - 1); }
        public void CursorRight() { this.Goto(this.cursor_pos + 1); }
        public byte Contrast  { get { return this.contrast;  } set { this.contrast = value;  this.UpdateColors(); } }
        public byte Backlight { get { return this.backlight; } set { this.backlight = value; this.UpdateColors(); } }
        public byte[] GetCustomChar(int i) { return this.cgrom.GetCustomChar(i); }
        public void SetCustomChar(int i, byte[] b) { this.cgrom.SetCustomChar(i, b); this.InvalidateAll(); }
        public byte[] CompleteContent { get { return (byte[])this.text.Clone(); } set { Array.Copy(value, this.text, value.Length); this.InvalidateAll(); } }
    }
}

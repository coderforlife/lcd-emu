using System;
using System.Threading;
using System.Xml;

namespace LCD.Emulator
{
    public class LCDChip
    {
        private const int VERSION = 0x20; // Emulated USB2LCD+ Firmware version (v2.0)
        private const int MODULE  = 0xEE; // Module version (EE for emulating)

        /**
         * EEPROM Memory Locations
         */
        // We have max 256 bytes, this uses 0x00-0xF0, 0xFF (0-240, 255), there are 14 bytes left
        private const byte EEP_DISPLAY      = 0x00; // display, cursor, and blink
        private const byte EEP_DISPLAY_MIN  = 0x01;
        private const byte EEP_BACKLIGHT    = 0x02;
        private const byte EEP_CONTRAST     = 0x03;

        private const byte EEP_GPO_0        = 0x04;
        private const byte EEP_GPO_1        = 0x05;
        private const byte EEP_GPO_2        = 0x06;
        private const byte EEP_GPO_3        = 0x07;
        private const byte EEP_GPO_4        = 0x08;
        private readonly byte[] EEP_GPO     = { EEP_GPO_0, EEP_GPO_1, EEP_GPO_2, EEP_GPO_3, EEP_GPO_4, };

        private const byte EEP_GPO_PWM_0    = 0x09;
        private const byte EEP_GPO_PWM_1    = 0x0A;
        private const byte EEP_GPO_PWM_2    = 0x0B;
        private const byte EEP_GPO_PWM_3    = 0x0C;
        private const byte EEP_GPO_PWM_4    = 0x0D;
        private readonly byte[] EEP_GPO_PWM = { EEP_GPO_PWM_0, EEP_GPO_PWM_1, EEP_GPO_PWM_2, EEP_GPO_PWM_3, EEP_GPO_PWM_4, };

        private const byte EEP_SER_NUM_0    = 0x0E;
        private const byte EEP_SER_NUM_1    = 0x0F;

        private const byte EEP_CHAR_0       = 0x10;
        private const byte EEP_CHAR_1       = 0x18;
        private const byte EEP_CHAR_2       = 0x20;
        private const byte EEP_CHAR_3       = 0x28;
        private const byte EEP_CHAR_4       = 0x30;
        private const byte EEP_CHAR_5       = 0x38;
        private const byte EEP_CHAR_6       = 0x40;
        private const byte EEP_CHAR_7       = 0x48;
        private readonly byte[] EEP_CHAR    = { EEP_CHAR_0, EEP_CHAR_1, EEP_CHAR_2, EEP_CHAR_3, EEP_CHAR_4, EEP_CHAR_5, EEP_CHAR_6, EEP_CHAR_7, }; // get character x

        private const byte LINE_LEN         = 0x28; // 40
        private const byte EEP_MSG_START    = 0x50;
        private const byte EEP_LINE_0       = EEP_MSG_START;
        private const byte EEP_LINE_1       = EEP_MSG_START + LINE_LEN;
        private const byte EEP_LINE_2       = EEP_MSG_START + LINE_LEN*2;
        private const byte EEP_LINE_3       = EEP_MSG_START + LINE_LEN*3;
        private readonly byte[] EEP_LINE    = { EEP_LINE_0, EEP_LINE_1, EEP_LINE_2, EEP_LINE_3 };

        private const byte EEP_FIRMWARE     = 0xFF; // this byte is set to 1 when the firmware will be reprogrammed after a RESET (not used in the emulated version)

        private Link link;
        private LCDDisplay lcd;

        private bool remember = false;
        private EEPROM eeprom;

        private DCB DCB_state = DCB.General;
        private byte onFor = 0;
        private Timer t;

        private bool[] gpo = new bool[5];
        private byte[] gpo_val = new byte[5];

        public LCDChip(XmlNode settings)
        {
            XmlNode chip = settings["Chip"];
            this.link = Link.CreateLink(chip["Link"].InnerText);
            this.link.ByteRecieved += this.ProccessByte;
            this.eeprom = new EEPROM(chip["EEPROM"].InnerText);
            this.lcd = new LCDDisplay(settings["Display"], this);
            this.lcd.FormClosed += FormClosed;
            this.t = new Timer(this.TurnOffDisplay);

            this.lcd.Clear();
            this.DCB_state = (DCB)this.eeprom.ReadByte(EEP_DISPLAY);
            this.lcd.Display    = this.DCB_state.Has(DCB.Display);
            this.lcd.Blink      = this.DCB_state.Has(DCB.Blink);
            this.lcd.ShowCursor = this.DCB_state.Has(DCB.Cursor);
            SetTimeout(this.onFor = this.eeprom.ReadByte(EEP_DISPLAY_MIN));
            this.lcd.Backlight = this.eeprom.ReadByte(EEP_BACKLIGHT);
            this.lcd.Contrast = this.eeprom.ReadByte(EEP_CONTRAST);
            for (int i = 0; i < EEP_GPO.Length; ++i)
            {
                this.gpo[i] = this.eeprom.ReadByte(EEP_GPO[i]) > 0;
                this.gpo_val[i] = this.eeprom.ReadByte(EEP_GPO_PWM[i]);
                this.RefreshGPO(i);
            }
            for (int i = 0; i < EEP_CHAR.Length; ++i) { this.lcd.SetCustomChar(i, this.eeprom.ReadBytes(EEP_CHAR[i], 8)); }
            for (int i = 0; i < EEP_LINE.Length; ++i) { this.lcd.Goto(1, (byte)(i + 1)); this.lcd.Write(this.eeprom.ReadBytes(EEP_LINE[i], LINE_LEN)); }
            System.Windows.Forms.Application.Run(this.lcd);
        }
        ~LCDChip() { this.Close(); }
        public void Close() { this.link.Close(); this.eeprom.Close(); }
        private void FormClosed(object sender, System.Windows.Forms.FormClosedEventArgs e) { this.Close(); }

        private void SetTimeout(int min) { t.Change(min == 0 ? Timeout.Infinite : (min*60*1000), Timeout.Infinite); }
        private void TurnOffDisplay(object state) { SetTimeout(0); lcd.Display = false; }

        private readonly bool[] button_is_down = new bool[5];
        private readonly static byte[] button_ups   = { (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e' };
        private readonly static byte[] button_downs = { (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E' };

        private void buttonClick(object o) { int i = (int)o; this.ButtonDown(i); Thread.Sleep(50); this.ButtonUp(i); }
        private void buttonDown (object o) { int i = (int)o; if (!this.button_is_down[i]) { this.button_is_down[i] = true;  this.link.WriteUnsolicitedByte(button_downs[i]); } }
        private void buttonUp   (object o) { int i = (int)o; if ( this.button_is_down[i]) { this.button_is_down[i] = false; this.link.WriteUnsolicitedByte(button_ups  [i]); } }

        public void ButtonClick(int i) { new Thread(buttonClick).Start(i); }
        public void ButtonDown (int i) { new Thread(buttonDown ).Start(i); }
        public void ButtonUp   (int i) { new Thread(buttonUp   ).Start(i); }

        private void RememberIt(int addr, byte data) { if (remember) { eeprom[addr] = data; } }

        private void RefreshGPO(int i)
        {
            // TODO: some outward indication of GPO value / state
        }

        private byte BYTE { get { return link.ReadByte(); } }
        private void ProccessByte(byte b)
        {
            if (b != 254) { lcd.Write(b); return; }
            switch ((Command)BYTE)
            {
                case Command.DisplayOn:  DCB_state |= DCB.Display; RememberIt(EEP_DISPLAY, (byte)DCB_state); lcd.Display = true;  RememberIt(EEP_DISPLAY_MIN, onFor = BYTE); SetTimeout(onFor); break;
                case Command.DisplayOff: DCB_state &=~DCB.Display; RememberIt(EEP_DISPLAY, (byte)DCB_state); lcd.Display = false; SetTimeout(onFor = 0); break;
                case Command.CursorOn:   DCB_state |= DCB.Cursor;  RememberIt(EEP_DISPLAY, (byte)DCB_state); lcd.ShowCursor = true;  break;
                case Command.CursorOff:  DCB_state &=~DCB.Cursor;  RememberIt(EEP_DISPLAY, (byte)DCB_state); lcd.ShowCursor = false; break;
                case Command.BlinkOn:    DCB_state |= DCB.Blink;   RememberIt(EEP_DISPLAY, (byte)DCB_state); lcd.Blink      = true;  break;
                case Command.BlinkOff:   DCB_state &=~DCB.Blink;   RememberIt(EEP_DISPLAY, (byte)DCB_state); lcd.Blink      = false; break;
                case Command.ClearDisplay: this.lcd.Clear();       break;
                case Command.Home:         this.lcd.Home();        break;
                case Command.CursorLeft:   this.lcd.CursorLeft();  break;
                case Command.CursorRight:  this.lcd.CursorRight(); break;
                case Command.Position:     b = BYTE; this.lcd.Goto(b, BYTE); break;
                case Command.Contrast:     this.RememberIt(EEP_CONTRAST, this.lcd.Contrast = BYTE); break;
                case Command.Backlight_:
                case Command.Backlight:    this.RememberIt(EEP_BACKLIGHT, this.lcd.Backlight = BYTE); break;
                case Command.SaveBacklight: this.eeprom[EEP_BACKLIGHT] = this.lcd.Backlight = BYTE; break;
                case Command.DefineCustom:  b = BYTE; this.lcd.SetCustomChar(b, this.link.ReadBytes(8)); break;
                case Command.GPOoff: b = (byte)(BYTE - 1); if (b < this.gpo.Length) { this.gpo[b] = false; this.RefreshGPO(b); } break;
                case Command.GPOon:  b = (byte)(BYTE - 1); if (b < this.gpo.Length) { this.gpo[b] = true;  this.RefreshGPO(b); } break;
                case Command.GPOpwm_:
                case Command.GPOpwm: b = (byte)(BYTE - 1); if (b < this.gpo.Length) { this.gpo_val[b] = BYTE; this.RefreshGPO(b); } break;
                case Command.RememberGPOpwm: b = (byte)(BYTE - 1); this.eeprom[EEP_GPO_PWM[b]] = BYTE; break;
                case Command.RememberGPO:    b = (byte)(BYTE - 1); this.eeprom[EEP_GPO[b]] = (byte)((BYTE > 0) ? 1 : 0); break;
                case Command.Remember:       this.remember = BYTE==1u; break;
                case Command.RememberCustom: this.eeprom.WriteBytes(EEP_CHAR[BYTE], this.link.ReadBytes(8)); break;
                case Command.SaveStartup:    this.eeprom.WriteBytes(EEP_MSG_START, this.link.ReadBytes(160)); break;

                case Command.ReadButton: b = (byte)(BYTE - 1); this.link.WriteByte(b < this.button_is_down.Length ? (this.button_is_down[b] ? button_downs[b] : button_ups[b]) : (byte)' '); break;

                case Command.SetSerialNum:   eeprom.WriteBytes(EEP_SER_NUM_0, link.ReadBytes(2)); break;
                case Command.ReadSerialNum:  link.WriteBytes(eeprom.ReadBytes(EEP_SER_NUM_0, 2)); break;
                case Command.ReadVersion:    link.WriteByte(VERSION); break;
                case Command.ReadModuleType: link.WriteByte(MODULE); break;

                case Command.ReadDisplay:    link.WriteByte((byte)DCB_state); break;
                case Command.ReadDisplayMin: link.WriteByte(onFor); break;
                case Command.ReadContrast:   link.WriteByte(lcd.Contrast); break;
                case Command.ReadBacklight:  link.WriteByte(lcd.Backlight); break;
                case Command.ReadCustom:     link.WriteBytes(lcd.GetCustomChar(BYTE)); break;
                case Command.ReadMessage:    link.WriteBytes(lcd.CompleteContent); break;
                case Command.ReadGPO:        b = (byte)(BYTE - 1); link.WriteByte((byte)((b < gpo.Length && gpo[b]) ? 1 : 0)); break;
                case Command.ReadGPOpwm:     b = (byte)(BYTE - 1); link.WriteByte((byte)(b < gpo.Length ? gpo_val[b] : 0)); break;

                case Command.ReadSavedDisplay:    link.WriteByte(eeprom[EEP_DISPLAY]); break;
                case Command.ReadSavedDisplayMin: link.WriteByte(eeprom[EEP_DISPLAY_MIN]); break;
                case Command.ReadSavedContrast:   link.WriteByte(eeprom[EEP_CONTRAST]); break;
                case Command.ReadSavedBacklight:  link.WriteByte(eeprom[EEP_BACKLIGHT]); break;
                case Command.ReadSavedCustom:     link.WriteBytes(eeprom.ReadBytes(EEP_CHAR[BYTE], 8)); break;
                case Command.ReadSavedMessage:    link.WriteBytes(eeprom.ReadBytes(EEP_MSG_START, 160)); break;
                case Command.ReadSavedGPO:        b = (byte)(BYTE - 1); link.WriteByte((byte)((b < gpo.Length && eeprom[EEP_GPO[b]] > 0) ? 1 : 0)); break;
                case Command.ReadSavedGPOpwm:     b = (byte)(BYTE - 1); link.WriteByte((byte)(b < gpo.Length ? eeprom[EEP_GPO_PWM[b]] : 0)); break;

                case Command.Char254: lcd.Write(254); break;

                ////case Command.PORDevice: resetInit(); break;
                ////case Command.Firmware:  firmwareReprogramInit(); break;
            }
        }
    }

    [Flags] internal enum DCB : byte
    {
        General = 0x08,
        Display = 0x04,
        Blink = 0x02,
        Cursor = 0x01,
    }

    internal static class DCBExtensions
    {
        public static bool Has(this DCB dcb, DCB test) { return (dcb & test) == test; }
    }
}

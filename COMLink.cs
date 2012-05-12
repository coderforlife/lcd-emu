using System;
using System.IO.Ports;

namespace LCD.Emulator
{
    internal class COMLink : Link
    {
        private readonly SerialPort port;
        public COMLink(string spec)
        {
            string[] parts = spec.Split(':');
            if (parts.Length > 2) { throw new ArgumentException(); }
            this.port = new SerialPort(parts[0], parts.Length > 1 ? Convert.ToInt32(parts[1]) : 9600, Parity.None, 8, StopBits.One);
            this.port.Open();
            this.port.DiscardInBuffer();
            base.Initialize(this.port.BaseStream);
        }
    }
}

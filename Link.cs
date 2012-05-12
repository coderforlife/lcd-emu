using System;
using System.IO;
using System.Threading;

namespace LCD.Emulator
{
    public abstract class Link
    {
        public static Link CreateLink(string linkSpec)
        {
            string[] parts = linkSpec.Split(new char[]{':'}, 2);
            switch (parts[0].ToLower())
            {
                case "com":  return new COMLink(parts[1]);  // COM:name[:baud]
                case "tcp":  return new TCPLink(parts[1]);  // TCP:port[:LOCAL]
                case "pipe": return new PipeLink(parts[1]); // PIPE:name
            }
            throw new ArgumentException();
        }

        private Stream stream = null;
        private readonly Thread thread;
        private readonly object readLock = new object();

        protected Link()
        {
            this.thread = new Thread(ByteMonitor);
            this.thread.Name = "Byte Monitor";
        }

        protected void Initialize(Stream stream)
        {
            this.stream = stream;
            this.thread.Start();
        }

        public bool IsOpen { get { return this.stream != null; } }
        public virtual void Close() { if (this.stream != null) { this.stream.Close(); this.stream = null; } }
        ~Link() { this.Close(); }

        public delegate void ByteRecievedHandler(byte b);
        public event ByteRecievedHandler ByteRecieved;
        
        private void ByteMonitor()
        {
            Thread.Sleep(100);
            while (this.IsOpen)
            {
                int b = -1;
                lock (this.readLock)
                {
                    try
                    {
                        b = this.stream.ReadByte();
                    }
                    catch (Exception) { }
                }
                if (b != -1 && ByteRecieved != null)
                    ByteRecieved((byte)b);
            }
        }

        public byte ReadByte()
        {
            int b;
            lock (this.readLock) { b = this.stream.ReadByte(); }
            if (b == -1) { throw new EndOfStreamException(); }
            return (byte)b;
        }

        public void ReadBytes(byte[] b, int off, int n)
        {
            int total = 0;
            lock (this.readLock)
            {
                int read;
                while ((read = this.stream.Read(b, off + total, n - total)) > 0 && (total += read) < n) ;
            }
            if (total != n) { throw new EndOfStreamException(); }
        }
        public byte[] ReadBytes(int n) { byte[] b = new byte[n]; this.ReadBytes(b, 0, n); return b; }

        public void WriteByte(byte b)                    { this.stream.WriteByte(b);          this.stream.Flush(); }
        public void WriteBytes(byte[] b, int off, int n) { this.stream.Write(b, off, n);      this.stream.Flush(); }
        public void WriteBytes(byte[] b)                 { this.stream.Write(b, 0, b.Length); this.stream.Flush(); }

        public void WriteUnsolicitedByte(byte b) { if (this.IsOpen) { this.stream.WriteByte(b); this.stream.Flush(); } }
    }
}

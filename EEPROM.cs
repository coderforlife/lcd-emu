using System;
using System.Collections.Generic;
using System.IO;

namespace LCD.Emulator
{
    internal class EEPROM : IList<byte>
    {
        // Simulates a Memory-Mapped file. The file is so small (256 bytes) that this shouldn't be a problem that it isn't a real MMF.

        private FileStream file;
        private byte[] data = new byte[256]; // a cache of what is in the file, optimizing for reads

        public EEPROM(string file)
        {
            this.file = File.Open(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            int missing = (int)(data.Length - this.file.Length);
            if (missing > 0)
            {
                this.file.Seek(0, SeekOrigin.End);
                byte[] b = new byte[missing];
                this.file.Write(b, 0, missing);
                this.file.Seek(0, SeekOrigin.Begin);
            }
            int total = 0;
            int read;
            while ((read = this.file.Read(this.data, total, this.data.Length - total)) != 0 && (total += read) < this.data.Length) ;
            if (total != this.data.Length) { throw new EndOfStreamException(); }
        }
        ~EEPROM() { this.Close(); }
        public void Close() { this.file.Close(); }

        public byte ReadByte(byte addr)                             { return this.data[addr]; }
        public byte[] ReadBytes(byte addr, byte n)                  { byte[] b = new byte[n]; Array.Copy(this.data, addr, b, 0, n); return b; }
        public void ReadBytes(byte addr, byte[] b, byte n)          { Array.Copy(this.data, addr, b, 0,   n); }
        public void ReadBytes(byte addr, byte[] b, int off, byte n) { Array.Copy(this.data, addr, b, off, n); }

        public void WriteByte(byte addr, byte x)
        {
            if (this.data[addr] != x)
            {
                this.data[addr] = x;
                this.file.Seek(addr, SeekOrigin.Begin);
                this.file.WriteByte(x);
            }
        }
        public void WriteBytes(byte addr, byte[] x)
        {
            Array.Copy(x, 0, this.data, addr, x.Length);
            this.file.Seek(addr, SeekOrigin.Begin);
            this.file.Write(x, 0, x.Length);
        }
        public void WriteBytes(byte addr, byte[] x, byte n)
        {
            Array.Copy(x, 0, this.data, addr, n);
            this.file.Seek(addr, SeekOrigin.Begin);
            this.file.Write(x, 0, n);
        }
        public void WriteBytes(byte addr, byte[] x, int off, byte n)
        {
            Array.Copy(x, off, this.data, addr, n);
            this.file.Seek(addr, SeekOrigin.Begin);
            this.file.Write(x, off, n);
        }

        public byte this[int index]
        {
            get { return this.data[index]; }
            set { WriteByte((byte)index, value); }
        }

        public int Count { get { return this.data.Length; } }
        public bool IsReadOnly { get { return false; } }
        public int IndexOf(byte item) { return ((IList<byte>)this.data).IndexOf(item); }
        public bool Contains(byte item) { return ((IList<byte>)this.data).Contains(item); }

        public void CopyTo(byte[] array, int arrayIndex) { this.data.CopyTo(array, arrayIndex); }
        public IEnumerator<byte> GetEnumerator() { foreach (byte b in this.data) { yield return b; } }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return this.GetEnumerator(); }

        public void Insert(int index, byte item) { throw new NotSupportedException(); }
        public void Add(byte item) { throw new NotSupportedException(); }
        public void RemoveAt(int index) { throw new NotSupportedException(); }
        public bool Remove(byte item) { throw new NotSupportedException(); }
        public void Clear() { throw new NotSupportedException(); }
    }
}

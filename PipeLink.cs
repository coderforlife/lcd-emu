using System;
using System.IO.Pipes;
using System.Threading;

namespace LCD.Emulator
{
    internal class PipeLink : Link
    {
        private readonly NamedPipeServerStream pipe;

        public PipeLink(string spec)
        {
            this.pipe = new NamedPipeServerStream(spec, PipeDirection.InOut, 1, PipeTransmissionMode.Byte);
            Thread t = new Thread(this.Connect);
            t.Name = "Pipe Connector";
            t.Start();
        }
        private void Connect()
        {
            this.pipe.WaitForConnection();
            base.Initialize(this.pipe);
        }
    }
}

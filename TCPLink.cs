using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LCD.Emulator
{
    internal class TCPLink : Link
    {
        private TcpListener server;
        private Socket tcp;

        public TCPLink(string spec)
        {
            string[] parts = spec.Split(':');
            if (parts.Length > 2 || parts.Length == 2 && parts[1].ToLower() != "local") { throw new ArgumentException(); }
            int port = Convert.ToInt32(parts[0]);
            IPAddress addr = parts.Length == 2 ? IPAddress.Parse("127.0.0.1") : IPAddress.Any;
            this.server = new TcpListener(addr, port);
            Thread t = new Thread(this.Connect);
            t.Name = "TCP Connector";
            t.Start();
        }
        private void Connect()
        {
            this.server.Start();
            this.tcp = this.server.AcceptSocket();
            this.server.Stop();
            this.server = null;
            base.Initialize(new NetworkStream(this.tcp, true));
        }
        public override void Close()
        {
            if (this.server != null) { this.server.Stop(); this.server = null; }
            base.Close();
        }
    }
}

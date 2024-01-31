using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NetCoreTorrent
{
    /// <summary>
    /// IP for a single info_hash
    /// </summary>
    public class Peer
    {
        public IPEndPoint EndPoint { get; set; }
        public InfoHash InfoHash { get; set; }
    }
}

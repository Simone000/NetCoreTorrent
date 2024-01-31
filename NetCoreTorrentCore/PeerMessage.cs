using System;
using System.Collections.Generic;
using System.Text;

namespace NetCoreTorrent
{
    public class PeerMessage
    {
        public Enum_MessageType MessageType { get; set; }
        public byte[] Content { get; set; }
    }
}

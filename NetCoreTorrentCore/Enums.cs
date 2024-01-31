using System;
using System.Collections.Generic;
using System.Text;

namespace NetCoreTorrent
{
    public enum Enum_MessageType : byte
    {
        //keep-alive: <len=0000>
        KeepAlive = 10,
        //choke: <len=0001><id=0>
        Choke = 0,
        //unchoke: <len=0001><id=1>
        Unchoke = 1,
        //interested: <len=0001><id=2>
        Interested = 2,
        //not interested: <len=0001><id=3>
        NotInterested = 3,
        //have: <len=0005><id=4><piece index>
        Have = 4,
        //bitfield: <len=0001+X><id=5><bitfield>
        Bitfield = 5,
        //request: <len=0013><id=6><index><begin><length>
        Request = 6,
        //piece: <len=0009+X><id=7><index><begin><block>
        Piece = 7,
        //cancel: <len=0013><id=8><index><begin><length>
        Cancel = 8,
        //port: <len=0003><id=9><listen-port>
        Port = 9
    }
}

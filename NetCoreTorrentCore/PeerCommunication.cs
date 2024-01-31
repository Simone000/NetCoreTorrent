using BEncoding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetCoreTorrent
{
    public class PeerCommunication : IDisposable
    {
        //todo: add timeout e cancellationtoken

        private Peer _Peer;
        private TcpClient _TcpClient;
        private bool _IsChoked = true;
        private bool _IsInterested = false;

        public PeerCommunication(Peer Peer)
        {
            _Peer = Peer;
            _TcpClient = new TcpClient();
        }

        public async Task ConnectAsync()
        {
            await _TcpClient.ConnectAsync(_Peer.EndPoint.Address.ToString(), _Peer.EndPoint.Port)
                            .ConfigureAwait(false);
        }

        //https://wiki.theory.org/BitTorrentSpecification#Handshake (<pstrlen><pstr><reserved><info_hash><peer_id>)
        public async Task SendAndReceiveHandshakeAsync()
        {
            int pstrlen = 19; //pstr.Length;
            string pstr = "BitTorrent protocol";
            byte[] reserved = new byte[8]; //all 0 if I don't need to support extension protocols
            byte[] infohash = _Peer.InfoHash.Hash;
            string peerId = "-AZ5720-0EXIIjF1kKd6";

            //send handshake to the peer
            byte[] handshake = (new byte[] { (byte)pstrlen })
                               .Concat(Encoding.ASCII.GetBytes(pstr))
                               .Concat(reserved)
                               .Concat(infohash)
                               .Concat(Encoding.ASCII.GetBytes(peerId))
                               .ToArray();

            await _TcpClient.GetStream()
                            .WriteAsync(handshake, 0, handshake.Length)
                            .ConfigureAwait(false);

            //receive the handshake from the peer
            byte[] resp_handshake = new byte[handshake.Length];
            int resp_pstrlen;
            byte[] resp_pstr;
            byte[] resp_reserved;
            byte[] resp_infoHash;
            byte[] resp_peerId;
            if (await _TcpClient.GetStream().ReadAsync(resp_handshake, 0, resp_handshake.Length).ConfigureAwait(false) > 0)
            {
                resp_pstrlen = (int)resp_handshake[0];
                resp_pstr = resp_handshake.SubArray(1, resp_pstrlen);
                resp_reserved = resp_handshake.SubArray(1 + resp_pstrlen, 8);
                resp_infoHash = resp_handshake.SubArray(1 + resp_pstrlen + 8, 20);
                resp_peerId = resp_handshake.SubArray(1 + resp_pstrlen + 8 + 20, 20);
            }
        }

        //<length prefix><message ID><payload>
        public async Task<PeerMessage> ReceiveMessageAsync()
        {
            int read;
            int msgLength = 0;

            byte[] bufferLength = new byte[4];
            if ((read = await _TcpClient.GetStream().ReadAsync(bufferLength, 0, bufferLength.Length).ConfigureAwait(false)) > 0)
            {
                msgLength = BitConverter.IsLittleEndian ?
                                BitConverter.ToInt32(bufferLength.Reverse().ToArray(), 0)
                                : BitConverter.ToInt32(bufferLength, 0);

                if(msgLength <= 0)
                {
                    //keep-alive: <len=0000>
                    return new PeerMessage() { MessageType = Enum_MessageType.KeepAlive };
                }
            }

            if (read == 0)
            {
                Debugger.Break();
                return null;
            }
            
            try
            {
                byte[] buffer = new byte[msgLength];
                if ((read = await _TcpClient.GetStream().ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    byte msgTypeByte = buffer[0];
                    Enum_MessageType msgType = (Enum_MessageType)msgTypeByte;
                    byte[] msgContent = buffer.SubArray(1, msgLength - 1);

                    return new PeerMessage() { MessageType = msgType, Content = msgContent };
                }
                Debugger.Break();
            }
            catch (Exception Exc)
            {
                Debugger.Break();
                throw Exc;
            }

            Debugger.Break();
            return null;
        }
        
        public async Task SendMessageAsync(Enum_MessageType MessageType, byte[] Content = null)
        {
            int msgLength = 0;
            if(MessageType != Enum_MessageType.KeepAlive)
            {
                msgLength++; //add a bit for the id
            }
            if(Content != null)
            {
                msgLength += Content.Length;
            }
            byte[] msgLength_b = BitConverter.IsLittleEndian?
                                BitConverter.GetBytes(msgLength).Reverse().ToArray()
                                : BitConverter.GetBytes(msgLength);

            switch (MessageType)
            {
                case Enum_MessageType.KeepAlive:
                    //keep-alive: <len=0000>
                    byte[] keepalive = new byte[] { 0, 0, 0, 0 };
                    await _TcpClient.GetStream()
                                    .WriteAsync(keepalive, 0, keepalive.Length)
                                    .ConfigureAwait(false);
                    break;
                case Enum_MessageType.Choke:
                    //choke: <len=0001><id=0>
                    byte[] choke = msgLength_b.Append((byte)0).ToArray();
                    await _TcpClient.GetStream()
                                    .WriteAsync(choke, 0, choke.Length)
                                    .ConfigureAwait(false);
                    break;
                case Enum_MessageType.Unchoke:
                    //unchoke: <len=0001><id=1>
                    byte[] unchoke = msgLength_b.Append((byte)1).ToArray();
                    await _TcpClient.GetStream()
                                    .WriteAsync(unchoke, 0, unchoke.Length)
                                    .ConfigureAwait(false);
                    break;
                case Enum_MessageType.Interested:
                    //interested: <len=0001><id=2>
                    byte[] interested = msgLength_b.Append((byte)2).ToArray();
                    await _TcpClient.GetStream()
                                    .WriteAsync(interested, 0, interested.Length)
                                    .ConfigureAwait(false);
                    break;
                case Enum_MessageType.NotInterested:
                    //not interested: <len=0001><id=3>
                    byte[] notinterested = msgLength_b.Append((byte)3).ToArray();
                    await _TcpClient.GetStream()
                                    .WriteAsync(notinterested, 0, notinterested.Length)
                                    .ConfigureAwait(false);
                    break;
                case Enum_MessageType.Have:
                    //have: <len=0005><id=4><piece index>
                    byte[] have = msgLength_b.Append((byte)4).Concat(Content).ToArray();
                    await _TcpClient.GetStream()
                                    .WriteAsync(have, 0, have.Length)
                                    .ConfigureAwait(false);
                    break;
                case Enum_MessageType.Bitfield:
                    //bitfield: <len=0001+X><id=5><bitfield>
                    byte[] bitfield = msgLength_b.Append((byte)5).Concat(Content).ToArray();
                    await _TcpClient.GetStream()
                                    .WriteAsync(bitfield, 0, bitfield.Length)
                                    .ConfigureAwait(false);
                    break;
                case Enum_MessageType.Request:
                    //request: <len=0013><id=6><index><begin><length>
                    byte[] request = msgLength_b.Append((byte)6).Concat(Content).ToArray();
                    await _TcpClient.GetStream()
                                    .WriteAsync(request, 0, request.Length)
                                    .ConfigureAwait(false);
                    break;
                case Enum_MessageType.Piece:
                    //piece: <len=0009+X><id=7><index><begin><block>
                    byte[] piece = msgLength_b.Append((byte)7).Concat(Content).ToArray();
                    await _TcpClient.GetStream()
                                    .WriteAsync(piece, 0, piece.Length)
                                    .ConfigureAwait(false);
                    break;
                case Enum_MessageType.Cancel:
                    //cancel: <len=0013><id=8><index><begin><length>
                    byte[] cancel = msgLength_b.Append((byte)8).Concat(Content).ToArray();
                    await _TcpClient.GetStream()
                                    .WriteAsync(cancel, 0, cancel.Length)
                                    .ConfigureAwait(false);
                    break;
                case Enum_MessageType.Port:
                    //port: <len=0003><id=9><listen-port>
                    byte[] port = msgLength_b.Append((byte)9).Concat(Content).ToArray();
                    await _TcpClient.GetStream()
                                    .WriteAsync(port, 0, port.Length)
                                    .ConfigureAwait(false);
                    break;
            }

            //todo: while(true) per content troppo lunghi?
        }


        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _TcpClient.Dispose();
                }
                
                disposedValue = true;
            }
        }
        void IDisposable.Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}

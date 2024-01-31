using BEncoding;
using NetCoreTorrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace NetCoreTorrentCoreTests
{
    [TestClass]
    public class TestMain
    {
        public string TorrentPath1
        {
            get
            {
                return Path.Combine(AppContext.BaseDirectory, "Samples", "Torrents", "ubuntu.torrent");
            }
        }
        public string TorrentPath2
        {
            get
            {
                return Path.Combine(AppContext.BaseDirectory, "Samples", "Torrents", "ubuntu2.torrent");
            }
        }

        [TestMethod]
        public void TestLoadTorrent()
        {
            Torrent torrent1 = Torrent.CreateFromTorrentFilePath(TorrentPath1);
            Assert.IsNotNull(torrent1);

            //test info hash
            Assert.AreEqual(@"%9F%91e%D9%A2%81%A9%B8%E7%82%CDQv%BB%CC%82V%FD%18q", torrent1.InfoHash.EncodedHash);

            //test total file length
            Assert.AreEqual(1513308160, torrent1.InfoLength);

            //test pieces length
            Assert.AreEqual(524288, torrent1.InfoPieceLength);

            //test pieces length (hash list)
            Assert.AreEqual(57740, torrent1.InfoPieces.Length);

            //pieces length should be composed of 20 bytes hash each
            Assert.IsTrue(torrent1.InfoPieces.Length % 20 == 0);

            //test lengths makes sense
            int numberOfPieces1 = (int)Math.Ceiling((float)torrent1.InfoLength / torrent1.InfoPieceLength);
            int numberOfPieces2 = torrent1.InfoPieces.Length / 20;
            Assert.AreEqual(numberOfPieces1, numberOfPieces2);
            Assert.AreEqual(numberOfPieces1, torrent1.NumberOfPieces);
            Assert.AreEqual(2887, torrent1.NumberOfPieces);


            //Bitfields requires x number of bytes. x = number of pieces/8.
            //As each bit of byte represents one piece.
            int numBytes = Convert.ToInt32(Math.Ceiling(torrent1.NumberOfPieces / 8.0)); //361

            //Convert.ToInt32(Math.Ceiling(numPieces / 8.0))

            //EndianBitConverter.Big.ToInt32(bytes,0) != expectedLength

            //test file name
            Assert.AreEqual("ubuntu-16.04.1-desktop-amd64.iso", torrent1.InfoName);
        }

        [TestMethod]
        public void TestAnnounceUrl()
        {
            Torrent torrent1 = Torrent.CreateFromTorrentFilePath(TorrentPath1);
            Assert.IsNotNull(torrent1);
            Assert.AreEqual(@"http://torrent.ubuntu.com:6969/announce", torrent1.AnnounceUrl);
        }

        [TestMethod]
        public async Task TestTracker_FailureMsg()
        {
            Torrent torrent = Torrent.CreateFromTorrentFilePath(TorrentPath1);
            Assert.IsNotNull(torrent);

            //Announce
            AnnounceRisModel announceRis = await Tracker.AnnounceAsync(torrent);
            Assert.IsNotNull(announceRis);

            Assert.IsFalse(announceRis.IsSuccessful);
            Assert.AreEqual(@"Requested download is not authorized for use with this tracker.", announceRis.FailureReason);
        }

        [TestMethod]
        public async Task TestTracker_Peers()
        {
            Torrent torrent = Torrent.CreateFromTorrentFilePath(TorrentPath2);
            Assert.IsNotNull(torrent);

            //Announce
            AnnounceRisModel announceRis = await Tracker.AnnounceAsync(torrent);
            Assert.IsNotNull(announceRis);

            Assert.IsTrue(announceRis.IsSuccessful);
            Assert.IsNotNull(announceRis.Peers);
            Assert.IsTrue(announceRis.Peers.Count > 0);
        }

        [TestMethod]
        [Ignore]
        public async Task TestHandshakePeer_OLD()
        {
            Torrent torrent = Torrent.CreateFromTorrentFilePath(TorrentPath2);
            AnnounceRisModel announceRis = await Tracker.AnnounceAsync(torrent);

            Peer choosedPeer = announceRis.Peers.First();

            using (var client = new TcpClient())
            {
                await client.ConnectAsync(choosedPeer.EndPoint.Address.ToString(), choosedPeer.EndPoint.Port);

                //https://wiki.theory.org/BitTorrentSpecification#Handshake (<pstrlen><pstr><reserved><info_hash><peer_id>)
                int pstrlen = 19; //pstr.Length;
                string pstr = "BitTorrent protocol";
                byte[] reserved = new byte[8]; //all 0 if I don't need to support extension protocols
                byte[] infohash = torrent.InfoHash.Hash;
                string peerId = "-AZ5720-0EXIIjF1kKd6";

                //send handshake to the peer
                byte[] handshake = (new byte[] { (byte)pstrlen })
                                   .Concat(Encoding.ASCII.GetBytes(pstr))
                                   .Concat(reserved)
                                   .Concat(infohash)
                                   .Concat(Encoding.ASCII.GetBytes(peerId))
                                   .ToArray();

                await client.GetStream().WriteAsync(handshake, 0, handshake.Length);


                //receive the handshake from the peer
                byte[] resp_handshake = new byte[handshake.Length];
                int resp_pstrlen;
                byte[] resp_pstr;
                byte[] resp_reserved;
                byte[] resp_infoHash;
                byte[] resp_peerId;
                if (await client.GetStream().ReadAsync(resp_handshake, 0, resp_handshake.Length) > 0)
                {
                    resp_pstrlen = (int)resp_handshake[0];
                    resp_pstr = resp_handshake.SubArray(1, resp_pstrlen);
                    resp_reserved = resp_handshake.SubArray(1 + resp_pstrlen, 8);
                    resp_infoHash = resp_handshake.SubArray(1 + resp_pstrlen + 8, 20);
                    resp_peerId = resp_handshake.SubArray(1 + resp_pstrlen + 8 + 20, 20);
                }

                //<length prefix><message ID><payload>
                byte[] buffer = new byte[16 * 1024];
                int read;
                if ((read = await client.GetStream().ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] msgLengthB = buffer.SubArray(0, 4);
                    int msgLength = BitConverter.IsLittleEndian ?
                                    BitConverter.ToInt32(msgLengthB.Reverse().ToArray(), 0)
                                    : BitConverter.ToInt32(msgLengthB, 0);
                    byte msgType = buffer[4];
                    byte[] msgContent = buffer.SubArray(5, msgLength);
                }
            }
        }

        [TestMethod]
        public async Task TestHandshakePeer()
        {
            Torrent torrent = Torrent.CreateFromTorrentFilePath(TorrentPath2);
            AnnounceRisModel announceRis = await Tracker.AnnounceAsync(torrent);

            Peer choosedPeer = announceRis.Peers.First();

            using (var handler = new PeerCommunication(choosedPeer))
            {
                //1 connect
                await handler.ConnectAsync();

                //2 send handshake and receive the requested handshake
                await handler.SendAndReceiveHandshakeAsync();
            }
        }

        [TestMethod]
        public async Task TestReceiveMessage()
        {
            Torrent torrent = Torrent.CreateFromTorrentFilePath(TorrentPath2);
            AnnounceRisModel announceRis = await Tracker.AnnounceAsync(torrent);

            Peer choosedPeer = announceRis.Peers.First();

            using (var handler = new PeerCommunication(choosedPeer))
            {
                //1 connect
                await handler.ConnectAsync();

                //2 send handshake and receive the requested handshake
                await handler.SendAndReceiveHandshakeAsync();

                //3 start receiving messages...
                int numberOfMessages = 2;
                for (int i = 0; i < numberOfMessages; i++)
                {
                    var msg = await handler.ReceiveMessageAsync();
                }
            }
        }

        [TestMethod]
        public async Task TestReceiveMessage_WithMissingBitfield()
        {
            Torrent torrent = Torrent.CreateFromTorrentFilePath(TorrentPath2);
            AnnounceRisModel announceRis = await Tracker.AnnounceAsync(torrent);

            int tentativi = 0;
            foreach (var choosedPeer in announceRis.Peers)
            {
                tentativi++;
                using (var handler = new PeerCommunication(choosedPeer))
                {
                    //1 connect
                    await handler.ConnectAsync();

                    //2 send handshake and receive the requested handshake
                    await handler.SendAndReceiveHandshakeAsync();

                    //3 start receiving messages...
                    var msg = await handler.ReceiveMessageAsync();
                    if (msg.MessageType == Enum_MessageType.Bitfield)
                    {
                        if (msg.Content.Where(p => p == 0).Any()
                            && msg.Content.Where(p => p != 0).Any())
                        {
                            Debugger.Break();
                        }
                    }
                }
            }

        }

        [TestMethod]
        public async Task TestSendMessage()
        {
            Torrent torrent = Torrent.CreateFromTorrentFilePath(TorrentPath2);
            AnnounceRisModel announceRis = await Tracker.AnnounceAsync(torrent);

            Peer choosedPeer = announceRis.Peers.First();

            using (var handler = new PeerCommunication(choosedPeer))
            {
                //1 connect
                await handler.ConnectAsync();

                //2 send handshake and receive the requested handshake
                await handler.SendAndReceiveHandshakeAsync();

                //3 start send message
                await handler.SendMessageAsync(Enum_MessageType.Unchoke);
            }
        }

        [TestMethod]
        public async Task TestSendMessage_BitField()
        {
            Torrent torrent = Torrent.CreateFromTorrentFilePath(TorrentPath2);
            AnnounceRisModel announceRis = await Tracker.AnnounceAsync(torrent);

            Peer choosedPeer = announceRis.Peers.First();

            using (var handler = new PeerCommunication(choosedPeer))
            {
                //1 connect
                await handler.ConnectAsync();

                //2 send handshake and receive the requested handshake
                await handler.SendAndReceiveHandshakeAsync();

                //3 start send message
                byte[] bitfieldEmpty = new byte[361];
                await handler.SendMessageAsync(Enum_MessageType.Bitfield, bitfieldEmpty);

                int numberOfMessages = 3;
                for (int i = 0; i < numberOfMessages; i++)
                {
                    var msg = await handler.ReceiveMessageAsync();
                }
            }
        }

        [TestMethod]
        public async Task TestPeerCommunication()
        {
            Torrent torrent = Torrent.CreateFromTorrentFilePath(TorrentPath2);
            AnnounceRisModel announceRis = await Tracker.AnnounceAsync(torrent);

            Peer choosedPeer = announceRis.Peers.First();

            using (var handler = new PeerCommunication(choosedPeer))
            {
                //1 connect
                await handler.ConnectAsync();

                //2 send handshake and receive the requested handshake
                await handler.SendAndReceiveHandshakeAsync();

                //3 start receiving messages...
                var receiving = Task.Run(async () =>
                {
                    var msg = await handler.ReceiveMessageAsync();
                });

                /*var sending = Task.Run(async () =>
                {

                });*/

                await Task.Delay(TimeSpan.FromMinutes(2));
            }
        }

        [TestMethod]
        public async Task TestReceiveFirstBlock()
        {
            Torrent torrent = Torrent.CreateFromTorrentFilePath(TorrentPath2);
            AnnounceRisModel announceRis = await Tracker.AnnounceAsync(torrent);

            foreach (var choosedPeer in announceRis.Peers)
            {
                using (var handler = new PeerCommunication(choosedPeer))
                {
                    //1 connect
                    await handler.ConnectAsync();

                    //2 send handshake and receive the requested handshake
                    await handler.SendAndReceiveHandshakeAsync();

                    //3 start send message
                    byte[] bitfieldEmpty = new byte[361];
                    await handler.SendMessageAsync(Enum_MessageType.Bitfield, bitfieldEmpty);

                    var msg = await handler.ReceiveMessageAsync();
                    if (msg.MessageType == Enum_MessageType.Bitfield)
                    {
                        if (msg.Content.Where(p => p != 0).Any())
                        {
                            //await handler.SendMessageAsync(Enum_MessageType.Unchoke);
                            await handler.SendMessageAsync(Enum_MessageType.Interested);

                            for (int i = 0; i < 5; i++)
                            {
                                var msg2 = await handler.ReceiveMessageAsync();
                            }
                        }
                    }
                }
            }
        }
    }
}
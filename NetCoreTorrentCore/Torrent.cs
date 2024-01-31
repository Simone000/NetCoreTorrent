using BEncoding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetCoreTorrent
{
    public class Torrent
    {
        public string TorrentFilePath { get; private set; }
        public BDictionary TorrentDictionary { get; private set; }

        public InfoHash InfoHash { get; private set; }
        public int InfoLength { get; private set; }
        public int InfoPieceLength { get; set; }
        public byte[] InfoPieces { get; set; } //Info hashes (20 bytes for each piece hash)

        public int NumberOfPieces {
            get
            {
                //uguale a (int)Math.Ceiling((float)torrent1.InfoLength / torrent1.InfoPieceLength);
                return InfoPieces.Length / 20;
            }
        }

        public string InfoName { get; set; } //todo: what about multiple files in the same torrent?

        public string AnnounceUrl { get; private set; }
        public List<string> AnnounceListUrl { get; private set; }

        private void LoadTorrent()
        {
            //Get the info part of the torrent bdictionary
            var info = TorrentDictionary.Values
                                        .Where(p => p.Key as BString != null)
                                        .Where(p => ((BString)p.Key).Value == "info")
                                        .First().Value
                                        as BDictionary;

            //Calculate infohash
            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                byte[] infohash = sha1.ComputeHash(info.SourceBytes);
                InfoHash = new InfoHash(infohash);
            }

            //torrent file length
            InfoLength = (info.Values
                             .Where(p => p.Key as BString != null)
                             .Where(p => ((BString)p.Key).Value == "length")
                             .First().Value as BInteger)
                             .Value;

            //piece length
            InfoPieceLength = (info.Values
                             .Where(p => p.Key as BString != null)
                             .Where(p => ((BString)p.Key).Value == "piece length")
                             .First().Value as BInteger)
                             .Value;

            //pieces (hash list)
            InfoPieces = info.Values
                             .Where(p => p.Key as BString != null)
                             .Where(p => ((BString)p.Key).Value == "pieces")
                             .First().Value.SourceBytes;


            //file name (not implemented for multiple files in the same torrent)
            InfoName = (info.Values
                             .Where(p => p.Key as BString != null)
                             .Where(p => ((BString)p.Key).Value == "name")
                             .First().Value as BString)
                             .Value;


            //Load trackers (announce and announce list)
            var announce = TorrentDictionary.Values
                                            .Where(p => p.Key as BString != null)
                                            .Where(p => ((BString)p.Key).Value == "announce")
                                            .FirstOrDefault();
            if (announce.Key != null && announce.Value != null)
            {
                AnnounceUrl = ((BString)announce.Value).Value;
            }
            var announceList = TorrentDictionary.Values
                                            .Where(p => p.Key as BString != null)
                                            .Where(p => ((BString)p.Key).Value == "announce-list")
                                            .FirstOrDefault();
            if (announceList.Key != null && announceList.Value != null)
            {
                var announceBList = announceList.Value as BList;
                if (announceBList != null)
                {
                    AnnounceListUrl = announceBList.Values
                                                   .Select(p => p as BList).SelectMany(p => p.Values)
                                                   .Select(p => p as BString).Select(p => p.Value)
                                                   .ToList();
                }
            }
        }

        public bool LoadFromTorrentFile()
        {
            //check file exists
            if (!File.Exists(TorrentFilePath))
                return false;

            //read torrent bytes
            byte[] torrentBytes = File.ReadAllBytes(TorrentFilePath);

            //decode into a dictionary
            TorrentDictionary = BEncoding2.Decode(torrentBytes) as BDictionary;
            if (TorrentDictionary == null)
                return false;

            LoadTorrent();

            return true;
        }

        public static Torrent CreateFromTorrentFilePath(string TorrentFilePath)
        {
            var torrent = new Torrent() { TorrentFilePath = TorrentFilePath };
            if (!torrent.LoadFromTorrentFile())
                return null;
            
            return torrent;
        }


        
        //enum_statoTorrent
        //MaxNumberOfConnections: per limitare i peers per torrent
        //Bitfield: per sapere cosa ho e cosa mi manca

        public async Task RunAsync()
        {
            //Implemento la strategia di download
            //in base al bitfield contatto n peer e invio i msg necessari
            //Elaboro qui le risposte e gli invii per ogni singolo peer
        }
    }
}

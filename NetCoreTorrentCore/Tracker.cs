using BEncoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NetCoreTorrent
{
    public class Tracker
    {
        //default a true, provo una volta a usarlo, se va in errore lo metto a false
        //public bool IsScrapeAvailable { get; private set; }

        public static async Task<AnnounceRisModel> AnnounceAsync(Torrent Torrent)
        {
            string uriWithParameters = Torrent.AnnounceUrl
                + "?info_hash=" + Torrent.InfoHash.EncodedHash
                + "&peer_id=" + "-AZ5720-0EXIIjF1kKd6"
                + "&port=" + "6881"
                + "&uploaded=" + "100"
                + "&downloaded=" + "100"
                + "&left=" + "100"
                + "&compact=" + "1"
                + "&numwant=" + "100"
                + "&key=" + "7FNoDITw";

            //query["ip"] = "79.16.154.200";
            //query["event"] = "started"; //stopped
            //builder.Add("supportcrypto", 1);
            //builder.Add("requirecrypto", 1);
            //builder.Add("trackerid", _trackerId);

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Accept", "text/plain");
                var response = await httpClient.GetAsync(uriWithParameters).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                byte[] risBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                BDictionary announceRis = (BDictionary)BEncoding2.Decode(risBytes);


                //parse result
                var announceRisModel = new AnnounceRisModel();
                if (announceRis == null)
                {
                    announceRisModel.IsSuccessful = false;
                    announceRisModel.FailureReason = "Empty response";
                    return announceRisModel;
                }

                //check failure
                var failureReason = announceRis.Values
                                               .Where(p => p.Key as BString != null)
                                               .Where(p => ((BString)p.Key).Value == "failure reason")
                                               .FirstOrDefault();
                if(failureReason.Key != null && failureReason.Value != null)
                {
                    announceRisModel.IsSuccessful = false;
                    announceRisModel.FailureReason = (failureReason.Value as BString).Value;
                    return announceRisModel;
                }


                //parse peers
                var peersDict = announceRis.Values
                                       .Where(p => p.Key as BString != null)
                                       .Where(p => ((BString)p.Key).Value == "peers")
                                       .FirstOrDefault();
                if(peersDict.Key != null && peersDict.Value != null)
                {
                    //peers can be of type bdictionary or bytes array
                    var peers = new List<Peer>();
                    BString peersBString = peersDict.Value as BString;
                    if (peersBString != null) //bytes array
                    {
                        for (int i = 0; i < peersBString.SourceBytes.Length; i = i + 6)
                        {
                            byte[] peerIpBytes = peersBString.SourceBytes.SubArray(i, 4);
                            byte[] peerPortBytes = peersBString.SourceBytes.SubArray(i + 4, 2);
                            var peerIp = new IPAddress(peerIpBytes);
                            UInt16 peerPort = (UInt16)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(peerPortBytes, 0));

                            var peer = new Peer()
                            {
                                EndPoint = new IPEndPoint(peerIp, peerPort),
                                InfoHash = new InfoHash(Torrent.InfoHash.Hash)
                            };
                            peers.Add(peer);
                        }
                    }
                    announceRisModel.IsSuccessful = true;
                    announceRisModel.Peers = peers;
                }

                return announceRisModel;
            }
        }
    }

    public class AnnounceRisModel
    {
        public bool IsSuccessful { get; set; }
        public string FailureReason { get; set; }
        public List<Peer> Peers { get; set; }
    }
}

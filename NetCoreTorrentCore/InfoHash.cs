using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NetCoreTorrent
{
    /// <summary>
    /// http://stackoverflow.com/questions/28348678/what-exactly-is-the-info-hash-in-a-torrent-file
    /// </summary>
    public struct InfoHash
    {
        public readonly byte[] Hash;
        public InfoHash(byte[] Hash)
        {
            this.Hash = Hash;
        }
        public string EncodedHash
        {
            get
            {
                byte[] encodedInfoHash = System.Net.WebUtility.UrlEncodeToBytes(this.Hash, 0, this.Hash.Length);
                string encodedInfoHash_s = System.Text.Encoding.ASCII.GetString(encodedInfoHash);
                return encodedInfoHash_s;
            }
        }

        public override string ToString()
        {
            return BitConverter.ToString(Hash).Replace("-", string.Empty);
        }
    }
}

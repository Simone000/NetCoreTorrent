using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetCoreTorrent
{
    public class TorrentsManager
    {
        private List<Torrent> _Torrents;
        public TorrentsManager()
        {
            _Torrents = new List<Torrent>();
        }

        /// <summary>
        /// E' il main dell'applicativo (potrebbe essere in un progetto separato per potere usare il resto come libreria)
        /// </summary>
        /// <returns></returns>
        public async Task RunAsync()
        {
            //ITracker per chiedere piu' peer
            //ITracker = new ...
            
            //Carico i torrent (da file o tramite metodi aggiuntivi AddTorrent, gestione torrent attivi ecc...)

            //Awaito tutti i torrent
            //await torrent.RunAsync(ITracker) //diventerà await tutti con whenAny per piu' torrent (come aggiungo task a lista di task in await???)
        }
    }
}

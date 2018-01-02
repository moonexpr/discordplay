using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace discordplay
{
    class GooglePlay
    {
        public bool playing = false;
        public IDictionary<string, string> song;
        public IDictionary<string, bool> rating;
        public IDictionary<string, int> time;
        public string songLyrics = "";
        public string shuffle = "";
        public string repeat = "";
    }
}

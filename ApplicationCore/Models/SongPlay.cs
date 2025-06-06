using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Models
{
    public class SongPlay
    {
        public string Id { get; set; }
        public DateTime PlayTime { get; set; }
        public string Platform { get; set; } 
        public int MsPlayed { get; set; }
        public string TrackName { get; set; }
        public string ArtistName { get; set; }
        public string AlbumName { get; set; }
        public string ReasonStart { get; set; }
        public string ReasonEnd { get; set; }
        public bool Shuffle { get; set; }
        public bool Skip { get; set; }


    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ApplicationCore.Models
{
    public class SongPlay
    {
        public int Id { get; set; }
        public string URI { get; set; }
        public DateTime PlayTime { get; set; }
        public string Platform { get; set; } 
        public int MsPlayed { get; set; }
        public int SongId { get; set; }
        [JsonIgnore]
        public Song Song { get; set; }
        public string ReasonStart { get; set; }
        public string ReasonEnd { get; set; }
        public bool Shuffle { get; set; }
        public bool Skip { get; set; }
    }

}

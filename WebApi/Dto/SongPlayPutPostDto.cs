using ApplicationCore.Models;

namespace WebApi.Dto
{
    public class SongPlayPutPostDto
    {
        public int Id { get; set; }
        public string URI { get; set; }
        public DateTime PlayTime { get; set; }
        public string Platform { get; set; }
        public int MsPlayed { get; set; }
        public int SongId { get; set; }
        public string ReasonStart { get; set; }
        public string ReasonEnd { get; set; }
        public bool Shuffle { get; set; }
        public bool Skip { get; set; }
    }
}

namespace WebApi.Dto
{
    public class SongPlayPostDto
    {
        public string URI { get; set; }
        public DateTime PlayTime { get; set; }
        public string Platform { get; set; }
        public int MsPlayed { get; set; }
        public string ReasonStart { get; set; }
        public string ReasonEnd { get; set; }
        public bool Shuffle { get; set; }
        public bool Skip { get; set; }
    }
}

namespace WebApi.Dto
{
    public class SongRankElementDto
    {
        public string TrackName { get; set; }
        public string ArtistName { get; set; }
        public string AlbumName { get; set; }
        public int PlayCount { get; set; }  // zamiast TotalMsPlayed
    }

}

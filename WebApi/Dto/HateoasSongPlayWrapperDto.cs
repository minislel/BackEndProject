using ApplicationCore.Models;

namespace WebApi.Dto
{
    public class HateoasSongPlayWrapperDto
    {
        public SongPlay SongPlay { get; set; }
        public List<HateoasLinkDto> Links { get; set; } = new();
    }
}

using ApplicationCore.Models;

namespace WebApi.Dto
{
    public class HateoasSongWrapperDto
    {

            public Song Song { get; set; }
            public List<HateoasLinkDto> Links { get; set; } = new();
        
    }
}

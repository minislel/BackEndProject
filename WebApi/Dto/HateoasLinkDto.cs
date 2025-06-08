namespace WebApi.Dto
{
    public class HateoasLinkDto
    {
        public string Href { get; set; }
        public string Rel { get; set; }
        public string Method { get; set; }

        public HateoasLinkDto(string href, string rel, string method)
        {
            Href = href;
            Rel = rel;
            Method = method;
        }
    }

}

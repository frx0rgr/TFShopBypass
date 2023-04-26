namespace TfShop.Models
{
    public class ShopJson
    {

        public List<string> directories { get; set; }
        public string? success { get; set; }
        public string referrer { get; set; }
        public Dictionary<string,TitleDb> titledb { get; set; }
        public List<string> headers { get; set; }
        public object locations { get; set; }
    }
}
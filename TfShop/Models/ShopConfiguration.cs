namespace TfShop.Models
{
    public class ShopConfiguration
    {

        public string UID { get; set; }
        public string Theme { get; set; }
        public string Version { get; set; }
        public string Protocol { get; set; }
        public string Host { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string HAuth { get; set; }
        public string UAUTH { get; set; }
        public string Basic { get; set; }

        public bool HideSuccess { get; set; }


    }
}
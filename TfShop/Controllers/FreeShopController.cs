using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using TfShop.Models;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace TfShop.Controllers
{
    [ApiController]
    [Route("")]
    public class FreeShopController : ControllerBase
    {

        private static readonly Dictionary<string,byte[]> TinfoilFiles = new Dictionary<string, byte[]>();

        private readonly ILogger<FreeShopController> _logger;
        // requires using Microsoft.Extensions.Configuration;
        private readonly IConfiguration _configuration;

        private ShopConfiguration shopConfiguration;

        public FreeShopController(ILogger<FreeShopController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration=configuration;

        }

        private ShopConfiguration GetConfiguration()
        {
            if (shopConfiguration == null)
            {
                this.shopConfiguration = _configuration.GetSection("Shop").Get<ShopConfiguration>();
            }
            if (this.shopConfiguration == null)
            {
                throw new Exception("No Shop Configuration");

            }else if (string.IsNullOrEmpty(this.shopConfiguration.UID)){
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

                var random = new Random();
                var length = 10;
                var randomString = new string(Enumerable.Repeat(chars, length)
                                                        .Select(s => s[random.Next(s.Length)]).ToArray());
                this.shopConfiguration.UID= ComputeSha256Hash(randomString);
                var path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                var json =  System.IO.File.ReadAllText(path);
                var jsonObj = JsonSerializer.Deserialize<JsonObject>(json)!;
                jsonObj["Shop"]["UID"] = this.shopConfiguration.UID;
                string output = JsonSerializer.Serialize<dynamic>(jsonObj, new JsonSerializerOptions { WriteIndented = true }); ;
                System.IO.File.WriteAllText(path, output);
            }
            return this.shopConfiguration;
            
        }
        static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString().ToUpper();
            }
        }

        [HttpGet("File/{id}")]
        public FileResult GetFileResult(string id)
        {

            if(!TinfoilFiles.ContainsKey(id))
            {
                throw new Exception("File Key does not exist " + id);

            }
            var bytes = TinfoilFiles[id];
            return File(bytes, System.Net.Mime.MediaTypeNames.Application.Octet, id);
        }

        private string GetUrl()
        {
            var shopConfiguration = GetConfiguration();
            return shopConfiguration.Protocol+"://"+shopConfiguration.UserName+":"+shopConfiguration.Password+"@"+shopConfiguration.Host;
        }
        private HttpClient GetHttpClient()
        {
            var shopConfiguration = GetConfiguration();
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.Add("UID", shopConfiguration.UID);
            client.DefaultRequestHeaders.Add("HAuth", shopConfiguration.HAuth);
            client.DefaultRequestHeaders.Add("UAUTH", shopConfiguration.UAUTH);
            client.DefaultRequestHeaders.Add("Theme", shopConfiguration.Theme);
            client.DefaultRequestHeaders.Add("Version", shopConfiguration.Version);
            client.DefaultRequestHeaders.Add($"Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(($"{shopConfiguration.UserName}:{shopConfiguration.Password}")))}");

            return client;
        }
        [HttpGet("FreeShop/GetFilesWithoutChange")]
        public async Task<string> GetFilesWithoutChange()
        {

            HttpClient client = GetHttpClient();
            string url = this.GetUrl();

            string json = await client.GetStringAsync(url);

            return json;
        }


        [HttpGet()]
        public async Task<string> GetFiles()
        {

            HttpClient client = GetHttpClient();
            string url = this.GetUrl();

            string json = await client.GetStringAsync(url);

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull

            };
            ShopJson? shopJson =
                JsonSerializer.Deserialize<ShopJson>(json, options);
            if (shopJson != null)
            {
                shopJson.headers = null;

                var newDirectories = new List<string>();
                shopJson.referrer = null;
                int i = 0;
                if (shopJson.directories !=null && shopJson.directories.Count > 0)
                {
                    shopJson.success = "Welcome to Anonymous Shop";
                    foreach (string directory in shopJson.directories)

                    {
                        if (directory != null)
                        {
                            var location =
                                new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}");

                            var url2 = location.AbsoluteUri;
                            if (url2.EndsWith("/")) url2 = url2[..^1];

                            string fileUrl = directory.Replace(GetConfiguration().Protocol + "://",
                                GetConfiguration().Protocol + "://" + GetConfiguration().UserName + ":" +
                                GetConfiguration().Password + "@");
                            var file = await client.GetByteArrayAsync(fileUrl);
                            var key = i++ + ".tfl";
                            fileUrl = url2 + "/File/" + key;

                            if (TinfoilFiles.ContainsKey(key))
                            {
                                TinfoilFiles.Remove(key);
                            }
                            TinfoilFiles.Add(key, file);
                            newDirectories.Add(fileUrl);
                        }
                    }
                    shopJson.success = GetConfiguration().HideSuccess ? null : shopJson.success;
                }
                shopJson.directories = newDirectories;
                return JsonSerializer.Serialize(shopJson, options);
            }

            return "No Shop reachable";
        }

    }
}
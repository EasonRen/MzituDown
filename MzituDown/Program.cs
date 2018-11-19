using HtmlAgilityPack;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using Polly;
using Polly.Retry;

namespace mzitudown
{
    [Command(Description = "Mzitu Download, Version 0.1.7")]
    class Program
    {
        public const string BASE_URL = "https://www.mzitu.com/";
        private static readonly HttpClient _httpClient;
        private static readonly HtmlDocument _htmlDocument;
        private static readonly RetryPolicy _retryThreeTimesPolicy;

        static Program()
        {
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(BASE_URL)
            };
            _htmlDocument = new HtmlDocument();

            _retryThreeTimesPolicy = Policy
                       .Handle<Exception>()
                       .Retry(3, (ex, count) =>
                       {
                           Console.WriteLine("Request Error Retry {0}", count);
                       });
        }

        public static int Main(string[] args)
        {

#if DEBUG
            args = new string[] { "147294", "-p", "da" };
#endif
            return CommandLineApplication.Execute<Program>(args);
        }

        [Argument(0, Description = "Required;Mzitu album id(e.g. https://www.mzitu.com/137224 id is 137224)")]
        [Required]
        public string Id { get; }

        [Option("-p|--path", Description = "Photo save path(default is the path where the current command executes)")]
        public string Path { get; } = Environment.CurrentDirectory;

        private int OnExecute()
        {
            int pages = 0;
            string albumName = string.Empty;
            string htmlString = string.Empty;
           
            try
            {
                _retryThreeTimesPolicy.Execute(() =>
                {
                    htmlString = GetHtmlStringAsync(Id).Result;
                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Excuted Failed,Message: ({e.Message})");
                return 0;
            }

            _htmlDocument.LoadHtml(htmlString);

            albumName = _htmlDocument.DocumentNode.SelectSingleNode("//h2[@class='main-title']")?.InnerText;
            if (string.IsNullOrEmpty(albumName))
            {
                Console.WriteLine("Page Not Found.");
                return 0;
            }
            pages = Convert.ToInt32(_htmlDocument.DocumentNode.SelectSingleNode("//div[@class='pagenavi']/a[last()-1]")?.InnerText);          

            Console.WriteLine($"Found {pages} Photos");

            for (int p = 1; p <= pages; p++)
            {
                if (p == 1)
                {
                    var imageNode = _htmlDocument.DocumentNode.SelectSingleNode("//div[@class='main-image']/p/a/img");
                    if (imageNode != null)
                    {
                        try
                        {
                            _retryThreeTimesPolicy.Execute(() =>
                            {
                                DownloadImageAsync(albumName, imageNode.Attributes["src"].Value).GetAwaiter().GetResult();
                            });
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Excuted DownloadImage Failed,Message: ({e.Message})");
                            continue;
                        }
                    }
                }
                else
                {
                    try
                    {
                        _retryThreeTimesPolicy.Execute(() =>
                        {
                            htmlString = GetHtmlStringAsync($"{Id}/{p}").Result;
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Excuted Failed,Message: ({e.Message})");
                        continue;
                    }
                    _htmlDocument.LoadHtml(htmlString);

                    var imageNode = _htmlDocument.DocumentNode.SelectSingleNode("//div[@class='main-image']/p/a/img");
                    if (imageNode != null)
                    {
                        try
                        {
                            _retryThreeTimesPolicy.Execute(() =>
                            {
                                DownloadImageAsync(albumName, imageNode.Attributes["src"].Value).GetAwaiter().GetResult();
                            });
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Excuted DownloadImage Failed,Message: ({e.Message})");
                            continue;
                        }
                    }
                }
            }
            return 0;
        }

        public async Task<string> GetHtmlStringAsync(string path)
        {
            string html = string.Empty;
            HttpResponseMessage response = await _httpClient.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                html = await response.Content.ReadAsStringAsync();
            }
            return html;
        }

        public async Task DownloadImageAsync(string folderName, string picUrl)
        {
            string fileName = picUrl.Split('/')[picUrl.Split('/').Length - 1];
            string folderPath = System.IO.Path.Combine(Path, folderName.Replace(":", "").Replace("?", ""));

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.167 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Referrer = new Uri($"{BASE_URL}{Id}");

            using (var response = await _httpClient.GetAsync(picUrl))
            {
                response.EnsureSuccessStatusCode();

                using (FileStream fsWrite = new FileStream(System.IO.Path.Combine(folderPath, fileName), FileMode.Create))
                {
                    await response.Content.ReadAsStreamAsync().Result.CopyToAsync(fsWrite);
                }
            }

            Console.WriteLine($"{fileName} download complete.");
        }
    }
}

using McMaster.Extensions.CommandLineUtils;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Threading.Tasks;

namespace MzituDown
{
    [Command(Description = "Mzitu Download")]
    class Program
    {
        public const string BASE_URL = "http://www.mzitu.com/";
        private static readonly HttpClient _httpClient;

        static Program()
        {
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(BASE_URL)
            };
        }

        public static int Main(string[] args)
        {

#if DEBUG
            args = new string[] { "137224", "-p", "da" };
            //Console.WriteLine("ASDSA");
#endif
            return CommandLineApplication.Execute<Program>(args);
        }

        [Argument(0, Description = "Required;Mzitu album id(e.g. http://www.mzitu.com/137224 id is 137224)")]
        [Required]
        public string Id { get; }

        [Option("-p|--path", Description = "Photo save path(default is the path where the current command executes)")]
        public string Path { get; } = Environment.CurrentDirectory;

        private int OnExecute()
        {
            Console.WriteLine(GetHtmlStringAsync(Id).Result);
            Console.WriteLine(Path);
            Console.ReadKey();
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

        public void DownloadImage()
        {
            _httpClient.DefaultRequestHeaders.Referrer = new Uri($"{BASE_URL}{Id}");
        }
    }
}

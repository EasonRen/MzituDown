using McMaster.Extensions.CommandLineUtils;
using System;
using System.ComponentModel.DataAnnotations;

namespace MzituDown
{
    [Command(Description = "Mzitu Download")]
    class Program
    {

        public static int Main(string[] args)
        {

#if DEBUG
            args = new string[] { "baidu.com", "-p", "da" };
            Console.WriteLine("ASDSA");
            Console.ReadKey();
#endif
            return CommandLineApplication.Execute<Program>(args);

        }

        [Argument(0, Description = "Required;Mzitu url")]
        [Required]
        public string Url { get; }

        [Option("-p|--path", Description = "Photo save path(default is the path where the current command executes)")]
        public string Path { get; } = Environment.CurrentDirectory;

        private int OnExecute()
        {
            Console.WriteLine(Url);
            Console.WriteLine(Path);
            return 0;
        }
    }
}

// See https://aka.ms/new-console-template for more information
using System.Net;
using WebEx.services;


//!!!! демо на странице 
//http://localhost:8080/Index.html

namespace WebEx
{
    static class Global
    {
        public static CancellationTokenSource Token = new CancellationTokenSource();
        public const string WWW = "http://localhost:8080/"; //80 порт не будет работать потому что на нем WebSocket!
        public static readonly string XXX = "ws://127.0.0.1:" + PORT; //wss 443
        public static readonly string YYY = "ws://127.0.0.1:" + PORT; //? будет ли wss 443
        public const string IP = "127.0.0.1";
        public const int PORT = 82; 
        
        //если 443, то и wss дописать и сертификат надо!
    };


    class Program
    {
        public static WebPage InstantiatePage(string url)
        {
            switch (url)
            {
                case Global.WWW:
                case Global.WWW + "/":
                case Global.WWW + "Index":
                case Global.WWW + "index":
                case Global.WWW + "Index.html":
                case Global.WWW + "index.html":
                    return new IndexTemplate();
                default:
                    return new Error404Template();
            }
        }

        static async Task Main(string[] args)
        {
            //Ctrl+C SIGKILL
            Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e)
                {
                    e.Cancel = true;
                    Global.Token.Cancel();
                };

            Grinder grinder = new();

            grinder.RegisterService(new PongService());
            grinder.RegisterService(new EchoService());
            grinder.RegisterService(new EventService());
            grinder.RegisterService(new UrlService());

            if (args.Length < 4)
            {
                Console.WriteLine("Command line parameters: WWWAddress WSSAddress Port Meta.html");
                Console.WriteLine($"Example: {Global.WWW} {Global.IP} {Global.PORT} meta.html");
                Console.WriteLine(">>> RUNNING IN LOCAL DEMO MODE <<<");
                //сюда можно перелить аргументы командной строки
                await grinder.Run(
                    Global.WWW,
                    IPAddress.Parse(Global.IP),
                    Global.PORT,
                    "meta.html",
                    Global.Token.Token
                    );
                Console.WriteLine(">>> FINISHED <<<");
            }
            else
            {
                Console.WriteLine(">>> READY <<<");
                await grinder.Run(
                    args[0],
                    IPAddress.Parse(args[1]),
                    int.Parse(args[2]),
                    args[3],
                    Global.Token.Token
                    );
                Console.WriteLine(">>> FINISHED <<<");
            }
        }
    }
}
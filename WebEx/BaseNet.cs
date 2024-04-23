using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;


namespace WebEx
{
    public delegate Task<bool> ReadBlock(byte[] data, ulong position, ulong length);
    public delegate Task Progress(float percent, CancellationToken token);
    public delegate Task<string> EventResult(Stream stream, string[] operands);

    //сохраняю родной Type от WebSock
    public enum MessageType : int
    {
        Error = -1,
        Continue = 0,
        String = 1,
        Data = 2,
        Close = 8,
        Ping = 9,
        Pong = 10,
    }

    public static class CONST
    {
        public const string HOST="host.html";
        public const int BUFFERSIZE = 0x1000; //максимальны размер буффера чтения потоковых данных 
        public const int PINGSIZE = 12;
        public const int PINGMSGSIZE = 10;
        public const int PONGSIZE = 16;
        public const int DELAY = 100;
        public const int MINDATASIZE = 3;
        public const int PINGINTERVAL = 128;
        public const int UPDATEDELAY = 2000; //2сек

        //эти хеши каждый запуск разные потому что для безопасности сделан их расчет с разым числом в базе!
        //список MIME https://ru.wikipedia.org/wiki/%D0%A1%D0%BF%D0%B8%D1%81%D0%BE%D0%BA_MIME-%D1%82%D0%B8%D0%BF%D0%BE%D0%B2#text
        //наиболее частые к верху (зависит от проекта), и учесть закешированые
        public static List<(int, string)> ContentType = new List<(int, string)>{
            (new string("svg").GetHashCode(),"image/svg+xml"), //много раз, обязательно
            //(new string("html").GetHashCode(),"text/html"),    //много раз, обязательно //но сокеты там не сработают или сбросятся
            (new string("jpg").GetHashCode(),"image/jpg"),     //много раз, не обязательно
            (new string("png").GetHashCode(),"image/png"),     //много раз, не обязательно
            
            (new string("ttf").GetHashCode(),"application/x-font-ttf"), //несколько раз, кешируется
            (new string("ico").GetHashCode(),"image/x-icon"),           //один раз, кешируется
            (new string("css").GetHashCode(),"text/css"),               //один раз, кешируется
            (new string("js").GetHashCode(),"application/javascript"),  //один раз, кешируется
            
            (new string("xml").GetHashCode(),"text/xml"),            //дополнительно
            (new string("json").GetHashCode(),"application/json"),   //дополнительно
            (new string("jpeg").GetHashCode(),"image/jpg"),          //дополнительно, придерживаться jpg
            (new string("pdf").GetHashCode(),"application/pdf"),     //скорее всего 1-2 на проект и то по запросу
            //(new string("zip").GetHashCode(),"application/zip"),    //это может быть большой файл, его надо отправить через поток и в закачку
        };
    }

    public class Transfer
    {
        public uint id; //может быть какой то идентификатор данных, хотя не факт
        public byte[]? data = null;  //данные
        //для вычисления окончания данных и процента загрузки
        public ulong length;
        public ulong position;
        public bool LastFrame { get => position == length; } //а у последнего позиция на 100%, иначе 100% прогрессбары не покажут
    }

    public interface IService
    {
        public void Register(Grinder grinder);
    }

    public class ServiceManager
    {
        public delegate Task ParserData(WebSession session, Transfer transfer);
        public delegate Task ParserPong(WebSession session, byte[] data, uint id);
        public delegate Task ParserLson(WebSession session, string text);
        public delegate Task ParserUpdate(WebSession session);

        public event ParserData OnData = ((a,b)=>Task.CompletedTask);
        public event ParserPong OnPong = ((a, b, c) => Task.CompletedTask);
        public event ParserLson OnLson = ((a, b) => Task.CompletedTask);
        public event ParserUpdate OnUpdate = ((a) => Task.CompletedTask);

        public Task ParseData(WebSession session, Transfer transfer) => OnData.Invoke(session, transfer);
        public Task ParsePong(WebSession session, byte[] data, uint id) => OnPong.Invoke(session, data, id);
        public Task ParseLson(WebSession session, string text) => OnLson.Invoke(session, text);
        public Task ParseUpdate(WebSession session) => OnUpdate.Invoke(session);
    }

    public class Grinder
    {
        public ServiceManager parser =new ();
        private string ApplicationPath = "";
        private string TemporalPath = "";
        private int Hash_zip = new string("zip").GetHashCode();
        private int Hash_html = new string("html").GetHashCode();
        

        private static byte[] Host = new byte[0];
        //private static string framework_h = "<script src='grinder.server.js'></script>\r\n";
        //private static string framework_b = $"<script>document.addEventListener('DOMContentLoaded', Start('{Global.YYY}'));</script>\r\n";
        //private static string host = $"<!doctype html><html lang='en'><script src='framework/grinder.server.js'></script><head><title>Grinder Pages</title></head><body><h2>Using default page!</h2></body><script>document.addEventListener('DOMContentLoaded', Start('{Global.YYY}'));</script></html>";

        //public static IEnumerable<string> FindAllHtmlFiles(string path) 
        //{
        //    var list=Directory.GetFiles(ApplicationPath + path, "*.html");
        //    foreach(var n in list) yield return n;//(Path.GetFileName(n));
        //}

        public void RegisterService(IService service) => service.Register(this);

        private HttpListener Initialize(string webaddress, string metafile)
        {
            ApplicationPath = Directory.GetCurrentDirectory();
            TemporalPath = Path.GetTempPath();
            var meta=File.ReadAllText(metafile, Encoding.UTF8);
            Host = Encoding.UTF8.GetBytes($@"
<!doctype html>
<html lang='en'>
    <head>
        {meta}
        <script src='grinder.server.js'></script>
        <script type=""text/javascript"">
            server.init(['{Global.YYY}','URL']);
        </script>
    </head>
    <body></body>
</html>");
            //foreach (var n in FindAllHtmlFiles(path)) pages.Add(("/"+Path.GetFileName(n), ProcessPage(n)));
            //var _host=ProcessPage(File.ReadAllText(CONST.HOST, Encoding.UTF8));
            //if (host!=string.Empty) host=_host; 
            //else host = ProcessPage(host);
            HttpListener server = new HttpListener();
            server.Prefixes.Add(webaddress);
            return server;
        }

        //private static string ProcessPage(string text)
        //{
        //    //string text = File.ReadAllText(name, Encoding.UTF8);
        //    var i1 = text.IndexOf("</head>");
        //    if (i1 == -1) return string.Empty;
        //    string ret=text.Insert(i1, framework_h);
        //    var i2 = ret.IndexOf("</html>");
        //    if (i2==-1) return string.Empty;
        //    return ret.Insert(i2,framework_b);
        //}


        public async Task<string> Response(HttpListenerContext context)
        {
            //Console.WriteLine($"Request: {context.Request.RawUrl}");
            var url = context.Request.RawUrl;

            //это здесь потому что часто запрашивается
            if (string.IsNullOrEmpty(url) || url == "/") {
                await Answer("/index.html", context.Response);
                return "/";
            }

            //всегда слать одну и ту же страницу, а потом ориентироваться по названию
            var param=url.Split('?');
            var hash = param[0][(url.LastIndexOf('.') + 1)..].GetHashCode();
            if (hash == Hash_html) {
                await Answer(url, context.Response);
                return url;
            }


            var path = string.Empty;
            if (url.StartsWith("_")) path = TemporalPath + url; //только ничего там лишнего не должно быть!
            else path = ApplicationPath + url;

            

            //Скачка больших файлов в Загрузки. Можно устраивать временные линки, надо ли?
            //Можно использовать temp Path.GetTempPath(); подменить
            if (hash == Hash_zip)
            {
                _ = Task.Factory.StartNew((ctx) =>
                {
                    WriteLongFile(context, path);// @"C:\LargeFile.zip");
                }, context, TaskCreationOptions.LongRunning);
                return string.Empty;
            }

            var found = CONST.ContentType.Find(x => x.Item1 == hash);
            if (found == default)
            {
                Console.WriteLine($"Bad request from {context.Request.RemoteEndPoint}:{context.Request.RawUrl}");
                return string.Empty;
            }

            //читать только если распознали расширение
            byte[]? contents = TryReadFile(path);
            if (contents == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.Close();
                return string.Empty;
            }
            else
            {
                context.Response.ContentType = found.Item2;
                context.Response.ContentEncoding = Encoding.UTF8;
                context.Response.ContentLength64 = contents.LongLength;
                context.Response.Close(contents, true);
                return string.Empty;
            }
        }

        //проверено 231128
        private byte[]? TryReadFile(string path)
        {
            if (File.Exists(path))
            {
                try { return File.ReadAllBytes(path); }
                catch { return null; }
            }
            return null;
        }

        //проверено 231128
        public async Task Answer( string url, HttpListenerResponse response)
        {
            //все подряд страницы мы не показываем, а только те что заданы изначально (безопасность)
            //var elem = pages.Find(x => string.Compare(x.name, url,true) == 0);
            //if (elem == default)
            //{
            //    response.StatusCode = (int)HttpStatusCode.NotFound;
            //    response.Close();
            //    return false;
            //}
            //else
            //{
                
                //byte[] buffer = Encoding.UTF8.GetBytes(Host);//elem.content);
                response.ContentLength64 = Host.LongLength;
                using Stream output = response.OutputStream;
                await output.WriteAsync(Host);
                await output.FlushAsync();
            //}
        }

        //проверено 231128
        public void WriteLongFile(HttpListenerContext ctx, string path)
        {
            var response = ctx.Response;
            using (FileStream fs = File.OpenRead(path))
            {
                string filename = Path.GetFileName(path);
                //response is HttpListenerContext.Response...
                response.ContentLength64 = fs.Length;
                response.SendChunked = false;
                response.ContentType = System.Net.Mime.MediaTypeNames.Application.Octet;
                response.AddHeader("Content-disposition", "attachment; filename=" + filename);

                byte[] buffer = new byte[CONST.BUFFERSIZE];
                int read;
                using (BinaryWriter bw = new BinaryWriter(response.OutputStream))
                {
                    while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        bw.Write(buffer, 0, read);
                        bw.Flush(); //seems to have no effect
                    }

                    bw.Close();
                }

                response.StatusCode = (int)HttpStatusCode.OK;
                response.StatusDescription = "OK";
                response.OutputStream.Close();
            }
        }

        public async Task Run(string webaddress, IPAddress ipaddress, int port, string meta, CancellationToken token)
        {
            var listener = new TcpListener(ipaddress, port);

            using (var server = Initialize(webaddress,meta))
            {

                //while (true) { //для дебага однопоток
                _ = Task.Factory.StartNew(async () =>
                {
                    server.Start();
                    while (!token.IsCancellationRequested)
                    {
                        try //сессия не должна класть сервер
                        {
                            //await mapget(await Request(server));
                            var context = await server.GetContextAsync().WaitAsync(token);
                            //context.Request.Cookies
                            await Response(context);
                            //await Answer(context.Request, context.Response, context.User);
                        }
                        catch (Exception e) { Console.WriteLine($"HTTPListener exception: {e}"); } //cancellation
                    }
                    server.Stop();
                }, TaskCreationOptions.LongRunning);
                //}
                listener.Start();
                while (!token.IsCancellationRequested)
                {
                    try //сессия не должна класть сервер
                    {

                        var client = await listener.AcceptTcpClientAsync(token);
                        _ = Task.Factory.StartNew(async () =>
                        {
                            if (await HandShake(client, token)) await Node(new WebSession(client,null), parser, token);
                        }, TaskCreationOptions.LongRunning);
                        //if (await HandShake(client, token)) await Node(new WebSession(client,null), parser, token); //для дебага однопоток
                    }
                    catch (IOException ex)
                    {
                        if (ex is IOException) Console.WriteLine("The client inperrupted the connection!");
                        else Console.WriteLine($"{ex.GetType} {ex}");
                    }
                    catch (OperationCanceledException) { };
                }
                //server.Close();
            }
            //socket.Close();  
            listener.Stop();
        }

        //Шарп коннектится с шарпом, никакого рукопожатия не надо
        public async Task Connect(string address, int port)
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                await socket.ConnectAsync(address, port);
                byte[] data = Encoding.UTF8.GetBytes(DateTime.Now.ToLongTimeString());
                await socket.SendAsync(data);
            }
        }



        public List<string> Info(HttpListenerRequest request)//, HttpListenerResponse response, IPrincipal? user)
        {
            List<string> ret = new List<string>();
            //var req = request.GetRequest;
            ret.Add($"Дата:{(DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"))}");
            ret.Add($"адрес приложения: {request.LocalEndPoint}");
            ret.Add($"адрес клиента: {request.RemoteEndPoint}");
            //ret.Add(req.RawUrl);
            ret.Add($"Запрошен адрес: {request.Url}");
            ret.Add("Заголовки запроса:");
            foreach (string item in request.Headers.Keys)
            {
                ret.Add($"{item}:{request.Headers[item]}");
            }
            return ret;
        }

        //Вебсокеты коннектятся с шарпом - надо рукопожатие и расшифровку
        public async Task<bool> HandShake(TcpClient client, CancellationToken token)
        {
            //TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("A client connected.");
            NetworkStream stream = client.GetStream();
            // enter to an infinite cycle to be able to handle every change in stream
            while (!token.IsCancellationRequested)
            {
                if (!stream.DataAvailable) { await Task.Delay(100, token); continue; }
                if (client.Available < 3) { await Task.Delay(100, token); continue; }

                byte[] bytes = new byte[client.Available];
                stream.Read(bytes, 0, bytes.Length);
                string s = Encoding.UTF8.GetString(bytes);

                if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))// match against "get"
                {
                    Console.WriteLine($"=====Handshaking from client=====");

                    // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                    // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                    // 3. Compute SHA-1 and Base64 hash of the new value
                    // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                    string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                    string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                    byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                    string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                    // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                    byte[] response = Encoding.UTF8.GetBytes(
                        "HTTP/1.1 101 Switching Protocols\r\n" +
                        "Connection: Upgrade\r\n" +
                        "Upgrade: websocket\r\n" +
                        "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                    stream.Write(response, 0, response.Length);
                    // и зарегистрировать клиента как существующего до разраыва соединения
                    // при разрыве поймать событие и запустить деструкторы
                    return true;
                }
                else break;
            }
            return false;
        }

        public async Task PageUpdate(WebSession session, ServiceManager parser, CancellationToken token, bool delay) 
        {
            //NetworkStream stream = client.GetStream();
            if (delay) await Task.Delay(CONST.UPDATEDELAY);
            //token.ThrowIfCancellationRequested();
            while (session.client.Connected && !token.IsCancellationRequested)
            {
                try
                {
                    await parser.ParseUpdate(session);
                }
                catch (Exception e) { } //и что?
                await Task.Delay(CONST.UPDATEDELAY); //чтоб после паузы не сработало обновление в случае остановки
            }
        }

        public async Task<int> Node(WebSession session, ServiceManager parser, CancellationToken token)
        {
            Console.WriteLine("The session started.");
            NetworkStream stream = session.client.GetStream();
            CancellationTokenSource updateToken= CancellationTokenSource.CreateLinkedTokenSource(token);
            //оно само переписывается opcode
            //int offset=0;
            //ulong length = 0;
            ushort id = 0;
            ushort ping = 0;
            byte[] masks = new byte[4];

            //update task отложен, с промежутком, в случае отмены запуск заново отложен
            var update= Task.Factory.StartNew(()=> PageUpdate(session, parser, updateToken.Token, true));

            while (session.client.Connected && !token.IsCancellationRequested)
            {
                if (ping++ == CONST.PINGINTERVAL) { await SendPing(stream, id++); ping = 0; }
                if (!stream.DataAvailable) { await Task.Delay(CONST.DELAY, token); continue; }
                if (session.client.Available < CONST.MINDATASIZE) { await Task.Delay(CONST.DELAY, token); continue; }
                DateTime start = DateTime.Now;

                int header0 = stream.ReadByte();
                if (header0 < 0) continue; //байт упакованый в int, отрицательный это 32бит
                bool fin = (header0 & 0b10000000) != 0; //1 - the last fragment of the message
                MessageType opcode = (MessageType)(header0 & 0b00001111); //0-continuation 1 - text message 2-data message 0x8-close 0x9-ping 0xA-pong              

                Console.WriteLine($"opcode {opcode}");

                if (opcode == MessageType.Continue) continue; //неправильный тип данных
                if (opcode == MessageType.Close) return 0; //завершение сессии?
                //if (opcode == 9) //Ping? похоже реализовано в TcpListener

                int header1 = stream.ReadByte();
                if (header1 < 0) continue;//байт упакованый в int, отрицательный это 32бит
                //var mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"
                //Предполагается обязательно разрывать соединение! rfc6455 section-5.1
                //это ограничение websocket потому что надо ксорить содержимое чтоб его в пути не подделали
                if ((header1 & 0b10000000) == 0) return -1;
                ulong length = (ulong)(header1 & 0b01111111);


                switch (header1 & 0b01111111)
                {
                    case 0:
                        continue; //пустой
                    case 126:
                        //в обратном порядке
                        {
                            byte[] bytes = new byte[2];
                            int read = stream.Read(bytes, 0, bytes.Length);
                            if (read < 2) continue;
                            WORD w = new WORD(bytes, 0, true);
                            length = w.us;
                        }
                        //msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                        break;
                    case 127:
                        //в обратном порядке
                        {
                            byte[] bytes = new byte[8];
                            int read = stream.Read(bytes, 0, bytes.Length);
                            if (read < 8) continue;
                            QWORD q = new QWORD(bytes, 0, true);
                            length = q.ul;
                        }
                        //msglen = BitConverter.ToUInt64(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] }, 0);
                        break;
                }

                if (stream.Read(masks, 0, masks.Length) < masks.Length) return 0;

                //ulong position = 0L;
                //public Stream stream;
                //public int service;
                //public int _event;
                //public Guid _object;
                //public string message;
                //public byte[] data;
                //public int type;
                //public int fin;

                switch (opcode)
                {
                    //отновная коммуникация
                    case MessageType.String:
                        {
                            byte[] buffer = new byte[length];
                            //while (msg.position < msg.length && !token.IsCancellationRequested)
                            //{
                            int read = stream.Read(buffer, 0, buffer.Length);
                            //var data = Decode(masks, buffer, read);
                            //msg.position += (ulong)read;
                            //if (msg.LastFrame) msg.opcode += 128; //fin
                            await parser.ParseLson(session, Encoding.UTF8.GetString(Decode(masks, buffer, read)));// Decode(masks, buffer, read), code, position, length)) return 0; //закрытие сессии
                                                                                                                 ////code=0;
                                                                                                                 //opcode = 0; //0 значит продолжение данных, как в самом вебсокет
                                                                                                                 //}
                        }
                        break;
                    //дополнительно режим передачи данных
                    case MessageType.Data:
                        {
                            Transfer transfer = new Transfer();
                            //msg.opcode = header0;
                            transfer.position = 0L;
                            transfer.length = length;

                            //byte[] header = new byte[4];
                            //if (stream.Read(header, 0, 4) < 4) return 0;
                            //DWORD h = new DWORD(header, 0, masks);
                            //msg._service = h.us1;
                            //msg._event = h.us2;
                            //msg.position += 4L;
                            //if (msg._service != 0)
                            //{
                            //    byte[] obj = new byte[16];
                            //    if (stream.Read(obj, 0, 16) < 16) return 0;
                            //    msg._object = new Guid(Decode(masks, obj, 16));
                            //    msg.position += 16L;
                            //}

                            //может просто открыть поток?
                            //маска мешает
                            //parser.ParseStream(stream, length);

                            byte[] buffer = new byte[CONST.BUFFERSIZE];
                            ////int code = (length <= Global.BLOCK) ? header0 & 3 : 128 + header0 & 3;  
                            while (transfer.position < transfer.length && !token.IsCancellationRequested)
                            {
                                int read = stream.Read(buffer, 0, buffer.Length);
                                transfer.data = Decode(masks, buffer, read);
                                transfer.position += (ulong)read;
                                //if (msg.LastFrame) msg.opcode += 128; //fin
                                //тут можно проконтролить передачу данных
                                //чтото =
                                await parser.ParseData(session, transfer);// Decode(masks, buffer, read), code, position, length)) return 0; //закрытие сессии
                                                                         ////code=0;
                                                                         //opcode = 0; //0 значит продолжение данных, как в самом вебсокет
                            }
                        }
                        break;
                    case MessageType.Pong:
                        {
                            byte[] buffer = new byte[length];
                            int read = stream.Read(buffer, 0, buffer.Length);
                            await parser.ParsePong(session, Decode(masks, buffer, read), id);
                        }
                        break;
                }


                DateTime end = DateTime.Now;
                TimeSpan delta = end - start;
                Console.WriteLine($"R:{delta}");
            }
            return 0;
        }

        //это защита маской от кеширования данных взломаными узлами
        public byte[] Decode(byte[] masks, byte[] srs, int length)
        {
            byte[] decoded = new byte[length];
            for (int i = 0; i < length; ++i)
                decoded[i] = (byte)(srs[i] ^ masks[i % 4]);
            return decoded;
        }

        public async Task<bool> ReadData(Stream stream, byte[] masks, ulong length, ReadBlock block, CancellationToken token)
        {
            byte[] buffer = new byte[CONST.BUFFERSIZE];
            ulong position = 0;
            while (position < length && !token.IsCancellationRequested)
            {
                int read = stream.Read(buffer, 0, buffer.Length);
                position += (ulong)read;
                if (!await block(Decode(masks, buffer, read), position, length)) return false;
            }
            return true;
        }

        //статичные функции, они не зависят от экземпляра 
        //так как сервисы отправляют события к компонентам, очень неудобно туда тащить делегаты

        public static byte[] Header(int opcode, ulong length)
        {
            byte[] raw;
            switch (length)
            {
                case ulong when length <= 125:
                    raw = new byte[2];
                    raw[0] = (byte)opcode;
                    raw[1] = (byte)length;
                    break;
                case ulong when length >= 126 && length <= 65535:
                    raw = new byte[4];
                    raw[0] = (byte)opcode;
                    raw[1] = 126;
                    WORD w = new WORD((ushort)length);
                    //!в обратном порядке
                    raw[2] = w.b2;
                    raw[3] = w.b1;
                    break;
                default:
                    raw = new byte[10];
                    raw[0] = (byte)opcode;
                    raw[1] = 127;
                    QWORD q = new QWORD(length);
                    //!в обратном порядке
                    raw[2] = q.b8;
                    raw[3] = q.b7;
                    raw[4] = q.b6;
                    raw[5] = q.b5;
                    raw[6] = q.b4;
                    raw[7] = q.b3;
                    raw[8] = q.b2;
                    raw[9] = q.b1;
                    break;
            }
            return raw;
        }

        

        //предполагается отправка кусочка за раз
        //если это что то готовое то нет лимита на размер - оно всё равно сгенерится в памяти сервера
        //если это что то полученное, то полученное уже разбито на кусочки размера буффера приема
        public static async Task SendText(Stream stream, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            byte[] data = Encoding.UTF8.GetBytes(text);
            byte[] header = Header(129, (ulong)data.Length);
            await stream.WriteAsync(header);
            await stream.WriteAsync(data);
        }



        //никак не работает передача в несколько пакетов - с той стороны придется это собирать назад
        //это временная эмуляция отправки, еще надо имя задать...
        //скорее всего будет отправка текста с именем и размером, потом пойдут пакеты
        //js будет их накапливать в файл и писать прогресс
        public static async Task<bool> SendData(Stream stream, Transfer transfer, int _stream)
        {
            if (transfer.data == null) return false;
            byte[] header = Header(130, (ulong)transfer.data.Length);
            await stream.WriteAsync(header);
            await stream.WriteAsync(transfer.data);
            return true;
        }

        public class Pong
        {
            public readonly ushort counter;
            public readonly TimeSpan ping = TimeSpan.MinValue;
            public Pong(byte[] data)
            {
                if (data.Length != CONST.PINGMSGSIZE) return;
                WORD d = new WORD(data, 0);
                counter = d.us;
                QWORD q = new QWORD(data, 2);
                ping = DateTime.UtcNow - new DateTime((long)q.ul);
            }

            public bool IsCorrect { get => ping != TimeSpan.MinValue; }
        }

        public static async Task<bool> SendPing(Stream stream, ushort hash)
        {
            byte[] raw = new byte[CONST.PINGSIZE];
            raw[0] = 0x89;
            raw[1] = (byte)CONST.PINGMSGSIZE;

            WORD d = new WORD(hash);
            raw[2] = d.b1;
            raw[3] = d.b2;


            QWORD q = new QWORD((ulong)DateTime.UtcNow.Ticks);
            raw[4] = q.b1;
            raw[5] = q.b2;
            raw[6] = q.b3;
            raw[7] = q.b4;
            raw[8] = q.b5;
            raw[9] = q.b6;
            raw[10] = q.b7;
            raw[11] = q.b8;

            await stream.WriteAsync(raw, 0, CONST.PINGSIZE);
            return true;
        }

        //утилиты =====================================================================================================
        #region utilities

        

        public async Task CopyStream(Stream input, Stream output, Progress onProgress, CancellationToken token = default)
        {
            if (input != null && input.Length != 0)
            {
                byte[] buffer = new byte[CONST.BUFFERSIZE];
                await onProgress(0f, token);
                int read = 0;
                while ((read = await input.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    await output.WriteAsync(buffer, 0, read, token);
                    await onProgress(input.Position * 100 / input.Length, token); //Delay сюда же
                }
                await onProgress(100f, token);
            }
        }

        public async Task SaveFile(Stream file, string path, Progress onProgress, CancellationToken token = default)
        {
            if (file != null && file.Length != 0)
            {
                if (string.IsNullOrEmpty(path)) return;
                using (Stream writer = new System.IO.FileStream(path, FileMode.CreateNew))
                {
                    await CopyStream(file, writer, onProgress, token);
                }
            }
        }


        public async Task LoadFile(string path, Stream file, Progress onProgress, CancellationToken token = default)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (string.IsNullOrEmpty(path)) return;
                using (Stream reader = new System.IO.FileStream(path, FileMode.Open))
                {
                    await CopyStream(reader, file, onProgress, token);
                }
            }
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct WORD
        {
            [FieldOffset(0)] public byte b1;
            [FieldOffset(1)] public byte b2;
            [FieldOffset(0)] public ushort us;

            public WORD(ushort x)
            {
                us = x;
            }

            public WORD(byte[] b, int offset)
            {
                b1 = b[offset];
                b2 = b[offset + 1];
            }

            public WORD(byte[] b, int offset, bool reverced)
            {
                b1 = b[offset + 1];
                b2 = b[offset];
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct DWORD
        {
            [FieldOffset(0)] public byte b1;
            [FieldOffset(1)] public byte b2;
            [FieldOffset(2)] public byte b3;
            [FieldOffset(3)] public byte b4;
            [FieldOffset(0)] public uint ui;
            [FieldOffset(0)] public ushort us1;
            [FieldOffset(2)] public ushort us2;

            public DWORD(uint x)
            {
                ui = x;
            }

            public DWORD(byte[] b, int offset)
            {
                b1 = b[offset];
                b2 = b[offset + 1];
                b2 = b[offset + 2];
                b2 = b[offset + 3];
            }

            public DWORD(byte[] b, int offset, byte[] mask)
            {
                b1 = (byte)(b[offset] ^ mask[0]);
                b2 = (byte)(b[offset + 1] ^ mask[1]);
                b2 = (byte)(b[offset + 2] ^ mask[2]);
                b2 = (byte)(b[offset + 3] ^ mask[3]);
            }

            //public DWORD(byte[] b, int offset, bool reverced)
            //{
            //    b1 = b[offset + 3];
            //    b1 = b[offset + 2];
            //    b1 = b[offset + 1];
            //    b2 = b[offset];
            //}
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct QWORD
        {
            [FieldOffset(0)] public byte b1;
            [FieldOffset(1)] public byte b2;
            [FieldOffset(2)] public byte b3;
            [FieldOffset(3)] public byte b4;
            [FieldOffset(4)] public byte b5;
            [FieldOffset(5)] public byte b6;
            [FieldOffset(6)] public byte b7;
            [FieldOffset(7)] public byte b8;
            [FieldOffset(0)] public ulong ul;

            public QWORD(ulong x)
            {
                ul = x;
            }

            public QWORD(byte[] b, int offset)
            {
                b1 = b[offset];
                b2 = b[offset + 1];
                b3 = b[offset + 2];
                b4 = b[offset + 3];
                b5 = b[offset + 4];
                b6 = b[offset + 5];
                b7 = b[offset + 6];
                b8 = b[offset + 7];
            }
            public QWORD(byte[] b, int offset, bool reverced)
            {
                b1 = b[offset + 7];
                b2 = b[offset + 6];
                b3 = b[offset + 5];
                b4 = b[offset + 4];
                b5 = b[offset + 3];
                b6 = b[offset + 2];
                b7 = b[offset + 1];
                b8 = b[offset];
            }
        }
        #endregion
        //работающее, но не оптимизировано
        //public static async Task SendData(int opcode, Stream stream, byte[] data, ulong length)
        //{

        //    ulong len = (ulong)(data.Length); //(ulong)Encoding.UTF8.GetByteCount(text);
        //    byte[] raw = new byte[10 + len];
        //    data.CopyTo(raw, 10);
        //    //Encoding.UTF8.GetBytes(text, 0, (int)len, raw, 10);
        //    int start = 0;
        //    switch (len)
        //    {
        //        case ulong when len <= 125:
        //            raw[8] = (byte)(opcode & 255);
        //            raw[9] = (byte)length;
        //            start = 8;
        //            break;
        //        case ulong when len >= 126 && len <= 65535:
        //            raw[6] = (byte)(opcode & 255);
        //            raw[7] = 126;
        //            WORD w = new WORD((ushort)length);
        //            //в обратном порядке
        //            raw[8] = w.b2;
        //            raw[9] = w.b1;
        //            //raw[8] = (byte)((len >> 8) & 255);
        //            //raw[9] = (byte)((len) & 255);
        //            start = 6;
        //            break;
        //        default:
        //            raw[0] = (byte)(opcode & 255);
        //            raw[1] = 127;
        //            QWORD q = new QWORD(length);
        //            //в обратном порядке
        //            raw[2] = q.b8;
        //            raw[3] = q.b7;
        //            raw[4] = q.b6;
        //            raw[5] = q.b5;
        //            raw[6] = q.b4;
        //            raw[7] = q.b3;
        //            raw[8] = q.b2;
        //            raw[9] = q.b1;
        //            //raw[2] = (byte)((len >> 56) & 255);
        //            //raw[3] = (byte)((len >> 48) & 255);
        //            //raw[4] = (byte)((len >> 40) & 255);
        //            //raw[5] = (byte)((len >> 32) & 255);
        //            //raw[6] = (byte)((len >> 24) & 255);
        //            //raw[7] = (byte)((len >> 16) & 255);
        //            //raw[8] = (byte)((len >> 8) & 255);
        //            //raw[9] = (byte)((len) & 255);
        //            start = 0;
        //            break;
        //    }
        //    await stream.WriteAsync(raw, start, raw.Length - start);
        //}

        //public static async Task SendFile(Stream stream, string path, Progress onProgress, CancellationToken token = default)
        //{

        //    if (!string.IsNullOrEmpty(path))
        //    {
        //        if (string.IsNullOrEmpty(path)) return;
        //        using (Stream reader = new System.IO.FileStream(path, FileMode.Open))
        //        {

        //            ulong len = (ulong)(reader.Length); //(ulong)Encoding.UTF8.GetByteCount(text);
        //            byte[] raw = new byte[10+Global.BLOCK];
        //            ulong position=0;
        //            while (position<len){
        //                long read=reader.Read(raw,10,raw.Length-10);
        //                if (position==0 && len>Global.BLOCK) raw[0]=0b00000001;
        //            //Encoding.UTF8.GetBytes(text, 0, (int)len, raw, 10);
        //            raw[0] = 129;
        //            int headlen = 0;
        //            switch (len)
        //            {
        //                case ulong when len <= 125:
        //                    raw[1] = (byte)len;
        //                    headlen = 2;
        //                    break;
        //                case ulong when len >= 126 && len <= 65535:
        //                    raw[1] = 126;
        //                    WORD w = new WORD((ushort)len);
        //                    //в обратном порядке
        //                    raw[2] = w.b2;
        //                    raw[3] = w.b1;
        //                    //raw[8] = (byte)((len >> 8) & 255);
        //                    //raw[9] = (byte)((len) & 255);
        //                    headlen = 4;
        //                    break;
        //                default:
        //                    raw[1] = 127;
        //                    QWORD q = new QWORD(len);
        //                    //в обратном порядке
        //                    raw[2] = q.b8;
        //                    raw[3] = q.b7;
        //                    raw[4] = q.b6;
        //                    raw[5] = q.b5;
        //                    raw[6] = q.b4;
        //                    raw[7] = q.b3;
        //                    raw[8] = q.b2;
        //                    raw[9] = q.b1;
        //                    //raw[2] = (byte)((len >> 56) & 255);
        //                    //raw[3] = (byte)((len >> 48) & 255);
        //                    //raw[4] = (byte)((len >> 40) & 255);
        //                    //raw[5] = (byte)((len >> 32) & 255);
        //                    //raw[6] = (byte)((len >> 24) & 255);
        //                    //raw[7] = (byte)((len >> 16) & 255);
        //                    //raw[8] = (byte)((len >> 8) & 255);
        //                    //raw[9] = (byte)((len) & 255);
        //                    headlen = 10;
        //                    break;
        //            }
        //            await stream.WriteAsync(raw, 0, headlen);

        //            await FILE.CopyStream(reader, stream, onProgress, token);
        //        }
        //        //await stream.WriteAsync(data, 0, data.Length);
        //    }
        //}

        //public static async Task SendData2(int opcode, Stream stream, byte[] data)
        //{
        //    ulong len = data.LongLength;
        //    byte[] raw = new byte[len + 10];
        //    data.CopyTo(raw, 10);
        //    int start = 10;
        //    if (opcode > 0)
        //    {

        //        switch (len)
        //        {
        //            case ulong when len <= 125:
        //                raw[8] = 129;
        //                raw[9] = (byte)len;
        //                start = 8;
        //                break;
        //            case ulong when len >= 126 && len <= 65535:
        //                raw[6] = 129;
        //                raw[7] = 126;
        //                WORD w = new WORD((ushort)len);
        //                //в обратном порядке
        //                raw[8] = w.b2;
        //                raw[9] = w.b1;
        //                //raw[8] = (byte)((len >> 8) & 255);
        //                //raw[9] = (byte)((len) & 255);
        //                start = 6;
        //                break;
        //            default:
        //                raw[0] = 129;
        //                raw[1] = 127;
        //                QWORD q = new QWORD(len);
        //                //в обратном порядке
        //                raw[2] = q.b8;
        //                raw[3] = q.b7;
        //                raw[4] = q.b6;
        //                raw[5] = q.b5;
        //                raw[6] = q.b4;
        //                raw[7] = q.b3;
        //                raw[8] = q.b2;
        //                raw[9] = q.b1;
        //                //raw[2] = (byte)((len >> 56) & 255);
        //                //raw[3] = (byte)((len >> 48) & 255);
        //                //raw[4] = (byte)((len >> 40) & 255);
        //                //raw[5] = (byte)((len >> 32) & 255);
        //                //raw[6] = (byte)((len >> 24) & 255);
        //                //raw[7] = (byte)((len >> 16) & 255);
        //                //raw[8] = (byte)((len >> 8) & 255);
        //                //raw[9] = (byte)((len) & 255);
        //                start = 0;
        //                break;
        //        }
        //    }
        //    await stream.WriteAsync(raw, start, raw.Length - start);
        //}


    }
}

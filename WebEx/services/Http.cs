using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebEx.services
{
    //этот сервис отправляет страницу если запрос был именно URL
    //теоретически у сервера могут спрашивать и другие системы, например приложение
    //тогда надо высылать данные соответственно логике этого приложения
    public class UrlService : IService
    {
        public void Register(Grinder grinder)
        {
            grinder.parser.OnLson += OnLson;
            grinder.parser.OnUpdate += OnUpdate;
        }

        public UrlService() {  }

        public async Task OnLson(WebSession session, string text)// byte[] frame, int opcode, ulong pos, ulong len)
        {
            //string text = Encoding.UTF8.GetString(msg.data);
            string[] operands = Lson.Parse(text);
            if (operands.Length == 0) return;
            var _command = operands[0];


            //// тут еще page release с таймером
            
            if (operands[0] == "URL") //operands.length==1
            {
                Console.WriteLine($"Requested page: {text}");
                var _url = (operands.Length>1) ? operands[1] : Global.WWW + "Err404.html";
                //if (operands.Length<2) return await Task.FromResult(false);//{
                //    //переход на главную
                //}
                var _operands= _url.Split('?');
                var _location = _operands[0];
                if (string.Compare(_location,Global.WWW)==0) _location=Global.WWW+ "Index.html";
                var _parameters= (operands.Length > 1) ? operands[1] : "";

                //if (_location==CurrentPage.Url) await Task.FromResult(true); //а может рефреш?



                session.page = Program.InstantiatePage(_location);// .Find(x => string.Compare(x.Url, _location) == 0);

                //if (p == null) session.page = templates[0].Instantiate(); //404
                //else session.page = p.Instantiate();

                //await CurrentPage.OnInitialize();
                await session.page.Create(session.client.GetStream(), _url, new Dictionary<string, string>());
                //await CurrentPage.OnParametersSet("");
                            //Console.WriteLine($"Error404: ({operands.Length})");
                Console.WriteLine($"Location: ({operands.Length}) {_parameters}");
                
                                    //существующая страница может принять другие параметры
                    //if (param.Length > 1)
                    //{ //а хрен его знает сколько этих '?' там
                    //    var parameters = Lson.Url(param[1]);
                    //    if (parameters.Count > 0) await CurrentPage.OnParametersSet(parameters);
                    //    await CurrentPage.OnRender(stream, param[0], parameters);
                    //    Console.WriteLine($"Parameters: ({operands.Length}) {param[1]}");
                    //}
                    //else await CurrentPage.OnRender(stream, param[0], new Dictionary<string, string>());
                 return;
                
            }
            if (operands[0] == "UPD") {
                if (session.page== null) return;
                //await session.page.Update();
                await session.page.Render(session.client.GetStream(), "", true, new Dictionary<string, string>());
                return;
            }
            //if (operands[0] == "REL")
            //{
            //    session.page.Release(session.client.GetStream());
            //}
                if (operands.Length>1 && session.page != null) await session.page.Event(session.client.GetStream(), operands);
            return;
        }
        public async Task OnUpdate(WebSession session)// byte[] frame, int opcode, ulong pos, ulong len)
        {
            if (session.page == null) return;
            //await session.page.Update();
            await session.page.Render(session.client.GetStream(), "", false, new Dictionary<string, string>());
        }

        public static string? GetOperand(string[] operands, int n, string? def = null)
        {
            if (operands.Count() > n) return operands[n];
            return def;
        }
    }
}

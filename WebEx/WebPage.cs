using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebEx.services;

namespace WebEx
{
    //page почти не отличается от компонента, можно любую ветку дерева считать корнем
    //однако тут есть название


    public class WebPage
    {
        //private string _url = string.Empty;
        //public string Url { get => _url; }
        
        protected List<WebComponent> body=new List<WebComponent>();

        //public WebPage(WebPage template) {
        //    //_url=template.Url;
        //    body = new List<WebComponent>();
        //    foreach (var x in template.body) this.body.Add(x.Instantiate()); //инстанс
        //}

        ////инициализация шаблона
        //public WebPage()
        //{
        //    //_url = url;
        //    body = new List<WebComponent>();
        //}

        //public virtual Task OnInitialize() { return Task.CompletedTask; }
        //public virtual Task OnParametersSet(Dictionary<string, string> parameters) { return Task.CompletedTask; }

        public async Task Create(Stream stream, string Url, Dictionary<string, string> parameters)
        {
            await OnCreate(stream, Url, parameters);
            foreach (var x in body) await x.Create(stream);
            await Task.CompletedTask;
        }
        public async Task Render(Stream stream, string Url, bool refresh, Dictionary<string, string> parameters)
        {
            await OnRender(stream, Url, refresh, parameters);
            //PageNotFoundText.Content = $"Страница <span class='red'>{Url}</span> не найдена!";
            foreach (var x in body) await x.Render(stream, refresh);
            await Task.CompletedTask;
        }

        public async Task Event(Stream stream, string[] operands)
        {
            await OnEvent(stream,operands);
            foreach (var x in body) await x.Event(stream, operands);
            await Task.CompletedTask;
        }

        //public async Task Update()
        //{
        //    await OnUpdate();
        //    foreach (var x in body) await x.Update();
        //    await Task.CompletedTask;
        //}
        public virtual Task OnCreate(Stream stream, string Url, Dictionary<string, string> parameters) { return Task.CompletedTask; }
        public virtual Task OnRender(Stream stream, string Url, bool refresh, Dictionary<string, string> parameters) { return Task.CompletedTask; }

        public virtual Task OnEvent(Stream stream, string[] operands) { return Task.CompletedTask; }

        public virtual Task OnUpdate() { return Task.CompletedTask; }
    }
    public class IndexTemplate : WebPage
    {

        private readonly Text ResultText = new Text("t14 cg87", "", "");// = new Text("t14 cg87", "", "Страница не найдена!");
        private readonly Block BlockWithText;
        

        public IndexTemplate()// : base(Global.WWW + "Index.html")
        {


            //frame это div полной высоты без фона,
            //    контейнер с вертикальны флексом,
            //    вставляется в body
            body = new List<WebComponent>(){
            new Frame("_white", "margin:24px;padding:12px;height:100%;width:auto",new List<WebComponent>() {
                { BlockWithText=new Block("col", "margin:auto", new List<WebComponent>() {
                    new Text("t24 cg87","margin-bottom:8px","Главная страница"),

                    new Button(){
                        Class="sb",
                        style="position:relative;margin:56px auto 0",
                        height="40px",
                        Content="<a class='bcs white' style='width:100%;height:100%;line-height:38px'>НА СТРАНИЦУ 404</a>",
                        onclick=(async (__s, __o)=>{return await Task.FromResult($"URL|{Global.WWW}Err404.html");})
                    },
                    new Block("row", "margin:56px auto", new List<WebComponent>() {
                        new Button(){
                            Class="wb",
                            style="margin:8px",
                            height="40px",
                            Content="<a class='bws custom' style='width:100%;height:100%;line-height:38px'>АПЕЛЬСИНЫ</a>",
                            onclick=(async (__s, __o)=>{
                                ResultText.SetText("АЕЛЬСИНЫ КРУГЛЫЕ ОРАНЖЕВЫЕ КРУПНЫЕ");
                                await ResultText.Render(__s,true, BlockWithText);
                                return await Task.FromResult("");
                            })
                        },
                        new Button(){
                            Class="wb",
                            style="margin:8px",
                            height="40px",
                            Content="<a class='bws custom' style='width:100%;height:100%;line-height:38px'>ЯБЛОКИ</a>",
                            onclick=(async (__s,__o)=>{
                                ResultText.SetText("ЯБЛОКИ КРУГЛЫЕ КРАСНЫЕ МАЛЕНЬКИЕ");
                                await ResultText.Render(__s,true, BlockWithText);
                                return await Task.FromResult("");
                                /*return await Task.FromResult("UPD| ");*/})
                        }
                    }),
                    { ResultText },

                }) }
            })
            };
        }
    }

    public class Error404Template : WebPage
    {
        private readonly Text PageNotFoundText;// = new Text("t14 cg87", "", "Страница не найдена!");

        public Error404Template() //: base(Global.WWW + "Err404.html")
        {

            //frame это div полной высоты без фона,
            //    контейнер с вертикальны флексом,
            //    вставляется в body
            body = new List<WebComponent>(){
            new Frame("_white", "margin:24px;padding:12px;height:100%;width:auto",new List<WebComponent>() {
                new Block("col", "margin:auto", new List<WebComponent>() {
                    new Text("t24 red","margin-bottom:8px; filter: drop-shadow(1px 1px 1px #FFFFF0) drop-shadow(-1px -1px 1px #A0A0AF)","Error404"),
                    { PageNotFoundText = new Text("t14 cg87", "", "Страница не найдена!") },
                    new Button(){
                        Class="rb",
                        style="margin:56px auto 0",
                        height="40px",
                        Content="<a class='bcs white' style='width:100%;height:100%;line-height:38px;'>НА ГЛАВНУЮ</a>",
                        onclick=(async (__s,__o)=>{return await Task.FromResult($"URL|{Global.WWW}Index.html"); })
                        }
                    })
                })

            };
        }

        public override Task OnRender(Stream stream, string Url, bool refresh, Dictionary<string, string> parameters)
        {
            PageNotFoundText.Content = $"Страница <span class='red'>{Url}</span> не найдена!";
            return Task.CompletedTask;
            //await base.OnRender(stream, Url, refresh, parameters);
        }

    }
}

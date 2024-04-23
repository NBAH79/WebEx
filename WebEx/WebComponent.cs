using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static WebEx.Grinder;

namespace WebEx
{
    //пакет
    //int service
    //int event
    //Guid object (в случае если не системное)
    //другие данные или строка




    public abstract class WebComponent
    {
        protected readonly string id;
        
        private readonly List<WebComponent> children;

        protected int hash=0;

        //public long parent = 0;
        public string Tag = "div";//string.Empty;
        public string Class = string.Empty;
        public string Style = string.Empty;
        public string Content = string.Empty;
        
        public Dictionary<string, string> Attributes = new Dictionary<string, string>();
        //тут аттрибуты и события
        public List<string> Events = new();
        

        public string Id { get => id; }


        //конструктор шаблона
        public WebComponent()
        {
            id = Convert.ToBase64String(BitConverter.GetBytes(DateTime.UtcNow.Ticks));
            children = new List<WebComponent>();
        }

        //конструктор шаблона
        public WebComponent(List<WebComponent> template)
        {
            id = Convert.ToBase64String(BitConverter.GetBytes(DateTime.UtcNow.Ticks));
            children = new List<WebComponent>();
            foreach (var x in template) this.children.Add(x.Instantiate()); //инстанс
        }

        //Инстанс
        public abstract WebComponent Instantiate();
        //{
        //    var ret=this.MemberwiseClone();
        //    return ret as WebComponent;
        //    ////WebComponent copy= (WebComponent) _copy;
        //    //id = Convert.ToBase64String(BitConverter.GetBytes(DateTime.UtcNow.Ticks));
        //    //children = new ();
        //    //foreach (var x in copy.children) children.Add(x.Instantiate());
        //    //hash=0;
        //    //Tag=copy.Tag;
        //    //Class=copy.Class;
        //    //Style=copy.Style;
        //    //Content=copy.Content;
        //    //Attributes=new ();
        //    //foreach(var a in copy.Attributes) Attributes.Add(a.Key, a.Value);
        //    //Events=new();
        //    //foreach (var e in copy.Events) this.Events.Add(new string(e));
        //    ////сделать hash=OnUpdate и он будет другой потому что id!
        //    //_ = OnUpdate();
        //}

        

        //надо чтоб возвращало просто строку
        public async Task OnCreate(Stream stream, WebComponent? parent = null)
        {
            await OnUpdate();
            string res = $"|{id}|{parent?.id}|{Tag}|{Class}|{Style}|";
            foreach (var a in Attributes) res += $"{a.Key}={a.Value};";
            res += "|";
            foreach (var e in Events) res += $"{e};";
            res += $"|{Content}";
            hash = res.GetHashCode();
            await SendText(stream, res);
        }

        //просто тупая отправка
        //надо чтоб возвращало просто строку
        public async Task OnRender(Stream stream, bool refresh, WebComponent? parent = null)
        {
            await OnUpdate();
            string res = $"|{id}|{parent?.id}|{Tag}|{Class}|{Style}|";
            foreach (var a in Attributes) res += $"{a.Key}={a.Value};";
            res += "|";
            //евенты не обновлять, даже лучше их отдельно сделать
            //if (refresh) foreach (var e in Events) res += $"{e};";
            res += $"|{Content}";
            int newhash = res.GetHashCode();
            //if (newhash == hash && !refresh) return;
            await SendText(stream, res);
            hash = newhash;
        }

        //Create и Render зашиты, а эти непонятные переопеределяются
        public abstract Task OnEvent(Stream stream, string[] operands);// { await Task.CompletedTask; }
        public abstract Task OnUpdate();// { await Task.CompletedTask; }
        public abstract void OnRelease();// { }
        public async Task Create(Stream stream, WebComponent? parent = null)
        {
            await OnCreate(stream, parent);
            foreach (var x in children) await x.Create(stream, this);
        }

        public async Task Render(Stream stream, bool refresh, WebComponent? parent = null)
        {
            await OnRender(stream, refresh, parent);
            foreach (var c in children) await c.Render(stream, refresh, this);
        }

        public async Task Event(Stream stream, string[] operands)
        {
            await OnEvent(stream, operands);
            foreach (var x in children) await x.Event(stream, operands);
        }

        //если есть параметры то это должно сработать до render
        //public async Task Update()
        //{
        //    await OnUpdate();
        //    foreach (var x in children) await x.Update();
        //}

        //с задержкой дает возможность прокрутить анимацию
        public async Task Destroy(Stream stream, int msec)
        {
            OnRelease();
            await OnUpdate();
            await Render(stream, true);
            await SendText(stream, $"{id}|{msec}");
            //_=Task.Factory.StartNew(async () =>
            //{
            //      await Task.Delay(msec);
            //      await Node.SendText(stream, $"{id}|");
            //});
        }
    }

    public class Frame : WebComponent
    {
        public override WebComponent Instantiate()
        {
            var ret = this.MemberwiseClone();
            return ret as WebComponent;
        }
        public Frame(string Class, string Style, List<WebComponent> children):base(children)
        {
            Tag = "div";
            this.Class = "frame " + Class;
            this.Style = Style;
        }
        public override async Task OnEvent(Stream stream, string[] operands) { await Task.CompletedTask; }
        public override async Task OnUpdate() { await Task.CompletedTask; }
        public override void OnRelease() { }
    }

    public class Block : WebComponent
    {
        public override WebComponent Instantiate()
        {
            var ret = this.MemberwiseClone();
            return ret as WebComponent;
        }
        public Block(string Class, string Style, List<WebComponent> children) : base(children)
        {
            Tag = "div";
            this.Class = Class;
            this.Style = Style;
        }
        public override async Task OnEvent(Stream stream, string[] operands) { await Task.CompletedTask; }
        public override async Task OnUpdate() { await Task.CompletedTask; }
        public override void OnRelease() { }
    }
    public class Text : WebComponent
    {
        public override WebComponent Instantiate()
        {
            var ret = this.MemberwiseClone();
            return ret as WebComponent;
        }
        public Text(string Class, string Style, string text)
        {
            Tag = "p";
            this.Class = Class;
            this.Style = Style;
            this.Content = text;
        }
        public override async Task OnEvent(Stream stream, string[] operands) { await Task.CompletedTask; }
        public override async Task OnUpdate() { await Task.CompletedTask; }
        public override void OnRelease() { }
        public void SetText(string newtext)
        {
            Content = newtext;
            hash = 0;
        }
    }

    public class Button : WebComponent
    {
        public override WebComponent Instantiate()
        {
            var ret = this.MemberwiseClone();
            return ret as WebComponent;
        }
        public string height { get; set; } = "48px";
        public string opacity { get; set; } = "1";
        public string title { get; set; } = "BUTTON";

        //public string left{ get; set; } = "0";
        public string style { get; set; } = "position:relative;margin:56px auto 0";

        public int position = 0;
        public EventResult onclick { get; set; } = (async (__s, __o) => { return await Task.FromResult("UPD|"); });

        public Button(){
            Events.Clear();
            Events.Add("click:3");
        }

        public override async Task OnUpdate()
        {
            //Tag = "div";
            //Class = "wb";
            position += 50;
            //left = $"{position}px";
            if (position > 300) position = 0;
            Style = $"{style};left:{position}px;transition: all 2s ease-out 0s;width:200px;height:{height};opacity:{opacity}";
            //Events.Clear();
            //Events.Add("click:3");//,$"sendText('{this.id}onclick')");
            hash = 0;
            await Task.CompletedTask;
            //Content="title";
        }

        public override void OnRelease()
        {
            height = "0";
            opacity = "0";
        }

        //public override string OnRender()
        //{
        //    return $"<button ref='{id}' style='transition: all 2s ease-out 0s;width:200px;height:{height}'>{title}</button>";
        //    //return base.OnRender(operands);
        //}

        public override async Task OnEvent(Stream stream, string[] operands)
        {
            if (operands[0] == id && operands[1] == "click") await SendText(stream, await onclick(stream, operands));
        }
    }
    public class Input : WebComponent
    {

        public override WebComponent Instantiate()
        {
            var ret = this.MemberwiseClone();
            return ret as WebComponent;
        }

        public override async Task OnEvent(Stream stream, string[] operands) { await Task.CompletedTask; }
        public override async Task OnUpdate() { await Task.CompletedTask; }
        public override void OnRelease() { }

        public string type { get; set; } = "text";
        public string placeholder { get; set; } = "SOME TEXT";

        //public override string OnRender()
        //{
        //    return $"<input ref='{id}' style='width:200px;height:48px' type='{type}' placeholder='{placeholder}'><label>INPUT</label></input>";
        //    //return base.OnRender(operands);
        //}
    }

}

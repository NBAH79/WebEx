using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebEx
{
    //[LanguageInjection(InjectedLanguage.Html)]
    public static class HTMLPAGES
    {

        public const string basepage =
    @$"
<!doctype html>
<html lang=""en"">
  <style>
    textarea {{
      vertical-align: bottom;
    }}
    #output {{
      overflow: auto;
    }}
    #output > p {{
      overflow-wrap: break-word;
    }}
    #output span {{
      color: blue;
    }}
    #output span.error {{
      color: red;
    }}
  </style>
  <body>
    <h2>WebSocket Test</h2>
    <div style='display:flex;flex-direction:row;width:fit-content'>
        <button id=""button10"">Send 10</button>
        <button id=""button150"">Send 150</button>
        <button id=""button75000"">Send 7500</button>
        <button id=""button100500"">Send 100500</button>
    </div>
    <input type=""file"" id=""file-input"" />
    <textarea cols=""60"" rows=""6""></textarea>
    <button>send</button>
    <div id=""output""></div>
    <div id=""CONTAINER"" style=""width:100%;height:auto""></div>
  </body>
  <script>
    const container = document.querySelector(""CONTAINER"");
    const button = document.querySelector(""button"");
    const button10 = document.querySelector(""#button10"");
    const button150 = document.querySelector(""#button150"");
    const button75000 = document.querySelector(""#button75000"");
    const button100500 = document.querySelector(""#button100500"");
    const output = document.querySelector(""#output"");
    const textarea = document.querySelector(""textarea"");
    const inputfile = document.querySelector(""#file-input"");

    const wsUri = ""{Global.YYY}"";
    const websocket = new WebSocket(wsUri);
    websocket.binaryType = ""arraybuffer"";

    button.addEventListener(""click"", onClickButton);
    button10.addEventListener(""click"", function(){{ onClickButtonX(10);}}, false);
    button150.addEventListener(""click"", function(){{ onClickButtonX(150);}}, false);
    button75000.addEventListener(""click"", function(){{ onClickButtonX(75000);}}, false);
    button100500.addEventListener(""click"", function(){{ onClickButtonX(100000000);}}, false);
    inputfile.addEventListener(""change"", readDataFile, false);

    function readDataFile(e) {{
        var file = e.target.files[0];
        if (!file) {{
            return;
        }}
        var reader = new FileReader();
        reader.onload = function() {{
            var data = reader.result;
            var array = new Int8Array(data);
            doSendData(array);
        }};
        reader.readAsArrayBuffer(file);
    }}

    function readBase64File(e) {{
        var file = e.target.files[0];
        if (!file) {{
            return;
        }}
        var reader = new FileReader();
        reader.onload = function(e) {{
            var contents = e.target.result;
            doSendData(array);
        }};
        reader.readAsDataURL(file);
    }}

    function readTextFile(e) {{
        var file = e.target.files[0];
        if (!file) {{
            return;
        }}
        var reader = new FileReader();
        reader.onload = function(e) {{
            var contents = e.target.result;
            doSendData(contents);
        }};
        reader.readAsText(file);
    }}

    websocket.onopen = (e) => {{
      writeToScreen(""CONNECTED"");
      doSendString(""WebSocket"");
    }};

    websocket.onclose = (e) => {{
      writeToScreen(""DISCONNECTED"");
    }};


    websocket.onmessage = (e) => {{
    if (e.data instanceof ArrayBuffer){{
        var buffer = e.data;
        writeToScreen(`<span>RESPONSE DATA:(${{buffer.length}})</span>`);
        }}
    else {{
        //example
        //var jsonObject = JSON.parse(event.data);
        //var username = jsonObject.name;
        //var message = jsonObject.message;

        writeToScreen(`<span>RESPONSE STRING:(${{e.data.length}})</span>`);
        CONTAINER.innerHTML = e.data;
        }}
    }};

    websocket.onerror = (e) => {{
      writeToScreen(`<span class=""error"">ERROR:</span> ${{e.data}}`);
    }};

    function doSendString(message) {{
      writeToScreen(`SENT: ${{message}}`);
      websocket.send(message);
    }};

    function doSendData(data) {{
      
      var array = new Int8Array(data.length+4);
      for (let n=0;n<data.length;n++){{ array[n+4]=data[n];}};
      websocket.send(array);
      writeToScreen(`SENT: ${{(data.length)}}`);
    }};

    function writeToScreen(message) {{
      output.insertAdjacentHTML(""afterbegin"", `<p>${{message}}</p>`);
    }};

    function onClickButton() {{
      const text = textarea.value;
      text && doSendString(text);
      textarea.value = """";
      textarea.focus();
    }};

    function onClickButtonX(len) {{
      let text = """";
        let mask=[""A"",""B"",""C"",""D""];
        for (let n=0;n<len;n++) {{ text+= mask[n%4];}};
       doSendString(text);
    }};

//async function downloadFromStream(fileName, contentStreamReference) {{
// const arrayBuffer = await contentStreamReference.arrayBuffer();
// const blob = new Blob([arrayBuffer]);
// const url = URL.createObjectURL(blob);
// const anchorElement = document.createElement(""a"");
//  anchorElement.href = url;
//  anchorElement.download = fileName ?? """";
//  anchorElement.click();
//  anchorElement.remove();
//  URL.revokeObjectURL(url);
//}};

//const interval = setInterval(function ping() {{
//    wss.clients.forEach(function each(ws) {{
//        if (ws.isAlive === false) {{
//           console.log(""I am terminating"");
//           return ws.terminate();
//        }}
//       ws.isAlive = false;
//       ws.ping(self.noop);
//   }});
//}}, 30000);

  </script>
</html>
";

    }

}

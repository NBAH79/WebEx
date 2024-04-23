using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WebEx.Grinder;

namespace WebEx.services
{
    public class EventService:IService
    {
        Button button=new ();
        Input input=new ();

        public void Register(Grinder grinder)
        {
            grinder.parser.OnLson += OnLson;
        }
        
        public async Task OnLson(WebSession session, string text)// byte[] frame, int opcode, ulong pos, ulong len)
        {
            //string text = Encoding.UTF8.GetString(msg.data);
            string[] operands = Lson.Parse(text);

            await Task.CompletedTask;
            //Console.WriteLine($"Text: ({operands.Count()})");
            //switch (operands[0])
            //{
            //    case string when string.IsNullOrEmpty(text):
            //        break;
            //    case "button":
            //        {
            //            if (operands.Count() == 1) await button.Destroy(stream, 3000);
            //            else
            //            {
            //                string? temp = GetOperand(operands, 1);
            //                if (!string.IsNullOrEmpty(temp)) button.height = temp;
            //                temp = GetOperand(operands, 2);
            //                if (!string.IsNullOrEmpty(temp)) button.title = temp;
            //                button.opacity = "1";
            //                button.Class = "two";
            //                button.OnUpdate();
            //                await button.Render(stream,true,false);// $"<button style='width:200px;height:{operands[1]}'>BUTTON</button>");
            //            }
            //        }
            //        break;
            //    case "input":
            //        {
            //            string? temp = GetOperand(operands, 1);
            //            if (!string.IsNullOrEmpty(temp)) input.type = temp;
            //            temp = GetOperand(operands, 2);
            //            if (!string.IsNullOrEmpty(temp)) input.placeholder = temp;
            //            await input.Render(stream,true);
            //            // "<input style='width:200px;height:48px' type='text' placeholder='some text'><label>INPUT</label></input>");
            //        }
            //        break;
            //    default:
            //        //byte[] data = Encoding.UTF8.GetBytes(__t);
            //        //foreach (var d in data) Global.console.WriteLine($"{d:X}");
            //        //await Node.Send2(__s, "<p>UNKNOWN COMMAND</p>");
            //        await SendText(stream, operands[0]);
            //        break;
            //}
        }

        public static string? GetOperand(string[] operands, int n, string? def = null)
        {
            if (operands.Count() > n) return operands[n];
            return def;
        }
    }
}

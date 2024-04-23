using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static WebEx.Grinder;

namespace WebEx.services
{
    public class PongService:IService
    {
        public void Register(Grinder grinder)
        {
            grinder.parser.OnPong += OnPong;
            //sendPing = grinder.SendPing;
        }

        //public SendPong sendPing = (async (a, b) => { return await Task.FromResult(false); });

        public async Task OnPong(WebSession session, byte[] data, uint id)// byte[] frame, int opcode, ulong pos, ulong len)
        {
            Pong pong = new Pong(data);
            if (pong.IsCorrect) Console.WriteLine($"Pong: {id} ({pong.ping}) ");
            else Console.WriteLine($"Pong: error data! ");
            await Task.CompletedTask;
        }
    }
}

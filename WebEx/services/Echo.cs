using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WebEx.Grinder;

namespace WebEx.services
{
    public class EchoService : IService
    {
        //по логике передача файла должна начинаться с текстового соообщения, в котором сказано название файла и инициирован прием
        //вся сессия переключается на прием данных, и если это текст - значит что то пошло не так
        //если данные прервались - разорвать соединение

        public void Register(Grinder grinder)
        {
            grinder.parser.OnData += OnData;
        }

        public async Task OnData(WebSession session, Transfer transfer)// byte[] frame, int opcode, ulong pos, ulong len)
        {
            Console.WriteLine($"({transfer.position} of {transfer.length}) ");
            try
            {
                await SendData(session.client.GetStream(), transfer, 1);
            }
            catch { }
        }
    }
}

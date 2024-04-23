using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WebEx
{

    //от него будут наследоваться 
    public class WebSession
    {
        public WebPage? page;
        public TcpClient client=new ();

        public WebSession(TcpClient client, WebPage? template)
        {
            this.client = client;
            this.page = null;
        }
    }
}

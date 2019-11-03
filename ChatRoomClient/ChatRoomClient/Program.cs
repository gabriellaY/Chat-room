using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatRoomClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Client";

            ChatRoomClient.ConnectToServer();
            ChatRoomClient.RequestLoop();                    
        }
    }
}


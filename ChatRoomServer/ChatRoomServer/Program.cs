using System;
using System.Data.SqlClient;
using System.IO;

namespace ChatRoomServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Server";

            ChatRoomServer.Start();
        }
    }
}

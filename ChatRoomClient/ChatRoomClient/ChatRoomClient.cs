using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace ChatRoomClient
{
    public class ChatRoomClient
    {
        private static readonly Socket ClientSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private static readonly int port = 5050;

        public static void ConnectToServer()
        {
            while (!ClientSocket.Connected)
            {
                try
                {
                    ClientSocket.Connect(IPAddress.Loopback, port);
                }
                catch (SocketException)
                {
                    Console.Clear();
                }
            }

            Console.Clear();
            Console.WriteLine("Client connected");
            Console.WriteLine();

            Console.WriteLine(@"Choose command: 
-r -> Register as new user with nickname and password
-l -> Login with nickname and password if you have existing account
-s -> Change status
-d -> Logout by entering nickname
-m -> Send message to specific user
-a -> Send message to all users
-h -> See all possible commands for help");
        }

        public static void RequestLoop()
        {
            Console.WriteLine(@"""exit"" -> To disconnect client\n");
            Console.WriteLine();

            while (true)
            {
                Task.Run(RecieveLoop);
                SendRequest();
            }
        }

        private static void RecieveLoop()
        {
            while (true)
            {
                if (!Console.KeyAvailable)
                {
                    ReceiveResponse();
                }
            }
        }

        private static void ReceiveResponse()
        {
            var buffer = new byte[2048];
            int received = ClientSocket.Receive(buffer, SocketFlags.None);

            if (received == 0)
            {
                return;
            }

            var data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = Encoding.ASCII.GetString(data);
            Console.WriteLine(text);
            Console.WriteLine();
        }

        private static void SendRequest()
        {
            Console.WriteLine();

            string input = Console.ReadLine();

            var command = CreateCommand(input);

            var xmlString = CommandFromXmlFile(command);

            byte[] buffer = Encoding.ASCII.GetBytes(xmlString.ToString());
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);

            if (input.ToLower() == "exit")
            {
                Exit();
            }
        }

        private static Command CreateCommand(string input)
        {
            var splitText = input.Split();
            var commandType = splitText[0];

            var command = new Command();

            switch (commandType)
            {
                case "-r":
                    {
                        var nickname = new Parameter
                        {
                            Name = "Nickname",
                            Text = splitText[1]
                        };

                        var password = new Parameter
                        {
                            Name = "Password",
                            Text = splitText[2]
                        };

                        command = new Command
                        {
                            Name = "Registration",
                            Parameters = new List<Parameter>() { nickname, password }
                        };

                        break;
                    }

                case "-l":
                    {
                        var nickname = new Parameter
                        {
                            Name = "Nickname",
                            Text = splitText[1]
                        };

                        var password = new Parameter
                        {
                            Name = "Password",
                            Text = splitText[2]
                        };

                        command = new Command
                        {
                            Name = "Login",
                            Parameters = new List<Parameter>() { nickname, password }
                        };

                        break;
                    }

                case "-s":
                    {
                        var nickname = new Parameter
                        {
                            Name = "Nickname",
                            Text = splitText[1]
                        };

                        var status = new Parameter
                        {
                            Name = "Status",
                            Text = splitText[2]
                        };

                        command = new Command
                        {
                            Name = "Change status",
                            Parameters = new List<Parameter>() { nickname, status }
                        };

                        break;
                    }

                case "-d":
                    {
                        var nickname = new Parameter
                        {
                            Name = "Nickname",
                            Text = splitText[1]
                        };

                        /*var password = new Parameter
                        {
                            Name = "Password",
                            Text = splitText[2]
                        };*/

                        command = new Command
                        {
                            Name = "Logout",
                            Parameters = new List<Parameter>() { nickname }
                        };

                        break;
                    }

                case "-m":
                    {
                        var nickname = new Parameter
                        {
                            Name = "Nickname",
                            Text = splitText[1]
                        };

                        var message = new Parameter
                        {
                            Name = "Message",
                            Text = string.Join(" ", splitText, 2, splitText.Length - 2)
                        };

                        command = new Command
                        {
                            Name = "Send message to one user",
                            Parameters = new List<Parameter>() { nickname, message }
                        };

                        break;

                    }

                case "-a":
                    {
                        var message = new Parameter
                        {
                            Name = "Message",
                            Text = string.Join(' ', splitText.Skip(1).ToArray())
                        };

                        command = new Command
                        {
                            Name = "Send message to all users",
                            Parameters = new List<Parameter>() { message }
                        };

                        break;
                    }

                case "-h":
                    {
                        command = new Command
                        {
                            Name = "Help"
                        };

                        break;
                    }
            }

            return command;
        }

        private static string CommandFromXmlFile(Command command)
        {
            XmlDocument xdoc = new XmlDocument();

            XmlSerializer serializer = new XmlSerializer(command.GetType());

            using (MemoryStream stream = new MemoryStream())
            {
                serializer.Serialize(stream, command);
                stream.Position = 0;
                xdoc.Load(stream);
            }

            StringWriter sw = new StringWriter();
            XmlTextWriter tx = new XmlTextWriter(sw);
            xdoc.WriteTo(tx);

            return sw.ToString();
        }

        private static void SendString(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        private static void Exit()
        {
            SendString("exit");
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
            Environment.Exit(0);
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace ChatRoomServer
{
    public class ChatRoomServer
    {
        private static readonly Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> _clientSockets = new List<Socket>();
        private static readonly ConcurrentDictionary<string, Socket> _onlineUsers = new ConcurrentDictionary<string, Socket>();
        private static readonly ConcurrentDictionary<string, Socket> _bannedUsers = new ConcurrentDictionary<string, Socket>();

        private const int BUFFER_SIZE = 2048;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];

        readonly static string connectionString = GetConnectionString("Configurations.xml");
        readonly static int port = 5050;

        public ChatRoomServer()
        {
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            _serverSocket.Listen(0);
            _serverSocket.BeginAccept(AcceptCallback, null);
        }

        public static void Start()
        {
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            _serverSocket.Listen(0);
            _serverSocket.BeginAccept(AcceptCallback, null);

            Console.WriteLine("Server started.");
            Console.ReadLine();
        }

        private static string GetConnectionString(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("Parameter file name cannot be null.", nameof(fileName));
            }

            using (StreamReader reader = new StreamReader(fileName))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Configurations));
                Configurations configuration = (Configurations)serializer.Deserialize(reader);
                return configuration.ConnectionString;
            }
        }

        private static string GetKeyByValue(Socket socket)
        {
            var nickname = _bannedUsers.FirstOrDefault(onlineUser => onlineUser.Value.Equals(socket)).Key;

            return nickname;
        }

        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = _serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            _clientSockets.Add(socket);

            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("New user connected, waiting for user input..");

            _serverSocket.BeginAccept(AcceptCallback, null);
        }

        private static void ExecuteCommand(Command command, Socket currentUser)
        {
            switch (command.Name)
            {
                case "Registration":
                    RegisterNewUser(command, currentUser);
                    break;
                case "Login":
                    UserLogin(command, currentUser);
                    break;
                case "Change status":
                    ChangeStatus(command, currentUser);
                    break;
                case "Logout":
                    UserLogout(command, currentUser);
                    break;
                case "Send message to one user":
                    MessageTo(command, currentUser);
                    break;
                case "Send message to all users":
                    MessageAll(command, currentUser);
                    break;
                case "Help":
                    Help(currentUser);
                    break;
            }
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket currentUser = (Socket)AR.AsyncState;
            int received;

            try
            {
                received = currentUser.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client disconnected");
                currentUser.Close();
                _clientSockets.Remove(currentUser);
                return;
            }

            byte[] receivedBuffer = new byte[received];
            Array.Copy(buffer, receivedBuffer, received);

            string input = Encoding.ASCII.GetString(receivedBuffer);

            ToXmlFile(input);

            var command = FromXmlFile();

            ExecuteCommand(command, currentUser);

            currentUser.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, currentUser);
        }


        private static void ToXmlFile(string input)
        {
            XmlDocument xdoc = new XmlDocument();

            xdoc.LoadXml(input);

            xdoc.Save("Commands.xml");
        }

        private static Command FromXmlFile()
        {
            using (StreamReader reader = new StreamReader("Commands.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Command));

                Command command = (Command)serializer.Deserialize(reader);
                return command;
            }
        }

        private static void RegisterNewUser(Command command, Socket currentUser)
        {
            try
            {
                var nickname = command.GetCommandParameterByName("Nickname");
                var password = command.GetCommandParameterByName("Password");

                string response;

                if (!_onlineUsers.ContainsKey(nickname.Text))
                {
                    _onlineUsers.TryAdd(nickname.Text, currentUser);
                }
                else
                {
                    response = "Unsuccessful registration, user with this nickname already exists.";
                }

                if (command.Parameters.Count != 2)
                {
                    response = "Invalid input for registration, enter -h for help.";
                }
                else
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        var newUser = new UserBuilder();
                        var user = newUser.SetNickname(nickname.Text)
                                            .SetPassword(password.Text)
                                            .SetStatusId(connection.GetStatusId(Status.Online))
                                            .SetLastAvailable()
                                            .NewUser();

                        if (connection.Register(user))
                        {
                            response = $"Successfully registered user {user.Nickname}";
                        }
                        else
                        {
                            response = "Unsuccessful registration, user with this nickname already exists.";
                        }
                    }
                }

                byte[] bytes = Encoding.ASCII.GetBytes(response);

                currentUser.Send(bytes);
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void UserLogin(Command command, Socket currentUser)
        {
            try
            {
                var nickname = command.GetCommandParameterByName("Nickname");
                var password = command.GetCommandParameterByName("Password");

                string response;

                if (_onlineUsers.ContainsKey(nickname.Text))
                {
                    response = "Already logged in.";
                }
                else
                {
                    if (command.Parameters.Count != 2)
                    {
                        response = "Invalid input, enter -h for help.";
                    }
                    else
                    {
                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            var user = connection.GetByNickname(nickname.Text);

                            if (user.BannedUntil > DateTime.Now)
                            {
                                response = $"You cannot log in until {user.BannedUntil}";
                            }
                            else
                            {
                                if (connection.Login(user, password.Text))
                                {
                                    _onlineUsers.TryAdd(nickname.Text, currentUser);

                                    response = "Successfully logged in";
                                }
                                else
                                {
                                    user.FailedLoginAttempts++;

                                    connection.UpdateFailedLoginAttempts(user, user.FailedLoginAttempts);

                                    if (user.FailedLoginAttempts >= 3)
                                    {
                                        connection.Ban(user);
                                        _bannedUsers.TryAdd(user.Nickname, currentUser);
                                        response = "Three failed attempts for log in, you have been banned, try again later.";
                                    }
                                    else
                                    {
                                        response = "Unsuccessful login.";
                                    }
                                }
                            }
                        }
                    }
                }

                byte[] bytes = Encoding.ASCII.GetBytes(response);
                currentUser.Send(bytes);
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void ChangeStatus(Command command, Socket currentUser)
        {
            try
            {
                var nickname = command.GetCommandParameterByName("Nickname");
                var status = command.GetCommandParameterByName("Status");

                string response;

                if (command.Parameters.Count != 2)
                {
                    response = "Invalid input, enter -h for help.";
                }
                else
                {

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        var user = connection.GetByNickname(nickname.Text);

                        if (!Enum.TryParse(status.Text, out Status result))
                        {
                            response = "Invalid type of status.";

                        }

                        connection.ChangeStatus(user, result);
                        response = $"Successfully changed status to {result}.";
                    }
                }

                byte[] bytes = Encoding.ASCII.GetBytes(response);

                currentUser.Send(bytes);
                _onlineUsers.TryRemove(nickname.Text, out currentUser);
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void MessageAll(Command command, Socket currentUser)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    var message = command.GetCommandParameterByName("Message");
                    string response = "";

                    if (!_bannedUsers.IsEmpty)
                    {
                        var userNickname = GetKeyByValue(currentUser);
                        var bannedUser = connection.GetByNickname(userNickname);

                        if (bannedUser.FailedLoginAttempts == 3)
                        {
                            response = $"You cannot send messages, banned until {bannedUser.BannedUntil}";
                        }
                    }
                    else
                    {
                        response = "Message sent to all available users.";

                        byte[] messageBytes = Encoding.ASCII.GetBytes(message.Text);

                        foreach (var user in _onlineUsers)
                        {
                            if (user.Value != currentUser)
                            {
                                user.Value.Send(messageBytes);
                            }
                        }
                    }

                    byte[] bytes = Encoding.ASCII.GetBytes(response);

                    currentUser.Send(bytes);
                }
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void MessageTo(Command command, Socket currentUser)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    var nickname = command.GetCommandParameterByName("Nickname");
                    var message = command.GetCommandParameterByName("Message");

                    string response = "";

                    if (!_bannedUsers.IsEmpty)
                    {
                        var userNickname = GetKeyByValue(currentUser);
                        var bannedUser = connection.GetByNickname(userNickname);

                        if (bannedUser.FailedLoginAttempts == 3)
                        {
                            response = $"You cannot send messages, banned until {bannedUser.BannedUntil}";
                        }
                    }
                    else if (!_onlineUsers.ContainsKey(nickname.Text))
                    {
                        response = "This user is unavailable";
                    }
                    else
                    {
                        byte[] messageBytes = Encoding.ASCII.GetBytes(message.Text);

                        _onlineUsers[nickname.Text].Send(messageBytes);

                        response = $"Message sent to {nickname.Text}";
                    }

                    byte[] bytes = Encoding.ASCII.GetBytes(response);
                    currentUser.Send(bytes);
                }
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void UserLogout(Command command, Socket currentUser)
        {
            try
            {
                var nickname = command.GetCommandParameterByName("Nickname");

                string response;

                if (command.Parameters.Count != 1)
                {
                    response = "Invalid input for logout, enter -h for help.";
                }
                else
                {
                    _onlineUsers.Remove(nickname.Text, out currentUser);

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        var user = connection.GetByNickname(nickname.Text);

                        connection.Logout(user);

                        response = $"Successfully logged out";
                    }

                    byte[] bytes = Encoding.ASCII.GetBytes(response);

                    currentUser.Send(bytes);
                }
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void Help(Socket currentUser)
        {
            string response = @"-r -> Register as new user with nickname and password
-l -> Login with nickname and password if you have existing account
-s -> Change status by entering nickname and new status
-d -> Logout by entering nickname
-m -> Send message to specific user by entering user's nickname and the message
-a -> Send message to all users by entering the message
-h -> See all possible commands for help";

            byte[] bytes = Encoding.ASCII.GetBytes(response);

            currentUser.Send(bytes);
        }
    }
}


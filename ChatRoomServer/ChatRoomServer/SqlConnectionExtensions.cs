using Dapper;
using DevOne.Security.Cryptography.BCrypt;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace ChatRoomServer
{
    public static class SqlConnectionExtensions
    {
        public static T GetById<T>(this SqlConnection connection, string id)
        {
            return connection.QueryFirstOrDefault<T>($"SELECT * FROM { typeof(T).Name } WHERE Id = @Id", new { Id = id });
        }

        public static Guid GetStatusId(this SqlConnection connection, Status status)
        {
            var sqlQuery = "SELECT Id FROM [Status] WHERE Name = @statusName";

            return connection.QueryFirstOrDefault<Guid>(sqlQuery, new { statusName = status.ToString() });
        }

        public static bool IsUnique(this SqlConnection connection, User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var sqlQuery = @"SELECT Nickname FROM [User] WHERE Nickname = @nickname";
            var nickname = connection.QueryFirstOrDefault<string>(sqlQuery, new { nickname = user.Nickname });

            return nickname == default ? true : false;
        }

        public static User GetByNickname(this SqlConnection connection, string nickname)
        {
            if (nickname == null)
            {
                throw new ArgumentNullException(nameof(nickname));
            }
            else
            {
                var sqlQuery = @"SELECT * FROM [User] WHERE Nickname = @name";
                return connection.QueryFirstOrDefault<User>(sqlQuery, new { name = nickname });
            }
        }

        public static bool Register(this SqlConnection connection, User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (connection.IsUnique(user))
            {
                var sqlQuery = @"INSERT INTO [User] (Id, Nickname, Password, StatusId, LastAvailable, RegistrationDate) 
                             VALUES (@Id, @Nickname, @Password, @StatusId, @LastAvailable, @RegistrationDate)";

                DynamicParameters parameter = new DynamicParameters();

                parameter.Add("@Id", user.Id);
                parameter.Add("@Nickname", user.Nickname);

                string salt = BCryptHelper.GenerateSalt();
                string hashedPassword = BCryptHelper.HashPassword(user.Password, salt);

                parameter.Add("@Password", hashedPassword);
                parameter.Add("@StatusId", user.StatusId);
                parameter.Add("@LastAvailable", user.LastAvailable);
                parameter.Add("@RegistrationDate", user.RegistrationDate);

                connection.Query(sqlQuery, parameter);

                return true;
            }

            return false;
        }

        public static bool Login(this SqlConnection connection, User user, string password)
        {
            var sqlQuery = @"SELECT Password FROM [User] WHERE Nickname = @user";

            var hashedPassword = connection.QueryFirstOrDefault<string>(sqlQuery, new { user = user.Nickname });

            if (hashedPassword == null)
            {
                Console.WriteLine("Invalid username, please try again.");

                return false;
            }

            if (BCryptHelper.CheckPassword(password, hashedPassword.ToString()))
            {
                Console.WriteLine($"{user.Nickname} successfully logged in.");
                return true;
            }
            else
            {
                Console.WriteLine("Invalid password, please try again.");

                return false;
            }
        }

        public static void ChangeStatus(this SqlConnection connection, User user, Status newStatus)
        {
            var sqlQuery = @"UPDATE [User]
                            SET [StatusId] = @status, 
                                [LastAvailable] = @time
                            WHERE [Nickname] = @name";

            DynamicParameters parameter = new DynamicParameters();

            parameter.Add("@name", user.Nickname);
            parameter.Add("@status", connection.GetStatusId(newStatus));
            parameter.Add("@time", DateTime.Now);

            connection.Query(sqlQuery, parameter);
        }

        public static void Logout(this SqlConnection connection, User user)
        {
            if (user == null)
            {
                throw new ArgumentException("User cannot be null.", nameof(user));
            }

            var sqlQuery = @"UPDATE [User]
                            SET [LastAvailable] = @time,
                                [StatusId] = (SELECT Id FROM [Status] WHERE [Name] = 'Offline')
                            WHERE [Nickname] = @name";

            DynamicParameters parameter = new DynamicParameters();

            parameter.Add("@time", DateTime.Now);
            parameter.Add("@name", user.Nickname);

            connection.Query(sqlQuery, parameter);
        }

        public static void DisconnectUsers(this SqlConnection connection, IEnumerable<User> users)
        {
            foreach (var user in users)
            {
                connection.Logout(user);
            }
        }

        public static void Ban(this SqlConnection connection, User user)
        {
            var sqlQuery = @"UPDATE [User]
                             SET [StatusId] = @status,
                                 [LastAvailable] = @time,
                                 [BannedUntil] = @ban    
                            WHERE [Nickname] = @name";

            DynamicParameters parameter = new DynamicParameters();

            parameter.Add("@name", user.Nickname);
            parameter.Add("@status", connection.GetStatusId(Status.Offline));
            parameter.Add("@time", DateTime.Now);

            TimeSpan banTime = new TimeSpan(2, 0, 0);
            var bannedUntil = DateTime.Now.Add(banTime);

            parameter.Add("@ban", bannedUntil);

            connection.Query(sqlQuery, parameter);

            Console.WriteLine($"Successfully banned user {user.Nickname}.");
        }

        public static void Ban(this SqlConnection connection, IEnumerable<User> users)
        {
            foreach (var user in users)
            {
                connection.Ban(user);
            }
        }
        
        public static void UpdateFailedLoginAttempts(this SqlConnection connection, User user, int loginAttempts)
        {
            var sqlQuery = @"UPDATE [User]
                             SET [FailedLoginAttempts] = @attempts 
                            WHERE [Nickname] = @name";

            DynamicParameters parameter = new DynamicParameters();

            parameter.Add("@name", user.Nickname);
            parameter.Add("@attempts", loginAttempts);

            connection.Query(sqlQuery, parameter);
        }

        public static void Delete(this SqlConnection connection, User user)
        {
            var sqlQuery = @"DELETE FROM [User]                           
                            WHERE [Nickname] = @name";

            DynamicParameters parameter = new DynamicParameters();

            parameter.Add("@name", user.Nickname);

            connection.Query(sqlQuery, parameter);
            Console.WriteLine($"Successfully deleted user {user.Nickname}.");
        }

        public static void Delete(this SqlConnection connection, IEnumerable<User> users)
        {
            foreach (var user in users)
            {
                connection.Delete(user);
            }
        }
    }
}

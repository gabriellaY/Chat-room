using System;
using System.Collections.Generic;
using System.Text;

namespace ChatRoomServer
{
    public class UserBuilder
    {
        private readonly User _user = new User();

        public UserBuilder()
        {
            _user.Id = Guid.NewGuid();

            _user.RegistrationDate = DateTime.Now;

            _user.FailedLoginAttempts = 0;
        }

        public UserBuilder SetNickname(string nickname)
        {
            try
            {
                _user.Nickname = nickname;
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine(e.Message);
            }

            return this;
        }

        public UserBuilder SetPassword(string password)
        {
            try
            {
                _user.Password = password;
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine(e.Message);
            }

            return this;
        }

        public UserBuilder SetStatusId(Guid statusId)
        {
            _user.StatusId = statusId;

            return this;
        }

        public UserBuilder SetLastAvailable()
        {
            _user.LastAvailable = DateTime.Now;

            return this;
        }

        public User NewUser()
        {
            return _user;
        }

        public UserBuilder Clear()
        {
            return new UserBuilder();
        }
    }
}

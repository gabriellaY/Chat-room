using System;
using System.Collections.Generic;
using System.Text;

namespace ChatRoomServer
{
    public class User
    {
        private string _nickname;
        private string _password;

        public Guid Id { get; set; }

        public string Nickname
        {
            get => _nickname;
            set => _nickname = !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentNullException("The nickname cannot be null or whitespace.", nameof(value));
        }

        public string Password
        {
            get => _password;
            set => _password = !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentNullException("The password cannot be null or whitespace.", nameof(value));
        }

        public Guid StatusId { get; set; }

        public DateTime LastAvailable { get; set; }

        public int FailedLoginAttempts { get; set; }

        public DateTime BannedUntil { get; set; }

        public DateTime RegistrationDate { get; set; }
    }
}

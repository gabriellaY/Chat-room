using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChatRoomServer
{
    public static class CommandExtensions
    {
        public static Parameter GetCommandParameterByName(this Command command, string parameterName)
        {
            if (parameterName == null)
            {
                throw new ArgumentNullException("Command name cannot be null.", nameof(parameterName));
            }
            else
            {
                return command.Parameters.FirstOrDefault(parameter => string.Equals(parameter.Name, parameterName));
            }
        }
    }
}

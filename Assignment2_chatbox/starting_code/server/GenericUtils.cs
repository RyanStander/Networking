using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using shared;

namespace server
{
    public static class GenericUtils
    {
        public static bool IsUsernameUnique(Dictionary<string, TcpClient> clients, string chosenUsername)
        {
            //Check if the username is already taken
            return clients.All(client => !client.Key.Equals(chosenUsername));
        }

        public static bool IndexIsInRange(int index, int max)
        {
            return (index >= 0 && index < max);
        }

        #region Sending Messages

        //Note: currently this is only for strings, but i intend to add more variations when necessary.

        #region SendMessageToAll

        public static void SendMessageToAll(Dictionary<string, TcpClient> clients, string message)
        {
            foreach (var client in clients.Values)
            {
                SendMessageToClient(client, message);
            }
        }

        #endregion

        #region SendMessageToClient

        public static void SendMessageToClient(TcpClient client, string message)
        {
            var stream = client.GetStream();
            StreamUtil.Write(stream, Encoding.UTF8.GetBytes(message));
        }

        #endregion

        #endregion
    }
}
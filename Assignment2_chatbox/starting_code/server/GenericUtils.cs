using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using shared;

namespace server
{
    public static class GenericUtils
    {
        public static bool IsUsernameUnique(List<UserData> clients, string chosenUsername)
        {
            //Check if the username is already taken
            return clients.All(client => !client.Username.Equals(chosenUsername));
        }

        #region Sending Messages

        //Note: currently this is only for strings, but i intend to add more variations when necessary.

        #region SendMessageToAll

        public static void SendMessageToAll(List<UserData> clients, string message)
        {
            foreach (var client in clients)
            {
                SendMessageToClient(client.Client, message);
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
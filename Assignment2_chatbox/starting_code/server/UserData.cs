using System.Net.Sockets;

namespace server
{
    /// <summary>
    /// Class that holds that data of each user
    /// </summary>
    public class UserData
    {
        public TcpClient Client;
        public string Username;
        public float TimedOutDuration;

        public UserData(TcpClient tcpClient,string username)
        {
            Client = tcpClient;
            Username = username;
        }
    }
}
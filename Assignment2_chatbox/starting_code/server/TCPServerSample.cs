using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using shared;

namespace server
{
    /// <summary>
    /// This class implements a simple concurrent TCP Echo server.
    /// </summary>
    public class TcpServerSample
    {
        private static TcpListener listener;
        private static Dictionary<string, TcpClient> clients;

        private const int ServerPort = 55555;

        private static readonly Random RandomNumberGenerator = new Random();

        //Fun random usernames from xbox
        private static readonly string[] RandomUsernames =
        {
            "GingerEmpress", "LowercaseBeef", "BeefCurtain", "TinklyDiamond", "LintyStarfish", "LuxuriousSolid",
            "SinlessNutria", "GalacticPanda", "IAmNotAFish", "IrksomeSquid", "PartyMcFly", "SonicTheHedgeFund",
            "SuspiciousSquid"
        };

        private static readonly string[] RandomWelcomeMessages =
        {
            "Welcome, {0} We hope you've brought pizza.", "Good to see you, {0}", "A wild {0} appeared.",
            "{0} just showed up", "Glad you're here, {0}.", "{0} is here.", "{0} just slid into the server.",
            "Yay you made it, {0}!", "{0} hopped into the server", "Welcome {0}. Say hi!", "{0} just landed."
        };

        public static void Main(string[] args)
        {
            try
            {
                Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void Run()
        {
            Console.WriteLine("Server started on port " + ServerPort);

            listener = new TcpListener(IPAddress.Any, ServerPort);
            listener.Start();

            clients = new Dictionary<string, TcpClient>();

            while (true)
            {
                ProcessNewClients();
                ProcessExistingClients();
                CleanupFaultyClients();

                Thread.Sleep(100);
            }
        }

        private static void ProcessNewClients()
        {
            //check if a client is trying to connect, if so connect them.
            while (listener.Pending())
            {
                var newClient = listener.AcceptTcpClient();
                var chosenUsername = GenerateRandomUsername();

                GenericUtils.SendMessageToAll(clients, string.Format(
                    RandomWelcomeMessages[RandomNumberGenerator.Next(0, RandomWelcomeMessages.Length)],
                    chosenUsername));

                clients.Add(chosenUsername, newClient);

                GenericUtils.SendMessageToClient(newClient, "You joined the server as " + chosenUsername);
                Console.WriteLine("Accepted new client.");
            }
        }

        private static void ProcessExistingClients()
        {
            //Process clients messages if they send one
            foreach (var client in clients)
            {
                if (client.Value.Available == 0) continue;

                var stream = client.Value.GetStream();
                //Get the data being sent
                var receivedMessage = Encoding.UTF8.GetString(StreamUtil.Read(stream));

                if (receivedMessage.StartsWith("/"))
                {
                    //If we happen to modify the dictionary, we want to break out of the for loop as it would cause errors.
                    if (CommandManager.ProcessCommand(ref clients, client, receivedMessage))
                        break;
                }
                else
                    SendChatMessage(client, receivedMessage);
            }
        }

        private static void CleanupFaultyClients()
        {
            List<string> clientsToDelete = new();

            foreach (var client in clients)
            {
                if (!IsConnected(client.Value.Client))
                {
                    clientsToDelete.Add(client.Key);
                }
            }

            foreach (var badClient in clientsToDelete)
            {
                clients.Remove(badClient);
                Console.WriteLine("Removed bad client: " + badClient);
            }
        }

        private static void SendChatMessage(KeyValuePair<string, TcpClient> client, string receivedMessage)
        {
            var timeStamp = "[" + DateTime.Now.ToString("HH:mm") + "]";

            var output = timeStamp + client.Key + ": " + receivedMessage;

            GenericUtils.SendMessageToAll(clients, output);
        }

        private static string GenerateRandomUsername()
        {
            var index = RandomNumberGenerator.Next(0, RandomUsernames.Length);
            var chosenUsername = RandomUsernames[index];

            var usernameNumber = 1;

            while (!GenericUtils.IsUsernameUnique(clients, chosenUsername + usernameNumber))
            {
                usernameNumber += 1;
            }

            return chosenUsername + usernameNumber;
        }

        private static bool IsConnected(Socket socket)
        {
            try
            {
                //checks if there is a response from the socket
                return !(socket.Available == 0 && socket.Poll(1, SelectMode.SelectRead));
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}
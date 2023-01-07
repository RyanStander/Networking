using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        private static readonly Dictionary<string, float> timedOutClients = new();

        //How long until a client will be deleted
        private const float TimeOutLimit = 3;
        private const int CheckIntervalsInMilliseconds = 500;

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

        #region Processing New Clients

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

        #endregion

        #region Processing Existing Clients

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

        private static void SendChatMessage(KeyValuePair<string, TcpClient> client, string receivedMessage)
        {
            var timeStamp = "[" + DateTime.Now.ToString("HH:mm") + "]";

            var output = timeStamp + client.Key + ": " + receivedMessage;

            GenericUtils.SendMessageToAll(clients, output);
        }

        #endregion

        #region Cleanup Faulty Clients

        private static async void CleanupFaultyClients()
        {
            await HeartbeatForTimeout(CheckIntervalsInMilliseconds);
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

        private static void CheckForTimeout()
        {
            foreach (var client in clients)
            {
                if (!IsConnected(client.Value.Client))
                {
                    if (timedOutClients.ContainsKey(client.Key))
                    {
                        //adds the time passed if the dict already exists
                        timedOutClients[client.Key] += (float)CheckIntervalsInMilliseconds / 1000;
                    }

                    timedOutClients.Add(client.Key, 0);
                }
                else
                {
                    if (timedOutClients.ContainsKey(client.Key))
                    {
                        timedOutClients.Remove(client.Key);
                    }
                }
            }

            if (timedOutClients.Count == 0)
                return;

            var timedOutClientsToRemove = new List<string>();

            foreach (var timedOutClient in
                     timedOutClients.Where(timedOutClient => timedOutClient.Value >= TimeOutLimit))
            {
                clients.Remove(timedOutClient.Key);
                timedOutClientsToRemove.Add(timedOutClient.Key);
                Console.WriteLine("Client disconnected due to timeout: " + timedOutClient.Key);
            }

            foreach (var clientsToRemove in timedOutClientsToRemove)
            {
                timedOutClients.Remove(clientsToRemove);
            }
        }

        private static async Task HeartbeatForTimeout(int milliseconds)
        {
            await Task.Delay(milliseconds);
            CheckForTimeout();
        }

        #endregion
    }
}
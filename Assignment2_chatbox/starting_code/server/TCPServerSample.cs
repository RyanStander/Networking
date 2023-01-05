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
	class TcpServerSample
	{
		/**
	 * This class implements a simple concurrent TCP Echo server.
	 * Read carefully through the comments below.
	 */
		public static void Main (string[] args)
		{
			Console.WriteLine("Server started on port 55555");

			var listener = new TcpListener (IPAddress.Any, 55555);
			listener.Start ();

			var clients = new List<TcpClient>();

			while (true)
			{
				//First big change with respect to example 001
				//We no longer block waiting for a client to connect, but we only block if we know
				//a client is actually waiting (in other words, we will not block)
				//In order to serve multiple clients, we add that client to a list
				while (listener.Pending()) { 
					clients.Add(listener.AcceptTcpClient());
					Console.WriteLine("Accepted new client.");
				}

				//Second big change, instead of blocking on one client, 
				//we now process all clients IF they have data available
				foreach (var client in clients)
				{
					if (client.Available == 0) continue;

					SendMessage(client);
				}

				//Although technically not required, now that we are no longer blocking, 
				//it is good to cut your CPU some slack
				Thread.Sleep(100);
			}
		}
		
		private static void SendMessage(TcpClient client)
		{
			var stream = client.GetStream();

			var timeStamp = "["+DateTime.Now.ToString("HH:mm")+"]";
			var userName = client.Client.RemoteEndPoint.ToString();
			
			//Get the data being sent
			var receivedMessage =Encoding.UTF8.GetString(StreamUtil.Read(stream));

			var output = timeStamp + userName + ": " + receivedMessage;
			
			Console.WriteLine(output);

			StreamUtil.Write(stream, Encoding.UTF8.GetBytes(output));
		}
		
	}
}



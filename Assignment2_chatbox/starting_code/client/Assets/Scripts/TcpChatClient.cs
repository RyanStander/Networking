using shared;
using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/**
 * Assignment 2 - Starting project.
 * 
 * @author J.C. Wichman
 * @editor Ryan Stander
 */
public class TcpChatClient : MonoBehaviour
{
    [SerializeField] private PanelWrapper panelWrapper = null;
    [SerializeField] private string hostname = "localhost";
    [SerializeField] private int port = 55555;

    private TcpClient client;

    private void Start()
    {
        panelWrapper.OnChatTextEntered += OnTextEntered;
        ConnectToServer();
    }

    private void ConnectToServer()
    {
        try
        {
			client = new TcpClient();
            client.Connect(hostname, port);
            panelWrapper.ClearOutput();
            panelWrapper.AddOutput("Connected to server.");
        }
        catch (Exception e)
        {
            panelWrapper.AddOutput("Could not connect to server:");
            panelWrapper.AddOutput(e.Message);
        }
    }

    private void OnTextEntered(string pInput)
    {
        if (string.IsNullOrEmpty(pInput)) return;

        panelWrapper.ClearInput();

		try 
        {
			//echo client - send one, expect one (hint: that is not how a chat works ...)
			var outBytes = Encoding.UTF8.GetBytes(pInput);
			StreamUtil.Write(client.GetStream(), outBytes);

			var inBytes = StreamUtil.Read(client.GetStream());
            var inString = Encoding.UTF8.GetString(inBytes);
            panelWrapper.AddOutput(inString);
		} 
        catch (Exception e) 
        {
            panelWrapper.AddOutput(e.Message);
			//for quicker testing, we reconnect if something goes wrong.
			client.Close();
			ConnectToServer();
		}
    }

}


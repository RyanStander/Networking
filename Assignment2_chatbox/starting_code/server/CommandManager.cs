using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace server
{
    public static class CommandManager
    {
        private static readonly char[] BannedCharacters =
        {
            ' ', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '+', '=', '[', ']', '{', '}', '<', '>', ',', '.',
            '/', '?', '~', '`', ':', ';'
        };

        #region Command arrays

        //We hold these in arrays for easy modification

        private static readonly string[] SetNameCommands = { "/setname", "/sn" };
        private static readonly string[] ListCommands = { "/list", "/ls" };
        private static readonly string[] HelpCommands = { "/help", "/h" };
        private static readonly string[] WhisperCommands = { "/message", "/msg", "/whisper", "/w" };

        private static readonly string CommandExplanation =
            $"/setname <new_name>: change your name to the new specified name \n" +
            $"/list: get a list of all current players connected to your server \n" +
            $"/help: get a list of all commands that can be performed \n";

        #endregion


        //returns whether it changed the ref
        public static bool ProcessCommand(ref List<UserData> clients,
            UserData clientThatRanCommand, string command)
        {
            if (MatchesCommand(SetNameCommands, command))
            {
                return SetName(ref clients, clientThatRanCommand, command);
            }

            if (MatchesCommand(ListCommands, command))
            {
                ListUsers(clients, clientThatRanCommand);
                return false;
            }

            if (MatchesCommand(HelpCommands, command))
            {
                GetAllCommands(clientThatRanCommand);
                return false;
            }

            if (MatchesCommand(WhisperCommands, command))
            {
                MessageUser(clients, clientThatRanCommand, command);
                return false;
            }

            return false;
        }

        private static bool MatchesCommand(string[] commands, string enteredCommand)
        {
            return commands.Any(command => enteredCommand.ToLower().StartsWith(command));
        }

        private static bool SetName(ref List<UserData> clients,
            UserData clientThatRanCommand, string command)
        {
            var index = command.IndexOf(" ");

            if (index == -1)
            {
                GenericUtils.SendMessageToClient(clientThatRanCommand.Client, "Name cannot be empty");
                return false;
            }

            var newName = command.Substring(index + 1).ToLower();

            foreach (var bannedChar in BannedCharacters)
            {
                if (!newName.Contains(bannedChar)) continue;
                GenericUtils.SendMessageToClient(clientThatRanCommand.Client,
                    "Invalid name, your name cannot contain " + bannedChar);
                return false;
            }

            if (clients.Any(client => client.Username.Equals(newName)))
            {
                GenericUtils.SendMessageToClient(clientThatRanCommand.Client, "This username is already taken");
                return false;
            }

            GenericUtils.SendMessageToAll(clients,
                clientThatRanCommand.Username + " has changed their name to " + newName);
            clientThatRanCommand.Username = newName;

            return true;
        }

        private static void ListUsers(List<UserData> clients,
            UserData clientThatRanCommand)
        {
            var output = clients.Aggregate($"Players currently connected: \n",
                (current, tcpClient) => current + (tcpClient.Username + $"\n"));

            //Remove the newline
            output = output.Substring(0, output.Length - 1);

            GenericUtils.SendMessageToClient(clientThatRanCommand.Client, output);
        }

        private static void GetAllCommands(UserData clientThatRanCommand)
        {
            GenericUtils.SendMessageToClient(clientThatRanCommand.Client, CommandExplanation);
        }

        private static void MessageUser(List<UserData> clients,
            UserData clientThatRanCommand, string command)
        {
            var index = command.IndexOf(" ");

            if (index == -1)
            {
                GenericUtils.SendMessageToClient(clientThatRanCommand.Client, "You must enter a name");
                return;
            }

            var userName = command.Substring(index + 1);
            var messageStartIndex = userName.IndexOf(" ");

            if (messageStartIndex == -1 && messageStartIndex != userName.Length - 1)
            {
                GenericUtils.SendMessageToClient(clientThatRanCommand.Client, "Message cannot be empty");
                return;
            }

            var message = userName.Substring(messageStartIndex + 1);
            userName = userName.Substring(0, messageStartIndex);

            foreach (var client in clients)
            {
                if (!client.Username.Equals(userName))
                    continue;

                var timeStamp = "[" + DateTime.Now.ToString("HH:mm") + "]";

                GenericUtils.SendMessageToClient(clientThatRanCommand.Client,
                    timeStamp + clientThatRanCommand.Username + ": You whisper to <" + userName + "> " + message);

                GenericUtils.SendMessageToClient(client.Client,
                    timeStamp + clientThatRanCommand.Username + " whispers: " + message);
                return;
            }

            GenericUtils.SendMessageToClient(clientThatRanCommand.Client, "Could not find user: " + userName);
        }
    }
}
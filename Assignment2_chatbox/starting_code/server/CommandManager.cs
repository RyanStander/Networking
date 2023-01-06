using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using shared;

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
        private static readonly string[] HelpCommands = { "/help" };

        #endregion


        //returns whether it changed the ref
        public static bool ProcessCommand(ref Dictionary<string, TcpClient> clients,
            KeyValuePair<string, TcpClient> clientThatRanCommand, string command)
        {
            if (MatchesCommand(SetNameCommands, command))
            {
                return SetName(ref clients, clientThatRanCommand, command);
            }
            else if (MatchesCommand(ListCommands, command))
            {
                return false;
            }
            else if (MatchesCommand(HelpCommands, command))
            {
                return false;
            }
            else
            {
                return false;
            }
        }

        private static bool MatchesCommand(string[] commands, string enteredCommand)
        {
            return commands.Any(command => enteredCommand.ToLower().StartsWith(command));
        }

        private static bool SetName(ref Dictionary<string, TcpClient> clients,
            KeyValuePair<string, TcpClient> clientThatRanCommand, string command)
        {
            var index = command.IndexOf(" ");

            if (index == -1)
            {
                GenericUtils.SendMessageToClient(clientThatRanCommand.Value, "Name cannot be empty");
                return false;
            }

            var newName = command.Substring(index + 1).ToLower();

            foreach (var bannedChar in BannedCharacters)
            {
                if (!newName.Contains(bannedChar)) continue;
                GenericUtils.SendMessageToClient(clientThatRanCommand.Value,
                    "Invalid name, your name cannot contain " + bannedChar);
                return false;
            }

            if (clients.Any(client => client.Key.Equals(newName)))
            {
                GenericUtils.SendMessageToClient(clientThatRanCommand.Value, "This username is already taken");
                return false;
            }


            GenericUtils.SendMessageToClient(clientThatRanCommand.Value, "Changed username to " + newName);
            clients.Remove(clientThatRanCommand.Key);
            
            GenericUtils.SendMessageToAll(clients,clientThatRanCommand.Key + " has changed their name to " + newName);
            
            clients.Add(newName, clientThatRanCommand.Value);
            return true;
        }
    }
}
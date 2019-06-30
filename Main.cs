using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RabbitMQ.Client;
using RabbitMQ;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Discord.Net;
using System.Text.RegularExpressions;

namespace PhaseBridge
{
    class PhaseBridge
    {
        static void Main(string[] args)
            => new PhaseBridge().Initialize().GetAwaiter().GetResult();

        public static DiscordSocketClient client;
        public static Config config;
        public static Rabbit connection;
        public static SocketGuild guild;
        internal bool enabled = true;

        public async Task Initialize()
        {
            config = Config.Read();
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug

            });
            await client.LoginAsync(TokenType.Bot, config.BotToken);
            await client.StartAsync();

            client.MessageReceived += OnMessage;
            client.Ready += OnReady;
            client.Log += OnLog;

            // added this halfway through so you know, big brain
            guild = client.GetGuild(config.GuildID);

            await Task.Delay(-1);
        }

        private async Task OnLog(LogMessage args)
        {
            Console.WriteLine(args.Message);
        }

        private async Task OnReady()
        {
            if (connection != null)
                connection.Dispose();

            connection = new Rabbit(config.hostName, config.username, config.password, config.vhost);
            connection.NewMessage += OnPhaseMessage;

            JObject json = new JObject();
            json.Add("token", config.token);
            json.Add("type", "started");

            if (connection != null)
                connection.Publish(json.ToString());

            await client.GetGuild(config.GuildID).GetTextChannel(config.ChannelID).SendMessageAsync("Bridge connected.");
        }

        private void OnPhaseMessage(object sender, Message args)
        {
            var channel = client.GetGuild(config.GuildID).GetTextChannel(config.ChannelID);

            try
            {
                dynamic message = JObject.Parse(args.content);
                if (message.token == config.token)
                {
                    if (message.type == "chat")
                    {
                        try
                        {
                            string content = message.content;
                            if (content.StartsWith("[Phase]"))
                            {
                                string ReplaceString(Capture match)
                                    => match.Value.Substring(match.Value.IndexOf(':') + 1).TrimEnd(']');
                                Regex ColorRegex = new Regex("\\[c\\/\\w{3,6}:\\w+\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                                content = ColorRegex.Replace(content, ReplaceString);

                                string name = message.name;
                                content = content.Split('>')[1];
                                channel.SendMessageAsync(string.Format("{0}:{1}", name, content.Substring(1)));

                            }
                        }
                        catch (RateLimitedException ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async Task OnMessage(SocketMessage args)
        {
            if (args.Author.IsBot || args.Author.IsWebhook)
                return;

            if (args.Channel.Id == config.ChannelID)
            {
                foreach (string str in config.BlockedPrefixes)
                {
                    if (args.Content.StartsWith(str))
                        return;
                }

                var parameters = args.Content.Split(' ');
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].StartsWith("<@") || parameters[i].StartsWith("<#"))
                        parameters[i] = null;
                }

                Console.WriteLine(string.Join(" ", parameters));
                JObject json = new JObject();
                json.Add("token", config.token);
                json.Add("type", "player_chat");
                json.Add("name", client.GetGuild(config.GuildID).GetUser(args.Author.Id).Nickname ?? args.Author.Username);
                json.Add("accountName", args.Author.Username);
                json.Add("message", string.Join(" ", parameters));
                json.Add("id", args.Author.Id);

                if (connection != null)
                    connection.Publish(json.ToString());
            }
        }
    }
}

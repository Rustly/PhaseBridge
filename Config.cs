using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhaseBridge
{
    public class Config
    {
        private static string filepath = Path.Combine(Environment.CurrentDirectory, "PhaseBridge.json");
        public string token = "";
        public string hostName = "t.dark-gaming.com";
        public string exchangeName = "";
        public string username = "";
        public string password = "";
        public string vhost = "phase";
        public ulong ChannelID = 420;
        public ulong GuildID = 1337;
        public string BotToken = "mr krabs plankton has the krabby patty secret formula!";
        public List<string> BlockedPrefixes = new List<string>()
        {
            "!"
        };

        private static void Write(Config file)
        {
            try
            {
                File.WriteAllText(filepath, JsonConvert.SerializeObject(file, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception at 'Config.Write': {0}\nCheck logs for details.",
                        ex.Message);
                Console.WriteLine(ex.ToString());
            }
        }

        public static Config Read()
        {

            Config file = new Config();
            try
            {
                if (!File.Exists(filepath))
                {
                    Write(file);
                }
                else
                {
                    file = JsonConvert.DeserializeObject<Config>(File.ReadAllText(filepath));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception at 'Config.Read': {0}\nCheck logs for details.",
                        ex.Message);
                Console.WriteLine(ex.ToString());
            }
            return file;
        }
    }
}

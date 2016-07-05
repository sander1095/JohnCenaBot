/* YOU ARE ALLOWED TO USE AND MODIFY THIS PROGRAM AS YOU WISH
 * AS LONG AS YOU KEEP THIS COMMENT IN THE CODE AND IF YOU DO NOT CHANGE THE SendInfo() METHOD.
 * 
 * Original bot developed by https://www.github.com/sander1095
 */

using Discord;
using Discord.Audio;
using NAudio.Wave;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Program
{
    static void Main(string[] args)
    {
        new Program().Start();
    }

    private DiscordClient Bot;
    private IAudioClient VoiceClient;
    private string ServerToJoin = "SERVER_TO_JOIN_HERE";
    private string VoiceChannelToJoin = "VOICE_CHANNEL_TO_JOIN_HERE";
    private string Token = "TOKEN_HERE";

    public void Start()
    {
        bool isBusySendingSound = false;

        Bot = new DiscordClient();

        Bot.UsingAudio(x => // Opens an AudioConfigBuilder so we can configure our AudioService
        {
            x.Mode = AudioMode.Outgoing; // Tells the AudioService that we will only be sending audio
        });

        Bot.MessageReceived += async (s, originalMessage) =>
        {
            string message = originalMessage.Message.Text.ToLower().Trim();

            if (message.StartsWith("!johncena"))
            {
                string arguments = message.Substring(9).Trim();

                string userThatCalledTheBot = originalMessage.User.Mention;
                Channel channelInWhichTheBotWasCalled = originalMessage.Channel;

                switch (arguments)
                {
                    case "": //No arguments are given, play the sound
                        if (!isBusySendingSound)
                        {
                            isBusySendingSound = true;
                            await SendAudio();
                            isBusySendingSound = false;
                        }
                        break;

                    case "help": // Display help
                        await SendHelp(userThatCalledTheBot, channelInWhichTheBotWasCalled);
                        break;

                    case "info": //Display info
                        await SendInfo(userThatCalledTheBot, channelInWhichTheBotWasCalled);
                        break;
                }
            }
        };

        Bot.ExecuteAndWait(async () =>
        {
            Console.WriteLine("Connecting...");
            await Bot.Connect(Token); //The token from your user bot
            Console.WriteLine("Connected!");

        });
    }

    private async Task SendAudio()
    {
        var voiceChannel = Bot.FindServers(ServerToJoin)
                                        .FirstOrDefault().VoiceChannels
                                        .Single(x => x.Name == VoiceChannelToJoin); // Finds the 'VoiceChannelToJoin' on the server 'ServerToJoin'

        VoiceClient = await Bot.GetService<AudioService>() // We use GetService to find the AudioService that we installed earlier. In previous versions, this was equivelent to _client.Audio()
               .Join(voiceChannel); // Join the Voice Channel, and return the IAudioClient.

        //Get the root path (TODO: MAKE THE APPLICATION WORK WITH THE .opus and .libsodium DLL's and Resource folder when PUBLISHED!)
        string rootPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
        string resourcePath = rootPath + "\\Resources";
        string[] mp3Files = Directory.GetFiles(resourcePath); //Get the Mp3Files

        //Get a random file to play
        string filePathToPlay = mp3Files[new Random().Next(mp3Files.Length)];

        //Send the audio
        var channelCount = Bot.GetService<AudioService>().Config.Channels; // Get the number of AudioChannels our AudioService has been configured to use.
        var OutFormat = new WaveFormat(48000, 16, channelCount); // Create a new Output Format, using the spec that Discord will accept, and with the number of channels that our client supports.
        using (var MP3Reader = new Mp3FileReader(filePathToPlay)) // Create a new Disposable MP3FileReader, to read audio from the filePath parameter
        using (var resampler = new MediaFoundationResampler(MP3Reader, OutFormat)) // Create a Disposable Resampler, which will convert the read MP3 data to PCM, using our Output Format
        {
            resampler.ResamplerQuality = 60; // Set the quality of the resampler to 60, the highest quality
            int blockSize = OutFormat.AverageBytesPerSecond / 50; // Establish the size of our AudioBuffer
            byte[] buffer = new byte[blockSize];
            int byteCount;

            while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0) // Read audio into our buffer, and keep a loop open while data is present
            {
                if (byteCount < blockSize)
                {
                    // Incomplete Frame
                    for (int i = byteCount; i < blockSize; i++)
                        buffer[i] = 0;
                }
                VoiceClient.Send(buffer, 0, blockSize); // Send the buffer to Discord
            }
        }

        await VoiceClient.Disconnect();
    }

    private async Task SendHelp(string caller, Channel channel)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"**Hello {caller}**. I am a __**bot**__.");
        sb.AppendLine("You called for help, so I'll give you some **commands** you can use:");

        sb.AppendLine();

        sb.AppendLine($"**!johncena**\t I'll play a *John Cena* related tune in the {VoiceChannelToJoin} channel!");
        sb.AppendLine("**!help**\t I'll tell you this information again!");
        sb.AppendLine("**!info**\t I'll give you information about me and my creator!");

        sb.AppendLine();
        sb.AppendLine("__*More will be added in the future*__");

        sb.AppendLine("I hope I was helpful!");

        await channel.SendMessage(sb.ToString());
    }

    private async Task SendInfo(string caller, Channel channel)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"**Hello {caller}**. I am a __**bot**__.");
        sb.AppendLine("You requested some info, so I'll tell you about myself!");

        sb.AppendLine();

        sb.AppendLine("I am a __**bot**__ created for your pleasure!");
        sb.AppendLine("My (original) creator is <@137703863569350656>. You can find him and his other projects on **Github**:");
        sb.AppendLine("https://www.github.com/sander1095");

        sb.AppendLine();

        sb.AppendLine("My __Source Code__ is available at: https://github.com/sander1095/JohnCenaBot . If you find any bugs, message my creator on Discord or file an issue on my repo!");

        sb.AppendLine();

        sb.AppendLine("I was written in **C#** using the Discord.Net library. https://github.com/RogueException/Discord.Net");

        sb.AppendLine();

        sb.AppendLine("I hope I was helpful!");

        sb.AppendLine();
        sb.AppendLine("**DISCLAIMER**");
        sb.AppendLine("YOU ARE ALLOWED TO USE MY SOURCE CODE IF YOU DO **NOT** CHANGE MY  __***SendInfo()***__ method **AND** IF YOU DO **NOT** CHANGE THE COMMENT IN THE BEGINNING " +
            "OF THE __***Program.cs***__ FILE!");

        await channel.SendMessage(sb.ToString());
    }
}
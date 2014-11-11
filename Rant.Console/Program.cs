using System;
using System.Collections.Generic;
using System.Linq;

using Rant;
using Rant.Vocabulary;

namespace PCon
{
    class Program
    {
        private static readonly Dictionary<string, string> Arguments = new Dictionary<string, string>();
        private static readonly HashSet<string> Flags = new HashSet<string>();

        private static string GetArg(string name)
        {
            string arg;
            if (!Arguments.TryGetValue(name.ToLower(), out arg))
            {
                arg = "";
            }
            return arg;
        }

        static void Main(string[] args)
        {
            foreach (var argKeyVal in args.Where(arg => arg.StartsWith("-")).Select(arg => arg.TrimStart('-').Split(new[] {'='}, 2)))
            {
                if (argKeyVal.Length == 2)
                {
                    Arguments[argKeyVal[0].ToLower().Trim()] = argKeyVal[1];
                }
                else
                {
                    Flags.Add(argKeyVal[0]);
                }
            }

            var file = GetArg("file");
            var dicPath = GetArg("dicpath");


            Console.Title = "Rant Console" + (Flags.Contains("nsfw") ? " [NSFW]" : "");
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var rant = new RantEngine(String.IsNullOrEmpty(dicPath) ? "dictionary" : dicPath, Flags.Contains("nsfw") ? NsfwFilter.Allow : NsfwFilter.Disallow);
            rant.Hooks.AddHook("load", hArgs => hArgs.Length != 1 ? "" : rant.DoFile(hArgs[0]));

            if (!String.IsNullOrEmpty(file))
            {
                try
                {
                    PrintOutput(rant.DoFile(file));
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();
                }
            }

            while (true)
            {
                Console.ForegroundColor = Flags.Contains("nsfw") ? ConsoleColor.Magenta : ConsoleColor.Yellow;
                Console.Write("\u211d> "); // real number symbol
                Console.ResetColor();

                var input = Console.ReadLine();
#if DEBUG
                PrintOutput(rant.Do(input));
#else
                try
                {
                    PrintOutput(rant.Do(input));
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.Message);
                    Console.ResetColor();
                }
#endif
            }
        }

        static void PrintOutput(Output output)
        {
            foreach (var chan in output)
            {
                if (chan.Name != "main")
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("{0} ({1}):", chan.Name, chan.Visiblity);
                    Console.ResetColor();
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(chan.Value);
                Console.ResetColor();
            }
        }
    }
}

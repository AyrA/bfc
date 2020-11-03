using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace bfc
{
    /// <summary>
    /// Main class
    /// </summary>
    class Program
    {
        private class Arguments
        {
            public bool Optimize { get; private set; }
            public bool AutoConfirm { get; private set; }
            public string EngineName { get; private set; }
            public string InputFile { get; private set; }
            public string OutputFile { get; private set; }
            public string[] EngineArguments { get; private set; }

            public Arguments(string[] Args)
            {
                var EngineArgs = new List<string>();
                for (var i = 0; i < Args.Length; i++)
                {
                    var Last = i == Args.Length - 1;
                    var Arg = Args[i];
                    switch (Arg.ToLower())
                    {
                        case "/y":
                            AutoConfirm = true;
                            break;
                        case "/o":
                            Optimize = true;
                            break;
                        case "/e":
                            if (Last)
                            {
                                throw new ArgumentException("/e requires an argument");
                            }
                            if (EngineName != null)
                            {
                                throw new ArgumentException("Duplicate use of /e");
                            }
                            EngineName = Args[++i];
                            break;
                        case "/a":
                            if (Last)
                            {
                                throw new ArgumentException("/a requires an argument");
                            }
                            EngineArgs.Add(Args[++i]);
                            break;
                        default:
                            if (InputFile == null)
                            {
                                InputFile = Arg;
                            }
                            else if (OutputFile == null)
                            {
                                OutputFile = Arg;
                            }
                            else
                            {
                                throw new ArgumentException($"Extra argument: {Arg}");
                            }
                            break;
                    }
                }
                EngineArguments = EngineArgs.ToArray();
                if (string.IsNullOrWhiteSpace(EngineName))
                {
                    throw new ArgumentException("No engine name specified");
                }
                if (string.IsNullOrEmpty(InputFile))
                {
                    throw new ArgumentException("No input file specified");
                }
            }

            public string GenerateOutputName(string extension)
            {
                return OutputFile = Path.ChangeExtension(InputFile, extension);
            }
        }

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">Arguments</param>
        static void Main(string[] args)
        {
            Common.LoadEngines();
            if (args.Contains("/?") || args.Length == 0)
            {
                Help(args.FirstOrDefault(m => m != "/?"));
                return;
            }
            if (args.Any(m => m.ToLower() == "/l"))
            {
                ListEngines();
                return;
            }
            Arguments UserArgs;
            try
            {
                UserArgs = new Arguments(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error parsing arguments: {ex.Message}");
                return;
            }

            Common.LoadEngines();
            var EngineDescription = Common.GetEngine(UserArgs.EngineName);
            if (EngineDescription == null)
            {
                Console.Error.WriteLine($"Error parsing arguments: No engine named '{UserArgs.EngineName}' found");
                Console.Error.WriteLine($"Use /l argument to list all available engines");
                return;
            }
            ICodeGenerator Engine = EngineDescription.CreateInstance();

            if (UserArgs.EngineArguments.Length > 0)
            {
                try
                {
                    Engine.SetArguments(UserArgs.EngineArguments);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error setting Engine arguments.");
                    Console.Error.WriteLine("Arguments: {0}", string.Join(", ", UserArgs.EngineArguments));
                    Console.Error.WriteLine("Message: {0}", ex.Message);
                    return;
                }
            }
            if (UserArgs.OutputFile == null)
            {
                UserArgs.GenerateOutputName(EngineDescription.Extension);
            }

            if (!UserArgs.AutoConfirm && File.Exists(UserArgs.OutputFile))
            {
                Console.WriteLine("Overwrite {0}? [Y/N]", UserArgs.OutputFile);
                bool cont = true;
                while (cont)
                {
                    var CK = Console.ReadKey(true).Key;
                    switch (CK)
                    {
                        case ConsoleKey.Y:
                            cont = false;
                            break;
                        case ConsoleKey.N:
                            return;
                        default:
                            Console.Beep();
                            break;
                    }
                }
            }

            var Data = File.ReadAllText(UserArgs.InputFile);
            var Instructions = FilterBF(Data);
            if (UserArgs.Optimize)
            {
                Instructions = Optimize(Instructions);
            }
            var Tokens = Tokenize(Instructions);
            try
            {
                var Result = Engine.GenerateCode(Tokens);
                if (Result == null)
                {
                    throw new NullReferenceException($"{EngineDescription.Name} did not output any code (returned data is null)");
                }
                File.WriteAllText(UserArgs.OutputFile, new string(Result.ToArray()));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Unable to convert BF code.");
                Console.Error.WriteLine("Type: {0}", ex.GetType().Name);
                Console.Error.WriteLine("Message: {0}", ex.Message);
                return;
            }
        }

        /// <summary>
        /// Shows generic or engine specific help
        /// </summary>
        /// <param name="Engine"></param>
        private static void Help(string Engine)
        {
            if (string.IsNullOrWhiteSpace(Engine))
            {
                Console.WriteLine(@"bfc [/y] [/o] /e <engine> [/a <arg> [...]] <input> [output]
bfc /l
bfc /? [engine]

input    Source file
output   Destination file. If not supplied uses the input file name with
            the appropriate file extension from the engine
/e       Engine selection
/a       Supply argument to engine
/l       List engines
/y       Confirm prompts to overwrite the output
/o       Optimize BF code
");
            }
            else
            {
                var E = Common.GetEngine(Engine);
                if (E == null)
                {
                    Console.WriteLine("No such engine: {0}", Engine);
                }
                else
                {
                    E.CreateInstance().Help();
                }
            }
#if DEBUG
            Console.ReadKey(true);
#endif
        }

        /// <summary>
        /// Lists all engines accessible by this installation
        /// </summary>
        private static void ListEngines()
        {
            Console.WriteLine("Built-in engines:");
            Console.WriteLine("{0}\t{3}\t{2,-15}\t{1}", "Name", "Description", "Version", "Ext");
            foreach (var generator in Common.GetBuiltinEngines())
            {
                Console.WriteLine("{0}\t{3}\t{2,-15}\t{1}",
                    generator.Name,
                    generator.Description,
                    generator.Version,
                    generator.Extension);
            }

            if (Common.GetExternalEngines().Length > 0)
            {
                Console.WriteLine("External Engines:");
                Console.WriteLine("{0}\t{3}\t{2,-15}\t{1}", "Name", "Description", "Version", "Ext");
                foreach (var generator in Common.GetExternalEngines())
                {
                    Console.WriteLine("{0}\t{3}\t{2,-15}\t{1}",
                        generator.Name,
                        generator.Description,
                        generator.Version,
                        generator.Extension);
                }
            }
            else
            {
                Console.WriteLine("No external engines found");
            }
        }

        /// <summary>
        /// Filters a file and strips all non-bf characters (including whitespace)
        /// </summary>
        /// <param name="Input">Input</param>
        /// <returns>BF-only output</returns>
        private static IEnumerable<char> FilterBF(IEnumerable<char> Input)
        {
            var tokens = "[]+-<>.,";
            foreach (var c in Input)
            {
                if (tokens.Contains(c))
                {
                    yield return c;
                }
            }
        }

        /// <summary>
        /// Optimizes common BF constructs to use less instructions
        /// </summary>
        /// <param name="Input">BF-only code. See <see cref="FilterBF"/></param>
        /// <returns>Optimized BF code</returns>
        /// <remarks>The output will contain tokens not valid in BF</remarks>
        public static IEnumerable<char> Optimize(IEnumerable<char> Input)
        {
            var s = new string(Input.ToArray());
            return s.Replace("[-]", "!");
        }

        /// <summary>
        /// Converts BF code into tokens for processing
        /// </summary>
        /// <param name="Input">BF-only code. See <see cref="FilterBF"/> or <see cref="Optimize"/></param>
        /// <returns>BF Tokens</returns>
        public static IEnumerable<Token> Tokenize(IEnumerable<char> Input)
        {
            Token current = null;
            foreach (var c in Input)
            {
                if (current == null)
                {
                    //Create a new token if the current one is still the default
                    current = new Token(c);
                }
                else if (current.Instruction != c)
                {
                    //Return the old token, then create one for the differing instruction
                    yield return current;
                    current = new Token(c);
                }
                else
                {
                    //Increase the counter on the current instruction
                    current.AddCount();
                }
            }
            //Don't forget to return the last token (provided it's not null only)
            if (current != null)
            {
                yield return current;
            }
        }
    }
}

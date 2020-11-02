using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace bfc
{
    /// <summary>
    /// Data types for the memory available to BF
    /// </summary>
    public enum DataType : int
    {
        /// <summary>
        /// 8 bit unsigned
        /// </summary>
        Byte = 0,
        /// <summary>
        /// 16 bit signed
        /// </summary>
        Int16 = 1,
        /// <summary>
        /// 16 bit unsigned
        /// </summary>
        UInt16 = 2,
        /// <summary>
        /// 32 bit signed
        /// </summary>
        Int32 = 3,
        /// <summary>
        /// 32 bit unsigned
        /// </summary>
        UInt32 = 4,
        /// <summary>
        /// 64 bit signed
        /// </summary>
        Int64 = 5,
        /// <summary>
        /// 64 bit unsigned
        /// </summary>
        UInt64 = 6
    }

    public class EngineDescription
    {
        /// <summary>
        /// Name of the engine (for /e parameter)
        /// </summary>
        /// <remarks>This is case insensitive</remarks>
        public string Name { get; private set; }
        /// <summary>
        /// Short description of what this engine produces
        /// </summary>
        public string Description { get; private set; }
        /// <summary>
        /// Version number of the engine
        /// </summary>
        /// <remarks>Use <see cref="Assembly.GetExecutingAssembly().GetName().Version"/> to use your DLL version</remarks>
        public Version Version { get; private set; }
        /// <summary>
        /// Default file extension (in case the user did not specify an output)
        /// </summary>
        public string Extension { get; private set; }

        /// <summary>
        /// Engine
        /// </summary>
        public Type Engine { get; private set; }

        /// <summary>
        /// Creates an instance from an engine
        /// </summary>
        /// <param name="gen">Engine</param>
        public EngineDescription(ICodeGenerator gen)
        {
            Name = gen.GetEngineName();
            Description = gen.GetEngineDescription();
            Version = gen.GetEngineVersion();
            Extension = gen.GetDefaultExtension();
            Engine = gen.GetType();
            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new ArgumentException($"Engine {Engine.FullName} has no or invalid name");
            }
            if (string.IsNullOrWhiteSpace(Description))
            {
                Description = "<no description>";
            }
            if (string.IsNullOrWhiteSpace(Extension))
            {
                Extension = "txt";
            }
            //Replace leading and trailing dots and whitespace
            while (Extension != Extension.Trim().Trim('.').Trim())
            {
                Extension = Extension.Trim().Trim('.').Trim();
            }
            if (Extension.Length == 0 || Extension.Any(m => Path.GetInvalidFileNameChars().Contains(m)))
            {
                throw new ArgumentException($"Engine {Engine.FullName} has invalid default file extension specified");
            }
            if (Version == null)
            {
                throw new ArgumentException($"Engine {Engine.FullName} has no or invalid version specification");
            }
        }

        /// <summary>
        /// Creates a new instance of the engine with the default constructor
        /// </summary>
        /// <returns>Engine</returns>
        public ICodeGenerator CreateInstance()
        {
            return (ICodeGenerator)Engine
                .GetConstructors()
                .FirstOrDefault(m => m.GetParameters().Length == 0)
                .Invoke(null);
        }
    }

    /// <summary>
    /// Common function collection
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Holds all externally loaded engines
        /// </summary>
        private static EngineDescription[] ExternalEngines;

        /// <summary>
        /// Gets all external engines
        /// </summary>
        /// <returns>External engine list</returns>
        public static EngineDescription[] GetExternalEngines()
        {
            return (EngineDescription[])ExternalEngines.Clone();
        }

        /// <summary>
        /// Loads engines from the current assembly directory
        /// </summary>
        public static void LoadEngines()
        {
            using (var P = System.Diagnostics.Process.GetCurrentProcess())
            {
                LoadEngines(Path.GetDirectoryName(P.MainModule.FileName));
            }
        }

        /// <summary>
        /// Loads engines from the given directory
        /// </summary>
        /// <param name="Dir">Directory</param>
        public static void LoadEngines(string Dir)
        {
            if (ExternalEngines != null)
            {
                return;
            }
            if (Dir == null)
            {
                throw new ArgumentNullException(nameof(Dir));
            }
            var CurrentAssembly = Assembly.GetExecutingAssembly().Location;
            var Asm = new List<EngineDescription>();
            var Internals = GetBuiltinEngines().ToArray();
            var Names = new List<string>(Internals.Select(m => m.Name.ToLower()));
            foreach (var f in Directory.GetFiles(Dir, "*.dll"))
            {
                //The currently loaded assembly is ignored
                if (Path.GetFullPath(CurrentAssembly) == Path.GetFullPath(f))
                {
                    continue;
                }
                try
                {
                    var Engines = GetGenerators(Assembly.LoadFrom(f));
                    foreach (var E in Engines)
                    {
                        if (Names.Contains(E.Name.ToLower()))
                        {
                            Console.Error.WriteLine("Duplicate engine name: {0}", E.Name);
                        }
                        else
                        {
                            Asm.Add(E);
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Failed to load Assembly in {0}", Path.GetFileName(f));
                }
            }
            ExternalEngines = Asm.Where(m => !Internals.Any(n => n.Name.ToLower() == m.Name.ToLower())).ToArray();
        }

        /// <summary>
        /// Gets the main assembly version
        /// </summary>
        public static Version AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        /// <summary>
        /// Gets all builtin engines
        /// </summary>
        /// <returns>Builtin engines</returns>
        public static IEnumerable<EngineDescription> GetBuiltinEngines()
        {
            return GetGenerators(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// Gets all engines from an assembly
        /// </summary>
        /// <param name="A">Assembly</param>
        /// <returns>Engine list</returns>
        private static IEnumerable<EngineDescription> GetGenerators(Assembly A)
        {
            return A
                .GetTypes()
                .Where(m => m.GetInterfaces().Contains(typeof(ICodeGenerator)))
                .Select(m => new EngineDescription((ICodeGenerator)m.GetConstructors().First(m => m.GetParameters().Length == 0).Invoke(null)));
        }

        /// <summary>
        /// Gets the first engine matching the given name
        /// </summary>
        /// <param name="EngineName">Engine name</param>
        /// <returns>Engine, or null if not found</returns>
        public static EngineDescription GetEngine(string EngineName)
        {
            LoadEngines();
            var Engines = GetBuiltinEngines().Concat(ExternalEngines);
            return Engines.FirstOrDefault(m => m.Name.ToLower() == EngineName.ToLower());
        }
    }
}

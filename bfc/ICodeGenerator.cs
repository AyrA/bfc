using System;
using System.Collections.Generic;

namespace bfc
{
    /// <summary>
    /// Interface for a tool that converts brainfuck into another language
    /// </summary>
    public interface ICodeGenerator
    {
        /// <summary>
        /// Sets the arguments for the generator
        /// </summary>
        /// <param name="Arguments">Generator arguments</param>
        void SetArguments(string[] Arguments);
        /// <summary>
        /// Name of the engine (for /e argument)
        /// </summary>
        /// <returns>Engine name</returns>
        string GetEngineName();
        /// <summary>
        /// Description of the engine
        /// </summary>
        /// <returns>Engine description</returns>
        string GetEngineDescription();
        /// <summary>
        /// Version of the engine
        /// </summary>
        /// <returns>Engine version</returns>
        Version GetEngineVersion();
        /// <summary>
        /// Default output file extension
        /// </summary>
        /// <returns>File extension</returns>
        string GetDefaultExtension();
        /// <summary>
        /// Shows engine specific help
        /// </summary>
        void Help();
        /// <summary>
        /// Converts BF tokens into other code
        /// </summary>
        /// <param name="Instructions">BF tokens</param>
        /// <returns>Code</returns>
        IEnumerable<char> GenerateCode(IEnumerable<Token> Instructions);
    }
}

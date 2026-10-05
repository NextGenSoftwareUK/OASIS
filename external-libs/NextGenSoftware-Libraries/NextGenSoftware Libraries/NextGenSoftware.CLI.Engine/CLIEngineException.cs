using System;

namespace NextGenSoftware.CLI.Engine
{
    public class CLIEngineException : Exception
    {       
        public CLIEngineException()
        {

        }

        public CLIEngineException(string message) : base(message)
        {

        }

        public CLIEngineException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}

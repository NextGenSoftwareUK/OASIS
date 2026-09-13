using System;

namespace OGEditorSDK.MapFormat
{
    public class OGMapReadException : Exception
    {
        public string FilePath { get; }
        public int    Line     { get; }

        public OGMapReadException(string message, string filePath = null, int line = -1)
            : base(message)
        {
            FilePath = filePath;
            Line     = line;
        }
    }

    public class OGMapWriteException : Exception
    {
        public string FilePath { get; }

        public OGMapWriteException(string message, string filePath = null)
            : base(message)
        {
            FilePath = filePath;
        }
    }
}

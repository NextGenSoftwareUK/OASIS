using System.IO;
using System.Text;

namespace NextGenSoftware.Utilities
{
    public class NullTextWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.Default; // Required override
        public override void Write(char value) { } // Discards output
    }
}

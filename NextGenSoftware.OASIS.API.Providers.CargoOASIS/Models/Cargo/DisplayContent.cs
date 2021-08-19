using System.Collections.Generic;
using Enum;

namespace Models.Cargo
{
    public class DisplayContent
    {
        public DisplayContentType Type { get; set; }
        public IEnumerable<byte[]> Files { get; set; }
    }
}
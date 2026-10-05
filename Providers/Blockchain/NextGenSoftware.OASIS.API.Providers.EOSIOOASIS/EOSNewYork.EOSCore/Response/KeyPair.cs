using NLog;
using Cryptography.ECDSA;

namespace EOSNewYork.EOSCore.Response
{
    public class KeyPair
    {
        public string PrivateKey { get; set;}
        
        public string PublicKey { get; set; }

        public KeyPair()
        {
            
        }
    }

}

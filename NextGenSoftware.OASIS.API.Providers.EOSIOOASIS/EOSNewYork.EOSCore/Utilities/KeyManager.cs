using NLog;
using Cryptography.ECDSA;
using EOSNewYork.EOSCore.Response;

namespace EOSNewYork.EOSCore.Utilities
{
    public class KeyManager
    {
        private static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        
        public static KeyPair GenerateKeyPair()
        {
            KeyPair keyPair = new KeyPair();
            byte[] privateKey = Secp256K1Manager.GenerateRandomKey();
            keyPair.PrivateKey = WifUtility.GetPrivateWif(privateKey);
            byte[] publicKey = Secp256K1Manager.GetPublicKey(privateKey, true);
            keyPair.PublicKey = WifUtility.GetPublicWif(publicKey, "EOS");
            return keyPair;
        }
    }
}

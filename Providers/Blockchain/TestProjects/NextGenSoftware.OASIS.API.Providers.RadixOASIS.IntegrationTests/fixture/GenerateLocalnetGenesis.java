import com.radixdlt.crypto.ECKeyPair;
import com.radixdlt.crypto.RadixKeyStore;
import com.radixdlt.genesis.GenesisBuilder;
import com.radixdlt.genesis.GenesisConsensusManagerConfig;
import com.radixdlt.genesis.RawGenesisDataWithHash;
import com.radixdlt.rev2.Decimal;
import com.radixdlt.utils.Compress;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Base64;

/**
 * Writes a fresh single-validator localnet (network 240) for the RadixOASIS native storage fixture: the validator key
 * store (node.ks, key "node") and the compressed base64 genesis (genesis.b64) with a funded faucet. It uses the node's
 * own GenesisBuilder, so run it inside the babylon-node image with --enable-preview and the image's lib directory on
 * the class path. Arguments: output directory, key store password.
 */
public final class GenerateLocalnetGenesis {
    public static void main(String[] args) throws Exception {
        var directory = Path.of(args[0]);
        var validator = ECKeyPair.generateNew();
        try (var keyStore = RadixKeyStore.fromFile(directory.resolve("node.ks").toFile(), args[1].toCharArray(), true)) {
            keyStore.writeKeyPair("node", validator);
        }
        var genesis = GenesisBuilder.createTestGenesisWithSingleValidatorAndFaucetSupply(
            validator.getPublicKey(),
            Decimal.ofNonNegative(1_000_000),
            GenesisConsensusManagerConfig.Builder.testDefaults().epochExactRoundCount(1_000_000_000L),
            Decimal.ofNonNegative(1_000_000_000L));
        var raw = RawGenesisDataWithHash.fromGenesisData(genesis).genesisData().value();
        Files.writeString(directory.resolve("genesis.b64"), Base64.getEncoder().encodeToString(Compress.compress(raw)));
    }
}

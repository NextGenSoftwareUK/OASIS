using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.RPC;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Common;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.Common;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Rpc.Utilities;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Helpers;
using Newtonsoft.Json;
using NextGenSoftware.Utilities.ExtentionMethods;
using System.Linq;
using System.IO;
using System.Text;
using static Solnet.Programs.TokenProgram;
using static Solnet.Programs.AssociatedTokenAccountProgram;
using static Solnet.Programs.SystemProgram;
using static Solnet.Programs.MemoProgram;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISBlockchainStorageProvider,
    IOASISSmartContractProvider, IOASISNFTProvider, IOASISNETProvider, IOASISSuperStar
{
    private ISolanaRepository _solanaRepository;
    private ISolanaService _solanaService;
    private KeyManager _keyManager;
    private WalletManager _walletManager;
    private readonly Account _oasisSolanaAccount;
    private readonly IRpcClient _rpcClient;
    private readonly Uri _rpcEndpoint;
    private readonly System.Net.Http.HttpMessageHandler _nativeFeeReadHandler;
    private readonly Func<string,CancellationToken,Task<Solnet.Rpc.Models.TransactionMetaSlotInfo>> _transactionReader;

    private KeyManager KeyManager
    {
        get
        {
            _keyManager ??=
                new KeyManager(ProviderManager.Instance.GetStorageProvider(Core.Enums.ProviderType.SolanaOASIS));

            return _keyManager;
        }
    }

    private WalletManager WalletManager
    {
        get
        {
            _walletManager ??=
                new WalletManager(ProviderManager.Instance.GetStorageProvider(Core.Enums.ProviderType.SolanaOASIS));

            return _walletManager;
        }
    }

    /// <summary>
    /// The provider type stamped into <c>ProviderUniqueStorageKey</c> on every record this instance writes,
    /// and used for every provider-key read. It is <see cref="Core.Enums.ProviderType.SolanaOASIS"/> unless
    /// another SVM provider (e.g. EclipseOASIS) composes this implementation as its storage engine, in which
    /// case records belong to the composing provider.
    /// </summary>
    internal Core.Enums.ProviderType StorageProviderType => _storageProviderType;
    private readonly Core.Enums.ProviderType _storageProviderType;

    public SolanaOASIS(string hostUri, string privateKey, string publicKey)
        : this(hostUri, privateKey, publicKey, Core.Enums.ProviderType.SolanaOASIS) { }

    /// <summary>Composes the Solana storage implementation on behalf of another SVM provider.</summary>
    internal SolanaOASIS(string hostUri, string privateKey, string publicKey, Core.Enums.ProviderType storageProviderType)
        : this(hostUri, privateKey, publicKey, null, storageProviderType: storageProviderType) { }

    internal SolanaOASIS(string hostUri, string privateKey, string publicKey,
        System.Net.Http.HttpMessageHandler nativeFeeReadHandler,
        Func<string,CancellationToken,Task<Solnet.Rpc.Models.TransactionMetaSlotInfo>> transactionReader = null,
        Core.Enums.ProviderType storageProviderType = Core.Enums.ProviderType.SolanaOASIS)
    {
        if (storageProviderType is Core.Enums.ProviderType.None or Core.Enums.ProviderType.All or Core.Enums.ProviderType.Default)
            throw new ArgumentOutOfRangeException(nameof(storageProviderType), "A concrete storage provider type is required.");
        this._storageProviderType = storageProviderType;
        this.ProviderName = nameof(SolanaOASIS);
        this.ProviderDescription = "Solana Blockchain Provider";
        this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SolanaOASIS);
        this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        this._rpcClient = ClientFactory.GetClient(hostUri);
        this._rpcEndpoint = new Uri(hostUri, UriKind.Absolute);
        this._nativeFeeReadHandler = nativeFeeReadHandler;
        this._transactionReader = transactionReader ?? ((signature, cancellationToken) =>
            Infrastructure.SolanaTransactionReader.ReadAsync(_rpcEndpoint,signature,_nativeFeeReadHandler,cancellationToken));
        this._oasisSolanaAccount = new(privateKey, publicKey);

        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
    }

}

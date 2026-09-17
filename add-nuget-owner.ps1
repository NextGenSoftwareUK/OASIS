param(
    [Parameter(Mandatory=$true)]
    [string]$ApiKey,
    [string]$NewOwner = "OASISOmniverse"
)

$packages = @(
    "NextGenSoftware.OASIS.API.Apollo.Server",
    "NextGenSoftware.OASIS.API.Contracts",
    "NextGenSoftware.OASIS.API.Core",
    "NextGenSoftware.OASIS.API.Core.ARC.Membrane",
    "NextGenSoftware.OASIS.API.DNA",
    "NextGenSoftware.OASIS.API.Managers",
    "NextGenSoftware.OASIS.API.Native.Integrated.EndPoint",
    "NextGenSoftware.OASIS.API.ONODE.Core",
    "NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS",
    "NextGenSoftware.OASIS.API.Providers.AptosOASIS",
    "NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS",
    "NextGenSoftware.OASIS.API.Providers.AvalancheOASIS",
    "NextGenSoftware.OASIS.API.Providers.AWSOASIS",
    "NextGenSoftware.OASIS.API.Providers.AztecOASIS",
    "NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS",
    "NextGenSoftware.OASIS.API.Providers.BaseOASIS",
    "NextGenSoftware.OASIS.API.Providers.BitcoinOASIS",
    "NextGenSoftware.OASIS.API.Providers.BlockStack",
    "NextGenSoftware.OASIS.API.Providers.BNBChainOASIS",
    "NextGenSoftware.OASIS.API.Providers.CardanoOASIS",
    "NextGenSoftware.OASIS.API.Providers.CargoOASIS",
    "NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS",
    "NextGenSoftware.OASIS.API.Providers.COSMOSBlockChainOASIS",
    "NextGenSoftware.OASIS.API.Providers.ElrondOASIS",
    "NextGenSoftware.OASIS.API.Providers.EOSIOOASIS",
    "NextGenSoftware.OASIS.API.Providers.EthereumOASIS",
    "NextGenSoftware.OASIS.API.Providers.FantomOASIS",
    "NextGenSoftware.OASIS.API.Providers.GOMapOASIS",
    "NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS",
    "NextGenSoftware.OASIS.API.Providers.HashgraphOASIS",
    "NextGenSoftware.OASIS.API.Providers.HoloOASIS",
    "NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop",
    "NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity",
    "NextGenSoftware.OASIS.API.Providers.HoloWebOASIS",
    "NextGenSoftware.OASIS.API.Providers.IPFSOASIS",
    "NextGenSoftware.OASIS.API.Providers.LineaOASIS",
    "NextGenSoftware.OASIS.API.Providers.LocalFileOASIS",
    "NextGenSoftware.OASIS.API.Providers.MapBoxOASIS",
    "NextGenSoftware.OASIS.API.Providers.MidenOASIS",
    "NextGenSoftware.OASIS.API.Providers.MonadOASIS",
    "NextGenSoftware.OASIS.API.Providers.MongoDBOASIS",
    "NextGenSoftware.OASIS.API.Providers.MoralisOASIS",
    "NextGenSoftware.OASIS.API.Providers.NEAROASIS",
    "NextGenSoftware.OASIS.API.Providers.Neo4jOASIS",
    "NextGenSoftware.OASIS.API.Providers.Neo4jOASIS2",
    "NextGenSoftware.OASIS.API.Providers.ONIONProtocolOASIS",
    "NextGenSoftware.OASIS.API.Providers.OptimismOASIS",
    "NextGenSoftware.OASIS.API.Providers.OrionProtocolOASIS",
    "NextGenSoftware.OASIS.API.Providers.PinataOASIS",
    "NextGenSoftware.OASIS.API.Providers.PLANOASIS",
    "NextGenSoftware.OASIS.API.Providers.PolkadotOASIS",
    "NextGenSoftware.OASIS.API.Providers.PolygonOASIS",
    "NextGenSoftware.OASIS.API.Providers.RadixOASIS",
    "NextGenSoftware.OASIS.API.Providers.RootstockOASIS",
    "NextGenSoftware.OASIS.API.Providers.ScrollOASIS",
    "NextGenSoftware.OASIS.API.Providers.ScuttleButtOASIS",
    "NextGenSoftware.OASIS.API.Providers.SEEDSOASIS",
    "NextGenSoftware.OASIS.API.Providers.SOLANAOASIS",
    "NextGenSoftware.OASIS.API.Providers.SOLIDOASIS",
    "NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS",
    "NextGenSoftware.OASIS.API.Providers.StarknetOASIS",
    "NextGenSoftware.OASIS.API.Providers.SuiOASIS",
    "NextGenSoftware.OASIS.API.Providers.TelegramOASIS",
    "NextGenSoftware.OASIS.API.Providers.TelosOASIS",
    "NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS",
    "NextGenSoftware.OASIS.API.Providers.TONOASIS",
    "NextGenSoftware.OASIS.API.Providers.TRONOASIS",
    "NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS",
    "NextGenSoftware.OASIS.API.Providers.WRLD3DOASIS",
    "NextGenSoftware.OASIS.API.Providers.XRPLOASIS",
    "NextGenSoftware.OASIS.API.Providers.ZcashOASIS",
    "NextGenSoftware.OASIS.API.Providers.ZkSyncOASIS",
    "NextGenSoftware.OASIS.Common",
    "NextGenSoftware.OASIS.OASISBootLoader",
    "NextGenSoftware.OASIS.STAR",
    "NextGenSoftware.OASIS.STAR.APIClient",
    "NextGenSoftware.OASIS.STAR.CLI.Lib",
    "NextGenSoftware.OASIS.STAR.DNA",
    "NextGenSoftware.OASIS.Web10.Core",
    "NextGenSoftware.OASIS.Web6.Core",
    "NextGenSoftware.OASIS.Web7.Core",
    "NextGenSoftware.OASIS.Web8.Core",
    "NextGenSoftware.OASIS.Web9.Core"
)

$succeeded = 0
$failed = 0
$alreadyOwner = 0

foreach ($pkg in $packages) {
    try {
        $body = "ownerToAdd=$NewOwner&apiKey=$ApiKey"
        $response = Invoke-WebRequest `
            -Uri "https://www.nuget.org/api/v2/package/$pkg/owners" `
            -Method POST `
            -Body $body `
            -ContentType "application/x-www-form-urlencoded" `
            -ErrorAction Stop

        Write-Host "OK  $pkg" -ForegroundColor Green
        $succeeded++
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        if ($status -eq 409) {
            Write-Host "SKIP $pkg (already owner)" -ForegroundColor Yellow
            $alreadyOwner++
        } else {
            Write-Host ('FAIL ' + $pkg + ' - ' + $_.Exception.Message) -ForegroundColor Red
            $failed++
        }
    }
}

Write-Host ""
Write-Host ('Done: ' + $succeeded + ' added, ' + $alreadyOwner + ' already owner, ' + $failed + ' failed') -ForegroundColor Cyan

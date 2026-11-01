"""Check every SDK bridge shipped by Docker before any expensive image build."""
from pathlib import Path
from validate_provider_sdk_publish_manifest import validate
ROOT=Path(__file__).resolve().parents[1]
BRIDGES=(("CosmosBlockChain","Cosmos"),("Hashgraph","Hedera"),("NEAR","Near"),("Sui","Sui"),("Polkadot","Polkadot"))

def main():
    for provider,bridge in BRIDGES:
        name=f'NextGenSoftware.OASIS.API.Providers.{provider}OASIS'
        project=ROOT/'Providers/Blockchain'/name/(name+'.csproj')
        count=validate(project,bridge+'SdkBridge')
        print(f'{bridge}SdkBridge: {count} modules; source copy policies and import closure verified')

if __name__=='__main__':main()

"""Source publish-manifest regression controls; owned temporary fixtures only."""
from pathlib import Path
import tempfile
import unittest
from validate_provider_sdk_publish_manifest import validate
ROOT=Path(__file__).resolve().parents[1]
class PublishManifestTests(unittest.TestCase):
    def test_update_entries_preserve_required_import_closure(self):
        parent=ROOT/'TestResults/HostedCqlIntegrationCurrent';parent.mkdir(parents=True,exist_ok=True)
        with tempfile.TemporaryDirectory(prefix='publish-manifest-',dir=parent) as folder:
            root=Path(folder)
            for name,text in [('bridge.mjs',"import './required.mjs';"),('required.mjs','export const required=true;'),('package.json','{}'),('package-lock.json','{}')]:
                (root/name).write_text(text)
            entries=''.join(f'<None Update="{name}" Link="Bridge/{name}" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />' for name in ['bridge.mjs','required.mjs','package.json','package-lock.json'])
            project=root/'fixture.csproj';project.write_text('<Project><ItemGroup>'+entries+'</ItemGroup></Project>')
            self.assertEqual(2,validate(project,'Bridge'))
            # A module present in source but omitted from the publish list must fail.
            project.write_text(project.read_text().replace('<None Update="required.mjs" Link="Bridge/required.mjs" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />',''))
            with self.assertRaisesRegex(ValueError,'unpublished module'):validate(project,'Bridge')
    def test_shipping_polkadot_manifest_contains_transitive_imports(self):
        project=ROOT/'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.csproj'
        self.assertGreaterEqual(validate(project,'PolkadotSdkBridge'),31)
if __name__=='__main__':unittest.main()

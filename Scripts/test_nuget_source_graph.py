import unittest
from pathlib import Path
from unittest.mock import patch
from nuget_source_graph import Graph, compare, resolve_baseline, load_receipts


class SourceGraphTests(unittest.TestCase):
    def comparison(self, old, new):
        def fake_git(root, command, *args):
            commit = args[0].split(":", 1)[0]
            data = old if commit == "old" else new
            if command == "ls-tree":
                name = args[-1]
                value = data.get(name)
                return f"100644 blob {value}\t{name}" if value else ""
            if command == "show":
                return data[args[0].split(":", 1)[1] + ":xml"]
            raise AssertionError((command, args))
        with patch.object(Graph, "git", side_effect=fake_git):
            return compare(Path.cwd(), "old", "new", "A/A.csproj")

    def base(self):
        return {"A": "tree-a", "A/A.csproj": "project-a", "A/A.csproj:xml": '<Project><ItemGroup><ProjectReference Include="../B/B.csproj"/><None Include="../logo.png"/></ItemGroup></Project>',
                "B": "tree-b", "B/B.csproj": "project-b", "B/B.csproj:xml": "<Project/>", "logo.png": "logo"}

    def test_unchanged_and_unrelated_commit_does_not_bump(self):
        old = self.base()
        self.assertFalse(self.comparison(old, dict(old, unrelated="new"))["changed"])

    def test_transitive_dependency_change(self):
        old = self.base()
        new = dict(old)
        new["B"] = "changed-source"
        self.assertTrue(self.comparison(old, new)["changed"])

    def test_packed_readme_logo_or_build_property_change(self):
        for name in ("A", "logo.png", "Directory.Build.props"):
            old = self.base()
            new = dict(old)
            new[name] = "changed"
            self.assertTrue(self.comparison(old, new)["changed"], name)

    def test_dynamic_external_reference_fails_closed(self):
        old = self.base()
        old["A/A.csproj:xml"] = '<Project><ProjectReference Include="$(Dependency)/X.csproj"/></Project>'
        with self.assertRaisesRegex(ValueError, "dynamic external"):
            self.comparison(old, old)

    def test_reviewed_holonet_correction_requires_exact_identity(self):
        receipts = load_receipts(Path(__file__).resolve().parent.parent)
        receipt = receipts[("nextgensoftware.holochain.holonet.orm", "4.1.5")]
        correction = receipt["metadataCorrection"]
        item = {"root": str(Path.cwd()), "project": "HoloNET-ORM/X.csproj", "packageId": correction["packageId"], "previousVersion": "4.1.5", "publishedRepository": {"commit": correction["recordedCommit"], "url": correction["recordedUrl"]}}
        with patch.object(Graph, "location", return_value=(Path.cwd() / "HoloNET-ORM", correction["recordedCommit"], "X.csproj")), patch.object(Graph, "git", return_value=correction["actualOwner"]):
            self.assertEqual(receipt["sourceCommit"], resolve_baseline(item, receipts))
            item["publishedRepository"]["commit"] = "f" * 40
            with self.assertRaisesRegex(ValueError, "contradicts"):
                resolve_baseline(item, receipts)

    def test_unknown_foreign_publication_is_not_guessed(self):
        item = {"root": str(Path.cwd()), "project": "X.csproj", "packageId": "Unknown", "previousVersion": "1.0.0", "publishedRepository": {"commit": "a" * 40, "url": "https://github.com/foreign/repo"}}
        with self.assertRaisesRegex(ValueError, "reviewed parent"):
            resolve_baseline(item, {})

    def test_receipt_stale_url_must_match_exact_published_project(self):
        baseline = "b" * 40
        item = {"root": str(Path.cwd()), "project": "Editor/X.csproj", "packageId": "Editor", "previousVersion": "1.0.1", "publishedRepository": {"commit": baseline, "url": "https://github.com/old/url"}}
        receipt = {("editor", "1.0.1"): {"sourceCommit": baseline}}
        def git(root, command, *args):
            return 'https://github.com/NextGenSoftwareUK/OASIS' if command == 'remote' else '<Project><PackageId>Editor</PackageId><RepositoryUrl>https://github.com/old/url</RepositoryUrl></Project>'
        with patch.object(Graph, "location", return_value=(Path.cwd(), baseline, "Editor/X.csproj")), patch.object(Graph, "git", side_effect=git):
            self.assertEqual(baseline, resolve_baseline(item, receipt))
            item["publishedRepository"]["url"] = "https://github.com/arbitrary/foreign"
            with self.assertRaisesRegex(ValueError, "differs"):
                resolve_baseline(item, receipt)


if __name__ == "__main__":
    unittest.main()

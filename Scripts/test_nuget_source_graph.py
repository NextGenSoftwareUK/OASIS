import unittest
from pathlib import Path
from unittest.mock import patch
from nuget_source_graph import Graph, compare, resolve_baseline, load_receipts, compare_publication


class SourceGraphTests(unittest.TestCase):
    def test_standalone_receipt_requires_exact_catalog_identity(self):
        receipt = {"kind": "standalone", "ownerCommit": "a" * 40,
                   "ownerUrl": "https://github.com/owner/component", "dependencies": {}}
        item = dict(root=str(Path.cwd()), packageId="Component", previousVersion="4.4.0",
                    publishedRepository=dict(commit="a" * 40, url=receipt["ownerUrl"]))
        receipts = {("component", "4.4.0"): receipt}
        self.assertEqual(receipt, resolve_baseline(item, receipts))
        item["publishedRepository"]["commit"] = "b" * 40
        with self.assertRaisesRegex(ValueError, "contradicts standalone"):
            resolve_baseline(item, receipts)

    def test_standalone_comparison_keeps_dependency_and_parent_input_changes(self):
        root = Path.cwd()
        receipt = {"kind": "standalone", "ownerCommit": "a" * 40,
                   "ownerUrl": "https://github.com/owner/component", "ownerParent": "holochain-client-csharp",
                   "dependencies": {"holochain-client-csharp": {"url": "https://github.com/owner/component", "commit": "b" * 40}}, "run": 123}
        item = dict(root=str(root), project="Child/X.csproj", current="new", packageId="Component", previousVersion="4.4.0",
                    publishedRepository=dict(commit="a" * 40, url=receipt["ownerUrl"]))
        def fingerprint(graph, project):
            graph.inputs["dependency"] = "old" if graph.external_pins is not None else "new"
            return graph.inputs["dependency"]
        with patch.object(Graph, "location", return_value=(root / "Child", "new", "X.csproj")), \
             patch.object(Graph, "identity", return_value=receipt["ownerUrl"]), \
             patch.object(Graph, "git", return_value=receipt["ownerUrl"]), \
             patch.object(Graph, "fingerprint", fingerprint):
            result = compare_publication(item, {("component", "4.4.0"): receipt})
        self.assertTrue(result["changed"])
        self.assertEqual(["dependency"], result["changedInputs"])
        self.assertEqual("standalone", result["baselineKind"])
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

    def test_child_root_merge_identity_uses_content_tree(self):
        root = Path.cwd()
        def fingerprint(parent_commit, child_tree):
            def git(repo, command, *args):
                if command == "ls-tree":
                    self.assertEqual("Child", args[-1])
                    return f"160000 commit child-{parent_commit}\tChild"
                if command == "rev-parse":
                    return child_tree
                raise AssertionError((command, args))
            graph = Graph(root, parent_commit)
            with patch.object(Graph, "git", side_effect=git):
                graph.record(root / "Child", required=True)
            return graph.inputs
        self.assertEqual(fingerprint("old", "same-tree"), fingerprint("merge", "same-tree"))
        self.assertNotEqual(fingerprint("old", "same-tree"), fingerprint("changed", "new-tree"))

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

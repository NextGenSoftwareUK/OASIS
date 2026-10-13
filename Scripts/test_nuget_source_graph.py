import unittest
from pathlib import Path
from unittest.mock import patch
from nuget_source_graph import Graph, compare


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


if __name__ == "__main__":
    unittest.main()

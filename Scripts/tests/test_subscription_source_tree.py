import sys
from pathlib import Path
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from subscription_source_tree import SourceTree


class SourceTreeTests(unittest.TestCase):
    root = Path(__file__).resolve().parents[2]
    sha = "a" * 40

    def build(self, git):
        return SourceTree(self.root, {"STAR ODK": self.root / "existing-star"}, {"STAR ODK": self.sha})

    def lookup(self, repository, *args):
        if args[0] == "cat-file": return "commit\n"
        if args[0] == "rev-parse": return self.sha + "\n"
        if args[0] == "ls-tree": return "Project\nProject/Program.cs\nProject/obj\nProject/obj/Generated.cs\n"
        if args[0] == "show": return "immutable source"
        self.fail(f"Unexpected Git operation: {args}")

    def test_reads_exact_pinned_commit_without_branch_checkout_or_fetch(self):
        with patch.object(SourceTree, "_git", side_effect=self.lookup) as git:
            tree = self.build(git)
            path = self.root / "STAR ODK/Project/Program.cs"
            self.assertTrue(tree.exists(path))
            self.assertFalse(tree.exists(self.root / "STAR ODK/Project/Missing.cs"))
            self.assertEqual(tree.read(path), "immutable source")
            self.assertEqual(git.call_args.args[1:], ("show", self.sha + ":Project/Program.cs"))
            self.assertFalse(any(call.args[1] in {"checkout", "clone", "fetch"} for call in git.call_args_list))

    def test_parent_gitlink_mismatch_fails_instead_of_using_another_revision(self):
        def lookup(repository, *args):
            return "b" * 40 + "\n" if args[0] == "rev-parse" else self.lookup(repository, *args)
        with patch.object(SourceTree, "_git", side_effect=lookup):
            with self.assertRaisesRegex(ValueError, "gitlink differ"): self.build(None)

    def test_moving_ref_and_noncommit_objects_are_rejected(self):
        with self.assertRaisesRegex(ValueError, "immutable commit"):
            SourceTree(self.root, {"STAR ODK": self.root}, {"STAR ODK": "Development"})
        with patch.object(SourceTree, "_git", return_value="tree\n"):
            with self.assertRaisesRegex(ValueError, "not a commit"): self.build(None)

    def test_source_failure_propagates_without_workspace_fallback(self):
        with patch.object(SourceTree, "_git", side_effect=self.lookup): tree = self.build(None)
        with patch.object(SourceTree, "_git", side_effect=ValueError("source lookup failed")):
            with self.assertRaisesRegex(ValueError, "lookup failed"):
                tree.read(self.root / "STAR ODK/Project/Program.cs")

    def test_other_source_remains_explicitly_the_parent_worktree(self):
        with patch.object(SourceTree, "_git", side_effect=self.lookup): tree = self.build(None)
        path = self.root / "Scripts/validate_subscription_rollout_gate.py"
        self.assertEqual(tree.read(path), path.read_text(encoding="utf-8"))
        with self.assertRaises(ValueError): tree.read(self.root.parent / "outside.cs")


if __name__ == "__main__": unittest.main()

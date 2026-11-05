"""Mutation-owner controls with in-memory transport/state; no real service or secrets."""
from pathlib import Path
import contextlib
import copy
import importlib.util
import io
import types
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("configuration", Path(__file__).with_name("Publish-HostedSolidConfiguration.py"))
configuration = importlib.util.module_from_spec(spec)
spec.loader.exec_module(configuration)


class State:
    def __init__(self, records):
        self.records = records

    def __truediv__(self, name):
        return File(name, self.records)


class File:
    def __init__(self, name, records):
        self.name = name
        self.records = records

    def exists(self):
        return self.name in self.records


class ConfigurationOwnerTests(unittest.TestCase):
    def test_changed_protected_candidate_stops_before_hosted_access(self):
        before = {"ONE": "old"}
        with patch.object(configuration, "protected_read", side_effect=[before, {"ONE": "changed"}]), \
             patch.object(configuration, "variables") as observe, \
             patch.object(configuration, "write_new") as journal:
            with self.assertRaisesRegex(RuntimeError, "changed after review"):
                configuration.apply_values("localfile-public", ["ONE"],
                                           reviewed_snapshots=(before, {"ONE": "approved"}))
            observe.assert_not_called()
            journal.assert_not_called()

    def test_variable_observation_requires_a_complete_string_mapping(self):
        for payload in (b'[]', b'{"ONE":null}', b'{broken'):
            with self.subTest(payload=payload), patch.object(
                    configuration.subprocess, "run",
                    return_value=types.SimpleNamespace(returncode=0, stdout=payload)):
                with self.assertRaisesRegex(RuntimeError, "observation was invalid; output withheld"):
                    configuration.variables()

    def test_variable_observation_timeout_is_reported_without_output(self):
        with patch.object(configuration.subprocess, "run",
                          side_effect=configuration.subprocess.TimeoutExpired("railway", 30)) as run:
            with self.assertRaisesRegex(RuntimeError, "observation failed; output withheld"):
                configuration.variables()
            self.assertEqual(30, run.call_args.kwargs["timeout"])

    def exercise(self, mode):
        before = {"ONE": "old-one", "TWO": "old-two", "KEEP": "shared"}
        after = {"ONE": "new-one", "TWO": "new-two", "KEEP": "shared"}
        current = copy.deepcopy(before)
        records = {}
        sent = []
        observations = 0

        def observe():
            nonlocal observations
            observations += 1
            if mode == "concurrent" and observations == 3:
                current["KEEP"] = "another-owner"
            return copy.deepcopy(current)

        def send(arguments, *, input, capture_output, timeout):
            key = arguments[3]
            sent.append(key)
            self.assertIn("s3-public-" + key + ".intent.json", records)
            self.assertIn("--stdin", arguments)
            self.assertIn("--skip-deploys", arguments)
            current[key] = input.decode()
            if mode == "timeout":
                raise configuration.subprocess.TimeoutExpired(arguments, timeout)
            return types.SimpleNamespace(returncode=1 if mode == "unknown" else 0)

        def persist(file, data):
            self.assertNotIn(file.name, records)
            records[file.name] = data

        with patch.object(configuration, "STATE", State(records)), \
             patch.object(configuration, "protected_read", side_effect=lambda name: copy.deepcopy(before if "before" in name else after)), \
             patch.object(configuration, "variables", side_effect=observe), \
             patch.object(configuration, "write_new", side_effect=persist), \
             patch.object(configuration.subprocess, "run", side_effect=send), contextlib.redirect_stdout(io.StringIO()):
            if mode == "success":
                configuration.apply_values("s3-public", ["ONE", "TWO"])
                self.assertEqual(current, after)
                self.assertEqual(sent, ["ONE", "TWO"])
                self.assertTrue(all("s3-public-" + key + ".acknowledged.json" in records for key in sent))
            else:
                with self.assertRaises(RuntimeError):
                    configuration.apply_values("s3-public", ["ONE", "TWO"])
                self.assertEqual(sent, ["ONE"])
                self.assertNotIn("s3-public-TWO.intent.json", records)
                if mode in ("unknown", "timeout"):
                    self.assertNotIn("s3-public-ONE.acknowledged.json", records)
                else:
                    self.assertEqual(current["KEEP"], "another-owner")
                with self.assertRaises(RuntimeError):
                    configuration.apply_values("s3-public", ["ONE", "TWO"])
                self.assertEqual(sent, ["ONE"])  # An uncertain/partial batch is never replayed.

    def test_observed_success_preserves_unrelated_values(self):
        self.exercise("success")

    def test_unknown_mutation_outcome_keeps_intent_and_blocks_replay(self):
        self.exercise("unknown")

    def test_timed_out_mutation_keeps_intent_and_blocks_replay(self):
        self.exercise("timeout")

    def test_concurrent_configuration_change_stops_before_next_mutation(self):
        self.exercise("concurrent")


if __name__ == "__main__":
    unittest.main()

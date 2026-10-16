import unittest
from validate_release_dependency_freshness import validate


class FreshnessTests(unittest.TestCase):
    def test_current_pins_pass(self):
        values = dict(HOLOCHAIN_REPO="h", HOLOCHAIN_COMMIT="new",
                      LIBRARIES_REPO="l", LIBRARIES_COMMIT="new")
        self.assertEqual([], validate(values, lambda repo: "new"))

    def test_stale_pin_fails_without_mutating_manifest(self):
        values = dict(HOLOCHAIN_REPO="h", HOLOCHAIN_COMMIT="old",
                      LIBRARIES_REPO="l", LIBRARIES_COMMIT="new")
        self.assertEqual(1, len(validate(values, lambda repo: "new")))
        self.assertEqual("old", values["HOLOCHAIN_COMMIT"])

    def test_remote_failure_is_not_a_fallback(self):
        def unavailable(repo):
            raise RuntimeError("remote unavailable")
        with self.assertRaises(RuntimeError):
            validate(dict(HOLOCHAIN_REPO="h"), unavailable)


if __name__ == "__main__":
    unittest.main()

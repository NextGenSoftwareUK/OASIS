import unittest
from validate_railway_dev_watch_policy import validate, validate_dockerfile


class WatchPolicyTests(unittest.TestCase):
    def test_current_shipping_dockerfiles(self):
        self.assertEqual(validate()["watchPatterns"], ["**", "!/The OASIS.sln"])

    def test_future_solution_build_invalidates_exclusion(self):
        for extension in ("sln", "slnx"):
            with self.subTest(extension=extension), self.assertRaises(ValueError):
                validate_dockerfile(f'RUN dotnet publish "The OASIS.{extension}"', "future")

    def test_solution_reference_is_rejected_even_with_project_publish(self):
        with self.assertRaises(ValueError):
            validate_dockerfile('COPY "The OASIS.sln" .\nRUN dotnet publish "api.csproj"', "future")

    def test_missing_direct_project_publication_is_rejected(self):
        with self.assertRaises(ValueError):
            validate_dockerfile("RUN dotnet publish", "future")


if __name__ == "__main__":
    unittest.main()

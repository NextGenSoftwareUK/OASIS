import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from qualify_web4_full_package import image_tag


class ImageFlavorContract(unittest.TestCase):
    source = '38d4320ef7cffa3ce8d6c77cb312dee3f42acb9d'
    repository = 'ghcr.io/nextgensoftwareuk/oasis-web4:'

    def test_standard_cql_image_keeps_the_commit_tag(self):
        self.assertEqual(self.repository + self.source, image_tag(self.source, 'cql-api'))

    def test_teaching_cql_image_keeps_teaching_tag_without_full_runtime_scope(self):
        self.assertEqual(self.repository + self.source + '-teaching',
                         image_tag(self.source, 'cql-api', True))

    def test_existing_native_package_profiles_keep_their_teaching_tag(self):
        for profile in ('full', 'tron-api', 'maplibre'):
            with self.subTest(profile=profile):
                self.assertEqual(self.repository + self.source + '-teaching',
                                 image_tag(self.source, profile))


if __name__ == '__main__':
    unittest.main()

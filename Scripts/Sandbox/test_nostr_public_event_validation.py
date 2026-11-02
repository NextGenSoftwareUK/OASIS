"""Public BIP-340 vector and rejection controls; no account/signing/native calls."""
import hashlib
import json
import unittest
from unittest.mock import patch
import NostrPublicEventValidation as validation

# Official bitcoin/bips bip-0340/test-vectors.csv vector 0 (public fields only).
KEY = bytes.fromhex('F9308A019258C31049344F85F89D5229B531C845836F99B08601F113BCE036F9')
MESSAGE = bytes(32)
SIGNATURE = bytes.fromhex('E907831F80848D1069A5371B402410364BDF1C5F8307B0084C55F1CE2DCA8215'
                         '25F66A4A85EA8B71E482A74F382D2CE5EBEEE8FDB2172F477DF4900D310536C0')


class NostrValidationTests(unittest.TestCase):
    def test_official_public_vector(self):
        self.assertTrue(validation.verify_schnorr(KEY, MESSAGE, SIGNATURE))

    def test_modified_message_signature_and_wrong_lengths(self):
        self.assertFalse(validation.verify_schnorr(KEY, b'\x01' + MESSAGE[1:], SIGNATURE))
        self.assertFalse(validation.verify_schnorr(KEY, MESSAGE, SIGNATURE[:-1] + bytes([SIGNATURE[-1] ^ 1])))
        for key, message, signature in [(KEY[:-1], MESSAGE, SIGNATURE), (KEY, MESSAGE[:-1], SIGNATURE),
                                         (KEY, MESSAGE, SIGNATURE[:-1])]:
            self.assertFalse(validation.verify_schnorr(key, message, signature))

    def test_range_and_off_curve_rejection(self):
        self.assertFalse(validation.verify_schnorr(validation.P.to_bytes(32, 'big'), MESSAGE, SIGNATURE))
        self.assertFalse(validation.verify_schnorr(KEY, MESSAGE, validation.P.to_bytes(32, 'big') + SIGNATURE[32:]))
        self.assertFalse(validation.verify_schnorr(KEY, MESSAGE, SIGNATURE[:32] + validation.N.to_bytes(32, 'big')))
        off_curve = bytes.fromhex('EEFDEA4CDB677750A420FEE807EACF21EB9898AE79B9768766E4FAA04A2D4A34')
        self.assertFalse(validation.verify_schnorr(off_curve, MESSAGE, SIGNATURE))

    def event(self):
        event = {'pubkey': KEY.hex(), 'created_at': 123, 'kind': 1, 'tags': [['p', 'abc']],
                 'content': 'café\n"quoted"\\path', 'sig': SIGNATURE.hex()}
        raw = json.dumps([0, event['pubkey'], 123, 1, event['tags'], event['content']],
                         separators=(',', ':'), ensure_ascii=False).encode()
        event['id'] = hashlib.sha256(raw).hexdigest()
        return event

    def test_canonical_utf8_hash_is_checked_before_signature(self):
        event = self.event()
        with patch.object(validation, 'verify_schnorr', return_value=True) as verifier:
            validation.verify_event(event)
            self.assertEqual(verifier.call_args.args[1].hex(), event['id'])
            event['content'] += 'changed'
            with self.assertRaisesRegex(ValueError, 'hash mismatch'):
                validation.verify_event(event)
            self.assertEqual(verifier.call_count, 1)

    def test_signature_failure_is_not_a_successful_observation(self):
        with self.assertRaisesRegex(ValueError, 'signature rejected'):
            validation.verify_event(self.event())


if __name__ == '__main__':
    unittest.main()

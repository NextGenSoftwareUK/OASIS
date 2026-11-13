"""Offline qualification of public NIP-01 events; no signing or secret-key handling.

Public-input BIP-340 verification follows bitcoin/bips bip-0340. This is a probe
utility, not a replacement for the shipping provider's future crypto boundary.
"""
import hashlib
import json
import re

P = 0xFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2F
N = 0xFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEBAAEDCE6AF48A03BBFD25E8CD0364141
G = (0x79BE667EF9DCBBAC55A06295CE870B07029BFCDB2DCE28D959F2815B16F81798,
     0x483ADA7726A3C4655DA4FBFC0E1108A8FD17B448A68554199C47D08FFB10D4B8)


def add(left, right):
    if left is None:
        return right
    if right is None:
        return left
    x, y = left
    u, v = right
    if x == u and (y + v) % P == 0:
        return None
    slope = ((3 * x * x) * pow(2 * y, P - 2, P) if left == right
             else (v - y) * pow((u - x) % P, P - 2, P)) % P
    result_x = (slope * slope - x - u) % P
    return result_x, (slope * (x - result_x) - y) % P


def multiply(point, scalar):
    result = None
    while scalar:
        if scalar & 1:
            result = add(result, point)
        point = add(point, point)
        scalar >>= 1
    return result


def verify_schnorr(public_key, message, signature):
    """Verify only 32-byte event hashes, with strict BIP-340 range/point checks."""
    if len(public_key) != 32 or len(message) != 32 or len(signature) != 64:
        return False
    x = int.from_bytes(public_key, 'big')
    r = int.from_bytes(signature[:32], 'big')
    scalar = int.from_bytes(signature[32:], 'big')
    if x >= P or r >= P or scalar >= N:
        return False
    square = (pow(x, 3, P) + 7) % P
    y = pow(square, (P + 1) // 4, P)
    if y * y % P != square:
        return False
    if y & 1:
        y = P - y
    tag = hashlib.sha256(b'BIP0340/challenge').digest()
    challenge = int.from_bytes(hashlib.sha256(tag + tag + signature[:32] + public_key + message).digest(), 'big') % N
    point = add(multiply(G, scalar), multiply((x, y), (N - challenge) % N))
    return point is not None and point[1] % 2 == 0 and point[0] == r


def verify_event(event):
    """Caller enforces bounded envelope/types first; no relay-supplied id is trusted."""
    for field, width in [('id', 64), ('pubkey', 64), ('sig', 128)]:
        if not isinstance(event.get(field), str) or not re.fullmatch('[0-9a-f]{' + str(width) + '}', event[field]):
            raise ValueError('Canonical event hash/key/signature required.')
    canonical = json.dumps([0, event['pubkey'], event['created_at'], event['kind'], event['tags'], event['content']],
                           separators=(',', ':'), ensure_ascii=False, allow_nan=False).encode('utf-8')
    digest = hashlib.sha256(canonical).digest()
    if digest.hex() != event['id']:
        raise ValueError('NIP-01 event hash mismatch.')
    if not verify_schnorr(bytes.fromhex(event['pubkey']), digest, bytes.fromhex(event['sig'])):
        raise ValueError('BIP-340 event signature rejected.')

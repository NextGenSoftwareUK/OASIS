import base64
import hashlib
import hmac
import json
import os
import secrets
import uuid
from pathlib import Path

from algosdk import error, kmd, logic, transaction
from algosdk.v2client import algod
from Cryptodome.Cipher import AES


TOKEN = "a" * 64
CONTRACT = (
    Path(__file__).resolve().parents[2]
    / "Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AlgorandOASIS/Contracts"
)


def digest(data):
    return hashlib.sha256(data).digest()


def chunk_name(key, revision, index):
    return b"c" + digest(key + revision + index.to_bytes(8, "big"))


def manifest_name(key, revision):
    return b"m" + digest(key + revision)


def run(node, wallet, handle, writer, outsider):
    def submit(txn):
        signed = wallet.sign_transaction(handle, "", txn)
        txid = node.send_transaction(signed)
        assert txid == txn.get_txid()
        return transaction.wait_for_confirmation(node, txid, 8)

    def program(suffix):
        return base64.b64decode(
            node.compile((CONTRACT / f"RecordBoxes.{suffix}.teal").read_text(encoding="utf-8"))["result"],
            validate=True,
        )

    approval = program("approval")
    clear = program("clear")
    empty = transaction.StateSchema(0, 0)
    created = submit(transaction.ApplicationCreateTxn(
        writer, node.suggested_params(), transaction.OnComplete.NoOpOC,
        approval, clear, empty, empty,
    ))
    app = created["application-index"]
    native = node.application_info(app)["params"]
    assert native["creator"] == writer
    assert base64.b64decode(native["approval-program"], validate=True) == approval
    submit(transaction.PaymentTxn(writer, node.suggested_params(), logic.get_application_address(app), 5_000_000))

    def call(sender, args, names):
        return transaction.ApplicationNoOpTxn(
            sender, node.suggested_params(), app, app_args=args,
            boxes=[(0, name) for name in names],
        )

    def reject(txn):
        try:
            submit(txn)
        except error.AlgodHTTPError as exc:
            message = str(exc).lower()
            if "assert failed" not in message:
                raise
        else:
            raise AssertionError("The application accepted a disallowed transaction")

    def read(name):
        box = node.application_box_by_name(app, name)
        assert base64.b64decode(box["name"], validate=True) == name
        return base64.b64decode(box["value"], validate=True)

    if destination := os.environ.get("OASIS_ALGORAND_ENROLLED_FIXTURE_ENV"):
        fixture = uuid.uuid4()
        storage_key = secrets.token_bytes(32)
        key = hmac.digest(storage_key, f"oasis:algorand:box:v1:holon:{fixture}".encode(), "sha256")
        genesis = node.suggested_params().gh
        head = b"h" + key
        descriptor_name = b"d" + key
        format_name = "oasis-aes256gcm-v1:"
        descriptor_nonce = secrets.token_bytes(12)
        descriptor_cipher = AES.new(storage_key, AES.MODE_GCM, nonce=descriptor_nonce)
        descriptor_cipher.update((format_name + f"oasis:algorand:discovery:v1:{genesis}:{app}:{key.hex().upper()}").encode())
        descriptor_ciphertext, descriptor_tag = descriptor_cipher.encrypt_and_digest(str(fixture).encode())
        descriptor = format_name.encode() + base64.b64encode(descriptor_nonce + descriptor_tag + descriptor_ciphertext)
        predecessor = bytes(32)
        versions = () if os.environ.get("OASIS_ALGORAND_RECORD_WRITE_TEST") == "true" else (1, 2)
        for version in versions:
            revision = secrets.token_bytes(32)
            context = f"oasis:algorand:v1:{genesis}:{app}:holon:{fixture}:{revision.hex().upper()}"
            nonce = secrets.token_bytes(12)
            cipher = AES.new(storage_key, AES.MODE_GCM, nonce=nonce)
            cipher.update((format_name + context).encode("utf-8"))
            payload = json.dumps({
                "Id": str(fixture), "Version": version, "Name": f"Native version {version}",
                "Description": "native fixture " * 62,
            }).encode("utf-8")
            ciphertext, tag = cipher.encrypt_and_digest(payload)
            envelope = format_name.encode("ascii") + base64.b64encode(nonce + tag + ciphertext)
            chunks = [envelope[index:index + 896] for index in range(0, len(envelope), 896)]
            names = [chunk_name(key, revision, index) for index in range(len(chunks))]
            for index, (name, chunk) in enumerate(zip(names, chunks, strict=True)):
                submit(call(writer, [b"stage", key, revision, index.to_bytes(8, "big"), chunk], [name]))
            hashes = b"".join(digest(chunk) for chunk in chunks)
            submit(call(writer, [b"publish", key, predecessor, revision, hashes, descriptor],
                        [head, manifest_name(key, revision), descriptor_name, *names]))
            predecessor = digest(read(head))

        settings = {
                "OASIS_ALGORAND_RECORD_APP": app,
                "OASIS_ALGORAND_RECORD_GENESIS": genesis,
                "OASIS_ALGORAND_RECORD_WRITER": writer,
                "OASIS_ALGORAND_RECORD_PROGRAM": digest(approval).hex(),
                "OASIS_ALGORAND_RECORD_KEY": base64.b64encode(storage_key).decode("ascii"),
                "OASIS_ALGORAND_RECORD_ID": fixture,
        }
        if not versions:
            signing_key = base64.b64decode(wallet.export_key(handle, "", writer), validate=True)
            if len(signing_key) != 64:
                raise RuntimeError("Unexpected ephemeral LocalNet signing key format")
            settings["OASIS_ALGORAND_RECORD_WRITER_SEED"] = base64.b64encode(signing_key[:32]).decode("ascii")
        with open(destination, "w", encoding="utf-8") as output:
            for name, value in settings.items():
                output.write(f"{name}={value}\n")
        print("Prepared the enrolled record application and its owned LocalNet fixture.")
        return

    key = secrets.token_bytes(32)
    first_revision = secrets.token_bytes(32)
    second_revision = secrets.token_bytes(32)
    absent = bytes(32)
    head = b"h" + key
    descriptor_name = b"d" + key
    descriptor = secrets.token_bytes(95)
    chunks = [secrets.token_bytes(896), secrets.token_bytes(93)]
    first_names = [chunk_name(key, first_revision, index) for index in range(len(chunks))]
    first_manifest = manifest_name(key, first_revision)
    first_refs = [head, first_manifest, descriptor_name, *first_names]
    hashes = b"".join(digest(chunk) for chunk in chunks)
    stage_args = [b"stage", key, first_revision, bytes(8), chunks[0]]
    reject(call(outsider, stage_args, first_names[:1]))
    submit(call(writer, stage_args, first_names[:1]))
    reject(call(writer, stage_args, first_names[:1]))
    publish_args = [b"publish", key, absent, first_revision, hashes, descriptor]
    reject(call(writer, publish_args, first_refs))
    submit(call(writer, [b"stage", key, first_revision, (1).to_bytes(8, "big"), chunks[1]], first_names[1:]))
    reject(call(writer, [*publish_args[:4], bytes(len(hashes)), descriptor], first_refs))
    submit(call(writer, publish_args, first_refs))
    first_head = first_revision + absent + absent + hashes
    assert read(head) == first_head
    assert read(first_manifest) == first_head
    assert read(descriptor_name) == descriptor
    assert [read(name) for name in first_names] == chunks

    replacement = secrets.token_bytes(407)
    second_name = chunk_name(key, second_revision, 0)
    second_manifest = manifest_name(key, second_revision)
    second_refs = [head, second_manifest, descriptor_name, second_name]
    submit(call(writer, [b"stage", key, second_revision, bytes(8), replacement], [second_name]))
    reject(call(writer, [b"publish", key, absent, second_revision, digest(replacement), descriptor], second_refs))
    assert read(head) == first_head
    predecessor = digest(first_head)
    reject(call(writer, [b"publish", key, predecessor, second_revision, digest(replacement), b"different"], second_refs))
    assert read(head) == first_head
    submit(call(writer, [b"publish", key, predecessor, second_revision, digest(replacement), descriptor], second_refs))
    second_head = second_revision + first_revision + predecessor + digest(replacement)
    assert read(second_manifest) == second_head
    assert read(head) == second_head
    reject(call(writer, [b"publish", key, digest(second_head), first_revision, hashes, descriptor], first_refs))
    assert read(head) == second_head
    assert read(first_manifest) == first_head
    assert read(second_name) == replacement
    assert [read(name) for name in first_names] == chunks
    print("Native record boxes verified write authorization, immutable manifests and chunks, and historical lineage.")


def main():
    node = algod.AlgodClient(TOKEN, "http://127.0.0.1:4001")
    if node.suggested_params().gen not in {"sandnet-v1", "localnet-v1", "dockernet-v1"}:
        raise RuntimeError("The record-box fixture requires an owned LocalNet")
    wallet = kmd.KMDClient(TOKEN, "http://127.0.0.1:4002")
    wallet_id = next(item["id"] for item in wallet.list_wallets() if item["name"] == "unencrypted-default-wallet")
    handle = wallet.init_wallet_handle(wallet_id, "")
    try:
        writer, outsider = [
            address for address in wallet.list_keys(handle) if node.account_info(address)["amount"] > 6_000_000
        ][:2]
        run(node, wallet, handle, writer, outsider)
    finally:
        wallet.release_wallet_handle(handle)


if __name__ == "__main__":
    main()

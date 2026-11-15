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
    submit(transaction.PaymentTxn(writer, node.suggested_params(), logic.get_application_address(app), 10_000_000))

    def call(sender, args, names):
        return transaction.ApplicationNoOpTxn(
            sender, node.suggested_params(), app, app_args=args,
            boxes=[(0, name) for name in names],
        )

    def submit_group(calls):
        grouped = transaction.assign_group_id(calls)
        signed = [wallet.sign_transaction(handle, "", txn) for txn in grouped]
        txid = node.send_transactions(signed)
        assert txid == grouped[0].get_txid()
        transaction.wait_for_confirmation(node, txid, 8)

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

    def publication(sender, key, predecessor, revision, chunks, descriptor, hashes=None):
        head = b"h" + key
        names = [chunk_name(key, revision, index) for index in range(len(chunks))]
        stages = [call(sender, [b"stage", key, revision, index.to_bytes(8, "big"), chunk], [head, name])
                  for index, (name, chunk) in enumerate(zip(names, chunks, strict=True))]
        if hashes is None:
            hashes = b"".join(digest(chunk) for chunk in chunks)
        publish = call(writer, [b"publish", key, predecessor, revision, hashes, descriptor],
                       [head, manifest_name(key, revision), b"d" + key, *names])
        return stages + [publish]

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
        writing = os.environ.get("OASIS_ALGORAND_RECORD_WRITE_TEST") == "true"
        resuming = os.environ.get("OASIS_ALGORAND_RECORD_PURGE_RESUME") == "true"
        versions = (1, 2) if resuming or not writing else ()
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
            submit_group(publication(writer, key, predecessor, revision, chunks, descriptor))
            predecessor = digest(read(head))

        if resuming:
            current = read(head)
            submit(call(writer, [b"freeze", key, digest(current)], [head, descriptor_name]))
            checkpoint = read(head)
            assert checkpoint == revision + digest(current)
            submit(call(writer, [b"purge", key, checkpoint], [head, manifest_name(key, revision), *names]))
            assert read(head) == current[32:96]
            print("Paused the native deletion after removing the latest revision.")

        settings = {
                "OASIS_ALGORAND_RECORD_APP": app,
                "OASIS_ALGORAND_RECORD_GENESIS": genesis,
                "OASIS_ALGORAND_RECORD_WRITER": writer,
                "OASIS_ALGORAND_RECORD_PROGRAM": digest(approval).hex(),
                "OASIS_ALGORAND_RECORD_KEY": base64.b64encode(storage_key).decode("ascii"),
                "OASIS_ALGORAND_RECORD_ID": fixture,
        }
        if writing:
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
    hashes = b"".join(digest(chunk) for chunk in chunks)
    stage_args = [b"stage", key, first_revision, bytes(8), chunks[0]]
    reject(call(writer, stage_args, [head, first_names[0]]))
    def reject_group(group):
        try:
            submit_group(group)
        except error.AlgodHTTPError as exc:
            if "assert failed" not in str(exc).lower():
                raise
        else:
            raise AssertionError("The application accepted a disallowed publication group")

    reject_group(publication(outsider, key, absent, first_revision, chunks, descriptor))
    reject_group(publication(writer, key, absent, first_revision, chunks, descriptor, bytes(len(hashes))))
    assert not node.application_boxes(app)["boxes"]
    submit_group(publication(writer, key, absent, first_revision, chunks, descriptor))
    first_head = first_revision + absent + absent + hashes
    assert read(head) == first_head
    assert read(first_manifest) == first_head
    assert read(descriptor_name) == descriptor
    assert [read(name) for name in first_names] == chunks

    replacement = secrets.token_bytes(407)
    second_name = chunk_name(key, second_revision, 0)
    second_manifest = manifest_name(key, second_revision)
    reject_group(publication(writer, key, absent, second_revision, [replacement], descriptor))
    assert read(head) == first_head
    predecessor = digest(first_head)
    reject_group(publication(writer, key, predecessor, second_revision, [replacement], b"different"))
    assert read(head) == first_head
    submit_group(publication(writer, key, predecessor, second_revision, [replacement], descriptor))
    second_head = second_revision + first_revision + predecessor + digest(replacement)
    assert read(second_manifest) == second_head
    assert read(head) == second_head
    reject_group(publication(writer, key, digest(second_head), first_revision, chunks, descriptor))
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
            address for address in wallet.list_keys(handle) if node.account_info(address)["amount"] > 12_000_000
        ][:2]
        run(node, wallet, handle, writer, outsider)
    finally:
        wallet.release_wallet_handle(handle)


if __name__ == "__main__":
    main()

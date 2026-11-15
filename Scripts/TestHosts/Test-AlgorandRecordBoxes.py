import base64
import hashlib
import secrets
from pathlib import Path

from algosdk import error, kmd, logic, transaction
from algosdk.v2client import algod


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

    key = secrets.token_bytes(32)
    first_revision = secrets.token_bytes(32)
    second_revision = secrets.token_bytes(32)
    absent = bytes(32)
    head = b"h" + key
    chunks = [secrets.token_bytes(896), secrets.token_bytes(93)]
    first_names = [chunk_name(key, first_revision, index) for index in range(len(chunks))]
    first_manifest = manifest_name(key, first_revision)
    first_refs = [head, first_manifest, *first_names]
    hashes = b"".join(digest(chunk) for chunk in chunks)
    stage_args = [b"stage", key, first_revision, bytes(8), chunks[0]]
    reject(call(outsider, stage_args, first_names[:1]))
    submit(call(writer, stage_args, first_names[:1]))
    reject(call(writer, stage_args, first_names[:1]))
    publish_args = [b"publish", key, absent, first_revision, hashes]
    reject(call(writer, publish_args, first_refs))
    submit(call(writer, [b"stage", key, first_revision, (1).to_bytes(8, "big"), chunks[1]], first_names[1:]))
    reject(call(writer, [*publish_args[:4], bytes(len(hashes))], first_refs))
    submit(call(writer, publish_args, first_refs))
    first_head = first_revision + absent + absent + hashes
    assert read(head) == first_head
    assert read(first_manifest) == first_head
    assert [read(name) for name in first_names] == chunks

    replacement = secrets.token_bytes(407)
    second_name = chunk_name(key, second_revision, 0)
    second_manifest = manifest_name(key, second_revision)
    second_refs = [head, second_manifest, second_name]
    submit(call(writer, [b"stage", key, second_revision, bytes(8), replacement], [second_name]))
    reject(call(writer, [b"publish", key, absent, second_revision, digest(replacement)], second_refs))
    assert read(head) == first_head
    predecessor = digest(first_head)
    submit(call(writer, [b"publish", key, predecessor, second_revision, digest(replacement)], second_refs))
    second_head = second_revision + first_revision + predecessor + digest(replacement)
    assert read(second_manifest) == second_head
    assert read(head) == second_head
    reject(call(writer, [b"publish", key, digest(second_head), first_revision, hashes], first_refs))
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

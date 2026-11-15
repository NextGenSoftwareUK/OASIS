import os

from algosdk import account, encoding, kmd, transaction
from algosdk.v2client import algod

ALGOD_URL = "http://127.0.0.1:4001"
KMD_URL = "http://127.0.0.1:4002"
TOKEN = "a" * 64
AMOUNT = 200_000


def main():
    node = algod.AlgodClient(TOKEN, ALGOD_URL)
    parameters = node.suggested_params()
    if parameters.gen not in {"sandnet-v1", "localnet-v1", "dockernet-v1"}:
        raise RuntimeError(f"Unexpected LocalNet genesis ID: {parameters.gen}")

    wallets = kmd.KMDClient(TOKEN, KMD_URL)
    wallet_id = next(wallet["id"] for wallet in wallets.list_wallets()
                     if wallet["name"] == "unencrypted-default-wallet")
    handle = wallets.init_wallet_handle(wallet_id, "")
    try:
        sender = next(address for address in wallets.list_keys(handle)
                      if node.account_info(address)["amount"] > 2 * AMOUNT)
        _, recipient = account.generate_account()
        note = f"oasis-owned-indexer-test:{recipient}"
        payment = transaction.PaymentTxn(sender, parameters, recipient, AMOUNT, note=note.encode("utf-8"))
        signed = wallets.sign_transaction(handle, "", payment)
    finally:
        wallets.release_wallet_handle(handle)

    with open(os.environ["GITHUB_ENV"], "a", encoding="utf-8") as env:
        env.write(f"OASIS_ALGORAND_LOCALNET_SIGNED={encoding.msgpack_encode(signed)}\n")
        env.write(f"OASIS_ALGORAND_LOCALNET_TX={payment.get_txid()}\n")
        env.write(f"OASIS_ALGORAND_LOCALNET_RECEIVER={recipient}\n")
        env.write(f"OASIS_ALGORAND_LOCALNET_NOTE={note}\n")


if __name__ == "__main__":
    main()

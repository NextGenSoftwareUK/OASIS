"""Build one zero-value TestNet self-payment without retaining the signing key."""

import os

from algosdk import account, encoding, mnemonic, transaction
from algosdk.v2client import algod

ALGOD_URL = "https://testnet-api.algonode.cloud"
TESTNET_HASH = "SGO1GKSzyE7IEPItTxCByw9x8FmnrCDexi9/cOUJOiI="
MAX_FEE = 1_000


def main():
    private_key = mnemonic.to_private_key(os.environ["OASIS_ALGORAND_TESTNET_MNEMONIC"])
    sender = account.address_from_private_key(private_key)
    node = algod.AlgodClient("", ALGOD_URL)
    parameters = node.suggested_params()
    if parameters.gen != "testnet-v1.0" or parameters.gh != TESTNET_HASH:
        raise RuntimeError("Algod did not identify the expected public TestNet chain.")
    if parameters.min_fee > MAX_FEE:
        raise RuntimeError("TestNet minimum fee exceeds the fixture fee budget.")

    balance = node.account_info(sender)
    if balance["amount"] - balance["min-balance"] < MAX_FEE:
        raise RuntimeError("The protected TestNet account needs enough test ALGO to pay the transaction fee.")
    parameters.flat_fee = True
    parameters.fee = MAX_FEE
    parameters.last = parameters.first + 200
    payment = transaction.PaymentTxn(sender, parameters, sender, 0)
    signed = payment.sign(private_key)

    with open(os.environ["OASIS_ALGORAND_FIXTURE_ENV"], "x", encoding="utf-8") as env:
        env.write(f"OASIS_ALGORAND_TESTNET_SIGNED={encoding.msgpack_encode(signed)}\n")
        env.write(f"OASIS_ALGORAND_TESTNET_TX={payment.get_txid()}\n")
        env.write(f"OASIS_ALGORAND_TESTNET_SENDER={sender}\n")


if __name__ == "__main__":
    main()

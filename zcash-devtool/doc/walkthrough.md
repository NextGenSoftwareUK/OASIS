A Zcash-Devtool Primer
======================

This page documents a full walkthrough of how to set up and use the
`zcash-devtool` tooling. It is intended to serve as a guide for how to get set
up and how to add your own functionality to the tool.

Development Environment
-----------------------

In order to work with `zcash-devtool`, the first thing you will need is a Rust
development environment. If you don't already have Rust installed, follow the
directions at https://rustup.rs/ to get a Rust toolchain installed.

You'll also need the source code, as there is no binary distribution of
`zcash-devtool`. It is built by developers, for developers, for testing and
development of new Zcash functionality; DO NOT commit significant funds
to the management of the `zcash-devtool` embedded wallet.

Obtain the source code by cloning the github repository:

```bash
λ git clone https://github.com/zcash/zcash-devtool
```

First Steps
-----------

Now we're ready to take our first look at the capabilities that `zcash-devtool`
provides. We will build and run the tool using `cargo run`.

```bash
λ cargo run --release --all-features -- --help
```

This results in output like the following:

```bash
Usage: zcash-devtool [COMMAND]

Commands:
  inspect                  Inspect Zcash-related data
  wallet                   Manipulate a local wallet backed by `zcash_client_sqlite`
  zip48                    Manipulate multisig accounts
  pczt                     Send funds using PCZTs
  keystone                 Emulate a Keystone device
  create-multisig-address  Commands for managing multisig addresses
  help                     Print this message or the help of the given subcommand(s)

Options:
  -h, --help  Print help
```

You can get additional information about a given command:

```bash
λ cargo run --release --all-features -- wallet --help
```

Wallet Initialization
---------------------

For the purposes of this demo, we're going to set up a testnet wallet. We're
going to create the wallet in a `../dev-wallet` directory; this is
intentionally outside of the root of the git repository, just so that we don't
accidentally end up committing wallet data to the repository when we go to
commit code changes. In addition to the wallet databases and configuration, the
initialization process will generate an `age` key file at
`../dev-wallet/dev-key.txt`; this key will be used to encrypt the wallet's
mnemonic seed. We'll also use the `testnet.zec.rocks` server for this setup.

```bash
λ cargo run --release --all-features -- wallet -w ../dev-wallet init --name "ZDevTest" \
  -i ../dev-wallet/dev-key.txt -n test -s zecrocks
```

If we look at the `./../dev-wallet` directory now, we can see that a number of
files and directories have been created:

```bash
λ ll ../dev-wallet/
total 352
-rw-r--r-- 1 ... ...  16384 Mar  3 17:48 blockmeta.sqlite
drwxrwxr-x 2 ... ...   4096 Mar  3 17:48 blocks/
-rw-r--r-- 1 ... ... 323584 Mar  3 17:48 data.sqlite
-rw------- 1 ... ...    189 Mar  3 16:59 dev-key.txt
-rw-rw-r-- 1 ... ...    786 Mar  3 17:48 keys.toml
drwxrwxr-x 4 ... ...   4096 Mar  3 17:43 tor/
```

A look in the `keys.toml` file will show us our (encrypted) key, along with
basic wallet metadata:

```bash
λ cat ../dev-wallet/keys.toml
mnemonic = """
-----BEGIN AGE ENCRYPTED FILE-----
<...>
-----END AGE ENCRYPTED FILE-----
"""
network = "test"
birthday = 3274265
```

Receiving Payments
------------------

In order to receive a payment, we'll need an address:

```bash
λ cargo run --release --all-features -- wallet -w ../dev-wallet list-addresses
```

We can use another testnet wallet to send some payments to the wallet, or mine
coins to fund it. If you're running a zcashd testnet node, it's easy to enable
mining by adding the following configuration to your `~/.zcash/zcash.conf` file.

```
gen=1
equihashsolver=tromp
mineraddress=YOUR_ADDRESS_HERE
```

Now, in order to detect those funds, we need to scan the chain:

```bash
λ cargo run --release --all-features -- wallet -w ../dev-wallet sync -s zecrocks
```

Having scanned the chain, we can now check the balance

```bash
λ cargo run --release --all-features -- wallet -w ../dev-wallet balance
```

You can explore more of the wallet commands available using:

```bash
λ cargo run --release --all-features -- wallet --help
```

PCZT Support
------------

It's possible to use `zcash-devtool` as a "simulated hardware wallet" via use
of the `pczt` suite of commands. These commands provide reference
implementations for PCZT creation, proving, signing, combining, inspection, and
more.

This section will simulate an interaction between an online wallet that scans
the chain with a viewing key and an offline wallet that holds the signing keys.
For the purpose of this simulation, the `../dev-wallet` wallet we've created
will take on the role of our hardware device.

First, we're going to set up our "online wallet" by exporting a viewing key
from the development wallet, and using it to initialize a new view-only
wallet.

```bash
λ cargo run --release --all-features -- wallet -w ../dev-wallet/ list-accounts
```

That produces output like the following:

```bash
Account ef4cde11-ac2b-4ac3-becf-3df518ee2c97
     Name: ZDevTest
     UIVK: <...>
     UFVK: <dev_ufvk>
     Source: Derived { derivation: Zip32Derivation { seed_fingerprint: SeedFingerprint(<dev_seed_fp>), account_index: AccountId(0) }, key_source: None }
```

Using the UFVK value provided there, we can initialize a new view-only wallet
that we'll use as the online device in this exchange. After initializing it,
we'll also sync that wallet. It is important to also include the seed
fingerprint and HD account index, because we will need those to make it easy
for the signing wallet to choose the correct key.

```bash
λ cargo run --release --all-features -- wallet -w ../view-wallet/ init-fvk \
    --name ZDevView \
    --fvk "<dev_ufvk>" \
    --birthday 3274265 \
    --seed-fingerprint "<dev_seed_fp>" \
    --hd-account-index 0 \
    -s zecrocks
λ cargo run --release --all-features -- wallet -w ../view-wallet sync -s zecrocks
```

Asking for the balance here should show the same balance as we saw in our
original wallet:

```bash
λ cargo run --release --all-features -- wallet -w ../view-wallet balance
```

We're now going to use the view-only wallet to create a PCZT. If you don't have
a separate testnet wallet to receive these funds, you can always use the
wallet's own address, or you can create a separate wallet to better simulate
multiple independent wallets interacting.

```bash
λ cargo run --release --all-features -- pczt -w ../view-wallet create --address <...> --value 12340000 --memo "Hello from a PCZT!" > ../test_pczt.created
```

We can inspect that with the `pzct inspect` subcommand.

```bash
λ cargo run --release --all-features -- pczt inspect < ../test_pczt.created
```

We can create the proofs for that PCZT using the view-only (online) wallet:

```bash
λ cargo run --release --all-features -- pczt -w ../view-wallet prove < ../test_pczt.created > ../test_pczt.proven
```

Now, using the "offline" wallet (our original `../dev-wallet`), we're going to
add the signatures. Note that we use `../test_pczt.created` as the input, not
the version containing the proofs. Once we've created both the signed and
proven PCZTs separately, we'll combine them in a later step. Note that, because
we're making signatures, we have to provide the `age` identity file to use to
decrypt the spending key.

```bash
λ cargo run --release --all-features -- pczt -w ../dev-wallet sign --identity ../dev-wallet/dev-key.txt < ../test_pczt.created > ../test_pczt.signed
```

With proofs and signatures complete, we can combine the PCZTs. This operation
doesn't require a wallet at all.

```bash
λ cargo run --release --all-features -- pczt combine -i ../test_pczt.signed -i ../test_pczt.proven > ../test_pczt.combined
```

Now all that's left is to extract the finished transaction, and send it to the
chain. We'll do this using the "online" view-only wallet.

```bash
λ cargo run --release --all-features -- pczt -w ../view-wallet/ send -s zecrocks < ../test_pczt.combined
```

Local Development
-----------------

It's often useful to build with local versions of the crates that
`zcash-devtool` depends upon. Doing so is easy; just add a set of patch
directives to the root `Cargo.toml` file. The example below assumes that you
have [librustzcash](https://github.com/zcash/librustzcash) and
[incrementalmerkletree](https://github.com/zcash/incrementalmerkletree) checked
out locally. With these patch directives in place, local changes to the relevant
crates will be immediately usable in the devtool.

```
[patch.crates-io]
equihash = { path = "../librustzcash/components/equihash/" }
f4jumble = { path = "../librustzcash/components/f4jumble/" }
pczt = { path = "../librustzcash/pczt/" }
transparent = { package = "zcash_transparent", path = "../librustzcash/zcash_transparent/" }
zcash_address = { path = "../librustzcash/components/zcash_address/" }
zcash_client_backend = { path = "../librustzcash/zcash_client_backend" }
zcash_client_sqlite = { path = "../librustzcash/zcash_client_sqlite" }
zcash_encoding = { path = "../librustzcash/components/zcash_encoding/" }
zcash_keys = { path = "../librustzcash/zcash_keys/" }
zcash_primitives = { path = "../librustzcash/zcash_primitives" }
zcash_proofs = { path = "../librustzcash/zcash_proofs" }
zcash_protocol = { path = "../librustzcash/components/zcash_protocol" }
zip321 = { path = "../librustzcash/components/zip321/" }

incrementalmerkletree = { path = "../incrementalmerkletree/incrementalmerkletree/" }
shardtree = { path = "../incrementalmerkletree/shardtree/" }
```

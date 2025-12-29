Wallet App Threat Model
========================

This threat model is intended for curious technical users of the ECC wallet
apps as well as developers making use of the SDK in their own apps. The threat
model applies to the Zashi wallet, and should apply to any Zcash wallet built
on top of the ECC SDKs, unless significant modifications have been
made. See the [Invariant-Centric Threat Modeling](https://github.com/defuse/ictm)
for a complete explanation of the threat modeling methodology we use. Here's a
short summary of the methodology:

- This document lists "security invariants" that the apps and SDK should
  currently provide. Users and developers *should not* rely on any security or
  privacy properties that are not explicitly listed here. If there's a security
  or privacy property that you would like to be able to rely on, but isn't
  listed here, then please raise an issue on GitHub.
- We aim to state security invariants in a language that end-users would
  understand. If a security invariant uses technical language or involves
  complicated concepts then users are unlikely to understand it, which could
  lead to them being overconfident in their use of the software.

*If you are a security auditor, please try to break one of the security
invariants or think about which important security invariants might be
missing from this list!*

The security properties that the apps and SDKs provide depend on how powerful
the adversary trying to attack the user is. This threat model's security
invariants are organized into sections for each kind of adversary. We start
with the most powerful kind of adversary, one who has completely compromised
the lightwalletd server the wallet connects to, can intercept network
traffic, install apps on the user's phone, etc., and end with the weakest
kind of adversary, one who simply knows the user's address and can observe
the public blockchain. In order to simplify this document, we assume that the
stronger adversaries have all of the capabilities of the weaker adversaries.
For each kind of adversary, we list:

- Which security invariants we expect are satisfied against that adversary
  (and all weaker ones). If one of these are false, then it's a security bug.
  If a user is relying on a security invariant that's not in the threat model,
  then that's also considered a security bug.
- Known weaknesses: Which security invariants we know are *not* satisfied
  against that adversary (and all stronger ones). If a user does not understand
  one of these weaknesses, then that is a security problem in the app's
  UX/documentation. We include brief technical details of each weakness.

There are several weaknesses that we believe most users would find
counter-intuitive, or at least not expect to be the case based on their
experience using other cryptocurrency wallets or `zcashd`. We've highlighted
those ones in bold.

Let's begin with the most powerful kind of adversary considered by our model.

## Lightwalletd-Compromising Adversary

**Description:** The lightwalletd service the user connects to is compromised
or outright malicious. In addition to having complete control over
lightwalletd, the adversary can intercept all of the app's network traffic and
can run code as another app on the user's phone (e.g. a fake calculator app).
The adversary knows some addresses belonging to the user. Accidental reorgs
happen regularly.

We expect the following security invariants to be satisfied when the user is
attacked by this kind of adversary as well as any of the weaker ones in the
sections below. The adversary...

- can't execute arbitrary code on the user's phone.
- can't learn any of the user's cryptographic key material (spending keys,
  viewing keys, seed phrase, etc.)
- can't steal the user's funds.
- can't make the user send funds when they did not intend to.
- can't burn the user's funds or otherwise make them unspendable.
- can't cause the funds the user sends to someone else to be gone from their
  wallet but be unspendable by the recipient.
- can't make the funds go to someone else when someone was trying to send the
  user funds.
- can't make the user send funds to the wrong address.
- can't tell what the user's current shielded balance is (aside from it being
  zero when the wallet is created).
- can't learn information about the value, memo field, etc. of shielded
  transactions the user receives.
- can't learn information about the value, memo field, etc. of shielded
  transactions the user sends.
- can't learn who the user is sending/receiving funds to/from in fully-shielded
  transactions as long as the other user isn't using the same lightwalletd
  service provider and there is no collusion between the adversary and that
  other service provider.
- can't learn information about the user's shielded balance over time (aside
  from the assumption that it must be nonzero after they've received
  transactions).
- can't find out one of the user's wallet addresses unless the user has given
  it out or the adversary has a guess for what it is.
  - **Note that this adversary *can* check whether an address they have belongs
    to the user, see the weaknesses in the sections below.**
- can't send money to the user at any address of theirs that the adversary did
  not already know about.
- can't cause a transaction the user is receiving to fail on the Zcash network.
- can't make the user send the wrong amount of funds.
- can't make the user send a transaction with a memo field they did not intend.
- can't make it look like a payment the user received came from a transaction
  other than the one it actually came from.
- can't make it look (to someone else) like the user is sending funds to
  somewhere they are not.
- can't make it look (to someone else) like the user is receiving money from
  somewhere that they are not.
- can't cause the app to display a false official-looking message.
- can't see any of the user's wallet history when they connect to this
  lightwalled instance for the first time (after previously using a different one).
- can't make the wallet display transaction information where part of it comes
  from one transaction and another part of it comes from another transaction
  (such displaying the memo field from one transaction with the value of
  another transaction).

There are some known weaknesses this adversary can exploit. In addition to
all of the known weaknesses from the sections below (which also apply to this
adversary, because this one is capable of all the same things and more), the
adversary can...

- **make the user think they have (or will have) spendable funds when they don't.**
  - They can repeat a transaction to the user's wallet many times to cause the
    wallet to think it has more balance than it can actually spend.
- **make the user think their balance is lower than it actually is.**
  - They can omit transactions destined to user, so that the user's wallet can
    have spendable funds that it isn't aware of.
- **make the user think a transaction they sent or received succeeded when it actually failed.**
  - If the transaction gets reorged-away or never mined, the adversary could make
    it look like it was actually mined.
- **make the user think a transaction they sent or received failed when it actually succeeded.**
  - They could omit the transaction, making it look like it failed when in fact it
    was mined on the Zcash network.

We plan to eventually fix these issues by implementing the block header and
note commitment tree validation specified in [ZIP 307](https://github.com/zcash/zips/issues/341).

Let's move on to the second-most powerful kind of adversary we will consider.

## Typical Adversary

**Description:** There is a trust relationship between the user and the
lightwalletd server operator. Lightwalletd only ever provides valid
information coming from a consistent Zcash blockchain state. The information
is not guaranteed to be recent, and part of it may change (e.g. after a
reorg) and even revert to old state. The connection to lightwallletd is
protected by TLS, which we assume to be secure. The adversary (a) can
intercept all of the app's network traffic, (b) can run code as an app on the
user's phone (e.g. fake calculator app), and (c) can read (but not write to)
the lightwalletd server's private memory. The adversary knows some addresses
belonging to the user. Accidental reorgs happen regularly. **NB:** *If the
lightwalletd server gets compromised temporarily, which we should assume will
eventually happen, the security properties degrade to the
Lightwalletd-Compromising Adversary for the duration of the compromise, and
possibly longer if the effects of the attack persist.*

We expect the following security invariants to be satisfied when the user is
attacked by this kind of adversary as well as any of the weaker ones in the
sections below. The adversary...

- can't make the user think their balance is lower than it actually is.
- can't make the user think they have (or will have) spendable funds when
  they don't.
- can't make the user think a transaction they sent or received succeeded
  when it actually failed.
- can't make the user think a transaction they sent or received failed when
  it actually succeeded.

There are no known weaknesses that apply to this adversary specifically. All
of the known weaknesses in the sections below for weaker adversaries apply to
this adversary as well.

## Network- and Lightwalletd-Surveilling Adjacent-App Adversary

**Description:** The adversary can only (a) intercept all network traffic
between the app and the internet, (b) can run code as an app on the user's
phone (e.g. fake calculator app), and (c) intercept the traffic between the
lightwalletd server and the Internet. The adversary knows some addresses
belonging to the user. Accidental reorgs happen regularly. We assume defenses
are in place to detect eclipsing of the lightwalletd node.

There are no security invariants that we expect to be satisfied specifically
for this adversary but not the one discussed in the previous section.

There are several known weaknesses that this kind of adversary can exploit.
The adversary can...

- **tell *that* and *when* the user received a fully-shielded transaction.**
  - The wallet fetches the memo from lightwalletd separately, which uses
    more bandwidth, and that is visible even though the connection is
    encrypted.
  - If the bandwidth side channels are fixed, the act of sending and receiving
    transactions is visible to the lightwalletd server, so this would become a
    known weakness in the Typical scenario.
- **tell *that* and *when* the user sends a fully-shielded transaction.**
  - The act of sending a transaction uses more bandwidth, which is visible
    even though the connection is encrypted.
  - If the bandwidth side channels are fixed, the act of sending and receiving
    transactions is visible to the lightwalletd server, so this would become a
    known weakness in the Typical scenario.
- **learn who the user is sending/receiving funds to/from in fully-shielded transactions.**
  - When both users are using the same lightwalletd instance, even though the
    connections are encrypted, the adversary will be able to correlate
    bandwidth spikes that look like sends from one user with bandwidth spikes
    that look like receives from another user.
  - If the bandwidth side channels are fixed, the act of sending and receiving
    transactions is visible to the lightwalletd server, so this would become a
    known weakness in the Typical scenario.
- **tell how many transactions the user has sent or received over time.**
  - For the same reasons as above.
- **determine whether or not the user's wallet owns a particular address.**
  - The adversary could send funds to that address and watch to see if there
    are bandwidth spikes that look like the wallet fetched a memo around the
    same time.
  - If the bandwidth side channels are fixed, the act of receiving a
    transaction is still visible to the lightwalletd server so this would
    become a known weakness in the Typical scenario.
- **tell who the user is.**
  - The adversary knows the user's IP address, which could lead them to the
    user's real identity.
- **tell where the user is.**
  - The adversary could look up the user's IP address in a geolocation database
    to approximate their location.
- **tell that the user spends or receives money according to a certain pattern
  (e.g. recurring payments) using fully-shielded transactions.**
  - They would observe bandwidth spikes that look like sends/receives following
    the same pattern.
- make the user think outdated information (transactions, balance, etc.) is
  up-to-date.
  - By blocking the connection to lightwalletd.
- tell which of many users of the lightwalletd instance the user is.
  - If the user reconnects from the same IP address, they can usually assume it
    is the same user.
- silently prevent the user from receiving wallet security updates or security
  notices.
  - By blocking the phone's connection to the internet or app store.
- tell which cryptocurrencies the user is using if the SDK is used in a
  multi-currency wallet.
  - The adversary would be able to see that the wallet is connecting to a
    lightwalletd instance, which reveals they are using the Zcash component
    of the wallet.
- tell when the user is actively using the wallet.
    - They can see that the wallet is communicating with lightwalletd when
    it's in active use.
- tell when the user's wallet was created.
    - The wallet only downloads blocks starting with the last checkpoint
    before its birthday. By observing how much bandwidth gets used during the
    initial download, the adversary can determine approximately how many
    blocks it downloaded, and thus approximately what its birthday is.
- cause a transaction the user sends to fail.
    - They can block the connection between the wallet and lightwalletd.

These weaknesses also apply to the stronger adversaries in the sections above.

Let's move on to the weakest adversary considered in this model.

## Address-Knowing Adversary

**Description:** The adversary only knows some of the users' z-addresses and
t-addresses, can view the public blockchain, and has no other special
capabilities.

This is the weakest adversary in our model. The security invariants we expect
to be satisfied against this adversary are that the adversary...

- can't tell *that* or *when* the user has received a fully-shielded transaction.
- can't tell *that* or *when* the user sends a fully-shielded transaction.
- can't learn who the user is sending/receiving funds to/from in fully-shielded
  transactions.
- can't tell how many transactions the user has sent or received over time.
- can't determine whether or not the user's wallet owns a particular address.
- can't tell who the user is (i.e. their real name).
- can't tell where the user is.
- can't tell that the user spends money according to a certain pattern (e.g.
  recurring payments) using fully-shielded transactions.
- can't make the user think outdated information (transactions, balance, etc.)
  is up-to-date.
- can't tell which of the many users of the lightwalletd instance the user is.
- can't silently prevent the user from receiving wallet security updates or
  security notices.
- can't tell which cryptocurrencies the user is using if the SDK is in use in
  a multi-currency wallet.
- can't tell when the user is actively using the wallet.
- can't tell when the user's wallet was created.
- can't cause a transaction the user sends to fail.

There are several known weaknesses that this adversary can exploit.
The adversary can...

- **tell when the user spends shielded funds sent to them by the adversary.**
  - dust attack: the adversary can send many low-value notes, which will be
    spent in a transaction with many Sapling inputs (visible on the
    blockchain).
- tell when the user sends or receives a t-address transaction, and tell what
  their transparent balance is.
  - t-address are not private, users should use fully shielded transactions to
    obtain privacy.
- tell that the user is using this particular wallet app.
  - differences in note selection might distinguish it from other wallets.

These weaknesses also apply to all of the stronger adversaries in the sections above.

## Limitations

This threat model is missing important details, for example, about:

- Adversaries that have physical access to the device the app or SDK is running on.
- Secure usability of the SDK's API.
- Implicit assumptions about how the SDK is being used in a third-party app.
- More fine-grained models of adversaries, e.g. one that has eclipsed the
  lightwalletd node but has not been able to compromise it fully.

These shortcomings may be addressed in future updates to the threat model.

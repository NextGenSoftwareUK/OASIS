


# OASIS API / Our World / HoloNET / HoloUnity / .NET HDK ALPHA v0.1.7

## Contents
- [Introduction](#introduction)
- [Project Structure](#project-structure)
- [The OASIS API & Karma System](#the-oasis-api---karma-system)
  * [Your Karma Level Effects Your Real Life Too!](#your-karma-level-effects-your-real-life-too-)
  * [Gain Karma When You Earn HoloFuel For Sharing Your Nodes Resources To Power Our World](#gain-karma-when-you-earn-holofuel-for-sharing-your-nodes-resources-to-power-our-world)
  * [The OASIS API Will Use The Reputation Interchange From Sacred Capital](#the-oasis-api-will-use-the-reputation-interchange-from-sacred-capital)
  * [Open Karma Committee/Community Concensors](#open-karma-committee-community-concensors)
  * [The OASIS API Enables You To Earn Karma Anywhere](#the-oasis-api-enables-you-to-earn-karma-anywhere)
  * [Machine Learning Algorithms, Models & AI](#machine-learning-algorithms--models---ai)
  * [The Universal API To Connect Everything To Everything (No More Silos/Walled Gardens)](#the-universal-api-to-connect-everything-to-everything--no-more-silos-walled-gardens-)
  * [OAPPS - Write Once, Deploy Everywhere](#oapps---write-once--deploy-everywhere)
  * [OAPPS - Full Cross-API Support Across All Networks/Platforms/APIs/Protocols](#oapps---full-cross-api-support-across-all-networks-platforms-apis-protocols)
  * [OAPPS - Full support (create/deploy/consume) for Smart Contracts (SOLIDITY) across ANY supported Provider (Network/platform/API/protocol)](#oapps---full-support--create-deploy-consume--for-smart-contracts--solidity--across-any-supported-provider--network-platform-api-protocol-)
  * [One API To Rule Them All - Abstraction Layer Over The New Distributed Decentralised Internet (IoT)](#one-api-to-rule-them-all---abstraction-layer-over-the-new-distributed-decentralised-internet--iot-)
  * [OASIS API Can Manage All Of Your Tokens/Exchanges/Wallets All In One Place](#oasis-api-can-manage-all-of-your-tokens-exchanges-wallets-all-in-one-place)
  * [One Single Login For All Your Apps/Games/Websites/Services/Everything!](#one-single-login-for-all-your-apps-games-websites-services-everything-)
  * [Our World Is The XR/IR Unified Interface To The Holochain Ecosystem](#our-world-is-the-xr-ir-unified-interface-to-the-holochain-ecosystem)
  * [Satellite Apps/Games/Websites/Services (Consumers)](#satellite-apps-games-websites-services--consumers-)
  * [Protocols/Platforms/Networks Supported (Providers)](#protocols-platforms-networks-supported--providers-)
  * [Holochain Zomes/Services Used](#holochain-zomes-services-used)
  * [Calling The OASIS API](#calling-the-oasis-api)
  * [OASIS Open Standards](#oasis-open-standards)
  * [OASIS API Redundancy (Can Store Copies Of Your Data On Any Decentralised Network/Platform You Choose)](#oasis-api-redundancy--can-store-copies-of-your-data-on-any-decentralised-network-platform-you-choose-)
  * [User Has FULL Control Of Their Data](#user-has-full-control-of-their-data)
- [The OASIS Network (ONET)](#the-oasis-network--onet-)
  * [REST API, GraphQL & WebSockets Supported](#rest-api--graphql---websockets-supported)
  * [Can Run On The Holo Network](#can-run-on-the-holo-network)
  * [Earn Karma & HoloFuel For Running a ONODE](#earn-karma---holofuel-for-running-a-onode)
  * [ONODE Setup](#onode-setup)
  * [Detailed Management Console](#detailed-management-console)
  * [ONODE CORE & ONODE Providers](#onode-core---onode-providers)
  * [Encourages People To Self-Organise, Co-operate, Co-ordinate & Promotes A Decentralised Distributed Mindset.](#encourages-people-to-self-organise--co-operate--co-ordinate---promotes-a-decentralised-distributed-mindset)
  * [Sharing & Storing Your Data](#sharing---storing-your-data)
- [OAPP Web UI Components](#oapp-web-ui-components)
- [.NET HDK](#net-hdk)
- [The Power Of Holochain, .NET, Unity & NodeJS Combined!](#the-power-of-holochain--net--unity---nodejs-combined-)
  * [ARC & Noomap Integration](#arc---noomap-integration)
  * [Node.JS Integration](#nodejs-integration)
- [ARC, Noomap & IWG (Infinite World Game) Will Be Fully Integrated](#arc--noomap---iwg--infinite-world-game--will-be-fully-integrated)
- [Turbocharge the Holochain ecosystem!](#turbocharge-the-holochain-ecosystem-)
- [The OASIS Architecture](#the-oasis-architecture)
  * [Open Modular Design](#open-modular-design)
  * [General](#general)
  * [OASISWEBPORTAL/ NOOMAP INTERFACE](#oasiswebportal--noomap-interface)
  * [IOAPP (OAPP)](#ioapp--oapp-)
  * [Our World](#our-world)
  * [OAPI (OASIS API)](#oapi--oasis-api-)
  * [ONET (OASIS Network)](#onet--oasis-network-)
  * [OAPP (Legacy Apps/websites)](#oapp--legacy-apps-websites-)
  * [OAPP/DAPP](#oapp-dapp)
  * [OAPP/hAPP](#oapp-happ)
  * [OAPP/HAPP](#oapp-happ)
  * [Business OAPP](#business-oapp)
  * [H4OME / ARC](#h4ome---arc)
- [Our World/OASIS Will Act As The Bridge For All (Legasy, IPFS, Holochain, Ethereum, SOLID, Fediverse, Mastodon, Diaspora, WebFinger, ActivityPub, XMPP & More!)](#our-world-oasis-will-act-as-the-bridge-for-all--legasy--ipfs--holochain--ethereum--solid--fediverse--mastodon--diaspora--webfinger--activitypub--xmpp---more--)
  * [Implement Your Own Storage/Network/Renderer Provider](#implement-your-own-storage-network-renderer-provider)
  * [Switch To A Different Provider In RealTime](#switch-to-a-different-provider-in-realtime)
- [Fully Integrated Unified Interface](#fully-integrated-unified-interface)
  * [NextGen Social Network](#nextgen-social-network)
    + [OASIS Avatar/Profile/Karma Integration](#oasis-avatar-profile-karma-integration)
    + [Our World/OASIS API/Social Network Website](#our-world-oasis-api-social-network-website)
    + [Noomap Integration](#noomap-integration)
    + [Deep Integration Into Other Networks/Protocols/Platforms (Such as Gab, Mastodon, Diaspora, WebFinger, SOLID, Ethereum, Fediverse, ActivityPub, XMPP & More!)](#deep-integration-into-other-networks-protocols-platforms--such-as-gab--mastodon--diaspora--webfinger--solid--ethereum--fediverse--activitypub--xmpp---more--)
- [Platforms](#platforms)
    + [PC/Console Version](#pc-console-version)
    + [Smartphone Version](#smartphone-version)
- [NextGen Hardware](#nextgen-hardware)
- [Our World Overview](#our-world-overview)
  * [Introduction](#introduction-1)
  * [XR/IR Gamification Layer Of The New Interplanetary Operating System & The New Internet (Web 3.0)](#xr-ir-gamification-layer-of-the-new-interplanetary-operating-system---the-new-internet--web-30-)
  * [Open World/New Ecosystm/Asset Store/Internet/Operating System/Social Network](#open-world-new-ecosystm-asset-store-internet-operating-system-social-network)
  * [Infinite Alternate Reality Game (IARG)](#infinite-alternate-reality-game--iarg-)
  * [Our World Integrates The Commons Engine & Mutual Crypto Currency](#our-world-integrates-the-commons-engine---mutual-crypto-currency)
  * [Synergy Engine](#synergy-engine)
  * [Resource Based Economy](#resource-based-economy)
  * [First AAA MMO Game To Run On Holochain](#first-aaa-mmo-game-to-run-on-holochain)
  * [Smartphone Version](#smartphone-version-1)
  * [Console Version](#console-version)
  * [Engrossing Storyline](#engrossing-storyline)
  * [OASIS Asset Store](#oasis-asset-store)
  * [Virtual E-commerce](#virtual-e-commerce)
  * [We Accept Karma, Your Money Is No Good Here!](#we-accept-karma--your-money-is-no-good-here-)
  * [Our World Is Only The Beginning...](#our-world-is-only-the-beginning)
  * [The Tech Industry Have A Morale & Social Responsibility](#the-tech-industry-have-a-morale---social-responsibility)
  * [Teach Kids The Right Life Lessons](#teach-kids-the-right-life-lessons)
  * [Remember How Powerful YOU Are!](#remember-how-powerful-you-are-)
  * [Bringing People Together](#bringing-people-together)
  * [We are Building The Evolved Benevolent Version Of The OASIS](#we-are-building-the-evolved-benevolent-version-of-the-oasis)
  * [Ascension/God Training & Mirror Of Reality Technology](#ascension-god-training---mirror-of-reality-technology)
  * [7 Years Of Planning & R&D](#7-years-of-planning---r-d)
  * [Early Prototype](#early-prototype)
  * [We Are What You Have All Been Waiting For...](#we-are-what-you-have-all-been-waiting-for)
  * [Large Social Media Following](#large-social-media-following)
  * [UN Contacts](#un-contacts)
  * [Buckminster's World Peace Game](#buckminster-s-world-peace-game)
  * [The NextGen Office](#the-nextgen-office)
  * [Golden Investment Opportunity](#golden-investment-opportunity)
  * [Help Cocreate A Better World...](#help-cocreate-a-better-world)
- [NextGen Developer Training Programmes For EVERYONE! (Including Special Needs & Disadvantaged People)](#nextgen-developer-training-programmes-for-everyone---including-special-needs---disadvantaged-people-)
- [The Power Of Autism](#the-power-of-autism)
- [Better Than A Fornite Clone! ;-)](#better-than-a-fornite-clone-----)
- [HoloNET](#holonet)
  * [How To Use HoloNET](#how-to-use-holonet)
  * [The Power of .NET Async Methods](#the-power-of-net-async-methods)
  * [Events](#events)
    + [OnConnected](#onconnected)
    + [OnDisconnected](#ondisconnected)
    + [OnError](#onerror)
    + [OnGetInstancesCallBack](#ongetinstancescallback)
    + [OnDataReceived](#ondatareceived)
    + [OnZomeFunctionCallBack](#onzomefunctioncallback)
    + [OnSignalsCallBack](#onsignalscallback)
  * [Methods](#methods)
    + [Connect](#connect)
    + [CallZomeFunctionAsync](#callzomefunctionasync)
      - [Overload 1](#overload-1)
      - [Overload 2](#overload-2)
      - [Overload 3](#overload-3)
      - [Overload 4](#overload-4)
    + [ClearCache](#clearcache)
    + [Disconnect](#disconnect)
    + [GetHolochainInstancesAsync](#getholochaininstancesasync)
      - [Overload 1](#overload-1-1)
      - [Overload 2](#overload-2-1)
    + [SendMessageAsync](#sendmessageasync)
  * [Properties](#properties)
    + [Config](#config)
    + [Logger](#logger)
    + [NetworkServiceProvider](#networkserviceprovider)
    + [NetworkServiceProviderMode](#networkserviceprovidermode)
- [HoloOASIS](#holooasis)
  * [Using HoloOASIS](#using-holooasis)
  * [Events](#events-1)
    + [OnInitialized](#oninitialized)
    + [OnPlayerProfileSaved](#onplayerprofilesaved)
    + [OnPlayerProfileLoaded](#onplayerprofileloaded)
    + [OnHoloOASISError](#onholooasiserror)
  * [Methods](#methods-1)
  * [Properties](#properties-1)
- [OASIS API Core](#oasis-api-core)
    + [Using The OASIS API Core](#using-the-oasis-api-core)
  * [Interfaces](#interfaces)
    + [IOASISStorage](#ioasisstorage)
    + [IOASISNET](#ioasisnet)
  * [Events](#events-2)
  * [Methods](#methods-2)
  * [Properties](#properties-2)
- [HoloUnity](#holounity)
  * [Using HoloUnity](#using-holounity)
  * [Events](#events-3)
  * [Methods](#methods-3)
  * [Properties](#properties-3)
- [Road Map](#road-map)
- [Next Steps](#next-steps)
- [Donations Welcome! :)](#donations-welcome----)
- [Devs/Contributions Welcome! :)](#devs-contributions-welcome----)
- [Other Ways To Get Involved](#other-ways-to-get-involved)
- [HoloSource Licence](#holosource-licence)
- [Links](#links)


## Introduction

**NOTE: This documentation & code is a WIP and is still currently being written so please make sure you check back often...**

**We would love to hear from you regarding feedback on any of the vision, docs, code or would like to submit ideas. Even better, if you would like to get actively involved by coding, spreading the word, finding funding or any other role then please get in touch right NOW! :) Email us ourworld@nextgensoftware.co.uk. Thank you.**

The core OASIS (Open Advanced Sensory Immersion System) API that powers Our World and manages the central profile/avatar/karma system that other satellite apps/games plug into it and share. This allows karma to be earnt in the satellite apps/games by doing good deeds or progressing self help apps for example. This also includes HoloNET that allows .NET to talk to Holochain, which is where the profile/avatar is stored on a private decentralised, distributed network. This will be gifted forward to the Holochain community along with the HoloUnity3D SDK/Lib/Asset coming soon... ;-)

[The OASIS API is a global universal API that aims to connect everything to everything](#the-universal-api-to-connect-everything-to-everything--no-more-silos-walled-gardens-) to eliminate walled gardens/silos. There are a number of open protocols/platforms/networks (such as Gab, Mastodon, Diaspora, WebFinger,
 SOLID, Holochain, CEPTR Pluggable Protocol, Ethereum, Fediverse, ActivityPub, XMPP & more!) that the OASIS API will support. The majority of these are aimed at building a truly decentralised distributed internet (Web 3.0) and this is also the aim of the OASIS API. 

[Our World](#our-world-overview) is an exciting immersive next generation 3D XR/IR (Infinite Reality) educational game/platform/social network/ecosystem teaching people on how to look after themselves, each other and the planet using the latest cutting-edge technology. It teaches people the importance of real-life face to face connections with human beings and encourages them to get out into nature using Augmented Reality similar to Pokémon Go but on a much more evolved scale. This is our flagship product and is our top priority due to the massive positive impact it will make upon the world...

It is the XR/IR Gamification layer of the new interplanetary operating system & the new internet (Web 3.0), which is being built by the elite technical wizards stationed around the world. This will one day replace the current tech giants such as Google, FaceBook, etc and act as the technical layer of the New Earth, which is birthing now.  Unlike the current tech giants who's only aim is to ruthlessly maximize profits at the expense of people and the planet (as well as spying, exploitation, censorship & social engineering), our technology is based on true love & unity consciousness where money and profits are not our aim or intention, our aim and intention is to heal the entire planet & human race so we can all live in harmony with each other.  It is a 5th dimensional and ascension training platform, teaching people vital life lessons as well as acting as a real-time simulation of the real world.

Our World is built on top of the de-centralised, distributed nextgen internet known as Holochain.

Our World will be the first AAA MMO game and 2D/3D Social Network to run on HoloChain and the Blockchain. It will also be the first to integrate a social network with a MMO game/platform as well as all of these technologies and devices together. As with the rest of the game, it will be leading the way in what can be done with this NextGen Technology for the benefit and upliftment of humanity.

Our World is like a XR/IR Unified Interface into all of these hApps (this is the Operating System part of it), it's a bit like the XR UI front-end to Holochain where you can view and launch any apps from inside it but they integrate much more deeper than that through the OASIS API/Profile/Avatar/Karma system where they all share the central avatar/profile and can all add/subtract the profiles/avatars karma.

The first phase of Our World will be a de-centralised distributed XR Gamified 3D Map replacement for Google Maps along with the Avatar/Profile/Karma & OASIS API system. The satellite apps/games will be able to create their own 2D/3D object to appear on the real-time 3D map.

Next it will implement the ARC (Augmented Reality Computer) Membrane allowing the revolutionary next-gen operating system to fully interface & integrate with the 3D Map & Avatar/Karma system as well as render its own 3D interfaces and 2D HUD overlays on top of the map, etc.

Next, it will port Noomap to Unity and will implement a Synergy Engine allowing people to easily find and match solutions/desires/passions and to also find various solution providers, which again will be fully integrated with the 3D Map & Avatar/Karma system.

Read another more refined (and updated) summary on the github repo code base for the Our World Smartphone AR Prototype:<br>
https://github.com/NextGenSoftwareUK/Our-World-Smartphone-Prototype-AR

<br>

## Project Structure

The projects within this repo should be pretty self explanatory from their names but below is a brief description of each of them:

|Project  | Description |
|--|--|
|NextGenSoftware.Holochain.hApp.OurWorld  |The Holochain hApp implemented using the Rust HDK. In future this will be ported to use the new .NET HDK once we have created it!  |
|[NextGenSoftware.Holochain.HoloNET.Client.Core](#holonet)| The core code for the HoloNETClient containing the HoloNETClientBase abstract class.
|[NextGenSoftware.Holochain.HoloNET.Client.Desktop](#holonet)| The desktop implementation of the HoloNETClient using NLog as the Logger.
|[NextGenSoftware.Holochain.HoloNET.Client.Unity](#holonet)| The Unity implementation of the HoloNETClient. This will use a Unity compatible logger soon...
|[NextGenSoftware.Holochain.HoloNET.Client.TestHarness](#holonet)| The Test Harness for the HoloNETClient. This includes load tests for Holochain. So far looking good, the conductor is very fast! ;-)
|[NextGenSoftware.Holochain.HoloNET.HDK](#net-hdk)| A placeholder for the .NET HDK (Holochain Development Kit). 
|NextGenSoftware.NodeManager| A library to allow .NET code to call Node.js methods and retuen data from them. This is currently used by the ARC Membrane and Apollo Client/Server projects.
|[NextGenSoftware.OASIS.API.Core](#the-oasis-api---karma-system)| The core code for the OASIS API itself. This is where the Providers are injected and is the core part of the system.
|NextGenSoftware.OASIS.API.Core.Apollo.Client| Uses the NextGenSoftware.NodeManager library to call the Node.js Apollo GraphQL Client. This in turn calls the NextGenSoftware.OASIS.API.Core.Apollo.Server library, which wraps around the NextGenSoftware.OASIS.API.Core.WebAPI (REST API).
|NextGenSoftware.OASIS.API.Core.Apollo.Client.TestHarness| The test harness for the Apollo Client library.
|NextGenSoftware.OASIS.API.Core.Apollo.Server| Uses the NextGenSoftware.NodeManager library to call the Node.js Apollo GraphQL Server, which wraps around the NextGenSoftware.OASIS.API.Core.WebAPI (REST API).
|NextGenSoftware.OASIS.API.Core.Apollo.Server.TestHarness| The test harness for the Apollo Server library.
|[NextGenSoftware.OASIS.API.Core.ARC.Membrane](#arc---noomap-integration)| This contains the DeviceManager,PsyberManager & MappingManager allowing ARC to talk to any device and access all of it's hardware such as Bluetooth. It will also provide a wrapper around Unity allowing ARC to render it's 2D & 3D UI to Unity. It will also allow ARC to access the Our World 3D Map.
|[NextGenSoftware.OASIS.API.Core.ARC.Membrane.NodeJS](#arc---noomap-integration)| This is for testing purposes to simulate the ARC Core (written in NodeJS). It will test calls to the DeviceManager, PsyberManager & MappingManager.
|[NextGenSoftware.OASIS.API.Core.TestHarness](#the-oasis-api---karma-system)| This is a Test Harness for the main OASIS API.
|[NextGenSoftware.OASIS.API.FrontEnd.Web](#the-oasis-api---karma-system)| This is the Web front-end for the OASIS API and will show the user's Avatar/Profile along with their Karma levels (and where the karma came from). It will also show what Satellite apps/games/websites that are using the API. This will form the foundation of the NextGen Social Network (a sub-component of Our World).
|[NextGenSoftware.OASIS.API.FrontEnd.Unity](#holounity)| This shows how the OASIS API is used in Unity to render the users profile data to the 3D Avatar.
|NextGenSoftware.OASIS.API.WebAPI| This will expose the OASIS API as a RESTful service over HTTP. In future there will also be a websocket HTTP & websocket JSON RPC 2.0 interface.
|NextGenSoftware.OASIS.API.WebAP.IntegrationTests|Integration tests for the OASIS REST Web API.
|[NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core](#holooasis)| This contains the core code for the HoloOASIS Provider, that wraps around the HoloNETClient to talk to Holochain. This implements the [IOASISStorage](#ioasisstorage)interface allowing the OASIS API to read & write the users profile data to Holochain. It also implements the [IOASISNET](#ioasisnet) interface allowing it to share the user's profile/avatar as well as find Holons and players in their local area.
|[NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop](#holooasis)| This is the desktop implementation of the HoloOASIS Provider and uses the desktop version of the HoloNETClient.
|[NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity](#holooasis)| This is the Unity implementation of the HoloOASIS Provider and uses the Unity version of the HoloNETClient.
|[NextGenSoftware.OASIS.API.Providers.HoloOASIS.TestHarness](#holooasis)| This is the Test Harness of the HoloOASIS Provider.
|NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS| OASIS Provider for ThreeFold.
|NextGenSoftware.OASIS.API.Providers.IPFSOASIS| OASIS Provider for IPFS.
|NextGenSoftware.OASIS.API.Providers.SOLIDOASIS| OASIS Provider for SOLID.
|NextGenSoftware.OASIS.API.Providers.EthereumOASIS| OASIS Provider for Ethereum.
|NextGenSoftware.OASIS.API.Providers.EOSIOOASIS| OASIS Provider for EOSIO.
|NextGenSoftware.OASIS.API.Providers.TelosOASIS| OASIS Provider for Telos.
|NextGenSoftware.OASIS.API.Providers.SEEDSOASIS| OASIS Provider for SEEDS.
|NextGenSoftware.OASIS.API.Providers.AcitvityPubOASIS| OASIS Provider for AcitvityPub.
|NextGenSoftware.OASIS.API.Providers.BlockStackOASIS| OASIS Provider for BlockStack.
|NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS| OASIS Provider for Chainlink (Smart Contracts for all blockchains).
|NextGenSoftware.OASIS.API.Providers.MongoOASIS| OASIS Provider for MongoDB.
|NextGenSoftware.OASIS.API.Providers.PLANOASIS| OASIS Provider for PLAN.
|NextGenSoftware.OASIS.API.Providers.HoloWebOASIS| OASIS Provider for HoloWeb.
|NextGenSoftware.OASIS.API.Providers.HashgraphOASIS| OASIS Provider for Hashgraph.
|NextGenSoftware.OASIS.API.Providers.ScuttlebuttOASIS| OASIS Provider for Scuttlebutt.

<br>

<a name="the-oasis-api---karma-system"></a>
## The OASIS API & Karma System

Our World is much more than just a free open world game where you can build and create anything you can imagine and at the same time be immersed in an epic storyline. it is an entirely new ecosystem/asset store/internet, it is the future way we will be interacting with each other and the world through the use of technology. Smaller satellite apps/game will plug into it and share your central profile/avatar where you gain karma for doing good deeds such as helping your local communities, etc and lose karma for being selfish and not helping others since it mirrors the real world where you have free will. The karma unlocks certain abilities, special powers & items you can purchase in the game as well as quests and new areas to explore. 

We believe that the OASIS API & Karama System should be baked into the core of the new internet (Web 3.0+) that we are co-creating and will allow [Everything to talk to Everything](https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK#bridge) else and will act as the worlds universal API/protocol. At the center of this is the central profile/avatar where the user's data will be stored. Part of this data will include the karma they have earnt in Our World as well as the karma they have earnt in any of the inter-connected satellite apps/games that use the OASIS API.

This will increase peoples awareness of the good or bad karma they are collecting and will help them become more conscious of their moment by moment actions. This will help them strive to become a better person and to reach their full potential doing as much good as they can in the world. This will help manifest a better world for us all that much faster, when everyone is doing all they can to help co-create it.

**The OASIS API & Our World are expressions of true Unity Consciousness manifested within the technical sphere.**

The karma will be grouped into the following categories:

| Karma Type  | Description |
|--|--|
| Our World | Earnt by completing quests within Our Word itself.  |
| Self Help/Improvement | Earnt by helping people in Our World or in any other app/game/website. This could include counselling, healing, giving advice on a social network, donating, etc
| Helping People | Earnt by helping people in Our World or in any other app/game/website. This could include counselling, healing, giving advice on a social network, donating, etc
| Helping The Environment | Earnt by helping the environment such as planting a tree, saving the rain forest, campaigning to save your local park, picking up litter, cleaning up the ocean, etc
| Helping Animals | Earnt by helping animals such as donating to a animal shelter or charity.
| Contributing Towards A Good Cause - Contributor | Writing content for any good cause. This could also creating audio (podcast,etc) or video (YouTube,etc)
| Contributing Towards A Good Cause - Sharer | Sharing a good cause (including any content such as blogs, video etc).
| Contributing Towards A Good Cause - Administrator | Doing admin work for a good cause. If it is non-paid then you earn even more karma.
| Contributing Towards A Good Cause - Creator/Organiser | Organising/creating a good cause (this will give you more karma than the other good cause categories) | 
| Contributing Towards A Good Cause - Funder | Donate to a good cause/charity.
| Contributing Towards A Good Cause - Speaker | Do public speaking for a good cause.
| Contributing Towards A Good Cause - Peaceful Protester/Activist| Attending a peaceful protest to being about positive change in the world. |
| Other | Anything else not covered above.

The list above is subject to change with more categories likely to be added later as the system evolves and matures...

Sometimes you may earn karma in multiple categories for one action such as by donating to a animal shelter you will earn karma for both **Helping Animals** and for **Contributing Towards A Good Cause - Funder**. 

You will be able to see how the karma you have earnt is broken down into these categories on the users profile/avatar. Various quests, special powers, abilities, items, locations, etc will unlock once you have reached a certain minimum karma level. If you fall below that level by losing karma then they will become locked again. The minimum karma level would normally be your total karma level but it could also be a combination of the various karma categories above. For example to enter a special mystic temple in Our World you may need a total karma level of 1000, karma level of 500 in Self Help/Improvement & 500 karma level for Our World. You could need a karma level of 300 for Helping Animals to access a secret animal sanctuary within Our World.

You will also be able to view the karma levels of other users, this can help you reach out to them to help improve their karma in categories they are lacking in by inviting them on a Quest with you or your group. 

<a name="your-karma-level-effects-your-real-life-too-"></a>
### Your Karma Level Effects Your Real Life Too!

Your karma level effects your real life too, for example you may be entitled to free upgrades at shows, flights, events, hotels, etc. You may also be entitled to special discounts in shops, etc and if you have enough karma you can get free holidays, etc too. The higher your karma the more society will reward you. The list is endless of what is possible. We envision that eventually this will be deeply  integrated into all of society. This reflects how the Universe actually works and is part of the real-time simulation aspect of Our World.

### Gain Karma When You Earn HoloFuel For Sharing Your Nodes Resources To Power Our World

You can also gain karma for sharing your node's resources such as CPU, memory, bandwidth, etc

### The OASIS API Will Use The Reputation Interchange From Sacred Capital

We have partnered with Sacred Capital (another great Holochain project) to use the Reputation Interchange to help power the Karma system across the Holochain Ecosystem and beyond...

https://www.sacred.capital


<a name="open-karma-committee-community-concensors"></a>
### Open Karma Committee/Community Concensors

There will be a Open Karma Committer who will decide the algorithms for karma allocation through concensors with the community. The community can vote for any proposals the committee publish and only ones which receive enough votes will be made "OASIS Law". The community can also vote in representatives to sit on the committee so it is as open and democratic as possible.

**We wish to empower the community to feel into their own hearts for what is right for them. We want them to own the system.**

### The OASIS API Enables You To Earn Karma Anywhere

Our World will automatically support all of the platforms/networks/protocols listed above so your profile/avatar/karma will be available to any apps that use their platforms/networks/protocols. This will also make it easier to earn karma in a wider range of apps by supporting as many platforms/networks/protocols as possible. 

### Machine Learning Algorithms, Models & AI
Our World uses Machine Learning Alogrithms & Models to rate how positive comments are and then reward karma for positive comments and lose karma for negative comments. This is used in the social media part and chat, etc... We plan to make use of machine learning in many other parts of Our World as well as developing other advanced AI components in collaboration with the S7 Foundation.

<a name="the-universal-api-to-connect-everything-to-everything--no-more-silos-walled-gardens-"></a>
### The Universal API To Connect Everything To Everything (No More Silos/Walled Gardens)

**The OASIS API is a global universal API that aims to connect everything to everything to eliminate walled gardens/silos.** There are a number of open protocols/platforms/networks (such as Gab, Mastodon, Diaspora, WebFinger, SOLID, Holochain, CEPTR Pluggable Protocol, Ethereum, Fediverse, ActivityPub, XMPP & more!) that the OASIS API will support. The majority of these are aimed at building a truly decentralised distributed internet (Web 3/0/4.0/5.0) and this is also the aim of the OASIS API. 

<a name="oapps---write-once--deploy-everywhere"></a>
### OAPPS - Write Once, Deploy Everywhere

OAPP's (OASIS Apps) that use the OASIS API can be deployed anywhere across any network, platform, API or protocol. This includes Holochain, every popular blockchain, ActivityPub, IPFS, SOLID & many more... check the growing list of providers supported below..

Gone are the days of having to write multiple dApps and having to spend lots of time setting up your dev environment for each one such as the network, wallet, account, etc. The OASIS API can manage all of this for you with one simple to use UI (web, desktop and Unity versions planned). The Unity UI will in fact be Our World and will be the most feature rich through the XR interface.

You can also of course fully manage every feature and option through the API itself so you could even write your own UI to it if you so wished.

<a name="oapps---full-cross-api-support-across-all-networks-platforms-apis-protocols"></a>
### OAPPS - Full Cross-API Support Across All Networks/Platforms/APIs/Protocols

OAPP's that use the OASIS API can access all API functions of the various supported providers (networks/platforms/APIs/Protocols) through a simple asbstraction layer built on top of them. Gone are the days of having to learn many API's, you only need to learn one... the OASIS API and it will take care of the rest for you... happy days! :) 

You can of course still call through to a specefic provider API if you still need to or need greater control over that specefic provider.

<a name="oapps---full-support--create-deploy-consume--for-smart-contracts--solidity--across-any-supported-provider--network-platform-api-protocol-"></a>
### OAPPS - Full support (create/deploy/consume) for Smart Contracts (SOLIDITY) across ANY supported Provider (Network/platform/API/protocol)

Smart Contracts will be supported across any of the supported providers (network/platform/API/protocol), so you only need to write the contract once for your OAPP and then deploy your OAPP once and it will then take care of deploying and running your app/smart contract across all supported providers (network/platform/API/protocol).

<a name="one-api-to-rule-them-all---abstraction-layer-over-the-new-distributed-decentralised-internet--iot-"></a>
### One API To Rule Them All - Abstraction Layer Over The New Distributed Decentralised Internet (IoT)

The OASIS API will allow you to connect into everything including e-commerce, trading, security, social networks, blockchains, holochain networks, etc.

<a name="oasis-api-can-manage-all-of-your-tokens-exchanges-wallets-all-in-one-place"></a>
### OASIS API Can Manage All Of Your Tokens/Exchanges/Wallets All In One Place

The OASIS API will allow you to easily manage all of your tokens/exchanges/wallets in one place with a simple to use API. You will also be able to manage them through the OASIS API UI (web, desktop & Unity).
The OASIS API will allow you to easily manage all of your tokens/exchanges/wallets in one place with a simple to use API. You will also be able to manage them through the OASIS API UI (web, desktop & Unity).

<a name="one-single-login-for-all-your-apps-games-websites-services-everything-"></a>
### One Single Login For All Your Apps/Games/Websites/Services/Everything!

Your central profile/avatar from Our World is shared through the OASIS API to all your connecting satillite apps/games/websites/services, which means you only need to login once on any device and never again! Gone are the days of having to remember countless logins for all your different apps/games/websites/services. Happy days! :)

<a name="our-world-is-the-xr-ir-unified-interface-to-the-holochain-ecosystem"></a>
### Our World Is The XR/IR Unified Interface To The Holochain Ecosystem

Our World is like a XR/IR Unified Interface into all of these hApps (this is the Operating System part of it), it's a bit like the XR UI front-end to Holochain where you can view and launch any apps from inside it but they integrate much more deeper than that through the OASIS API/Profile/Avatar/Karma system where they all share the central avatar/profile and can all add/subtract the profiles/avatars karma.


<a name="satellite-apps-games-websites--consumers-"></a>
### Satellite Apps/Games/Websites/Services (Consumers)

As already mentioned, many satellite apps/games/websites will plug into Our World using the OASIS API. They can choose to just share the central avatar/profile and the karma system or they can choose to also define the 2D Sprite or 3D object that will appear in Our World at the geo-location specified through the API. This will be the visual representation of the app/game/website/organisation and when the player either walks into or interacts with (click, touch, etc) it will display info and meta data passed through the API. The player can then choose to launch the app/game/website from within Our World.

A list of of possible early adopters can be found below:

|Consumer|Description| Holochain App (hApp) | Integrated | 
|--|--|--|--|
| <a href="https://www.sacred.capital">Sacred Capital</a> |Our staking protocol allows network effects to accrue to reputation. We achieve this through a process known as 'Contextual Chaos'. This means you can port reputation across eco-systems in an appropriate and contextual manner.The result? Applications that facilitate new behaviour patterns of collaboration, interaction and sharing. This is the new economy coming alive in it's truest, varied, diverse colours! |Yes | Coming soon|
| <a href="https://cryptpad.fr/pad">Holo-REA</a> |The HoloREA team wants to create a framework for developing economic networks on Holochain. HoloREA will build some apps, but also intends the framework to be usable by any other apps that work in economic networks, supply chains, or joint ventures.| Yes| Coming soon|
|<a href="http://iwg.life/s7foundation/">Noomap</a>  |3D fractal mapping technology where you can map everything including your thoughts, passions, desires & so much more! |Not Yet | Coming soon |
|<a href="https://growora.com">Growora</a>  |Our mission is to provide free education to the world by enabling those who teach, the financial freedom to share their wisdom & knowledge. Teachers & students are rewarded karma for helping other people so this will easily plug straight into the OASIS API Karma System. |No | Coming soon |
|  <a href="https://mindlife.net">Uplift/MindLife</a> |UpLift is an App delivering a comprehensive series of interactive self-help interventions. Designed and developed by MindLife UK, the App helps people to improve their resilience, confidence and mental capital. |No| Coming soon |
| <a href="https://www.moneyofgood.org/">Money Of Good</a>|Money of Good is a social program that uses revolutionary proof-of-meditation technology to offer people the chance to earn money as a reward for practicing meditation, improving their health, rising consciousness and fostering a new economic model much more equitable and sustainable. |Yes |TBC |
| <a href="https://alptha.joatu.org">Joatu</a>|Junto is a new breed of social media that integrates a more consciously designed interface, distributed technology, and a non-profit approach to create a space where people can truly be themselves.|Yes | TBC |
| <a href="https://humm.earth/">Humm Earth</a> |Influence-free, beautifully simple peer-to-peer publishing: think of Humm as fully distributed WordPress + Patreon, made for the writers and content creators of the future, powering independent hApps and an intentional community. |Yes | TBC|
| <a href="[https://forum.holochain.org/c/projects/comet](https://forum.holochain.org/c/projects/comet)">Comet</a> |Comet is a distributed, Reddit alternative. Posts are created with tags instead of “subreddits,” and are voted on in relation to these tags. Votes can be fractional amounts; the score of a post/comment is determined on a per-person basis, depending on how they have previously voted on the other voters’ content. |Yes | TBC|
| <a href="https://docs.google.com/document/d/1LgBQX42jIOFkfnbHf1swP3GwDd0O-jzpjYYv63OjDRg/edit#heading=h.ofqm4bivlrj4">Global Brain Application</a>|Developing a GloCal Holistic Collaboration and Mapping Tool/UI named here as Global Brain Application |Yes? |TBC |
| <a href="http://www.joinseeds.com">Seeds</a>| A payment platform and financial ecosystem to empower humanity and heal our planet. |No|TBC |
| <a href="http://www.appsforgood.org">Apps For Good</a>| Creative tech courses for you to deliver in your classroom | No |TBC |
| <a href="voiceofhumanity.org">Voice Of Humanity</a>| |No | TBC |
| <a href="http://www.4ocean.com">4Ocean</a>| Cleaning the ocean from the profits made of the sale of bracelets |No | TBC |
| <a href="preseach.io">PreSearch</a> | Presearch is a decentralized search engine, powered by the community. |No| TBC |
| <a href="https://www.mapotic.com">Mapotic</a>|Mapotic is the intuitive mapping platform that empowers you to share knowledge about the places you know and love |No | TBC|
|  <a href="https://www.headspace.com">Headspace</a> | Leading meditation app. |No | TBC|
|  <a href="https://www.superbetter.com">Super Better</a> |SuperBetter builds resilience - the ability to stay strong, motivated and optimistic even in the face of change and difficult challenges. Playing SuperBetter unlocks heroic potential to overcome tough situations and achieve goals that matter most. |No | TBC|
| <a href="https://bridgit.io/">Bridge It</a> |A web overlay that advances the way the world views, shares, and engages with information on the web through community participation.|No | TBC|
| <a href="https://delegatecall.com">Delegate Call<a/> | Fully Blockchain based Q&A. Earn tokens for answering questions. This is built on Loom (which sits on top of Etherum) + Unity game engine. You earn karma for particpating, answering questions, etc so this is a REALLY good fit for the OASIS API and will automatically integrate with the Karma System with very little effort.| No|TBC
| <a href="https://www.gitcoin.com">Git Coin<a/> | GitCoin brings together freelance reosurcing and crowdfunding allowing you to not only attrack the funding for your open source projects but also the devs, so is perfect for the open source Our World/OASIS API code base! :) They also have kudos badges, which are perfect to integrate with the OASIS API Karma System, so we will be apporaching them in future to explore this further... | No | TBC
| <a href="https://www.joinseeds.com/">SEEDS<a/> | A payment platform and financial ecosystem to empower humanity and heal our planet. The karma system will be deeply integrated into their reputation system.| No | TBC

**More coming soon...**

We are in the process of reaching out to these to see if they wish to be one of the early adopters of the OASIS API. This list will grow over time, in time there will be thousands and even millions as our vision to connect everyone to everyone through the OASIS API/Our World becomes more and more of a reality.

**Early adopters will receive a special status and highlighting so they will stand out from the crowd in listings (website), on the map (smartphone version) & in the 3D VR world (Desktop & consoles). So if you wish to take advantage of this offer or know of anyone else who could please get in touch on ourworld@nextgensoftware.co.uk. We would love to hear from you! :)**

Please see the [Social Network](#socialnetwork) section for more info...


<a name="protocols-platforms-networks-supported--providers-"></a>
### Protocols/Platforms/Networks Supported (Providers)

The OASIS API aims to support as many platforms/networks/protocols as possible so the Karma System can be deeply integrated across the web in every application, device, etc.

Below is a list of the protocols/networks/platforms that the OASIS API will support (expect this list to grow in time):

|Protocol/Platform/Network| Description | Support Implemented  | Provider Name |
|--|--|--|--|
| [Holochain](https://holochain.org/) | Leading the way for the new decentralised distributed internet | Yes | [HoloOASIS](#holooasis)
| [ThreeFold](https://threefold.io//) | A true peer-to-peer internet. Empowering equality, autonomy and sustainability with game-changing technology built with a collaborative ecosystem. Live and distributed in 21 countries and expanding.| In Dev | ThreeFoldOASIS
| [Chainlink](https://chain.link/) | The Chainlink network provides reliable tamper-proof inputs and outputs for complex smart contracts on any blockchain.| In Dev | ChainlinkOASIS
| [SOLID](https://solid.inrupt.com/) | Inventor of the Internet, Sir Tim Berners-Lee new protocol for Web 3.0 to give users control of their data as well as remove silos/walled gardens using Pods & Linked Data. | In Dev | SOLIDOASIS
| [Ethereum](https://www.ethereum.org/) | One of the leading Blockchain implementations that is very popular. | In Dev| EthereumOASIS
| [EOSIO](https://eos.io//) | EOSIO is a next-generation, open-source blockchain protocol with industry-leading transaction speed and flexible utility. | In Dev | EOSIOOASIS
| [Telos](https://telosnetwork.io/) | The Telos Blockchain was launched in December of 2018 in response to what the founders saw as an opportunity to build, and improve upon the EOSIO software. | In Dev | TelosOASIS
| [SEEDS](https://www.joinseeds.com/) | A payment platform and financial ecosystem to empower humanity and heal our planet. | In Dev | SEEDSOASIS
| [IPFS](https://ipfs.io) | The InterPlanetary File System is a protocol and peer-to-peer network for storing and sharing data in a distributed file system. IPFS uses content-addressing to uniquely identify each file in a global namespace connecting all computing devices.| In Dev | IPFSOASIS
| [Elrond](https://elrond.com/) |A highly scalable, fast and secure blockchain platform for distributed apps, enterprise use cases and the new internet economy. | In Dev  | ElrondOASIS
| [HIVE](https://www.hiveblockchain.com/) |First publicly listed blockchain infrastructure company that bridges blockchain and cryptocurrencies to traditional capital markets. | In Dev  | HIVEOASIS
| [Orion Protocol](https://www.orionprotocol.io/) |Trade with the liquidity of the entire crypto market in one place - without ever giving up your private keys. | In Dev  | OrionProtocolOASIS
| [Hedera Hashgraph](https://www.hedera.com/) | Hedera is a decentralized public network where anyone can carve out a piece of cyberspace to transact, play, and socialize in a secure, trusted environment. | In Dev  | HashgraphOASIS
| [ScuttleButt](https://github.com/ssbc/) | A distributed and secure peer to peer social network | In Dev | ScuttleButtOASIS
| [HoloWeb](https://holoweb.io//) | We’re reinventing the way the web works, and taking a stand for our sovereignty in cyberspace.| In Dev | HoloWebOASIS
|[PLAN](https://www.plan-systems.org/) | Solving for privacy, ease of collaboration, and accessibility for all. We believe people profoundly benefit from having the tools to connect with each other, manage common resources, and to engage in meaningful projects. | In Dev | PLANOASIS
|[CEPTR Protocol For Pluggable Protocols](http://ceptr.org/projects/pcubed)|Complete interoperability: No more silos and brittle APIs. Self-Describing protocols and a universal parsing system allows anything to talk to anything. This is where the OASIS API is a stepping stone to get to this point by providing backwards compatibility with what is already out there.|Coming soon | 
|[Urbit](https://urbit.org/)|Urbit is a clean-slate OS and network for the 21st century.|  Coming Soon |
|[HSTP (Hyper Spacial Transport Protocol)](https://www.verses.io/)  | The new protocol for the new Spacial Web (Web 3.0) | Coming Soon |
| [WebFinger](https://webfinger.net/) | WebFinger is used to discover information about people or other entities on the Internet that are identified by a URI using standard Hypertext Transfer Protocol (HTTP) methods over a secure transport. A WebFinger resource returns a JavaScript Object Notation (JSON) object describing the entity that is queried. The JSON object is referred to as the JSON Resource Descriptor (JRD).| Coming Soon
| [ActivityPub](https://activitypub.rocks/) |ActivityPub is an open, decentralized social networking protocol based on Pump.io's ActivityPump protocol. It provides a client/server API for creating, updating and deleting content, as well as a federated server-to-server API for delivering notifications and content | Coming Soon
|[Core Network](https://core.network/) | Unify your social network silos into a single visual dashboard. Own your data.  Create and curate content, share privately or publicly, for free or for cryptocurrency.  Develop learning experiences.  Create community currencies.  Track impact, create communities, and reach your people, unfettered by paywalls.  Experience a futuristic VR-first dashboard, which gracefully degrades to standard mobile and desktop browsers. | Coming Soon| 
| [XMPP](https://xmpp.org/) | Extensible Messaging and Presence Protocol is an open XML technology for real-time communication, which powers a wide range of applications including instant messaging, presence and collaboration.  | Coming Soon
| [Loom](https://loomx.io/) | Loom Network is a Layer 2 scaling solution for Ethereum that is live in production. It is a network of DPoS sidechains, which allows for highly-scalable games. | Coming Soon
| [TRON](https://tron.network/) | TRON is one of the largest blockchain-based operating systems in the world. | Coming Soon
| [IOST](https://iost.io/) | IOST is building an ultra-high TPS blockchain infrastructure to meet the security and scalability needs of a decentralized economy. | Coming Soon
| [BlockStack](https://blockstack.org/) | Blockstack apps protect your digital rights and are powered by the Stacks blockchain.. | Coming Soon
| [Fediverse](https://fediverse.party/) | It is a common name for federated social networks running on free open software on a myriad of servers across the world. Historically, this term has included only micro-blogging platforms supporting a set of protocols called OStatus. This didn't do justice to a large number of projects that federate, share same values and are reasonably popular. With the appearance and wide adoption of a new standard protocol called ActivityPub it makes no sense to further divide the federated world into “OStatus” and “non-OStatus” projects. This guide unites all interoperable federated networks under one term “Fediverse”. | Coming Soon
| [Gab](https://gab.com) | Distributed social network promoting free speech | Coming Soon
| [Mastodon](https://joinmastodon.org/) | Distributed twitter style network of micro blogging servers using the Fediverse. | Coming Soon
| [Diaspora](https://diasporafoundation.org/) | Another distributed social network | Coming Soon|
| [Stellar](https://www.stellar.org/) | Stellar is an open network for storing and moving money. | TBC
| [Nexus](https://nexusearth.com/) | Nexus Earth is an innovative open source blockchain technology, designed to better the world through advanced peer to peer networks and digital currency.. | TBC
| [Ripple](https://www.ripple.com/) | Ripple is a real-time gross settlement system, currency exchange and remittance network created by Ripple Labs Inc., a US-based technology company.. | TBC
| [TON](https://github.com/ton-blockchain/ton) | Telegram's Open Network Blockchain also looks very promising. | TBC
| [DFINITY](https://dfinity.org) | DFINITY is building an open, decentralized compute platform designed to host the next generation of software and services with vastly improved performance | TBC |

**More coming soon...**

If you know of any other open protocols/platforms/networks that you feel are part of the new internet (Web 3.0+) we are all co-creating then please do get in touch on [ourworld@nextgensoftware.co.uk](mailto:ourworld@nextgensoftware.co.uk) and let us know, thank you! :)

<a name="holochain-zomes-services-used"></a>
### Holochain Zomes/Services Used

Below is a list of all the Holochain zomes/services used by Our World/The OASIS API:

| Zome/Service | Description  | Integrated
|--|--|--|
| <a href="https://www.sacred.capital">Sacred Capital</a> |Our staking protocol allows network effects to accrue to reputation. We achieve this through a process known as 'Contextual Chaos'. This means you can port reputation across eco-systems in an appropriate and contextual manner.The result? Applications that facilitate new behavior patterns of collaboration, interaction and sharing. This is the new economy coming alive in it's truest, varied, diverse colours! We will be using their Reputation Interchange to help power the Karma System | Coming Soon|
| <a href="https://cryptpad.fr/pad">Holo-REA</a> |The HoloREA team wants to create a framework for developing economic networks on Holochain. HoloREA will build some apps, but also intends the framework to be usable by any other apps that work in economic networks, supply chains, or joint ventures.  We will be using their framework to help power the in-app transactions as well as the supply chain & e-commerce components of Our World.| Coming Soon|
| [Orion Protocol](https://orionprotocol.io) | Cross chain trading, omni-exchange accessibility and liquidity. We will be using this to allow the OASIS API to talk to all available crypto exchanges. | Coming Soon
| [FileStorage Zome](https://github.com/holochain/file-storage-zome) | This Zome allows files to be stored within Holochain. | Coming Soon
| [Chat hApp](https://github.com/holochain/holochain-basic-chat) | This hApp facilitates real-time chat within Holochain. | Coming Soon
| [Cool Cats hApp](https://github.com/pythagorean/coolcats) | This is a basic Twitter style clone and will help power part of the social network component of Our World. | Coming Soon
| [Points Of Interest Zome](https://github.com/vanarchist/holochain-point-of-interest) | Will be used to help store the Points Of Interest on the Our World 3D Map. | Coming Soon
|InvestorEngine| Will be an excellent platform that can help generate some much needed funds for Our World! ;-) |
|CleFree - To copyright Our World on their blockchain tech (they currently use blockstack but will switch to holochain when its in beta).

### Calling The OASIS API

The OASIS API can be called using a number of ways:

| Method | Description  |
|--|--|
|GraphQL/Apollo  | This will be the main way of calling the API since it provides a really nice JSON formatted syntax to make calls across the various OASIS Providers. This will be done over a web socket.   |
|REST API | This will be the fall back way to call the API if for whatever reason the GraphQL option is not available.
| Embedded DLL | If the client is .NET or Unity (or anything that supports importing a .NET compilled DLL) then they can make calls directly though the [OASIS Interfaces](#interfaces) or the various Managers in the [OASIS API Core](#using-the-oasis-api-core).


### OASIS Open Standards

The OASIS API is all about pushing the Open Source & Open Community standards. As almost a sign we were on the right path we recently discovered this site:

https://www.oasis-open.org

They are all about pushing the same agenda except we are actually implementing a API to achieve our shared goals of getting everything to talk to everything else. It's very interesting we both chose the name OASIS, a coincidence or not? ;-) We are obviously in the process of reaching out to them to see how we can work together...

<a name="oasis-api-redundancy--can-store-copies-of-your-data-on-any-decentralised-network-platform-you-choose-"></a>
### OASIS API Redundancy (Can Store Copies Of Your Data On Any Decentralised Network/Platform You Choose)

The OASIS API has built in redundancy in that you can choose to store copies of your data on any decentralised network/platform. When calling the OASIS API you can specify which network/platform you wish to use (the ones available will be dependent on whether a Provider has yet been implemented for it, so far only Holochain is supported.) The default provider will be Holochain since the long term goal of the OASIS API is to get people to slowly migrate across to Holochain.


### User Has FULL Control Of Their Data

Any other OAPP (that uses the OASIS API) can also choose to share your profile/avatar/karma with any of the supported platforms/networks/protocols. The user will have fully access and control of where their data can be stored/shared along with granular permissions such as which apps/sites/users/groups/roles/networks/platforms can see what.

**More to come soon...**

<a name="the-oasis-network--onet-"></a>
## The OASIS Network (ONET)

The OASIS Network (ONET) is the distributed de-centralised network allowing the various providers that the OASIS API supports to be fully distributed globally across a large decentralised network. This builds on top of design principles from Holocahin in that it is fully distributed and there is no centralisation or bottlenecks (other than of course potential ones caused by Blockchains but the design should hopefully help alleviate these as much as possible). 

<a name="rest-api--graphql---websockets-supported"></a>
### REST API, GraphQL & WebSockets Supported

- Clients connect to various ONODE Providers through their selected ONODE CORE Gateway.

- Clients connect through their web browser using either REST, WS or GraphQL similar to Holoports except they only use WS as far as I know.

- If the client is an app or unity game it will connect directly using the REST, WS or GraphQL API's. 

<a name="rest-api--graphql---websockets-supported"></a>
### Can Run On The Holo Network

- It can use Holo Hosting to run ONODE's (OASIS API REST Service/GraphQL Server/WS endpoint) along with any ONODE Providers such as Blockchain, SOLID, IPFS, AcitivtyPub, legacy, etc

<a name="earn-karma---holofuel-for-running-a-onode"></a>
### Earn Karma & HoloFuel For Running a ONODE

- People earn Karma & HoloFuel for running the ONODE's (the more node's they run the more karma & HoloFuel they can earn).


### ONODE Setup

- ONODE Setup wizard for HoloPorts, Windows, Mac, Linux, XBOX ONE, PS4, etc. (will allow CPU, memory, disk & network storage/loads to be fully distributed)

- Windows will be first, quickly followed by Linux & Mac thanks to .NET CORE making it easy to deploy to them. HoloPorts (NixOS) will also be a priority once these have stabilised & the Altha Open TestNet has been successful and is stable. We may wait till the beta mainnet but will know closer to the time...

- ONODE Setup will install a lightweight webserver running REST API, Apollo Server (Node.js) & WebSocket Endpoint. It will also connect and configure ONODE Providers for whatever is found running on the ONODE device such as Holochain, Ethereum, IPFS, ActivityPub, SOLID, etc.

- There will be an option to download and install various Providers when installing, as well as the option to connect & configure a provider later.

- Can run remote distributed providers so they do not all need to be on the same device. This will help distribute the CPU, Memory, Network & Disk loads (Blockchains can be very high!)

- People also earn Krama & HoloFuel for running the distributed providers.


### Detailed Management Console

- There will be a detailed Management Console allowing the user to view network traffic, where the data is being stored (both for the user & node), OAPP's installed, Providers Installed/Connected (both local & remote), uptime, karma/holofuel earnt & lots more!

<a name="onode-core---onode-providers"></a>
### ONODE CORE & ONODE Providers

- A ONODE CORE Gateway can run multiple ONODE Providers either locally or remotely.

- Can run only one ONODE CORE instance but you can run multiple instances of the ONODE Providers, which may be able to help reduce various Blockchain bottlenecks, etc. Holochain does not suffer from these issues.

- ONODE Core can manage and load balance the various Providers keeping throughput as optimal as possible.

- When adding a ONODE Provider you can select whether it is local or remote.

- When installing & setting up a OASIS ONODE you can select if it will be a ONODE CORE or a ONODE Provider. If it will be a ONODE Provider then you need to enter the address of the ONODE CORE to connect to. ONODE CORE are a bit like controllers or gateways.

<a name="encourages-people-to-self-organise--co-operate--co-ordinate---promotes-a-decentralised-distributed-mindset"></a>
### Encourages People To Self-Organise, Co-operate, Co-ordinate & Promotes A Decentralised Distributed Mindset.

-  Reason it makes sense for people to self-organise & not everyone run a ONODE CORE is because the OAPP's that want to share to multiple providers will pick the ONODE's with the most providers as possible and it would be rare for a ONODE CORE to run all of the providers on the same device due to the large amount of resources this would take (running multiple blockchain networks nodes would require huge amounts of storage space just for starters). So this will encourage a decentralised distributed architecture along with co-ordination, co-operations, team work and a distributed de-centralised mindset. :)

- Another incentive is that you actually earn more Karma & HoloFuel for running a distributed ONODE Provider over a local one. The same goes for the ONODE CORE, who will earn more karma/holofuel for connecting to and using distributed ONODE Providers over a local one.

- Distributed ONODE's are likely to be faster and have less bottlenecks. We want to move away from a centralisation mindset! ;-)

<a name="sharing---storing-your-data"></a>
### Sharing & Storing Your Data

- If a client wishes to share their profile/data to other providers this can be done without having to install any ONODE Providers on their device but if they wish to store the data locally on their device then of course they will need to install the respective ONODE Providers.

- Your profile will only be available to you on your devices across your apps.

- Will add a FileOASIS Storage Provider to serialise and save your profile to your local device such as smartphone if you do not have the space or processing power to install/run any other providers such as hc, blockchain, etc. This will likely still go through the ONODE Core Gateway (the hope is to make this as lightweight as possible so can run no problem on your phone). HC is also pretty lightweight so hopefully this will also be on phones in future then the FileOASIS Provider may not be needed because your profile would be stored in your local chain instead.

- You will be able to choose right down to individual field level what you wish to be stored/shared on each provider.

- You can share to one of the following: Public, Friends, Family, Colleagues, CustomList.

- When a profile is shared, it is similar to store but will be in a ReadOnly state and will appear in the appropriate list (family, friends, etc) on their respective OAPP's that use the OASIS API.

## OAPP Web UI Components

Each OAPP will have access to a number of templates, scarrolding & Web components (including React, Angular,  Pure/Vanilla JS & more!) to render:

  - Your Profile/Avatar (Embeedded 3D WebGL Model) - includes karma and how you got it, where & what OAPP.
  - Family/Friends/Co-workers lists.
  - Simple Messaging.
  - Launch Our World.
  - Launch OAPP (list of installed OAPP's).
  - If there is a location (such as a business, organisation, etc) for how you earnt your karma then there will be a "Show In Our World Map" option.
  - If a OAPP has a location associated with it then it will also have a "Show In Our World Map".
  
**More to come soon...**
  
## .NET HDK

We will soon also begin work on the .NET HDK to open up the amazing Holochain to the massive .NET & Unity ecosystem's, which will help turbocharge the holochain ecosystem they are trying to build...

.NET supports compiling to WASM so we know this is possible... ;-)

We are looking for devs who would be interested in this exciting mini-project, so if you are interested please get in touch either on the channel below or by emailing us on ourworld@nextgensoftware.co.uk or david@nextgensoftware.co.uk. We look forward to hearing from you! :)

https://chat.holochain.org/appsup/channels/net-hdk 

https://github.com/NextGenSoftwareUK/Holochain-.NET-HDK

A placeholder has also been added for the work to begin in this repo in the project Holochain.NextGenSoftware.HoloNET.HDK. Just as with NextGenSoftware.Holochain.HoloNET.Client, this project may be split out into its own repo and then linked to this one as a sub-module in future...

We have been tracking a number of different solutions to allow .NET to compile to WASM and the most promising so far is CoreRT (a AOT (Ahead Of Time) Compiler for .NET Core):

https://github.com/dotnet/corert/blob/master/Documentation/how-to-build-WebAssembly.md

This will allow managed C# code to be compiled into any native language including WASM.

<a name="the-power-of-holochain--net--unity---nodejs-combined-"></a>
## The Power Of Holochain, .NET, Unity & NodeJS Combined!

The front-end is built in Unity, the middle layer is built in C#/.NET and the back-end is built-in Holochain. 

<a name="arc---noomap-integration"></a>
### ARC & Noomap Integration

The middle layer will also soon interface with the amazing ARC (Augmented Reality Computer) operating system being built by my good friend and cosmic brother Chris Lacombe over at S7 Foundation (previously called Noomap). He is also the creator of Noomap, a 3D fractal mind mapping tool that has some communities very excited! :)

http://noomap.info/

http://iwg.life/s7foundation/

ARC is currently being built in NodeJS and utilises a Semantic Graph to store and represent it's data, it will also contain a revolutionary AI system. We cannot say more on this at the moment because Chris wants to keep this project under the radar at the moment...

### Node.JS Integration

The OASIS Architecture will interface to ARC/Node.JS using Edge.js:

https://github.com/tjanczuk/edge

This will allow both .NET and NodeJS to run in the same process and make cross function calls as if they were native.

We are working very closely with Chris & S7 to fully synergise & intrgrate ARC & Noomap into the OASIS Architecture & Our World.

<a name="arc--noomap---iwg--infinite-world-game--will-be-fully-integrated"></a>
## ARC, Noomap & IWG (Infinite World Game) Will Be Fully Integrated

ARC, Noomap & IWG will be fully integrated into the OASIS Architecture. The IWG is VERY similar to Our World and has a LOT of overlap and is something we are currently exploring and synergising but it looks like they will for a start share the same central avatar/profile/karma system that is currently being built in this very repo.

<a name="turbocharge-the-holochain-ecosystem-"></a>
## Turbocharge the Holochain ecosystem!

Because the OASIS Architecture makes use of .NET, Unity, NodeJS & Holochain we have access to 3 massive well established ecosystems along with all their devs & resources. This will massively help turbocharge the holochain ecosystem as well as help raise awareness of it...


## The OASIS Architecture

The Architecture diagram can be found below or on our website (http://www.ourworldthegame.com) but it is also in the images folder of the repo cunningly named OASIS Architecture Diagram.png

![alt text](https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/blob/master/Images/OASIS%20Arcitecture.png "OASIS Architecture Diagram")

Our World will run on our own propriety game engine called OASIS 2.0 (Open Advanced Sensory Immersion System).

Our World will run on a secure, distributed and de-centralized architecture where user’s data will be stored on their machine and not a central server enabling the user to own their data, meaning it cannot be sold or exploited as others do. It will do this by running on a new nextgen Internet known as Holochain.

Our World will run on its own private secure network called ONET (OASIS Network) on top of HoloChain offering yet more security and performance benefits. It will not suffer from any bottlenecks as is the case with the current centralised server architecture in current games causing lag, which is very frustrating to gamers and can mean life or death in games.

Our World/OASIS & ONET can even distribute the computing power across the gamers machines so if some machines are struggling, they can borrow processing power from fellow gamers with more powerful rigs (if they give permission of course!). Sounding like the OASIS from Ready Player One (book version) yet? ;-)

It will also run on IPFS, the Ethereum blockchain, DAOStack ARC & H4OME.

**Our World is also a HApp (H4OME App), a HoloChain App & a DApp (Ethereum Distributed App), SOLID app implementing all of their respective interfaces.**

**It will also allow any HApp, HoloChain App, DApp, SOLID App to plug into Our World where they can share their data (as well as connect to the central avatar/profile) or even their full UI within the Our World VR/AR/XR/IR (Infinite Reality) world/universe. It will also allow any other legacy apps/games/systems to plug in using a HTTP API that implements the OAPI (OASIS API). It will act as the bridge between all the upcoming nextgen technology as well as supporting legacy systems until they are also migrated to the new nextgen internet**

**All of these apps that plug into the OASIS Engine (Our World) will be known as OApps (OASIS Apps). As well as these OApps being able to share their data/UI with any other OApp, they can also take advantage of the OASIS Asset Store where users can buy various add-ons for your app/game.**

**By supporting
 everything Our World/OASIS will help act as a bridge between the old and the new world.**

NextGen Software & Our World themselves will also be a DAO (Distributed Anonymous Organisation) registered with DAOStack meaning we can self-govern and cut out the expensive middlemen such as banks, lawyers, accountants, managers, contracts etc. This technology will allow us to realise our long-held dreams of running a flat decentralised organisation where every voice is heard, respected and is counted as an equal. This also prevents fraud, mistakes and corruption from occurring as is all too common now days.

**NOTE: The design is evolving all the time so the above is subject to change...**

### Open Modular Design

As you can see from the diagram the OASIS architecture is very modular, open and extensible meaning any component can easily be swapped out for another without having to make any changes to the rest of the system. It will use MEF (Managed Extensibility Framework) so the components can even be swapped out without having to re-compile any of the existing code, you simply drop the new component into a hot folder that the system will pick up on the next time you restart.

The components are split into 11 sub-systems/layers:

* Storage ([IOASISStorage](#ioasisstorage) Interface)
* Network ([IOASISNET](#ioasisnet) Interface)
* Renderer (IOASIS2DRenderer & IOASIS3DRenderer Interfaces)
* XR/Eye Tracking
* Haptic Feedback
* Realtime Emotional Feedback System
* Face Recognition
* Motion Tracking
* Input
* OAPP Templates
* [OASIS Engine/API](#oasisapicore)

Currently [HoloOASIS](#holooasis) implements the [IOASISStorage](#ioasisstorage) and [IOASISNET](#ioasisnet) interfaces.

**PLEASE MAKE SURE YOU READ THE DESCRIPTION BOXES ON THE DIAGRAM FOR MORE INFO ON HOW THE SYSTEM WILL WORK.**

Below are the copies of the description boxes found on the diagram, which some may find easier to read...

### General

This is an example implementation of the OASIS/OAPP Stack using the Unity game engine, HTC Vive VR headset, Oculus Rift VR headset, Apple ARKit, Google ARCore & Haptor VR Haptic feedback gloves. The smartphone version will be using Unity so its stack will look similar to this. The desktop/console version will likely be using the Cry Engine.

As you can see the system is highly modular so it supports any combination of components and allows for each to be swapped out for another without having to re-write the core game logic. The system can also be easily extended and adapted in future without expensive time consuming re-writes, it also will not break existing components and apps since the core API's will remain the same. This is done by following the standard good practise design principles such as SOLID. This also allows for each component to be developed and tested independently and removes any inter-dependencies making the system more stable, robust and easier to maintain. You are not forced to implement all of the interfaces below, only those you wish to use, the system is highly redundant and will still continue to work with whatever has been implemented and configured.

An OAPP has the choice to implement whatever interfaces they wish. They can choose to implement the various renderer, AR,VR, HapicFeedback, Input interfaces, etc (which Our World implements) or just the IOAPP interface, which will expose just the central avatar/profile where they can commit additional karma if they do good deeds in the app. A number of default implementations/templates will be provided for H4OME (HAPP), Holochain (hAPP), Ethereum (DAPP) and a simple HTTP Client, all of which will be implemented as extendable base classes & boiler plate/scaffolding code so the app developer can easily extend the out of the box basic functionality. This will enable developers to be as productive as possible within the OASIS System. They can also optionally choose to implement additional methods/interfaces if they wish to display a UI within Our World. In time simple wizards will also be provided to guide the developer through the creation of their app using simple step by step instructions.

<a name="oasiswebportal--noomap-interface"></a>
### OASISWEBPORTAL/ NOOMAP INTERFACE

As well as the smartphone and console/desktop version of Our World, there will also be a web portal, which will be a nextgen social network and will also interact with the central profile/avatar. Part of this will also include the Match Need System allowing them to search the Internet for products/services/info that will help improve their wellbeing where the results will be tailored to their current wellbeing requirements stored in their profile/central avatar.The user will be able to choose to visualise this in a traditional 2D interface or a nextgen 3D/VR interace partly powered by Noomap/S7 Foundation technology & WebGL. It will also use the IOAPP interface to communicate to the OASIS Engine/Profile/Avatar. It will also be an example of how to use the  HTTPOASISAPP template, since it will be built on top of it.

<a name="ioapp--oapp-"></a>
### IOAPP (OAPP)

The minimum API which a OAPP needs to implement to interact with the OASIS/Our World system. This will enable them to just access the central profile/avatar and also grant additional karma for good deeds, etc.

<a name="our-world"></a>
### Our World

Our World is an example of the first OAPP. It is also the UI for the OASIS ecosystem which all OAPP's plug into. If the OAPP has implemented the UI interfaces above it will appear in Our World. The UI can be implemented as anything they wish ranging from a simple object, shop, area, city, region, country, continent, planet, Galaxy or even a whole meta-verse such as X The Game or Infinite World Game. A number of default implementations and helper methods will be provided to make it as easy as possible to plug various OAPP's into Our World, H4OME will likely be the quickest and easiest way using its simple drag n drop interface so no programming knowledge will be required.

Our World is a OAPP, hAPP, DAPP & HAPP meaning it can talk to all networks (ONET, H4OME & Ethereum) and other apps. It acts as the bridge and central nexus point between all apps & networks. Additional networks & apps may be added in future...

<a name="oapi--oasis-api-"></a>
### OAPI (OASIS API)

Through the OAPI, an app/website can gain access to the central profile/avatar of a user and grant them additional karma for doing good deeds or making progress within the positive uplifting app/website. Only positive uplifting apps/websites will be granted access to the OAPI and the OASIS System. This will be yet another incentive for developers to start focusing on positive, benevolent uplifting apps/websites/games.

The OAPP API by default hides away complexities such as networking and storage but these can still be accessed by power users.

<a name="onet--oasis-network-"></a>
### ONET (OASIS Network)

The various OAPP's communicate over a private, secure desentralised & distributed network called ONET. A client/node can even choose to share some of their spare processing power to other less powerful clients/nodes in the network, they of course gain additional karma for doing this. This way the crowd is the cloud and acts as a distributed cloud. The network is powered by Holohain. 

<a name="oapp--legacy-apps-websites-"></a>
### OAPP (Legacy Apps/websites)

A OAPP does not need to implement the full UI stack above, it can just use and share the central profile/avatar through the OAPI and will talk directly to the OASIS Engine (where the profile/avatar is stored).

This make it fully backward compatible with existing apps, websites, etc.

<a name="oapp-dapp"></a>
### OAPP/DAPP

An Ethereum DAPP (Distributed App) can also be a OAPP if it implements the OAPI, this means it can talk to both other DAPPs on the Ethereum Blockchain as well as other OAPPs on the ONET Holochain. Our World is the first example of this being a OAPP, DAPP & HAPP.

<a name="oapp-happ"></a>
### OAPP/hAPP

Other Holochain Apps (hAPP) can also be an OAPP and can easily communicate with the ONET/OAPI since Holochain apps can communicate to multiple Holochain networks (ONET is a Holochain network).

<a name="oapp-happ2"></a>
### OAPP/HAPP

An OAPP can also be a HAPP, which means it can plug into both the H4OME & OASIS Systems. This will be the most common use case. This could work by there being some sort of Publish/Export as OAPP option within H4OME or simply a OASIS Engine compatible checkbox within H4OME. This will then ensure the HAPP also implements the OAPI for sharing the central profile Avatar within Our World. If the HAPP also wishes to have a UI within Our World then it will also need to implement the UI stack above, which can be jointly developed by NextGen Software & S7 Foundation (please see the H4OME OASIS implementation Diagram for an example of this).


### Business OAPP

Businesses can also implement OAPP's if they qualify as a benevolent organisation doing good for the people and the planet. They also have a central profile, which also has karma, the more good deeds they do for the people and the planet, the more karma they earn. The higher their karma the bigger and better location of virtual space they will be allocated within Our World. The higher their karma the more premium advertising space they will also be allocated, which will be very limited within Our World since we do not wish to bombard users with adverts as others do. Unlike others, money is not the currency Our World, karma is so they cannot buy their way in. This will encourage organisations to do more to help solve the worlds problems.

<a name="h4ome---arc"></a>
### H4OME / ARC

H4OME is currently being built by the S7 Foundation, a partner for Our World & NextGen Software. It allows users to create Holonic Apps (HAPPS) using a quick and easy to use drag n drop interface. It allows blocks of code/functionality to be coupled together so works like a 4th Generation Programming Language. The blocks are currently written in Javascript. It is built on top of Holochain, IPFS & Ethereum just like the OASIS Engine so they are a very good fit for each other.


**NOTE: This is still a WIP, so the above is likely to evolve and change as we progress...**

<a name="our-world-oasis-will-act-as-the-bridge-for-all--legasy--ipfs--holochain--ethereum--solid--fediverse--mastodon--diaspora--webfinger--activitypub--xmpp---more--"></a>
<a name="bridge"></a>
## Our World/OASIS Will Act As The Bridge For All (Legasy, IPFS, Holochain, Ethereum, SOLID, Fediverse, Mastodon, Diaspora, WebFinger, ActivityPub, XMPP & More!)

As you can see from the architecture diagram, the system will act as the for all platforms and devices due to it being very open and modular by design. In future there will be support for IPFS, Ethereum, SOLID, Fediverse, Mastodon, Diaspora, WebFinger, ActivityPub, XMPP plus many more. This will help users of both legacy apps/games/websites and blockchain slowly migrate to holochain since it will help expose it to them all. The OASIS API will act as a stepping stone as well as help Everything talk to Everything for maximum compatibility.

**Goodbye silos and walled gardens, hello full integration through ONE universal unified interface!**

<a name="implement-your-own-storage-network-renderer-provider"></a>
### Implement Your Own Storage/Network/Renderer Provider

Thanks to the system being very open/modular by design you can easily implement your own Storage/Network/Renderer Provider by simply implementing the [IOASISStorage](#ioasisstorage) / [IOASTNET](#ioasisnet) / IOASIS2DRenderer / IOASIS3DRenderer interfaces respectively. For example you could create a MongoDB, MySQL or SQL Server Storage Provider. This also ensures forward compatibility since if a new storage medium or network protocol comes out in the future you can easily write a new provider for them without having to change any of the existing system. 

The same applies if a new 3D Engine comes out you want to use.

### Switch To A Different Provider In RealTime

The system can even switch to a different Storage/Network Provider in real-time as a fall-over if one storage/network provider goes down for example. It could even use more than one Storage/Network provider since certain providers may be better suited for a given task than another, this way you get the best of both worlds as well as ensure maximum compatibility and uptime.

The same applies for the Renderer Provider, it could use one provider to render 2D and another for 3D, it could even use more than one for for both 2D and/or 3D.

## Fully Integrated Unified Interface

**Our World is much more than just a game or platform. It is also a social network, ecosystem, asset store, operating system, app store, e-commerce & soooooo much more! ;-) It is the XR Gamified Layer of the new interplanetary operating system and the new internet (Web 3.0 - The Spacial Web).  It is the future cyberspace we will all be fully immersed in...**

It combines everything out there into one unified fully integrated interface. You never need to leave the XR/IR interface, you can launch all your apps, surf the net, check your email, make video calls, check your social feeds, play games, use real-time 3D geo-location maps of the world, shop, run your business and do everything you can currently do with existing technology but on a much more evolved fully integrated XR way... If you want to get an idea of what this looks like then watch Ready Player One, the OASIS that features in that is about 40% of what Our World is and we have been designing it long before we had even heard of Ready Player One.

<a name="socialnetwork"></a>
### NextGen Social Network

The social network part of Our World will be a fully de-centralised distributed social network that has your privacy concerns built into the design. You store and own your data on the ONET (powered by Holochain) and choose what you share and to who so it is never stored on any central server where it can be sold to advertisers, etc as is the case with Facebook, Google,etc. 

<a name="oasis-avatar-profile-karma-integration"></a>
#### OASIS Avatar/Profile/Karma Integration

What's more, this is fully integrated into the rest of the system and the OASIS Avatar/Profile/Karma system. So your profile will contain your 3D avatar and you will gain karma if you help people on the network. Of course if you are abusive then you will lose karma so this is a good incentive to behave yourselves and be kind and loving to your fellow earthlings... ;-)

<a name="our-world-oasis-api-social-network-website"></a>
#### Our World/OASIS API/Social Network Website

As well as the smartphone & desktop/console versions of Our World/OASIS, there will also be a traditional website, which will be the social network part of the system where people can view people's profiles/avatars, their karma, chat, find people with similar passions & interests. You can also help other people in need and gain karma, etc. They can also view the various satellite apps/games/websites that are linked and integrated into the OASIS System. Just like the smartphone & console/desktop versions they can also launch the satellite app/game/website from the website.

There will also be a AR & VR version of the social network fully integrated into the smartphone and desktop/console versions of Our World.

Please see the [OASIS API/Karma System](#oasisapi) section for more info.

#### Noomap Integration

This is also of course linked to your Me Holon in Noomap along with all your passions, interests, etc as described earlier.

It will also be deeply integrated into every other aspect of the system as mentioned earlier (shopping, business, games, email, etc).

<a name="deep-integration-into-other-networks-protocols-platforms--such-as-gab--mastodon--diaspora--webfinger--solid--ethereum--fediverse--activitypub--xmpp---more--"></a>
#### Deep Integration Into Other Networks/Protocols/Platforms (Such as Gab, Mastodon, Diaspora, WebFinger, SOLID, Ethereum, Fediverse, ActivityPub, XMPP & More!)

We plan to also deeply integrate into any other aligned open freedom loving networks/platforms/protocols such as Gab, Mastodon, Diaspora, WebFinger, SOLID, Ethereum, Fediverse, ActivityPub, XMPP etc so you can share your profile data between the various networks. You no longer need to have many logins and apps, you just have ONE central portal to do ALL you need to in a very cool evolved XR way...

You can also choose to store your data on any other platform/server such as a SOLID Pod or Matteron Server but either way you will be able to share data between Holochain, SOLID, Etherum, Fediverse (ActivePub) and any other open standard protocol/platform using the OASIS API.

**Our vision is to connect everything to everything through one universal fully integrated interface.**

If only you could see what we see... there is a reason why we are called NextGen Software! ;-)


## Platforms

We will have both a Smartphone App version and a PC/Console version. We are aiming to get this released on as many platform’s as possible including iOS, Android, Windows Phone, iPad, Windows Tablet, Android Tablet, XBOX ONE, PS4 & PC.

Some of the hardware we will be pushing to the limits are below:

**Augmented Reality**
* Magic Leap
* Microsoft HoloLens
* Google Glass
* Google Tango
* Apple ARKit
* Google ARCore
* Others
 
 **Virtual Reality**
* Oculus Rift
* HTC Vive
* Samsung Gear VR
* Samsung Odyssey VR
* PlayStation VR
* Others
 
**Emotional Feedback**
* NeuroSky/MyndPlay
* Others
 
**Motion Detection/Voice Recognition/Eye Tracking**
* Kinect
* Leap Motion
* Others

**Haptic Feedback**
* Hapto VR 
* Others

If you check out the demos of the above, you will start to get an idea of the apps & games we are building. However, of course we are pushing these to the next level by building the next generation apps & games for today. The game is much bigger than just a game, it is more like a massive educational platform, with a LOT more revolutionary ideas, which at this time we cannot make public.

<a name="pc-console-version"></a>
#### PC/Console Version

Our World will have continuous expansions, add-ons and
 sub-games added to keep players immersed and wanting more and more. Our World is revolutionary and contains many elements never done before and so will not have any competition in the new genres it will be creating…

#### Smartphone Version

The smartphone version is a free app with in-app purchases. This is why Our World will be free to download and have many in-app purchases not only for items you can use but also for expansion packs and sub-games. All of which will leave the player wanting more and more…

## NextGen Hardware

Current devices such as phones, tablets, laptops, etc emit harmful EMF (Electro Magnetic Field) radiation. This includes Wi Fi and 3G/4G/5G. The faster and more powerful they become the more dangerous they are to us. We are electromagnetic beings and so we are sensitive to this radiation. 

We plan to tackle this with our nextgen devices, which not only shield you from these harmful effects but actually heal you. They will also never need charging using the latest nextgen technology (Torus Energy & Zero Point Energy). They will also have nextgen performance and usability and be fully integrated with our nextgen software.

We will provide fully integrated software/firmware/hardware solutions that are free of any spyware/backdoors/surveillance as sadly is the case today with most of the devices out there. We have already begun talking to various providers of Open Source hardware/operating systems for smartphone devices but this is something we will be moving onto at a later date, maybe by around 2021...

Read more on our blog post here:
https://www.ourworldthegame.com/single-post/2018/01/31/NextGen-Hardware---Devices-That-Heal-You-Never-Need-Charging 


## Our World Overview

### Introduction

Imagine playing a game more fun and immersive than Pokémon Go, Minecraft, World of Warcraft and Second Life combined? A game that is not only a lot of fun to play but also teaches you how to look after your wellbeing as well as looking after our beautiful planet. A game that changes the way we think and interact with each other and the world so together we can create a better world for all of us. One where we can come together and help each other for the greater good of all.

Imagine a world where there are no more wars, poverty or suffering.

Imagine a world where there is only peace, love & unity where we all co-exist living as one human race in harmony with each other and our beautiful planet.

This does not just have to be a dream; together we can create this world…

Let us introduce you to Our World, the game that will change the world. As well as helping to make the world a better place, this game will be pushing the boundaries of what is currently possible with technology. It will feature augmented reality, virtual reality, motion detection, voice recognition and real-time emotional feedback. It will use technology in ways that has not been done before and in areas where it has been done, it will innovate and take it to the next level...

The software industry has a morale & social responsibility to use technology to help create a better world rather than lead to further decline. It has the power to transform lives through engaging people with innovative products that help them to grow and develop. Recent popular examples include health apps, mindfulness apps and mind training games.

We wish to take this to the next level and help make the world a better place by using technology for good, by bringing people together and to support, guide and educate everyone on how we can all live happier, fulfilling lives and at the same time how we can help save our planet.

We will do this by creating a suite of nextgen apps & games using the latest cutting-edge technology such as Virtual Reality, Augmented Reality, Real-time emotional feedback, face/voice recognition, motion detection and so much more.

**Our World is an exciting immersive next generation 3D XR/IR (Infinite Reality) educational game/platform/social network/ecosystem teaching people on how to look after themselves, each other and the planet using the latest cutting-edge technology. It teaches people the importance of real-life face to face connections with human beings and encourages them to get out into nature using Augmented Reality similar to Pokémon Go but on a much more evolved scale. This is our flagship product and is our top priority due to the massive positive impact it will make upon the world...**

<a name="xr-ir-gamification-layer-of-the-new-interplanetary-operating-system---the-new-internet--web-30-"></a>
### XR/IR Gamification Layer Of The New Interplanetary Operating System & The New Internet (Web 3.0)
 
**It is the XR/IR Gamification layer of the new interplanetary operating system & the new internet (Web 3.0), which is being built by the elite technical wizards stationed around the world. This will one day replace the current tech giants such as Google, FaceBook, etc and act as the technical layer of the New Earth, which is birthing now.  Unlike the current tech giants who's only aim is to ruthlessly maximize profits at the expense of people and the planet (as well as spying, exploitation, censorship & social engineering), our technology is based on true love & unity consciousness where money and profits are not our aim or intention, our aim and intention is to heal the entire planet & human race so we can all live in harmony with each other.  It is a 5th dimensional and ascension training platform, teaching people vital life lessons as well as acting as a real-time simulation of the real world.**

As well as helping to make the world a better place, this game will be pushing the boundaries of what is currently possible with technology. It will feature augmented reality, virtual reality, motion detection, voice recognition and real-time emotional feedback. It will use technology in ways that has not been done before and in areas where it has been done, it will innovate and take it to the next level...

<a name="open-world-new-ecosystm-asset-store-internet-operating-system-social-network"></a>
### Open World/New Ecosystm/Asset Store/Internet/Operating System/Social Network

It is much more than just a free open world game where you can build and create anything you can imagine and at the same time be immersed in an epic storyline. it is an entirely new ecosystem/asset store/internet/Operating System/social network, it is the future way we will be interacting with each other and the world through the use of technology. Smaller satellite apps/game will plug into it and share your central profile/avatar where you gain karma for doing good deeds such as helping your local communities, etc and lose karma for being selfish and not helping others since it mirrors the real world where you have free will. There is nothing else out there like this, nothing even comes close, this will change everything... There is a reason we are called NextGen Software! ;-)

The game teaches people true unity consciousness where everyone benefits if people put their differences aside and work together. Our World is also an ecosystem and a virtual e-commerce platform and so, so, so much more, it will create a whole new genre and blaze a new path for others to try and follow…

Our World has now merged with our NextGen Social Network project, which was always planned to be the prequel to Our World, so it made sense to simply merge them together.

### Infinite Alternate Reality Game (IARG)

Our World is also a Infinite Alternate Reality Game where the line between virtual and reality blurs into one.

### Our World Integrates The Commons Engine & Mutual Crypto Currency

Our World will fully integrate the Commons Engine & Mutual Crypto Currency to educate the world in a fun gamified way how society can work much fairer and more effectively doing away with the current unfair fiat system that is controlled by the greedy banks, corportations & governments.

### Synergy Engine

Our World implements the Synergy Engine helping to solve the world’s problems by matching solutions to problems. It also teaches the co-creation wheel and a new holistic approach to living and technology to help co-create a better world. 

Our World implements the Synergy Engine helping to solve the world’s problems by matching solutions to problems. It also teaches the co-creation wheel and a new holistic approach to living and technology to help co-create a better world. 

### Resource Based Economy

Our World teaches people the benefits of a <a href="https://www.thevenusproject.com/resource-based-economy/">Resource Based Economy</a> (coined by Jacque Fresco, the founder of <a href="https://www.thevenusproject.com/">The Venus Project</a>) where the world's resources are freely available to everyone and people exchange products and services without the need for money. For this to be achieved all resources must be declared as the common heritage of all Earth’s inhabitants. Equipped with the latest scientific and technological marvels mankind could reach extremely high productivity levels and create abundance of resources. This also prevents money being hoarded through greed and corruption and can no longer be used to control & divide people. A Resource Based Economy is actually fully integrated into Our World.

### First AAA MMO Game To Run On Holochain

Our World is built on top of the de-centralised, distributed nextgen internet known as Holochain.

Our World will be the first AAA MMO game and 2D/3D Social Network to run on HoloChain and the Blockchain. It will also be the first to integrate a social network with a MMO game/platform as well as all of these technologies and devices together. As with the rest of the game, it will be leading the way in what can be done with this NextGen Technology for the benefit and upliftment of humanity. 

Read more here:

https://www.ourworldthegame.com/single-post/2019/02/22/Why-Our-World-Is-Powered-By-Holochain 

We will soon be launching our ICO to sell our OASIS Coin, more news on this soon...

### Smartphone Version

The smartphone version will be a geolocation game featuring Augmented Reality similar to Pokemon Go but on a much more evolved scale (yes, we were designing this long before Pokemon Go came out!).

### Console Version

The console/desktop version will be similar to a Sandbox and MMORPG (Massive Multiplayer Online Role Playing Game) but
 will be nothing like any other games such as World Of WarCraft & MineCraft. It will in fact define its own genre setting the new bar for others to follow, this truly has never been done before and will take the world by storm! The one thing it will share with them is that it will be a massive open world that billions of players can explore and build together... 

Both versions will share the same online world/multiverse where users logged in through the smartphone versions will be able to interact with the console/desktop versions in real-time within a massive scale persistent Multiverse.

### Engrossing Storyline

You can run around an open world completing quests in whatever order you choose but it will also contain an engrossing storyline, which will change depending on what choices both you and the collective take as a whole. The story is about the final epic battle for Earth between the forces of darkness and the light that has been raging across Galaxies & Universes for eons. You will fight demons, zombies, monsters, killer robots controlled by a dark evil AI and more. The main difference to other games is that you will not fight fellow humans (although the dark AI is trying to manipulate mankind in to doing just that), instead you will unite together against the new common enemy that is threatening the very existence of mankind and the planet. You will be free to build your own homes, communities, base defences, vehicles & ships (or purchase or win) either using traditional means, nextgen technology or even magic. The same goes for combat, you can choose to use pure strength, skill and melee, technology or magic. You can be whoever you want to be. You can even choose not to fight and instead focus on supporting the economy, farming, healing, R&D, construction, leadership, etc. The choice is yours...

As you proceed through the game you will discover that this dark evil AI has taken over the minds of many humans who are in positions of power & influence across the globe such as Governments, banking, corporations, educational institutions, pharmaceutical & the military and is using them as puppets for its evil plans for world domination. This secret society is known as The Dark Order. The dark AI (also known as The Beast) is manipulating humanity to create technology and robotic bodies for it to control to form its army of machines. It is also trying to get every human implanted with a chip so it can track and control them, this is known as the Mark Of The Beast. It is also trying to manipulate them to open a portal to other dimensions to bring forth its dark army in the final phase of its plan. It plans to exterminate 90% of the population and enslave the rest. Your mission along with the rest of mankind is to stop this before it is too late...

### OASIS Asset Store

Objects created (vehicles, building, etc) can be shared and even sold on the OASIS Asset Store. Objects created on other popular platforms such as Google Blocks and Microsoft's 3D Creator can also be imported and used in Our World.

### Virtual E-commerce

As well as smaller apps/games being allowed to plug into Our World either sharing just the central avatar/profile (data) or full UI integration, content creators/businesses can also create shops (where people can purchase real items in VR that are then delivered to your door so in effect is virtual e-commerce), buildings or even entire zones/lands/worlds. They can rent virtual spaces within the game. Please contact us on ourworld@nextgensoftware.co.uk if you wish to receive special early adopter discounts...

<a name="we-accept-karma--your-money-is-no-good-here-"></a>
### We Accept Karma, Your Money Is No Good Here!

Businesses can also sponsor or advertise in the game but unlike traditional models, money does not buy you the best spots, the companies collective karma does. The greener & the more they do good for the world including giving to charities, looking after their employees, the environment, etc the more karma they get. Advertising spots will be limited since we do not wish to bombard users with adverts so this will be an incentive for companies worldwide to improve and start focusing on what is important, and that's people and the environment, not money. As may be clear by now, the focus and goals of Our World is to create a better world, not to make as much money as possible. But we of course will still make more than enough (billions) to continue to expand & grow, the rest will go to good causes and charities such as our sister company Yoga4Autism.

### Our World Is Only The Beginning...

Our World is only the beginning... once we have learnt to resolve our problems and live in peace and harmony with each other and the planet then we will be ready to reach out and explore other worlds within our Universe and beyond...

**We cannot try to run from our problems and escape to other worlds (virtual or real), we need to stay and heal Our World first...
Once we have done that then we can transform Our World into the OASIS, a paradise on Earth...**

**Our World is just the first world of an infinite number of worlds, stars, systems, galaxies & universes to explore...**

**This multiverse is called The OASIS, which can only be reached through the OASIS we are co-creating on Our World.**

The OASIS will only be open to us once we have resolved our issues here, humanity must prove they are worthy to join the Galactic Societies waiting for us out there... How can we meet, interact and get on with other races out there when we cannot even get on with each other here?

It is time to stop running from our problems and face them united together...

<a name="the-tech-industry-have-a-morale---social-responsibility"></a>
### The Tech Industry Have A Morale & Social Responsibility

The software industry has the power to transform lives through engaging people with innovative products that help them to grow and develop.  Recent popular examples include health apps, mindfulness apps and mind training games.

We wish to take this to the next level and help make the world a better place by using technology for good, by bringing people together and to support, guide and educate everyone on how we can all live happier, fulfilling lives and at the same time how we can help save our planet.

People learn at a young age how to act and behave and this shapes the future generations and the world they will create. Due to the majority of games these days involving violence, sex, gambling, drugs & crime, this is conditioning the youth of today to the sort of world they will create tomorrow.

With the advent of Virtual Reality now making these violent games even more immersive and realistic where the boundaries between games and reality is shrinking by the day, it is imperative we take some social and moral responsibility and start using technology to help create a better world by improving people's life's as well as respecting the environment and planet that sustains us.

Kids today are playing very violent games such as Call Of Duty which are used as brainwashing techniques to desensitise us to violence and also act as a training and recruitment tool for the military (which they have now admitted). The same goes for flight simulators being used to train and recruit drone pilots.

We hope you will agree this is totally unacceptable and is part of why there is so much war, violence, etc in today’s world. It is time we start using technology to teach people the correct life lessons. Our World acts as a simulation for the real world and teaches them how to create a better world in the simulation and then shows how they can then implement these important lessons in real life. Read more on our previous blog post about violence in video games:

https://www.ourworldthegame.com/single-post/2018/03/14/Good-they-are-finally-start-to-take-the-violence-in-video-games-seriously 

Gambling is being forced onto kids more and more in the form of loot boxes where real money is asked for to receive a random prize and now it’s got so bad that money is actually needed to progress within the game. Everything seems to be geared around how much people can be exploited and how much money can be sucked out of them, this is even more wrong for kids. Read more on our previous post about this below:

https://www.ourworldthegame.com/single-post/2017/11/29/Do-you-think-its-right-kids-are-gambling-in-games 

We wish to reach the kids who are glued to their phones and consoles and never go outside, this game will encourage them to get out into parks and interact with people in fun creative ways face to face instead of through their phones.

### Teach Kids The Right Life Lessons

The game will also be teaching people especially kids important vital life lessons and show how they can then implement them in the real world. Part of the way this will be done is by merging the real world with Our World using the latest cutting-edge technology such as Augmented Reality. We wish to get kids and everyone else off their devices and back into nature and to start having real face to face interactions again. We wish to use technology for the upliftment and benefit of humanity and the planet and not just as an escape mechanism or a way to exploit people by selling their data to the highest bidder.

<a name="remember-how-powerful-you-are-"></a>
### Remember How Powerful YOU Are!

Our World reminds people how powerful they are and empowers them to be the person they have always wanted to be, to live their life to their FULL potential without any limitations. Everyone has a gift for the world and with Our World we can help them find it. We want to empower people to take responsibility for our beautiful planet, which is currently in crisis and so needs EVERYONE to help make a difference. The entire world is the Our World team, we want everyone to get involved
 so they can feel they are part of something greater than themselves and at the same time ensure there is a future for our kids and grandkids.

### Bringing People Together

We wish to bring people together, build online communities, encourage people to reach out and help strangers for the greater good of all. To encourage people to come and work together and to show how everyone benefits if they put their differences aside and start all rowing together. It will model the real world and also act as a simulation and training environment for how to make the real world a better place.

### We are Building The Evolved Benevolent Version Of The OASIS

We are building the evolved benevolent version of the OASIS featured in the popular Ready Player One novel and Spielberg film. The OASIS is only about 40% of what Our World is to give you an idea of the sheer size and magnitude of this project! It is aimed at saving the world rather than leading to its destruction due to the neglect it faced when everyone escaped into the OASIS. Ready Player One has proven so popular that Spielberg & Warner Brothers have now released the blockbuster film, which we hope will help promote Our World further. It is about someone with Autism who creates a revolutionary 3D VR Platform which takes the world by storm because it is so far ahead of everything else out there. The creator of the 3D VR platform known as the OASIS grew up in the 80's, is obsessed with the 80's and had guitar lessons as a kid, which also describes our founder David Ellams. 

Read more in our previous post here:

https://www.ourworldthegame.com/single-post/2017/09/08/Our-World-Is-The-Benevolent-Evolved-Sister-of-The-Ready-Player-One-OASIS-VR-Platform 

<a name="ascension-god-training---mirror-of-reality-technology"></a>
### Ascension/God Training & Mirror Of Reality Technology

These are the Last Days of Mortal Man through this God Training Programme.

The self-reflective immersive XR game that has been created as ascension technology to help the user to discover their higher self through learning important lessons in how to be, think and feel as a human being. Through “karma” each individual can build themselves to be a better version of themselves that on a sub conscious level will teach them how this can be applied in the real world. The game truly is a mirror for reality. 

Some important points about its potential capabilities and why it could be truly unique:

**Bio Scan technology**- through mapping of brain waves, it can suggest activities and exercises that correlate to the analysis it receives teaching the user to be more mindful about health and wellbeing.

**Life cycle** - There will be time constraints on how long each player can be in Our World for. The vital energy will correlate with reality meaning they will not be burn themselves out locked in the game. Teaching the individual once again the important lessons of having a balanced lifestyle. The character, like the player needs to be looked after.

**Virtual Advertising** - Companies can use the advertising and be awarded prime spots based on their own karma value meaning that those that act more responsibly and consciously in real life (such as giving to charity, being green, looking after their employees, etc) will have access to the most prime spots, as opposed to those who pay the most.

**Time Bending Treasures** - Messages and gifts can be buried as Easter eggs for others to collect at later dates bending the nature of time. 

<a name="7-years-of-planning---r-d"></a>
### 7 Years Of Planning & R&D

We actually started researching, planning & designing this over seven years (we have also been busy networking, building partnerships, etc) ago but we could not yet afford the large amount of money it would take to create this. On top of this, the technology did not yet exist to create the vision, but this is now changing. When Pokémon Go was released featuring more primitive versions of some of the technology featured in Our World, we realised we really need to get this game into production.

### Early Prototype

We need your investment/help so we can continue development of the cutting-edge prototype we have been working on for the last couple of years. This is the first Unity game to be powered by the revolutionary decentralised distributed network called Holochain. This means that your profile and data is stored locally on your device giving you back control of your own data. See screenshots here:

https://www.ourworldthegame.com/single-post/2018/08/14/First-look-at-our-Smartphone-Prototype

Check out the latest progress made with the protoype below:

https://www.ourworldthegame.com/single-post/2019/06/02/OASIS-ArchitectureHoloNETHolochainHoloWebBrowserPrototypeUpdate 

https://www.ourworldthegame.com/single-post/2019/07/03/BIG-UPDATE---Lots-Of-Progress-Made-On-Prototype- 

https://www.ourworldthegame.com/single-post/2019/08/03/Our-World-OASIS-API-HoloNET-Goes-Open-Source- 

We can then demo this to interested parties so we can get more investment to get the first version of this game released. This game will have continuous development with frequent upgrades and add-ons. It is so vast, that the development roadmap is never ending.

### We Are What You Have All Been Waiting For...

http://www.ccsinsight.com/press/company-news/2251-augmented-and-virtual-reality-devices-to-become-a-4-billion-plus-business-in-three-years 

 An exert from the above article states:

“VR (virtual reality) and AR (augmented reality) are exciting – Google Glass coming and going, Facebook’s $2 billion for Oculus, Google’s $542 million into Magic Leap, not to mention Microsoft’s delightful HoloLens. There are amazing early stage platforms and apps, but VR/AR in 2015 feels a bit like the smartphone market before the iPhone. We’re waiting for someone to say “One more thing…” in a way that has everyone thinking “so that’s where the market’s going!”

Well, we are what everyone has been waiting for, to take this technology to the next level, hence our name!

Pokémon Go has already started to lose users as we predicted due to not being nowhere near immersive enough so to keep users engaged in the game. Our World is set to be one of the most immersive games ever made so it will not suffer from this problem.

http://www.bbc.co.uk/news/technology-37176782 
 
 
### Large Social Media Following

With over 5628 likes on our Facebook page ( http://www.facebook.com/ourworldthegame ), which is growing daily, this very important project is being very well received and we constantly receive glowing feedback of how much of a wonderful good idea this is, one that is needed more than ever in today’s world!

We are currently building the smartphone prototype and hope to have this done by 2020 Q2. We are also looking for any other developers, designers, 3D modellers and anyone else who wish to get involved so please get in touch if this is YOU...

### UN Contacts

We are in talks with Be Earth, which is a UN IGO (United Nations Intergovernmental Organisation). Read more about this in this post:

https://www.ourworldthegame.com/single-post/2018/01/27/Our-Word-partners-with-the-Be-Earth-UN-IGO-United-Nations-Intergovernmental-Organisation 

<a name="buckminster-s-world-peace-game"></a>
### Buckminster's World Peace Game

Our World is Buckminster Fuller's World Peace Game, please read more here:

https://www.ourworldthegame.com/single-post/2018/01/21/Our-World-Is-The-Buckminster-Fuller-World-Peace-Game 

### The NextGen Office

Our NextGen Offices that we plan to build one day will be deeply integrated with nature so streams, trees, etc will be inside a bit like <a href="https://www.centerparcs.com">Center Parcs</a> in UK. They will also be built to Sacred Geometry specifications so they are actually healing to work inside one. They will also contain healing crystals & orgonite so it actually heals and energizes you while you work...  They are also similar to the ones the <a href="https://www.thevenusproject.com">Venus Project</a> intend to build. Read more below:

https://www.ourworldthegame.com/single-post/2019/08/10/The-NextGen-Office

### Golden Investment Opportunity

According to the latest research the VR/AR Market is set for VERY explosive growth with estimates of $674bn by 2025. The mobile app industry has been growing exponentially for a number of years now and is set to continue to accelerate. The mobile app market was valued last year at over 27 billion dollars and is set to reach 77 billion this year.

So, make sure you get in on the ground floor of the next Apple, which will be the GOLDEN OPPORTUNITY of a lifetime! 

https://www.ourworldthegame.com/single-post/2018/03/08/Get-In-On-The-Ground-Floor-Of-The-Next-Apple 

https://www.ourworldthegame.com/single-post/2017/09/04/Golden-Opportunity-Of-a-Lifetime-For-Investors 

### Help Cocreate A Better World...

It only seems to be a week or two before another terrorist attack or mass shooting or disaster after disaster. How much more suffering does there have to be before the people unite together to say enough is enough?


**READ MORE ON THE [WEBSITE](http://www.ourworldthegame.com "Our World") OR [CROWD FUNDING](https://www.gofundme.com/f/ourworldthegame) PAGES**



<a name="nextgen-developer-training-programmes-for-everyone---including-special-needs---disadvantaged-people-"></a>
## NextGen Developer Training Programmes For EVERYONE! (Including Special Needs & Disadvantaged People)

We also offer FREE training with our NextGen Developer Training Programme where I will teach everything I know from my many years of experience working in the industry. We know that people on the Autistic Spectrum are just as gifted with computers as I am (I was given the label of Asperger’s, Dyspraxia & Dyslexia) so they will be able to help me take what’s possible with technology to the next level. 

We want to help the people that the world has turned their back on, people who no longer believe in themselves, we are here to tell them that we believe in them and in time we will help them believe in themselves again. We are here to tell them to forget what society says you can or cannot do, for you can do whatever you want to, you can follow your heart and achieve your dreams. We want to empower people to be their own boss and we actively encourage their creativity and imagination and that anything is possible. We want to give them free reign to work on or create whatever they like or heart desires.

Read more on the <a href="http://www.ourworldthegame.com">Our World</a> website.

Check out the training PDF downloads under the cunningly named Training section on our website:

http://www.nextgensoftware.co.uk 

You can manually download using the links below:
 
<a href="https://docs.wixstatic.com/ugd/4280d8_ad8787bd42b1471bae73003bfbf111f7.pdf">NextGen Developer Training Programme</a>

<a href="https://docs.wixstatic.com/ugd/4280d8_999d98ba615e4fa6ab4383a415ee24c5.pdf">Junior NextGen Developer Training Programme</a>

<b>NEW</b> - The Justice League Training Accademny is a superhero themed upgrade to the above training course fully integrated with the OASIS API so you can earn karma for learning and even more karma for providing training material. This is a superhero training platform, are you ready to be the hero of your own life story? You also unlock power ups and badges as you progress, come join the FUN NOW! :)

https://www.thejusticeleagueaccademy.icu



## The Power Of Autism

This game, website and promotional videos were all designed and created by our founder and Managing Director David Ellams BSc(Hons) who was given the labels of Aspergers (High Functioning Autism), Dyspraxia & Dyslexia. But he did not let these labels define him and has worked very hard to get where he is today. A lot of this was down to the yoga, meditation & mindfulness that helped transform his life in the most amazing way and helped managed the symptoms of autism as well as allowing him to harness his natural gifts in IT. 

This is why he created <a href="http://www.yoga4autism.com">Yoga4Autism</a> to help teach other people the power of yoga thus enabling them to live happy fulfilling life's to their FULL potential without any limitations as he now enjoys. He then wishes to give them all FREE training & jobs to help create a better world and to show the world what people with autism and so-called "disabilities" can REALLY do...

Read more on the <a href="http://www.ourworldthegame.com">Our World</a> website.

<a name="better-than-a-fornite-clone-----"></a>
## Better Than A Fornite Clone! ;-)

There was recently a blog post from Holochain talking about how the community wanted to make a Fortnite clone using Holochain. But we say why do you want to make a clone of yet another game promotting and gloryifing violence? Wouldn't you rather co-create a game that helps make the world a better place and ensures a future for the next generation by teaching them the right life lessons?

Read more on one of our recent blog posts: </br>
https://www.ourworldthegame.com/single-post/2019/02/22/Why-Our-World-Is-Powered-By-Holochain


## HoloNET

This allows .NET to talk to Holochain, which is where the profile/avatar is stored on a private decentralised, distributed network. This will be gifted forward to the Holochain community If there is demand for HoloNET and people wish to contribute we may consider splitting it out into it's own repo...

This is also how Holochain can talk to Unity because Unity uses C#/.NET as it's backend scripting language/tech.

This will help massively turbo charge the holochain ecosystem by opening it up to the massive .NET and Unity communities and open up many more possibilities of the things that can be built on top of Holochain. You can build almost anything you can imagine with .NET and/or Unity from websites, desktop apps, smartphone apps, services, AAA Games and lots more! They can target every device and platform out there from XBox, PS4, Wii, PC, Linux, Mac, iOS, Android, Windows Phone, iPad, Tablets, SmartTV, VR/AR/XR, MagicLeap, etc

**We are a BIG fan of Holochain and are very passionate about it and see a BIG future for it! We feel this is the gateway to taking Holochain mainstream! ;-)**

**Although the rest of this repo is [HoloSourced](#holosource-liscence) (see below) in that you need to be granted permission to fork and use it, HoloNET is totally free and Open Sourced to be used anyway you wish. If you find it helpful, we would REALLY appreciate a donation to our crowd funding page, because this is our full-time job and so have no other income and will help keep us alive so we can continue to improve it for you all, thank you! :)**

**https://www.gofundme.com/ourworldthegame**

<br>

### How To Use HoloNET


**NOTE: This documentation is a WIP, it will be completed soon, please bare with us, thank you! :)**


You start by instantiating a new HoloNETClient class found in the [NextGenSoftware.Holochain.HoloNET.Client](#project-structure) project passing in the holochain websocket URI to the constructor as seen below:

````c#
HoloNETClient holoNETClient = new HoloNETClient("ws://localhost:8888");
````

Next, you can subscribe to a number of different events:

````c#
holoNETClient.OnConnected += HoloNETClient_OnConnected;
holoNETClient.OnDataReceived += HoloNETClient_OnDataReceived;
holoNETClient.OnZomeFunctionCallBack
 += HoloNETClient_OnZomeFunctionCallBack;
holoNETClient.OnGetInstancesCallBack += HoloNETClient_OnGetInstancesCallBack;
holoNETClient.OnSignalsCallBack += HoloNETClient_OnSignalsCallBack;
holoNETClient.OnDisconnected += HoloNETClient_OnDisconnected;
holoNETClient.OnError += HoloNETClient_OnError;
````

Now you can call the [Connect](#connect) method to connect to Holochain.

````c#
await holoNETClient.Connect();
````

Once you received a [OnConnected](#onconnected) event callback you can now call the [GetHolochainInstancesAsync](#getholochaininstancesasync) method to get back a list of instances the holochain conductor you connected is currently running.

````c#
if (holoNETClient.State == System.Net.WebSockets.WebSocketState.Open)
{
        await holoNETClient.GetHolochainInstancesAsync();
}
````

Now you can use the instance(s) as a parm to your future Zome calls...

Now you can call one of the [CallZomeFunctionAsync()](#callzomefunctionasync) overloads:

````c#
await holoNETClient.CallZomeFunctionAsync("1", "test-instance", "our_world_core", "test", ZomeCallback, new { message = new { content = "blah!" } });
````

Please see below for more details on the various overloads available for this call as well as the data you get back from this call and the other methods and events you can use...

<br>

### The Power of .NET Async Methods

You will notice that the above calls have the `await` keyword prefixing them. This is how you call an `async` method in C#. All of HoloNET, HoloOASIS & OASIS API methods are async methods. This simply means that they do not block the calling thread so if this is running on a UI thread it will not freeze the UI. Using the `await` keyword allows you to call an `async` method as if it was a synchronous one. This means it will not call the next line until the async method has returned. The power of this is that you no longer need to use lots of messy callback functions cluttering up your code as has been the pass with async programming. The code path is also a lot easier to follow and maintain.

Read more here:
https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/

<br>

### Events
<a name="events"></a>

You can subscribe to a number of different events:

| Event                  | Description                                                                                              |
| ---------------------- | -------------------------------------------------------------------------------------------------------- |
| [OnConnected](#onconnected)            | Fired when the client has successfully connected to the Holochain conductor.                             |
| [OnDisconnected](#ondisconnected)         | Fired when the client disconnected from the Holochain conductor.                                         |
| [OnError](#onerror)                | Fired when an error occurs, check the params for the cause of the error.                                 |
| [OnGetInstancesCallBack](#ongetinstancescallback) | Fired when the hc conductor has returned the list of hc instances it is currently running.               |
| [OnDataReceived](#ondatareceived)         | Fired when any data is received from the hc conductor. This returns the raw JSON data.                   |
| [OnZomeFunctionCallBack](#onzomefunctioncallback) | Fired when the hc conductor returns the response from a zome function call. This returns the raw JSON data as well as the actual parsed data returned from the zome function. It also returns the id, instance, zome and zome function that made the call.                                                               |
| [OnSignalsCallBack](#onsignalscallback)      | Fired when the hc conductor sends signals data. NOTE: This is still waiting for hc to flresh out the    details for how this will work. Currently this returns the raw signals data.                             | 
<br>

#### OnConnected
Fired when the client has successfully connected to the Holochain conductor. 

````c#
holoNETClient.OnConnected += HoloNETClient_OnConnected;

private static void HoloNETClient_OnConnected(object sender, ConnectedEventArgs e)
        {
            Console.WriteLine(string.Concat("Connected to ", e.EndPoint));
            Console.WriteLine("");
        }
````
<br>

|Parameter|Description  |
|--|--|
|EndPoint | The URI EndPoint of the Holochain conductor.
<br>

#### OnDisconnected
Fired when the client has successfully disconnected from the Holochain conductor. 

````c#
holoNETClient.OnDisconnected += HoloNETClient_OnDisconnected;

 private static void HoloNETClient_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            Console.WriteLine(string.Concat("Disconnected from ", e.EndPoint, ". Resason: ", e.Reason));
            Console.WriteLine("");
        }
````
<br>

|Parameter|Description  |
|--|--|
|EndPoint | The URI EndPoint of the Holochain conductor.
|Reason | The reason for the disconnection.
<br>

#### OnError
Fired when an error occurs, check the params for the cause of the error.       

````c#
holoNETClient.OnError += HoloNETClient_OnError;

 private static void HoloNETClient_OnError(object sender, HoloNETErrorEventArgs e)
        {
            Console.WriteLine(string.Concat("Error Occured. Resason: ", e.Reason,  ", EndPoint: ", e.EndPoint, ", Details: ", e.ErrorDetails.ToString()));
            Console.WriteLine("");
        }
````
<br>

|Parameter|Description  |
|--|--|
|EndPoint | The URI EndPoint of the Holochain conductor.
| Reason | The reason for the error.
| ErrorDetails | A more detailed description of the error, this normally includes a stacktrace to help you track down the cause.

<br>

#### OnGetInstancesCallBack
Fired when the hc conductor has returned the list of hc instances it is currently running.

````c#
holoNETClient.OnGetInstancesCallBack += HoloNETClient_OnGetInstancesCallBack;

private static void HoloNETClient_OnGetInstancesCallBack(object sender, GetInstancesCallBackEventArgs e)
{
            Console.WriteLine(string.Concat("OnGetInstancesCallBack: EndPoint: ", e.EndPoint, ", Id: ", e.Id, ", Instances: ", string.Join(",", e.Instances), ", DNA: ", e.DNA, ", Agent: ", e.Agent, ", Data: ", e.RawJSONData));
            Console.WriteLine("");
}
````
<br>

|Parameter|Description  |
|--|--|
|EndPoint | The URI EndPoint of the Holochain conductor.
|WebSocketResult| Contains more detailed technical information of the underlying websocket. This includes the number of bytes received, whether the message was fully received & whether the message is UTF-8 or binary. Please <a href="https://docs.microsoft.com/en-us/dotnet/api/system.net.websockets.websocketreceiveresult?view=netframework-4.8">see here</a> for more info.
| Id                 | The id that made the request.                      
| DNA | The DNA of the instance running on the Holochain conductor.
| Agent | The name of the agent running on the Holochain conductor.
| Instances | A list of instances currently running on the Holochain conductor.
|RawJSONData  | The raw JSON data returned from the Holochain conductor. |


<br>

#### OnDataReceived
Fired when any data is received from the hc conductor. This returns the raw JSON data.  

````c#
holoNETClient.OnDataReceived += HoloNETClient_OnDataReceived;

private static void HoloNETClient_OnDataReceived(object sender, DataReceivedEventArgs e)
{
      Console.WriteLine(string.Concat("Data Received: EndPoint: ", e.EndPoint, "RawJSONData: ", e.RawJSONData));
}
````
<br>

|Parameter|Description  |
|--|--|
|EndPoint | The URI EndPoint of the Holochain conductor.
|WebSocketResult| Contains more detailed technical information of the underlying websocket. This includes the number of bytes received, whether the message was fully received & whether the message is UTF-8 or binary. Please <a href="https://docs.microsoft.com/en-us/dotnet/api/system.net.websockets.websocketreceiveresult?view=netframework-4.8">see here</a> for more info.
|RawJSONData  | The raw JSON data returned from the Holochain conductor. |

<br>

#### OnZomeFunctionCallBack

Fired when the hc conductor returns the response from a zome function call. This returns the raw JSON data as well as the actual parsed data returned from the zome function. It also returns the id, instance, zome and zome function that made the call.                      

````c#
holoNETClient.OnZomeFunctionCallBack += HoloNETClient_OnZomeFunctionCallBack;

private static void HoloNETClient_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
{
            Console.WriteLine(string.Concat("ZomeFunction CallBack: EndPoint: ", e.EndPoint, ", Id: ", e.Id, ", Instance: ", e.Instance, ", Zome: ", e.Zome, ", ZomeFunction: ", e.ZomeFunction, ", Data: ",  e.ZomeReturnData, ", Raw Zome Return Data: ", e.RawZomeReturnData, ", Raw JSON Data: ", e.RawJSONData, ", IsCallSuccessful: ", e.IsCallSuccessful? "true" : "false"));
            Console.WriteLine("");
}
````             
<br>

 | Parameter          | Description                                        |
 | ------------------ | -------------------------------------------------- |
|EndPoint | The URI EndPoint of the Holochain conductor.
|WebSocketResult| Contains more detailed technical information of the underlying websocket. This includes the number of bytes received, whether the message was fully received & whether the message is UTF-8 or binary. Please <a href="https://docs.microsoft.com/en-us/dotnet/api/system.net.websockets.websocketreceiveresult?view=netframework-4.8">see here</a> for more info.
 | Id                 | The id that made the request.                      |
 | Instance           | The hc instance that made the request.             |
 | Zome               | The zome that made the request.                    |
 | ZomeFunction       | The zome function that made the request.           |
 | ZomeReturnData  
   | The parsed data that the zome function returned.   |
 | RawZomeReturnData  | The raw JSON data that the zome function returned. |
 | RawJSONData        | The raw JSON data that the hc conductor returned.  |
<br>

#### OnSignalsCallBack
Fired when the hc conductor sends signals data. NOTE: This is still waiting for Holochain to flesh out the details for how this will work. Currently this returns the raw signals data.
<br>

````c#
holoNETClient.OnSignalsCallBack += HoloNETClient_OnSignalsCallBack;

private static void HoloNETClient_OnSignalsCallBack(object sender, SignalsCallBackEventArgs e)
        {
            Console.WriteLine(string.Concat("OnSignalsCallBack: EndPoint: ", e.EndPoint, ", Id: ", e.Id , ", Data: ", e.RawJSONData));
            Console.WriteLine("");
        }
````   
<br>

 | Parameter          | Description                                        |
 | ------------------ | -------------------------------------------------- |
|EndPoint | The URI EndPoint of the Holochain conductor.
|WebSocketResult| Contains more detailed technical information of the underlying websocket. This includes the number of bytes received, whether the message was fully received & whether the message is UTF-8 or binary. Please <a href="https://docs.microsoft.com/en-us/dotnet/api/system.net.websockets.websocketreceiveresult?view=netframework-4.8">see here</a> for more info.
 | Id                 | The id that made the request.                     
 | RawJSONData        | The raw JSON data that the hc conductor returned.  |


<br>

### Methods

HoloNETClient contains the following methods:
<br>

|Method|Description  |
|--|--|
|[Connect](#connect)  | This method simply connects to the Holochain conductor. It raises the [OnConnected](#onconnected) event once it is has successfully established a connection. Please see the [Events](#events) section above for more info on how to use this event.
|[CallZomeFunctionAsync](#callzomefunctionasync)| This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the [OnZomeFunctionCallBack](#onzomefunctioncallback) event once it has received a response from the Holochain conductor. Please see the [Events](#events) section above for more info on how to use this event.
|[ClearCache](#clearcache) | Call this method to clear all of HoloNETClient's internal cache. This includes the JSON responses that have been cached using the [GetHolochainInstancesAsync](#getholochaininstancesasync) & [CallZomeFunctionAsync](#callzomefunctionasync) methods if the `cacheData` parm was set to true for any of the calls. |
|[Disconnect](#disconnect) | This method disconnects the client from Holochain conductor. It raises the [OnDisconnected](#ondisconnected) event once it is has successfully disconnected. Please see the [Events](#events) section above for more info on how to use this event. |
|[GetHolochainInstancesAsync](#getholochaininstancesasync) | This method will return a string array containing the instances that the holochain conductor is currently running. You will need to store the instance(s) in a variable to pass into the [CallZomeFunctionAsync](#callzomefunctionasync) later. This method raises the [OnGetInstancesCallBack](#ongetinstancescallback) event once it has received a response from the Holochain conductor. Please see the [Events](#events) section above for more info on how to use this event.|
|[SendMessageAsync](#sendmessageasync) |This method allows you to send your own raw JSON request to holochain. This method raises the [OnDataReceived](#ondatareceived) event once it has received a response from the Holochain conductor. Please see the [Events](#events) section above for more info on how to use this event. You would rarely need to use this and we highly recommend you use the [CallZomeFunctionAsync](#callzomefunctionasync) method instead.

<br>

#### Connect

This method simply connects to the Holochain conductor. It raises the [OnConnected](#onconnected) event once it is has successfully established a connection. Please see the [Events](#events) section above for more info on how to use this event.

```c#
public async Task Connect()
```
<br>

#### CallZomeFunctionAsync

This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data.

This method raises the [OnZomeFunctionCallBack](#onzomefunctioncallback) event once it has received a response from the Holochain conductor. Please see the [Events](#events) section above for more info on how to use this event.

<br>

##### Overload 1

````c#
public async Task CallZomeFunctionAsync(string id, string instanceId, string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool matchIdToInstanceZomeFuncInCallback = true, bool cachReturnData = false)
````
<br>

| Parameter                           | Description                                                                                    
| ----------------------------------- | ---------------------------------------------------------------------------------------------- |
| id                                  | The unique id you wish to assign for this call (NOTE: There is an overload that omits this     |  |                                     | param, use this overload if you wish HoloNET to auto-generate and manage the id's for you).    | 
| instance                            | The instance running on the holochain conductor you wish to target.                            |
| zome                                | The name of the zome you wish to target.                                                       |
| function                            | The name of the zome function you wish to call.                                                |
| delegate                            | A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event.                                             |
| paramsObject                        | A basic CLR object containing the params the zome function is expecting.                       |
| matchIdToInstanceZomeFuncInCallback | This is an optional param, which defaults to true. Set this to true if you wish HoloNET to give the instance, zome  zome function that made the call in the callback/event. If this is false then only the id will be given in the callback. This uses a small internal cache to match up                  the id to the given instance/zome/function. Set this to false if you wish to save a tiny amount of memory by not utilizing this cache. If it is false then the `Instance`, `Zome` and `ZomeFunction` params will be missing in the ZomeCallBack,you will need to manually match the `id` to the call yourself.                                                  |
| cachReturnData                      | This is an optional param, which defaults to false. Set this to true if you wish HoloNET to    cache the JSON response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.                                                         

<br>

#####  Overload 2

````c#
 public async Task CallZomeFunctionAsync(string instanceId, string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool cachReturnData = false)
 ````

This overload is similar to the one above except it omits the `id` and `matchIdToInstanceZomeFuncInCallback` param's forcing HoloNET to auto-generate and manage the id's itself. 

<br>

##### Overload 3

````c#
public async Task CallZomeFunctionAsync(string id, string instanceId, string zome, string function, object paramsObject, bool matchIdToInstanceZomeFuncInCallback = true, bool cachReturnData = false)
 ````

This overload is similar to the first one, except it is missing the `callback` param. For this overload you would subscribe to the `OnZomeFunctionCallBack` event. You can of course subscribe to this event for the other overloads too, it just means you will then get two callbacks, one for the event handler for `OnZomeFunctionalCallBack` and one for the callback delegate you pass in as a param to this method. The choice is yours on how you wish to use this method...

<br>

##### Overload 4

````c#
public async Task CallZomeFunctionAsync(string instanceId, string zome, string function, object paramsObject, bool cachReturnData = false)
 ````

This overload is similar to the one above except it omits the `id` and `matchIdToInstanceZomeFuncInCallback` param's forcing HoloNET to auto-generate and manage the id's itself. It is also missing the `callback` param. For this overload you would subscribe to the `OnZomeFunctionCallBack` event. You can of course subscribe to this event for the other overloads too, it just means you will then get two callbacks, one for the event handler for `OnZomeFunctionalCallBack` and one for the callback delegate you pass in as a param to this method. The choice is yours on how you wish to use this method...

<br>

#### ClearCache

Call this method to clear all of HoloNETClient's internal cache. This includes the JSON responses that have been cached using the [GetHolochainInstancesAsync](#getholochaininstancesasync) & [CallZomeFunctionAsync](#callzomefunctionasync) methods if the `cacheData` parm was set to true for any of the calls.

````c#
public void ClearCache()
````
<br>

#### Disconnect

This method disconnects the client from Holochain conductor. It raises the [OnDisconnected](#ondisconnected) event once it is has
 successfully disconnected. Please see the [Events](#events) section above for more info on how to use this event.

```c#
public async Task Disconnect()
```
NOTE: Currently when you call this method, you will receive the follow error:

> "The remote party closed the WebSocket connection without completing
> the close handshake."

This looks like an issue with the Holochain conductor and we will be raising this bug with them to see if it is something they need to address...

<br>

#### GetHolochainInstancesAsync

This method will return a string array containing the instances that the holochain conductor is currently running. You will need to store the instance(s) in a variable to pass into the [CallZomeFunctionAsync](#callzomefunctionasync) later. 

We did consider managing this part automatically but because we wanted to keep HoloNET as flexible as possible allowing you to make calls to multiple instances at once it made sense for the user to manage the instance id's themselves. But as with everything we are very open to any feedback or suggestions on this...

This method raises the [OnGetHolochainInstancesCallBack](#ongetinstancescallback) event once it has received a response from the Holochain conductor. Please see the [Events](#events) section above for more info on how to use this event.

There are two overloads for this method:

##### Overload 1

````c#
public async Task GetHolochainInstancesAsync(string id, bool cachReturnData = false)
````

##### Overload 2

````c#
public async Task GetHolochainInstancesAsync(bool cachReturnData = false)
````
<br>

| Parameter| Description  |
|--|--|
|id|The unique id you wish to assign for this call (NOTE: Use the overload that omits this                                       param if you wish HoloNET to auto-generate and manage the id's for you).   |
|cachReturnData | This is an optional param, which defaults to false. Set this to true if you wish HoloNET to    cache the JSON response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains. This would be a good method to enable caching if you know the instances are not going to change.  
<br>

#### SendMessageAsync

This method allows you to send your own raw JSON request to holochain. This method raises the [OnDataRecived](#ondatareceived) event once it has received a response from the Holochain conductor. Please see the [Events](#events) section above for more info on how to use this event.

You would rarely need to use this and we highly recommend you use the [CallZomeFunctionAsync](#callzomefunctionasync) method instead.

````c#
public async Task SendMessageAsync(string jsonMessage)
 ````
<br>

| Paramameter |Description  |
|--|--|
| jsonMessage | The raw JSON message you wish to send to the Holochain conductor.  |
<br>

### Properties

HoloNETClient contains the following properties:

<br>

| Property | Description  |
|--|--|
| [Config](#config)  | This property contains a struct called `HoloNETConfig` containing the sub-properties: TimeOutSeconds, NeverTimeOut, KeepAliveSeconds, ReconnectionAttempts, ReconnectionIntervalSeconds, SendChunkSize, ReceiveChunkSizeDefault, ErrorHandlingBehaviour, FullPathToExternalHolochainConductor, FullPathToHolochainAppDNA, SecondsToWaitForHolochainConductorToStart, AutoStartConductor & AutoShutdownConductor
| [Logger](#logger) | Property to inject in a [ILogger](#ilogger) implementation. |
| [NetworkServiceProvider](#networkserviceprovider) | This is a property where the network service provider can be injected. The provider needs to implement the `IHoloNETClientNET` interface.  |
| [NetworkServiceProviderMode](#networkserviceprovidermode) |This is a simple enum, which currently has these values: Websockets, HTTP & External. |

<br>

#### Config

This property contains a struct called `HoloNETConfig` containing the following sub-properties:
<br>

|Property|Description  |
|--|--|
|TimeOutSeconds  | The time in seconds before the connection times out when calling either method `SendMessage` or `CalLZomeFunction`. This defaults to 30 seconds.|
|NeverTimeOut|Set this to true if you wish the connection to never time out when making a call from methods 'SendMessage' and `CallZomeFunction`. This defaults to false.
|KeepAliveSeconds| This is the time to keep the connection alive in seconds. This defaults to 30 seconds.
|ReconnectionAttempts| The number of times HoloNETClient will attempt to re-connect if the connection is dropped. The default is 5.|
|ReconnectionIntervalSeconds|The time to wait between each re-connection attempt. The default is 5 seconds.|
|SendChunkSize| The size of the buffer to use when sending data to the Holochain conductor. The default is 1024 bytes.
|ReceiveChunkSizeDefault| The size of the buffer to use when receiving data from the Holochain conductor. The default is 1024 bytes. |
| ErrorHandlingBehaviour | An enum that specifies what to do when anm error occurs. The options are: `AlwaysThrowExceptionOnError`, `OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent` & `NeverThrowExceptions`). The default is `OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent` meaning it will only throw an error if the `OnError` event has not been subscribed to. This delegates error handling to the caller. If no event has been subscribed then HoloNETClient will throw an error. `AlwaysThrowExceptionOnError` will always throw an error even if the `OnError` event has been subscribed to. The `NeverThrowException` enum option will never throw an error even if the `OnError` event has not been subscribed to. Regardless of what enum is selected, the error will always be logged using whatever `ILogger` has been injected into the [Logger]("#logger") property. 
| FullPathToExternalHolochainConductor| The full path to the conductor exe (hc.exe) that HoloNET will auto-start.|
| FullPathToHolochainAppDNA | The full path to the hApp (Holochain App) DNA file that is the compiled WASM (compiled byt the conductor build options). |
| SecondsToWaitForHolochainConductorToStart | The seconds to wait for the Holochain Conductor to start before attempting to [connect](#connect) to it.|
| AutoStartConductor | Set this to true if you with HoloNET to auto-start the Holochain Conductor defined in the `FullPathToExternalHolochainConductor` parameter. Default is true. |
| AutoShutdownConductor | Set this to true if you wish HoloNET to auto-shutdown the Holochain Conductor after it [disconnects](#disconnect). Default is true.


 

<br>

#### Logger

Property to inject in a `ILogger` implementation.

`HoloNETClientBase` is an abstract class meaning it cannot be instantiated directly. You must inherit from it to use it.  This is where all the code for the HoloNETClient is.
 
`NextGenSoftware.Holochain.HoloNET.Client.Desktop` and `NextGenSoftware.Holochain.HoloNET.Client.Unity` projects both contain a `HoloNETClient` class that do just this.

They contain very little code. All they do is inject into the `Logger` property the logger implementation they wish to use. The implementation must implement the `ILogger` interface. 

````c#

using NextGenSoftware.Holochain.HoloNET.Client.Core;

namespace NextGenSoftware.Holochain.HoloNET.Client.Desktop
{
    public class HoloNETClient : HoloNETClientBase
    {
        public HoloNETClient(string holochainURI) : base(holochainURI)
        {
            this.Logger = new NLogger();
        }
    }
}

````

````c#

using NextGenSoftware.Holochain.HoloNET.Client.Core;

namespace NextGenSoftware.Holochain.HoloNET.Client.Unity
{
    public class HoloNETClient : HoloNETClientBase
    {
        public HoloNETClient(string holochainURI) : base(holochainURI)
        {
            //TODO: Add Unity Compat Logger Here (hopefully the Unity NLogger Download/Asset I found)
            // this.Logger = new NLogger();
            this.Logger = new DumbyLogger();
        }
    }
}
````

The desktop version uses a wrapper around the popular `NLog` logging framework, but unfortunately Unity does not support NLog so this is why this has had to be split out. We are currently looking into a good Logging Solution for Unity. We have found a possible port of NLog for Unity that so far is looking promising but this is still a different dll/library so the code must still remain as it is. This is also good practice to decouple the code as much as possible especially external dependencies such as logging.

<a name="ilogger"></a>
The ILogger interface is very simple:

````c#
namespace NextGenSoftware.Holochain.HoloNET.Client.Core
{
    public interface ILogger
    {
        void Log(string message, LogType type);
    }

    public enum LogType
    {
        Debug,
        Info,
        Warn,
        Error
    }
}
````

<br>

#### NetworkServiceProvider

This is a property where the network service provider can be injected. The provider needs to implement the `IHoloNETClientNET` interface. 

The interface currently looks like this:

````c#
	public interface IHoloNETClientNET
    {
        //async Task<bool> Connect(Uri EndPoint);
        bool Connect(Uri EndPoint);
        bool Disconnect();
        bool SendData(string Data);
        string ReceiveData();

        NetSocketState NetSocketState { get; set; }
    }
````

**NOTE: This is currently not used and is future work to be done...**

The two currently planned providers will be WebSockets & HTTP but if for whatever reason Holochain decide they need to use another protocol then a new one can easily be implemented without having to refactor any existing code.

Currently the WebSocket JSON RPC implementation is deeply integrated into the HoloNETClient so this needs splitting out into its own project. We hope to get this done soon... We can then also at the same time implement the HTTP implementation. 

<br>

#### NetworkServiceProviderMode

This is a simple enum, which currently has these values:

````c#
public enum NetworkServiceProviderMode
    {
        WebSockets,
        HTTP,
        External
    }
````

The plan was to have WebSockets and HTTP built into the current implementation (but will still be injected in from a separate project). If there is a need a cut-down lite version of HoloNETClient can easily be implemented with just one of them injected in.

The External enum was to be used by any other external implementation that implements the `IHoloNETClientNET` and would be for future use if Holochain decide they wish to use another protocol.

**More to come soon...**

<br>

## HoloOASIS

`HoloOASIS` uses the [HoloNETClient](#holonet) to implement a Storage Provider ([IOASISStorage](#ioasisstorage)) for the OASIS System. It will soon also implement a Network Provider ([IOASISNET](#ioasisnet))
 for the OASIS System that will leverage Holochain to create it's own private de-centralised distributed network called `ONET` (as seen on the [OASIS Architecture Diagram](#the-oasis-architecture) below).

This is a good example to see how to use [HoloNETClient](#holonet) in a real world game/platform (OASIS/Our World).

### Using HoloOASIS

You start by instantiating a new HoloOASIS class from either the [NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop](#project-structure) project or the [NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity](#project-structure) project.

````c#
Desktop.HoloOASIS _holoOASIS = new Desktop.HoloOASIS("ws://localhost:8888");
````

You pass into the constructor the URI to the Holochain conductor.

Next, wire up the events:

````c#
_holoOASIS.HoloNETClient.OnConnected += HoloNETClient_OnConnected;
_holoOASIS.OnInitialized += _holoOASIS_OnInitialized;
_holoOASIS.OnPlayerProfileLoaded += _holoOASIS_OnPlayerProfileLoaded;
_holoOASIS.OnPlayerProfileSaved += _holoOASIS_OnPlayerProfileSaved;
_holoOASIS.OnHoloOASISError += _holoOASIS_OnHoloOASISError;
````
<br>
Once HoloOASIS has finished initializing after the `OnInitialzed` event has fired you can create a new user Profile by creating a new Profile object and populating it with the required properties.

You can also add karma by calling the `AddKarma` method on the `Profile` object.

Finally you call the `SaveProfileAsync` method passing in the `Profile` object to save the profile to your local chain on Holochain.

````c#
private static void _holoOASIS_OnInitialized(object sender, EventArgs e)
{
            Console.WriteLine("Initialized.");
            Console.WriteLine("Saving Profile...");

            _savedProfile = new Profile { Username = "dellams", Email = "david@nextgensoftware.co.uk", Password = "1234", FirstName = "David", LastName = "Ellams", DOB = "11/04/1980", Id = Guid.NewGuid(), Title = "Mr", PlayerAddress = "blahahahaha" };
            _savedProfile.AddKarma(999);

            _holoOASIS.SaveProfileAsync(_savedProfile);
}
````
<br>
To load the `Profile` object back from your local chain on Holochain you simply call the `LoadProfileAsync` method passing in the desired profiles Holochain hash, which is returned as a param in the `OnPlayerProfileSaved` event handler.

````c#
private static void _holoOASIS_OnPlayerProfileSaved(object sender, ProfileSavedEventArgs e)
{
            Console.WriteLine("Profile Saved.");
            Console.WriteLine("Profile Entry Hash: " + e.Profile.HcAddressHash);
            Console.WriteLine("Loading Profile...");
            //_savedProfile.Id = new Guid(e.ProfileEntryHash);
            _holoOASIS.LoadProfileAsync(e.Profile.HcAddressHash);
}
````

### Events

HoloOASIS contains the following events:

|Event|Description |
|--|--|
| OnInitialized |Fired when the HoloOASIS Provider has initialized. This is after the embedded [HoloNETClient](#holonet) has finished connecting to the Holochain Conductor.  |
| OnPlayerProfileSaved|Fired when the users profile has finished saving. |
| OnPlayerProfileLoaded|Fired when the users profile has finished loading. |
| OnHoloOASISError|Fired when an error occurs within the provider. 
| OnStorageProviderError|This implements part of the [IOASISStorage](#ioasisstorage) interface. This is a way for the OASIS Providers to bubble up any errors to the ProfileManager contained in the [NextGenSoftware.OASIS.API.Core](#oasisapi) |

#### OnInitialized 

Fired when the HoloOASIS Provider has initialized. This is after the embedded [HoloNETClient](#holonet) has finished connecting to the Holochain Conductor.

````c#
_holoOASIS.OnInitialized += _holoOASIS_OnInitialized;

private static void _holoOASIS_OnInitialized(object sender, EventArgs e)
{
            Console.WriteLine("Initialized.");
            Console.WriteLine("Saving Profile...");

            _savedProfile = new Profile { Username = "dellams", Email = "david@nextgensoftware.co.uk", Password = "1234", FirstName = "David", LastName = "Ellams", DOB = "11/04/1980", Id = Guid.NewGuid(), Title = "Mr", PlayerAddress = "blahahahaha" };
            _savedProfile.AddKarma(999);

            _holoOASIS.SaveProfileAsync(_savedProfile);
}
````
<br>

#### OnPlayerProfileSaved

Fired when the users profile has finished saving.

````c#
_holoOASIS.OnPlayerProfileSaved += _holoOASIS_OnPlayerProfileSaved;

private static void _holoOASIS_OnPlayerProfileSaved(object sender, ProfileSavedEventArgs e)
{
            Console.WriteLine("Profile Saved.");
            Console.WriteLine("Profile Entry Hash: " + e.Profile.HcAddressHash);
            Console.WriteLine("Loading Profile...");
            //_savedProfile.Id = new Guid(e.ProfileEntryHash);
            _holoOASIS.LoadProfileAsync(e.Profile.HcAddressHash);
}
````
<br>

|Parameter|Description |
|--|--|
|Profile  | The profile object that has just been saved.  |
<br>

#### OnPlayerProfileLoaded
Fired when the users profile has finished loading.

````c#
 _holoOASIS.OnPlayerProfileLoaded += _holoOASIS_OnPlayerProfileLoaded;

 private static void _holoOASIS_OnPlayerProfileLoaded(object sender, ProfileLoadedEventArgs e)
        {
            Console.WriteLine("Profile Loaded.");
            Console.WriteLine(string.Concat("Id: ", e.Profile.Id));
            Console.WriteLine(string.Concat("HC Address Hash: ", e.Profile.HcAddressHash));
            Console.WriteLine(string.Concat("Name: ", e.Profile.Title, " ", e.Profile.FirstName, " ", e.Profile.LastName));
            Console.WriteLine(string.Concat("Username: ", e.Profile.Username));
            Console.WriteLine(string.Concat("Password: ", e.Profile.Password));
            Console.WriteLine(string.Concat("Email: ", e.Profile.Email));
            Console.WriteLine(string.Concat("DOB: ", e.Profile.DOB));
            Console.WriteLine(string.Concat("Address: ", e.Profile.PlayerAddress));
            Console.WriteLine(string.Concat("Karma: ", e.Profile.Karma));
            Console.WriteLine(string.Concat("Level: ", e.Profile.Level));
        }
````
<br>

#### OnHoloOASISError

Fired when an error occurs within the provider.

````c#
 _holoOASIS.OnHoloOASISError += _holoOASIS_OnHoloOASISError;

private static void _holoOASIS_OnHoloOASISError(object sender, HoloOASISErrorEventArgs e)
        {
            Console.WriteLine(string.Concat("Error Occured. Reason: ", e.Reason, (e.HoloNETErrorDetails != null ? string.Concat(", HoloNET Reason: ", e.HoloNETErrorDetails.Reason) : ""), (e.HoloNETErrorDetails != null ? string.Concat(", HoloNET Details: ", e.HoloNETErrorDetails.ErrorDetails.ToString()) : ""), "\n"));
        }
````
<br>

|Parameter|Description  |
|--|--|
| EndPoint | The URI EndPoint of the Holochain conductor.
| Reason | The reason for the error.  |
| ErrorDetails| More detailed technical details including stack trace.
| HoloNETErrorDetails| If the error was caused by HoloNET, then the error details returned from HoloNET will appear here.


### Methods

HoloOASIS contains the following methods:

| Method |Description  |
|--|--|
| AddKarmaToProfileAsync |This implements part of the [IOASISStorage](#ioasisstorage) interface. Call this method to add karma to the users profile/avatar.  |
|ConvertProfileToHoloOASISProfile | Internal utility method that converts a `OASIS.API.Core.Profile` object to a `HoloOASIS.Profile` object. The `HoloOASIS.Profile` object extends the `OASIS.API.Core.Profile` object by adding the `HcAddressHash` property to store the address hash returned from Holochain when adding new entries to the chain.
| GetHolonsNearMe | This implements part of the IOASISNET interface. This has not been implemented yet and is just a stub. This method will get a list of the Holons (items/objects) near the user/avatar.
|GetPlayersNearMe|This implements part of the IOASISNET interface. This has not been implemented yet and is just a stub. This method will get a list of the players/avatars near the player's user/avatar.
|HandleError| This is a private method where all errors are funnelled and handled.
|Initialize| Call this method to initilize the provider. Internally this will call the [Connect](#connect) method on the [HoloNETClient](#holonet) class.
|LoadProfileAsync| Call this method to load the users profile/avatar data and return it in a `Profile` object. This has 3 overloads.
|RemoveKarmaFromProfileAsync| Call this method to remove karma from the users profile/avatar.
| SaveProfileAsync | Call this method to save the user's profile/avatar.

### Properties

HoloOASIS contains the following properties:

|Property|Description  |
|--|--|
| HoloNETClient | This contains a ref to the [HoloNETClient](#holonet). You can use this property to access the underlying [HoloNETClient](#holonet) including all [events](#events), [methods](#methods) & [properties](#properties). |


**More to come soon...**

## OASIS API Core

This is where the main OASIS API is located and contains all of the interfaces that the various providers implement along with the base objects & managers to power the OASIS API.

It is located in the `NextGenSoftware.OASIS.API.Core` project.

#### Using The OASIS API Core

The API is still being developed so at the time of writing,  only the`ProfileManager` is available.

You start by instantiating the `ProfileManager` class:

````c#
// Inject in the HoloOASIS Storage Provider (this could be moved to a config file later so the 
// providers can be sweapped without having to re-compile.
ProfileManager = new ProfileManager(new HoloOASIS("ws://localhost:8888"));
````

The `ProfileManager` takes one param for the constructor of type [IOASISStorage](#ioasisstorage). This is where you inject in a Provider that implements the [IOASISStorage](#ioasisstorage) interface. Currently the only provider that has implemented this is the [HoloOASIS](#holooasis) provider. but expect more to follow soon...

````c#
public ProfileManager(IOASISStorage
 OASISStorageProvider)
{
            this.OASISStorageProvider = OASISStorageProvider;
            this.OASISStorageProvider.OnStorageProviderError += OASISStorageProvider_OnStorageProviderError;
}
````

Part of the [IOASISStorage](#ioasisstorage) interface has an event called OnStorageProviderError, which the provider fires to send errors back to the `ProfileManager`.

Once the `ProfileManager` has been instantiated. you can load the users Profile using the `LoadProfileAsync` method:

````c#
IProfile profile = await ProfileManager.LoadProfileAsync(username, password);

if (profile != null)
{
	//TODO: Bind profile info to Unity Avatar UI here.
}
````

### Interfaces

The OASIS API currently has the following interfaces defined:

|Interface|Description  |
|--|--|
|[IOASISStorage](#ioasisstorage)  | This is what a Storage Provider implements so the OASIS API can read & write the users profile/avatar to the storage medium/network. Currently only the HoloOASIS provider exists but more will follow soon...the first will be EthereumOASIS & SOLIDOASIS so the API can talk to both Ethereum & SOLID.  |
|[IOASISNET](#ioasisnet)| This is what a Network Provider implements so the OASIS API can share the users profile/avatar as well as fine Holons and players near them. 

**NOTE: Currently the interfaces are pretty basic, but expect a LOT more to be added in the future...  Additional interfaces will also be added such as the IOASISRenderer interface.**

#### IOASISStorage  

This is what a Storage Provider implements so the OASIS API can read & write the users profile/avatar to the storage medium/network. Currently only the [HoloOASIS](#holooasis) provider exists but more will follow soon...the first will be EthereumOASIS & SOLIDOASIS so the API can talk to both Ethereum & SOLID.

````c#
namespace NextGenSoftware.OASIS.API.Core
{
    // This interface is responsible for persisting data/state to storage, this could be a local DB or other local 
    // storage or through a distributed/decentralised provider such as IPFS or Holochain (these two implementations 
    // will be implemented soon (IPFSOASIS & HoloOASIS).
    public interface IOASISStorage
    {
        Task<IProfile> LoadProfileAsync(string providerKey);
        Task<IProfile> LoadProfileAsync(Guid Id);
        Task<IProfile> LoadProfileAsync(string username, string password);

        //Task<bool> SaveProfileAsync(IProfile profile);
        Task<IProfile> SaveProfileAsync(IProfile profile);
        Task<bool> AddKarmaToProfileAsync(IProfile profile, int karma);
        Task<bool> RemoveKarmaFromProfileAsync(IProfile profile, int karma);

        event StorageProviderError OnStorageProviderError;

        //TODO: Lots more to come! ;-)
    }
}
````
<br>

| Item|Description  |
|--|--|
| LoadProfileAsync | Loads the users profile/avatar. This has 3 overloads, one takes a providerKey (a unique key that the provider can use to identify a profile, [HoloOASIS](#holooasis) uses the address hash for this), one takes a username & password and the final one takes a Guid for the profileID, which is a unique id for the profile irrespective of which provider is providing it.  |
| SaveProfileAsync | Saves the users profile/avatar. 
| AddKarmaToProfileAsync | Add karma to the users profile/avatar.
| RemoveKarmaFromProfileAsync | Remove karma from the users profile/avatar.
| StorageProviderError | An event the provider fires when an error occurs that the `ProfileManager` can then handle.

#### IOASISNET

This is what a Network Provider implements so the OASIS API can share the users profile/avatar as well as find Holon's and players near them. 

````c#
namespace NextGenSoftware.OASIS.API.Core
{
    // This interface provides methods to discover and interact with other nodes/peers on the distributed/decentralised network (ONET)
    // This will involve peer to peer communication.
    public interface IOASISNET
    {
        List<IPlayer> GetPlayersNearMe();
        List<IHolon> GetHolonsNearMe(HolonType Type);
    }
}
````

Currently the [HoloOASIS](#holooasis) Provider defines some stubs for this interface, which will be fully implemented soon...

| Item | Description  |
|--|--|
| GetPlayersNearMe  | Gets a list of players near the user's current location.  |
| GetHolonsNearMe | Get a list of holon's near the user's current location.


### Events

### Methods

### Properties

**More to come soon...**


## HoloUnity

We will soon be creating a Asset for the Unity Asset Store that will include [HoloNET](#holonet) along with Unity wrappers and examples of how to use [HoloNET](#holonet) inside Unity.

In the codebase you will find a project called [NextGenSoftware.OASIS.API.FrontEnd.Unity](#project-structure), which shows how the `ProfileManager` found inside the `OASIS API Core` ([NextGenSoftware.OASIS.API.Core](#project-structure)) is used. When you instantiate the `ProfileManager` you inject into a Storage Provider that implements the [IOASISStorage](#ioasisstorage) interface. Currently the only provider implemented is the [HoloOASIS](#holooasis) Provider.

The actual Our World Unity code is not currently stored in this repo due to size restrictions but we may consider using GitHub LFS (Large File Storage) later on. We are also looking at GitLab and other alternatives to see if they allow greater storage capabilities free out of the box (since we are currently working on a very tight budget but you could change that by donating below! ;-) ).

![alt text](https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/blob/master/Images/HolochainTalkingToUnity.jpg "Holochain talking to Unity")

Here is a preview of the OASIS API/Avatar/Karma System... more to come soon... ;-)

**As with the rest of the project, if you have any suggestions we would love to hear from you! :)**

### Using HoloUnity

You start by instantiating the `ProfileManager` class found within the [NextGenSoftware.OASIS.API.Core](#project-structure) project.

````c#
// Inject in the HoloOASIS Storage Provider (this could be moved to a config file later so the 
// providers can be sweapped without having to re-compile.
ProfileManager = new ProfileManager(new HoloOASIS("ws://localhost:8888"));
````

Now, load the users Profile:

````c#
IProfile profile = await ProfileManager.LoadProfileAsync(username, password);

if (profile != null)
{
	//TODO: Bind profile info to Unity Avatar UI here.
}
````

The full code for the screenshot above that loads the users profile/avatar data from holochain and displays it in Unity is below:

````c#
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity;
using UnityEngine;
using TMPro;
using System.Threading.Tasks;

public class OASISAvatarManager : MonoBehaviour
{
    ProfileManager ProfileManager { get; set; }  //If the ProfileManager is going to contain additional business logic not contained in the providers then use this.
    public GameObject ProfileUsername;
    public GameObject ProfileFullName;
    public GameObject ProfileDOB;
    public GameObject ProfileEmail;
    public GameObject ProfileAddress;
    public GameObject ProfileKarma;
    public GameObject ProfileLevel;

    async Task Start()
    {
    	// Inject in the HoloOASIS Storage Provider (this could be moved to a config file later so the 
        // providers can be sweapped without having to re-compile.
        ProfileManager = new ProfileManager(new HoloOASIS("ws://localhost:8888"));
        ProfileManager.OnProfileManagerError += ProfileManager_OnProfileManagerError;
        ProfileManager.OASISStorageProvider.OnStorageProviderError += OASISStorageProvider_OnStorageProviderError;

        //StorageProvider = new HoloOASIS("ws://localhost:8888");
	
        await LoadProfile();    
    }

    private async Task LoadProfile()
    {
        //IProfile profile = await ProfileManager.LoadProfileAsync("dellams", "1234");
        IProfile profile = await ProfileManager.LoadProfileAsync("QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg");

        if (profile != null)
        {
            (ProfileFullName.GetComponent<TextMeshProUGUI>()).text = string.Concat(profile.Title, " ", profile.FirstName, " ", profile.LastName);
            (ProfileUsername.GetComponent<TextMeshProUGUI>()).text = profile.Username;
            (ProfileDOB.GetComponent<TextMeshProUGUI>()).text = profile.DOB;
            (ProfileEmail.GetComponent<TextMeshProUGUI>()).text = profile.Email;
            //(ProfileAddress.GetComponent<TextMeshProUGUI>()).text = profile.PlayerAddress;
            (ProfileKarma.GetComponent<TextMeshProUGUI>()).text = profile.Karma.ToString();
            (ProfileLevel.GetComponent<TextMeshProUGUI>()).text = profile.Level.ToString();
        }
    }

    private void OASISStorageProvider_OnStorageProviderError(object sender, ProfileManagerErrorEventArgs e)
    {
        Debug.Log("Error occured in the OASIS Storage Provider: " + e.Reason + ", Error Details: " + e.ErrorDetails);
    }

    private void ProfileManager_OnProfileManagerError(object sender, ProfileManagerErrorEventArgs e)
    {
        Debug.Log("Error occured in the OASIS Profile Manager: " + e.Reason + ", Error Details: " + e.ErrorDetails);
    }

    // Update is called once per frame
    void Update ()
    {
		
    }
}

````

Instead of using the OASIS `ProfileManager` to load the data, we could use [HoloNETClient](#holonet) directly, the code would then look like this:

````c#
using NextGenSoftware.OASIS.API.Core;
//using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity;
using NextGenSoftware.Holochain.HoloNET.Client.Unity;
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using UnityEngine;
using TMPro;
using Newtonsoft.Json;
using System.Threading.Tasks;

public class OASISAvatarManager : MonoBehaviour
{
    ProfileManager ProfileManager { get; set; }  //If the ProfileManager is going to contain additional
 business logic not contained in the providers then use this.
    public GameObject ProfileUsername;
    public GameObject ProfileFullName;
    public GameObject ProfileDOB;
    public GameObject ProfileEmail;
    public GameObject ProfileAddress;
    public GameObject ProfileKarma;
    public GameObject ProfileLevel;

    async Task Start()
    {
    	/*
    	// Inject in the HoloOASIS Storage Provider (this could be moved to a config file later so the 
        // providers can be sweapped without having to re-compile.
        ProfileManager = new ProfileManager(new HoloOASIS("ws://localhost:8888"));
        ProfileManager.OnProfileManagerError += ProfileManager_OnProfileManagerError;
        ProfileManager.OASISStorageProvider.OnStorageProviderError += OASISStorageProvider_OnStorageProviderError;
	
        await LoadProfile();
	*/
	
        HoloNETClient holoNETClient = new HoloNETClient("ws://localhost:8888");
        holoNETClient.OnZomeFunctionCallBack += HoloNETClient_OnZomeFunctionCallBack;
        holoNETClient.OnConnected += HoloNETClient_OnConnected;
        holoNETClient.OnError += HoloNETClient_OnError;
        holoNETClient.OnDataReceived += HoloNETClient_OnDataReceived;

        await holoNETClient.Connect();
        await holoNETClient.CallZomeFunctionAsync("test-instance", "our_world_core", "load_profile", new { address = "QmVtt5dEZEyTUioyh59XfFc3KWuaifK92Mc2KTXGauSbS9" });
    }

    private void HoloNETClient_OnDataReceived(object sender, DataReceivedEventArgs e)
    {
        Debug.Log(string.Concat("Data Received: EndPoint: ", e.EndPoint, "RawJSONData: ", e.RawJSONData));
    }

    private void HoloNETClient_OnError(object sender, HoloNETErrorEventArgs e)
    {
        Debug.Log(string.Concat("Error Occured. Resason: ", e.Reason, ", EndPoint: ", e.EndPoint, ", Details: ", e.ErrorDetails.ToString()));
    }

    private void HoloNETClient_OnConnected(object sender, ConnectedEventArgs e)
    {
        Debug.Log("Connected to Holochain Conductor: " + e.EndPoint);
    }

    private void HoloNETClient_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
    {
        Debug.Log(string.Concat("ZomeFunction CallBack: EndPoint: ", e.EndPoint, ", Id: ", e.Id, ", Instance: ", e.Instance, ", Zome: ", e.Zome, ", ZomeFunction: ", e.ZomeFunction, ", Data: ", e.ZomeReturnData, ", Raw Zome Return Data: ", e.RawZomeReturnData, ", Raw JSON Data: ", e.RawJSONData, ", IsCallSuccessful: ", e.IsCallSuccessful ? "true" : "false"));

        Profile profile = JsonConvert.DeserializeObject<Profile>(string.Concat("{", e.ZomeReturnData, "}"));

        if (profile != null)
        {
            (ProfileFullName.GetComponent<TextMeshProUGUI>()).text = string.Concat(profile.Title, " ", profile.FirstName, " ", profile.LastName);
            (ProfileUsername.GetComponent<TextMeshProUGUI>()).text = profile.Username;
            (ProfileDOB.GetComponent<TextMeshProUGUI>()).text = profile.DOB;
            (ProfileEmail.GetComponent<TextMeshProUGUI>()).text = profile.Email;
            //(ProfileAddress.GetComponent<TextMeshProUGUI>()).text = profile.PlayerAddress;
            (ProfileKarma.GetComponent<TextMeshProUGUI>()).text = profile.Karma.ToString();
            (ProfileLevel.GetComponent<TextMeshProUGUI>()).text = profile.Level.ToString();
        }
    }

    /*
    private async Task LoadProfile()
    {
        //StorageProvider = new HoloOASIS("ws://localhost:8888");

        //IProfile profile = await ProfileManager.LoadProfileAsync("dellams", "1234");
        IProfile profile = await ProfileManager.LoadProfileAsync("QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg");

        if (profile != null)
        {
            (ProfileFullName.GetComponent<TextMeshProUGUI>()).text = string.Concat(profile.Title, " ", profile.FirstName, " ", profile.LastName);
            (ProfileUsername.GetComponent<TextMeshProUGUI>()).text = profile.Username;
            (ProfileDOB.GetComponent<TextMeshProUGUI>()).text = profile.DOB;
            (ProfileEmail.GetComponent<TextMeshProUGUI>()).text = profile.Email;
            //(ProfileAddress.GetComponent<TextMeshProUGUI>()).text = profile.PlayerAddress;
            (ProfileKarma.GetComponent<TextMeshProUGUI>()).text = profile.Karma.ToString();
            (ProfileLevel.GetComponent<TextMeshProUGUI>()).text = profile.Level.ToString();
        }
    }

    private void OASISStorageProvider_OnStorageProviderError(object sender, ProfileManagerErrorEventArgs e)
    {
        Debug.Log("Error occured in the OASIS Storage Provider: " + e.Reason + ", Error Details: " + e.ErrorDetails);
    }

    private void ProfileManager_OnProfileManagerError(object sender, ProfileManagerErrorEventArgs e)
    {
        Debug.Log("Error occured in the OASIS Profile Manager: " + e.Reason + ", Error Details: " + e.ErrorDetails);
    }
    */

    // Update is called once per frame
    void Update ()
    {
		
    }
}
````

This is how other Unity developers would connect to Holochain using HoloNETClient, because they may not be using the OASIS API. 

Of course if they wanted use the OASIS API then the first code listing is how it would be done.


### Events

### Methods

### Properties

**More to come soon...**

## Road Map

**Version 1 - Smartphone Platform - The AR version** - Map of present day - In correlation with Time - **IN ACTIVE DEVELOPMENT**. We hope to have an early prototype of this around 2020 Q1/Q2 with more evolved prototypes being released throughout the year. Depending on how many resources/devs we can attract we hope to have a first altha release by 2021, possibly 2022.

**Version 2 - Desktop/Console Platforms - The VR Version** - Game version that starts in Past with a true history of Earth. Not Time Correlated. We hope work on this can begin by 2020 (if additional funds/resources can be secured by then) and will be done
 in parallel with the Smartphone version. Remember these are not seperate products, and fully integrate with each other where players share the same immersive persistent real-time open world.

**Version 3 - The XR/IR Version (The OASIS)** - The XR version that becomes the immersive, self reflective reality that combines both aspects of console and smartphone versions. We hope we will secure MASSIVE funds by 2021/2022 latest so this can begin dev around that time, this is Ready Player One OASIS time with life like graphics and things you can only begin to imagine right now! ;-)

## Next Steps

Not in priority order:

* Add HTTP support to HoloNET.
* Implement IOASISNET interface for HoloOASIS Provider.
* Add built-in HC Conductor to HoloNET so it can fire up it's own conductor without needing to do this manually.
* Add a ZomeProxyGenerator tool so it can auto-generate a C# Zome Proxy that wraps around HoloNET calls (the code would be similar to what is in HoloOASIS)
* Continue with Unity integration and development of HoloUnity, which will then also be gifted forward to the wonderful holochain community... :)
* Refactor HoloNET to split out the websocket JSON RPC 2.0 implementation from the holochain specific logic so the websocket JSON RPC code can be re-used with the OASIS API websocket implementation coming soon...
* Implement OASIS API Websocket JSON RPC 2.0 implementation.
* Implement OASIS API Websocket HTTP implementation.
* Implement OASIS API HTTP Restful WebAPI implementation.
* Finish implementing avatar screen in Unity.
* Place avatar on 3D map using users current location (geolocation) from their device GPS.
* Implement Mapping/Routing API for 3D Map in Unity.
* Implement Places Of Interest (Holons) on 3D Map.
* Implement ARC Membrane API.
* Implement Unity Nlogger.
* Implement animated cars, planes, water, etc on 3D Map.
* Fix bug so when zooming out on 3D Map it shows the full globe instead of going white.
* Implement demo satellite apps/games/websites to show how OASIS API works.
* Implement OASIS API in a number of real apps/games/websites that are waiting and ready...
* Implement Quests on 3D Map (geolocation).
* Implement AR Mode in parks, etc.
* Implement Synergy Engine matching solution providers to requesters.
* Port Noomap to Unity.
* Plus LOTS & LOTS more to come! ;-)

<a name="donations-welcome----"></a>
## Donations Welcome! :)

We are working full-time on this project so we have no other income so if you value it, we would really appreciate a donation to our crowd funding page below:

https://www.gofundme.com/f/ourworldthegame

**Every little helps, even if you can only manage £1 it can still help make all the difference! Thank you! :)**

**We would really appreciate if you could donate anything you can afford, even if it's just a pound, if everyone did that then we would be able to massively accelerate this very urgent and important project for a world in need right now. I think everyone can justify a pound if it meant saving the world don't you think?**

**It's even better to spend a pound on this project rather than buying a lottery ticket since you have very little chance of winning the jackpot, then even by some fluke you did win, there is no point having millions if there is no world left to enjoy it on.**

**It is time people start to priortise the future of our planet above all else...**

**Think about it...**

If you can't afford to contribute, then that's fine, you can still help by getting the word out there!

Our Facebook page is here:

https://www.facebook.com/ourworldthegame 

Please make sure you LIKE it and spread the word and get as many of your friends and family to LIKE it too, many thanks & much appreciated! :)

Every reward above £100 will automatically get your name added to the credits for the app/network which will be seen by billions...

Please ready more on the website:
http://www.ourworldthegame.com

* **What will be your legacy?**

* **Do you want to be in on the ground floor of the upcoming platform that will take the world by storm?
The platform that is going to win many rewards for the ground-breaking work it will do. Do you want to be a hero of your own life story?**

* **Want to tell your kids and grandkids that you helped make it happen and go down in history as a hero?**

* **What kind of world do you want to leave to the next generation?**

* **Want to be part of something greater than yourself?**

* **How can you do your part to create a better world?**

* **This is HOW you do your part...**

* **Be the change you wish to see in the world...**

**NOTE: WE HAVE ONLY DISCLOSED ABOUT 10% OF WHAT OUR WORLD / THE OASIS IS, IF YOU WISH TO GET INVOLVED OR INVEST THEN WE WILL BE HAPPY TO SHARE MORE, PLEASE GET IN TOUCH, WE LOOK FORWARD TO HEARING FROM YOU...**

**TOGETHER WE CAN CREATE A BETTER WORLD.**

<a name="devs-contributions-welcome----"></a>
## Devs/Contributions Welcome! :)

We would love to have some much needed dev resource on this vital project not only for Holochain but also for the world so if you are interested please contact us on either ourworld@nextgensoftware.co.uk or david@nextgensoftware.co.uk. Thank you, we look forward to hearing from you! :)

For more details on what we are looking for, please check out this doc:
https://drive.google.com/file/d/1b_G08UTALUg4H3jPlBdElZAFvyRcVKj1/view 

## Other Ways To Get Involved

If you cannot code or donate, then no problem, you can help in other ways! :) You can share our website/posts, give us valuable feedback on our site, etc as well as submit ideas for Our World. We are also looking for people to join for every department/area such as PR, Sales, Support, Admin, Accounting, Management, Strategy, Operations, etc  

So if you feel you want to help or get involved please contact us on ourworld@nextgensoftware.co.uk, we would love to hear from you! :)

You can also get involved on our forum here:

http://www.ourworldthegame.com/forum


## HoloSource Licence

This repo uses a new type of Open Source where more control is needed over the codebase to make sure things do not go off on a tangent that is not beneficial to the original intention and vision of this very important critical project to help save the world and make it a better place for all. Too much is at stake to stop this falling into the wrong hands so to speak! ;-)

This means permission will need to be requested for any forks, etc 

The whole point of opening up this codebase to the public is we wish to empower the whole world to take responsibility for our beautiful planet and this is why the whole world is the
 Our World team. It will be one of the biggest most ambitious projects the world has ever seen and this is why it needs to be open to all...

<a href="https://docs.google.com/document/d/1I3qbBnfVPLrGxv5paDAaCFSBxkb_34vkhA6fc8iNMew/edit#heading=h.s2ub9y5otad7">Read More Here</a>

**Ready to do your part? ;-)**

**Although the rest of this repo is HoloSourced in that you need to be granted permission to fork and use it, HoloNET is totally free and Open Sourced to be used anyway you wish. If you find it helpful, we would REALLY appreciate a donation to our crowd funding page, because this is our full-time job and so have no other income and will help keep us alive so we can continue to improve it for you all, thank you! :)**

**https://www.gofundme.com/ourworldthegame**


## Links

Read more on this project on our websites below:

In Love,Light & Hope,

David Ellams BSc(Hons)<br/> 
https://www.linkedin.com/in/david-ellams-77132142/ 

Proud & Liberated Aspie <br/>
Founder & Managing Director <br/>
NextGen Software Ltd <br/>
NextGen World Ltd <br/>
Yoga4Autism Ltd 
<br/> 
<br/> 

**Our World Smartphone AR Prototype**

https://github.com/NextGenSoftwareUK/Our-World-Smartphone-Prototype-AR

**Sites**

http://www.ourworldthegame.com <br/>
http://www.nextgensoftware.co.uk <br/>
http://www.yoga4autism.com <br/> 
https://www.thejusticeleagueaccademy.icu <br/> 

**Social**

|Type  |Link  |
|--|--|
|Facebook| http://www.facebook.com/ourworldthegame  |
|Twitter | http://www.twitter.com/ourworldthegame |
|YouTube| https://www.youtube.com/channel/UC0_O4RwdY3lq1m3-K-njUxA | 
|Discord| https://discord.gg/q9gMKU6 |
|Hylo| https://www.hylo.com/c/ourworld |
|Telegram| https://t.me/ourworldthegamechat (General Chat) |
|| https://t.me/ourworldthegame (Announcements) |
|| https://t.me/ourworldtechupdate (Tech Updates) |
|| https://t.me/oasisapihackalong (OASIS API Weekly Hackalongs) |

**Blog/Forum**

<a href="http://www.ourworldthegame.com/blog">Blog</a><br>
<a href="http://www.ourworldthegame.com/forum">Forum</a><br>

**Misc**

<a href="https://drive.google.com/file/d/1nnhGpXcprr6kota1Y85HDDKsBfJHN6sn/view?usp=sharing">The POWER Of The OASIS API</a><br>
<a href="https://drive.google.com/file/d/1QPgnb39fsoXqcQx_YejdIhhoPbmSuTnF/view?usp=sharing">Dev Plan/Roadmap</a><br>
<a href="https://drive.google.com/file/d/1b_G08UTALUg4H3jPlBdElZAFvyRcVKj1/view">Join The Our World Tribe (Dev Requirements)</a><br>
<a href="https://drive.google.com/file/d/12pCk20iLw_uA1yIfojcP6WwvyOT4WRiO/view?usp=sharing">Mission/Summary</a><br>
<a href="https://drive.google.com/file/d/1G8jJ2aMFU9lObddgHJVwcOKRZlpz12xJ/view?usp=sharing">OASIS API & SEEDS API Integration Proposal</a><br>
<a href="https://drive.google.com/file/d/1tFSK54mHxuUP1Z1Zc7p3ZxK5gQpoUjKW/view?usp=sharing">Our World & Game Of SEEDS Proposal</a><br>
<a href="https://drive.google.com/file/d/1_UFi37UvDPaqW6g8WGJ7SyBPpbSXLfUV/view?usp=sharing">SEEDS Camppaign Proposal</a><br>
<a href="https://forum.holochain.org/c/projects/our-world">Holochain Forum</a>

**NextGen Developer Training  Programmes**

<a href="https://docs.wixstatic.com/ugd/4280d8_ad8787bd42b1471bae73003bfbf111f7.pdf">NextGen Developer Training Programme</a> <br/> 
<a href="https://docs.wixstatic.com/ugd/4280d8_999d98ba615e4fa6ab4383a415ee24c5.pdf">Junior NextGen Developer Training Programme</a>

**Business Plan**

<a href="https://docs.wixstatic.com/ugd/4280d8_8b62b661334c43af8e4476d1a1b2afcb.pdf">Executive Summary</a><br>
<a href="https://docs.wixstatic.com/ugd/4280d8_9f8ed61eaf904905a6f94fcebf8650ef.pdf">Business Plan Summary</a><br>
<a href="https://docs.wixstatic.com/ugd/4280d8_cb55d40e7e1b457c879383561e051fff.pdf">Business Plan Detailed</a><br>
<a href="https://docs.wixstatic.com/ugd/4280d8_698b48f342804534ac73829628799d33.xlsx?dn=NextGen%20Software%20Financials.xlsx">Financials</a><br>
<a href="https://d4de5c45-0ca1-451c-86a7-ce397b9225cd.filesusr.com/ugd/4280d8_50d17252aa3247eaae80013d0e0bf70d.pptx?dn=NextGen%20Software%20PitchDeck%20Lite.pptx">Pitch Deck</a><br>

**Funding**

**https://www.gofundme.com/ourworldthegame** <br/> 
**https://www.patreon.com/davidellams**

**Key Videos**
<a href="https://www.youtube.com/watch?v=wdYa5wQUfrg">Our World Introduction</a>>br>
<a href="https://www.youtube.com/watch?v=DB75ldfPzlg&t=7s">OASIS API DEMO With David Ellams (James Halliday) NO SONG</a><br>
<a href="https://www.youtube.com/watch?v=2oY4_LZBW4M">Latest prototype for the Our World Smartphone version... :)</a><br>
<a href="https://www.youtube.com/watch?v=SB97mvzJiRg&t=1s">Founders Introduction To Our World May 2017 (Remastered Nov 2017)</a><br>
<a href="https://www.youtube.com/watch?v=rvNJ6poMduo">Our World Smartphone Prototype AR Mode Part 1</a><br>
<a href="https://www.youtube.com/watch?v=zyVmciqD9rs">Our World Smartphone Prototype AR Mode Part 2</a><br>
<a href="https://www.youtube.com/watch?v=3KIW3wlkUs0">Our World - Smartphone Version Prototype In AR Mode</a><br>
<a href="https://www.youtube.com/watch?v=U1IEfQQQeLc&t=1s">Our World Smartphone Version Preview</a><br>
<a href="https://www.youtube.com/watch?v=3VFp5ltvPEM&t=611s">Games Without Borders Ep 03 ft David Ellams from Our World</a><br>
<a href="https://www.youtube.com/watch?v=2ntJCTEihnw&t=1s">AWAKEN DREAM SYNERGY DREAM # 19 Our World & The OASIS API By David Ellams - (Presentation Only)</a><br>
<a href="https://www.youtube.com/watch?v=kqTNINBFNV4&t=1s">Interview Between Moving On TV & Our World Founder David Ellams - Part 1</a><br>
<a href="https://www.youtube.com/watch?v=HxZixdkc-Ns&t=1s">Interview Between Moving On TV & Our World Founder David Ellams - Part 2</a><br>
<a href="https://www.youtube.com/watch?v=UICajpltv1Y">Our World Interviews With David Atkinson, Commercial Director of Holochain – Part 1</a><br>
<a href="https://www.youtube.com/watch?v=SsNsEDPglos">Our World Interviews With David Atkinson, Commercial Director of Holochain – Part 2</a><br>
<a href="https://www.youtube.com/watch?v=H5JJyLxGFe0">ThreeFold, Our World, Protocol Love, Soulfie API Meeting</a><br>

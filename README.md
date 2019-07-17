# OASIS API / Our World / HoloNET Altha v.0.0.1
The core OASIS (Open Advanced Sensory Immersion System) API that powers Our World and manages the central profile/avatar/karma system that other satellite apps/games plug into it and share. This also includes HoloNET that allows .NET to talk to Holochain, which is where the profile/avatar is stored on a private decentralised, distributed network. This will be gifted forward to the Holochain community along with the HoloUnity3D SDK/Lib/Asset coming soon... ;-)

The first phase of Our World will be a de-centralised distributed XR Gamified 3D Map replacement for Google Maps along with the Avatar/Profile/Karma & OASIS API system. The satellite apps/games will be able to create their own 2D/3D object to appear on the real-time 3D map.

Next it will implement the ARC (Augmented Reality Computer) Membrane allowing the revolutionary next-gen operating system to fully interface & integrate with the 3D Map & Avatar/Karma system as well as render its own 3D interfaces and 2D HUD overlays on top of the map, etc.

Next, it will port Noomap to Unity and will implement a Synergy Engine allowing people to easily find and match solutions/desires/passions and to also find various solution providers, which again will be fully integrated with the 3D Map & Avatar/Karma system.

## HoloNET

This allows .NET to talk to Holochain, which is where the profile/avatar is stored on a private decentralised, distributed network. This will be gifted forward to the Holochain community If there is demand for HoloNET and people wish to contribute we may consider splitting it out into it's own repo...

This is also how Holochain can talk to Unity because Unity uses C#/.NET as it's backend scripting language/tech.

This will help massively turbo charge the holochain ecosystem by opening it up to the massive .NET and Unity communities and open up many more possibilities of the things that can be built on top of Holochain. You can build almost anything you can imagine with .NET and/or Unity from websites, desktop apps, smartphone apps, services, AAA Games and lots more! They can target every device and platform out there from XBox, PS4, Wii, PC, Linux, Mac, iOS, Android, Windows Phone, iPad, Tablets, SmartTV, VR/AR/XR, MagicLeap, etc

We feel this is the gateway to taking Holochain mainstream! ;-)

### How To Use HoloNET

You start by instaniating a new HoloNETClient class found in the NextGenSoftware.Holochain.HoloNET.Client project passing in the holochain websocket URI to the constructor as seen below:

Next, you can subscribe to a number of different events:

* **OnConnected** - Fired when the client has successfully connected to the Holochain conductor.
* **OnDisconnected** - Fired when the client disconnected from the Holochain conductor.
* **OnError** - Fired when an error occurs, check the params for the cause of the error.
* **OnGetInstancesCallBack** - Fired when the hc conductor has returned the list of hc instances it is currently running.
* **OnDataReceived** - Fired when any data is received from the hc conductor. This returns the raw JSON data.
* **OnZomeFunctionCallBack** - Fired when the hc conductor returns the response from a zome function call. This returns the raw JSON data as well as the actual parsed data returned from the zome function. It also returns the id, instance, zome and zome function that made the call.

 private static void HoloNETClient_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
        {
            Console.WriteLine(string.Concat("ZomeFunction CallBack: Id: ", e.Id, ", Instance: ", e.Instance, ", Zome: ", e.Zome, ", ZomeFunction: ", e.ZomeFunction, ", Data: ",  e.ZomeReturnData, ", Raw Zome Return Data: ", e.RawZomeReturnData, ", Raw JSON Data: ", e.RawJSONData, ", IsCallSuccessful: ", e.IsCallSuccessful? "true" : "false"));
            Console.WriteLine("");
        }
        
 **Id:** The id that made the request.
 **Instance:** The hc instance that made the request.
 **Zome:** The zome that made the request.
 **ZomeFunction:** The zome function that made the request.
 **ZomeReturnData:** The parsed data that the zome function returned.
 **RawZomeReturnData:** The raw JSON data that the zome function returned.
 **RawJSONData:** The raw JSON data that the hc conductor returned.
        
* **OnSignalsCallBack** - Fired when the hc conductor sends signals data. NOTE: This is still waiting for hc to flresh out the details for how this will work. Currently this returns the raw signals data.

Now you can call the Connect() method to connect to Holochain.

Once you received a OnConnected event callback you can now call the GetInstances() method to get back a list of instances the holochain conductor you connected is currently running.

Now you can use the instance(s) as a parm to your future Zome calls...

Now you can call one of the CallZomeFunction() overloads:



## HoloOASIS

HoloOASIS uses HoloNET to implement a Storage Provider (IOASISStorage) for the OASIS System. It will soon also implement a Network Provider (IOASISNET) for the OASIS System that will leverage Holochain to create it's own private de-centralised distributed network called ONET (as seen on the OASIS Architecture Diagram below).

This is a good example to see how to use HoloNET in a real world game/platform (OASIS/Our World).

## HoloUnity

We will soon be creating a Asset for the Unity Asset Store that will include HoloNET along with Unity wrappers and examples of how to use HoloNET inside Unity.

In the codebase you will find a project called NextGenSoftware.OASIS.API.FrontEnd.Unity, which shows how the ProfileManager found inside the OASIS API Core (NextGenSoftware.OASIS.API.Core) is used. When you instantiate the ProfileManager you inject into a Storage Provider that implements the IOASISStorage interface. Currently the only provider implemented is the HoloOASIS Provider.

The actual Our World Unity code is not currently stored in this repo due to size restrictions but we may consider using GitHub LGE later on. We are also looking at GitLab and other alternatives to see if they allow greater storage capabilities free out of the box (since we are currently working on a very tight budget but you could change that by donating below! ;-) ).

## .NET HDK

We will soon also begin work on the .NET HDK to open up the amazing Holochain to the massive .NET & Unity ecosystem's, which will help turbocharge the holochain ecosystem they are trying to build...

.NET supports compiling to WASM so we know this is possible... ;-)

We are looking for devs who would be interested in this exciting mini-project, so if you are interested please get in touch either on the channel below or by emailing us on ourworld@nextgensoftware.co.uk or david@nextgensoftware.co.uk. We look forward to hearing from you! :)

https://chat.holochain.org/appsup/channels/net-hdk 

https://github.com/NextGenSoftwareUK/Holochain-.NET-HDK

A placeholder has also been added for the work to begin in this repo in the project Holochain.NextGenSoftware.HoloNET.HDK. Just as with NextGenSoftware.Holochain.HoloNET.Client, this project may be split out into its own repo and then linked to this one as a sub-module in future...

## Architecture Diagram

The Architecture diagram can be found on our website below but it is also in the root of the repo cunningly named OASIS Architecture Diagram.png

https://github.com/dellams/OASIS-API-And-HoloNET/blob/master/OASIS%20Arcitecture.png

## Open Modular Design

As you can see from the diagram the OASIS architecture is very modular, open and extensible meaning any component can easily be swapped out for another without having to make any changes to the rest of the system. It will use MEF (Managed Extensibility Framework) so the components can even be swapped out without having to re-compile any of the existing code, you simply drop the new component into a hot folder that the system will pick up on the next time you restart.

The components are split into 6 sub-systems:

* Storage (IOASISStorage Interface)
* Network (IOASISNET Interface)
* Renderer (IOASIS2DRenderer & IOASIS3DRenderer Interfaces)
* XR
* Haptic Feedback
* Input

Currently HoloOASIS implements the IOASISStorage interface. In future it will also implement the IOASISNET interface.

## Our World/OASIS Will Act As The Bridge For All

As you can see from the architecture diagram, the system will act as the bridge for all platforms and devices due to it being very open and modular by design. In future there will be support for IPFS & Ethereum plus many more. This will help users of both legacy apps/games/websites and blockchain slowly migrate to holochain since it will help expose it to them all. The OASIS API will act as a stepping stone as well as help Everything talk to Everything for maximum compatibility.

## Implement Your Own Storage/Network/Renderer Provider

Thanks to the system being very open/modular by design you can easily implement your own Storage/Network/Renderer Provider by simply implementing the IOASISStorage/IOASTNET/IOASIS2DRenderer/IOASIS3DRenderer interfaces respectively. For example you could create a MongoDB, MySQL or SQL Server Storage Provider. This also ensures forward compatibility since if a new storage medium or network protocol comes out in the future you can easily write a new provider for them without having to change any of the existing system. 

The same applies if a new 3D Engine comes out you want to use.

## Switch To A Different Provider In RealTime

The system can even switch to a different Storage/Network Provider in real-time as a fall-over if one storage/network provider goes down for example. It could even use more than one Storage/Network provider since certain providers may be better suited for a given task than another, this way you get the best of both worlds as well as ensure maximum compatibility and uptime.

The same applies for the Renderer Provider, it could use one provider to render 2D and another for 3D, it could even use more than one for for both 2D and/or 3D.

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

## Donations Welcome! :)

We are working full-time on this project so we have no other income so if you value it, we would really appreciate a donation to our crowd funding page below:

https://www.gofundme.com/f/ourworldthegame

Every
 little helps, even if you can only manage £1 it can still help make all the difference! Thank you! :)

We would really appreciate if you could donate anything you can afford, even if it's just a pound, if everyone did that then we would be able to massively accelerate this very urgent and important project for a world in need right now. I think everyone can justify a pound if it meant saving the world don't you think? 

It's even better to spend a pound on this project rather than buying a lottery ticket since you have more chance of being hit by a car than winning the jackpot, then even by some fluke you did win, there is no point having millions if there is no world left to enjoy it on. 

Think about it...

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

## Devs/Contributions Welcome! :)

We would love to have some much needed dev resource on this vital project not only for Holochain but also for the world so if you are interested please contact us on either ourworld@nextgensoftware.co.uk or david@nextgensoftware.co.uk. Thank you, we look forward to hearing from you! :)

## Other Ways To Get Involved

If you cannot code or donate, then no problem, you can help in other ways! :) You can share our website/posts, give us valuable feedback on our site, etc as well as submit ideas for Our World. We are also looking for people to join for every department/area such as PR, Sales, Support, Admin, Accounting, Management, Strategy, Operations, etc  

So if you feel you want to help or get involved please contact us on ourworld@nextgensoftware.co.uk, we would love to hear from you! :)

You can also get involved on our forum here:

http://www.ourworldthegame.com/forum

## Websites

Read more on this project on our websites below:

http://www.ourworldthegame.com

http://www.nextgensoftware.co.uk

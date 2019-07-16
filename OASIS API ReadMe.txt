# OASIS-API
The core OASIS (Open Advanced Sensory Imersion System) API that powers Our World and manages the central profile/avatar/karma system that other satellite apps/games plug into it and share. This also includes HoloNET that allows .NET to talk to Holochain, which is where the profile/avatar is stored on a private decentralised, distributed network. This will be gifted forward to the Holochain community along with the HoloUnity3D SDK/Lib/Asset coming soon... ;-)


## HoloNET

If there is demand for HoloNET and people wish to contribuite we may consider splitting it out into it's own repo...

## HoloOASIS

HoloOASIS uses HoloNET to implement a Storage Provider for the OASIS System.

## .NET HDK

We will soon also begin work on the .NET HDK to open up the amazing Holochain to the massive .NET & Unity ecosystem's, which will help turbocharge the holochain ecosystem they are trying to build...

https://chat.holochain.org/appsup/channels/net-hdk 

https://github.com/dellams/Holochain-.NET-HDK 

## Arcitecture Diagram

The Architecture diagram can be found on our website below but it is also in the root of the repo cunningly named OASIS Arcitecture Diagram.png

## Open Modular Design

As you can see from the diagram the OASIS architecture is very modular, open and extensible meaning any component can easily be swapped out for another without having to make any changes to the rest of the system. It will use MEF (Managed Extensibility Framework) so the components can even be swapped out without having to re-complie any of the existing code, you simply drop the new component into a hot folder that the system will pick up on the next time you restart.

The components are split into 6 sub-systems:

* Storage (IOASISStorage Interface)
* Network (IOASISNET Interface)
* Renderer (IOASIS2DRenderer & IOASIS3DRenderer Interfaces)
* XR
* Hapic Feedback
* Input

Currently HoloOASIS implements the IOASISStorage interface. In future it will also implement the IOASISNET interface.

## Our World/OASIS Will Act As The Bridge For All

As you can see from the arcitecture diagram 

## Next Steps

1. Add HTTP support to HoloNET.
2. Add built-in HC Conductor to HoloNET so it can fire up it's own conductor without needing to do this manually.
3. Add a ZomeProxyGenerator tool so it can auto-generate a C# Zome Proxy that wraps around HoloNET calls (the code would be similar to what is in HoloOASIS)
4. Contuine with Unity integration and development of HoloUnity, which will then also be gifted forward to the wonderful holochain community... :)
5. Refactor HoloNET to split out the websocket JSON RPC 2.0 implementation from the holochain specefic logic so the websocket JSON RPC code can be re-used with the OASIS API websocket implemtnation coming soon...
6. Implement OASIS API Websocket JSON RPC 2.0 implementation.
7. Implement OASIS API Websocket HTTP implementation.
8. Implement OASIS API HTTP Restful WebAPI implementation.
9. Finish implementing avatar screen in Unity.
10. Place avatar on 3D map using users current location (geolocation) from their device GPS.
10. Implement Mapping/Routing API for 3D Map in Unity.
11. Implement Places Of Interet (Holons) on 3D Map.
12. Implement ARC Membrane API.
13. Implement Unity NLogger.

## Donations Welcome! :)

We are working full-time on this project so we have no other income so if you value it, we would really appreciate a donation to our crowd funding page below:

https://www.gofundme.com/f/ourworldthegame

Every little helps, even if you can only manage £1 it can still help make all the difference! Thank you! :)

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

* What will be your legacy? 

* Do you want to be in on the ground floor of the upcoming platform that will take the world by storm?
The platform that is going to win many rewards for the ground-breaking work it will do. Do you want to be a hero of your own life story? 

* Want to tell your kids and grandkids that you helped make it happen and go down in history as a hero?

* What kind of world do you want to leave to the next generation? 

* Want to be part of something greater than yourself?  

* How can you do your part to create a better world? 

* This is HOW you do your part...

* Be the change you wish to see in the world...

NOTE: WE HAVE ONLY DISCLOSED ABOUT 10% OF WHAT OUR WORLD / THE OASIS IS, IF YOU WISH TO GET INVOLVED OR INVEST THEN WE WILL BE HAPPY TO SHARE MORE, PLEASE GET IN TOUCH, WE LOOK FORWARD TO HEARING FROM YOU...

**TOGETHER WE CAN CREATE A BETTER WORLD.**

## Devs/Contributions Welcome! :)

We would love to have some much needed dev resource on this vital project not only for Holochain but also for the world so if you are intersted please contact us on either ourworld@nextgensoftware.co.uk or david@nextgensoftware.co.uk. Thank you, we look forward to hearing from you! :)

## Other Ways To Get Involved

If you cannot code or donate, then no problem, you can help in other ways! :) You can share our website/posts, give us valuable feedback on our site, etc as well as submit ideas for Our World. We are also looking for people to join for every department/area such as PR, Sales, Support, Admin, Accounting, Management, Strategy, Opertations, etc  

So if you feel you want to help or get involved please contact us on ourworld@nextgensoftware.co.uk, we would love to hear from you! :)

You can also get involved on our forum here:

http://www.ourworldthegame.com/forum

## Websites

Read more on this project on our websites below:

http://www.ourworldthegame.com

http://www.nextgensoftware.co.uk

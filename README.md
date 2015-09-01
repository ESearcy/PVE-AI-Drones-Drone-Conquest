AIDroneConquest
This is a project I started to help give players a more immersive experience while in the game.

There Two main functions to this mod being the automation of personalized defense drones for players and the implementation of an intelligent Enemy/AI system for the game.

===---Conquest Drone Information: How To Use Them---===
*Build a ship in the game how ever you would like and test it to ensure it has acceptable navigation capabilities (they need to turn quick, slow down quick, and accelerate quick). *Place a RemoteControl Block in the ship (well armored), this is the drones core so if it breaks so does the drone. *Also add a few guns facing the same direction of the remote control block.

Name RemoteControl block : #ConquestDrone#

--WARNING-- If there is no mothership then the conquest drones will simply guard the area they were built at against anything that is not friendly to it. Multipule drones can be placed and will join in squads together automatically. this allows for players or admins to have planned fleet battles and stuff without the need of a monthership.

ACTIVATING A MOTHERSHIP //todo Create a how to videos for onlining a Conquest Drone and onlining a mothership

There is a demo mothership that can be downloaded here LINK HERE

Simply place any number of these ships in your server, each one will simply be considered an extension of the main mothership and will take control of a portion of the available conquest drones.

--Mothership Fleet Information-- Max number of Conquest Drones = 30; Max number of Drone Squads = 10; Max number of Drones Per Squad = 3;

----How to Build Custom Mothership Drone Factory---- //todo Create a how to video on building a mothership Factory

Needed Blocks

-#Piston# - any number of pistons with this name will expand and retract when a new build job comes in
-#Welder# - all welders turn on when build job starts
-#Projector# - all projectors turn on when build job starts
-#Grinder# - all grinders turn on for last 30 seconds of build job
===---Player Drone Information: How To Use Them---===
//todo Create how to video for onlining a personalized defense Drone

*Build a ship in the game how ever you would like and test it to ensure it has acceptable navigation capabilities (they need to turn quick, slow down quick, and accelerate quick). *Place a RemoteControl Block in the ship (well armored), this is the drones core so if it breaks so does the drone. *Also add a few guns facing the same direction of the remote control block.

Name RemoteControl block : #PlayerDrone# Additional Arguments for RemoteControl name field (none of these are required and all have default values)

"on/off:power" -set power {on, off} Default value = on on = player can not manually override their drones controls when drone is online off = makes drone give up control of the remote control block for manual override

"order:type" -set OrderName {guard, patrol, sentry} Default value = Guard. Orders drone to follow and orbit you, engaging any nearby enemies Patrol = Orders drone to patrol around their current location rather then follow their leader Sentry = Orders drones to stay put and only move to attack enemies that come near its area

"broadcast:Type" -set Type {antenna, beacon} Default = Beacon Antenna = stats will be broadcasted via antenna if you do not set this then the drones stats will be broadcasted via Beacon

"standing:type" -set type (passive, agressive) Default type = agressive

"orbitrange:Number" - set Number (whole positive) default value = based on mass/size Number = Sets the drones orbit range. must be a non-negative number, value will be rounded down if not a whole number

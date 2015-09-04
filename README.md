How to Contribute:
Create a fork of this repository
Create a new branch
Do your changes in this branch
Commit to this branch
When it is done, create a pull request for me to bring your branch into the main code aftr I review it.

AIDroneConquest
This is a project I started to help give players a more immersive experience while in the game.

There Two main functions to this mod being the automation of personalized defense drones for players and the implementation of an intelligent Enemy/AI system for the game.

===---Conquest Drone Information: How To Use Them---===

CONFIGURATION FILE PATH = \%AppData%\Roaming\SpaceEngineers\Storage\DroneConquest_DroneConquest\DroneConquestSettingsFile.txt
--Mothership Fleet Information--
Max number of Conquest Drones = 30;
Max number of Drone Squads = 10;
Max number of Drones Per Squad = 3;
Conquest Range of Influence = 8000;

*Build a ship in the game how ever you would like and test it to ensure it has acceptable navigation capabilities (they need to turn quick, slow down quick, and accelerate quick). *Place a RemoteControl Block in the ship (well armored), this is the drones core so if it breaks so does the drone. *Also add a few guns facing the same direction of the remote control block.

Name RemoteControl block : #ConquestDrone# to add custom drones to mothership fleet
All default drones as well as the mothership will spawn automatically

--WARNING-- If there is no mothership then the conquest drones will simply guard the area they were built at against anything that is not friendly to it. Multipule drones can be placed and will join in squads together automatically. this allows for players or admins to have planned fleet battles and stuff without the need of a monthership.



===---Player Drone Information: How To Use Them---===
//todo Create how to video for onlining a personalized defense Drone

*Build a ship in the game how ever you would like and test it to ensure it has acceptable navigation capabilities (they need to turn quick, slow down quick, and accelerate quick). *Place a RemoteControl Block in the ship (well armored), this is the drones core so if it breaks so does the drone. *Also add a few guns facing the same direction of the remote control block.

Name RemoteControl block : #PlayerDrone# Additional Arguments for RemoteControl name field (none of these are required and all have default values)

"on/off:power" -set power {on, off} Default value = on on = player can not manually override their drones controls when drone is online off = makes drone give up control of the remote control block for manual override

"order:type" -set OrderName {guard, patrol, sentry} Default value = Guard. Orders drone to follow and orbit you, engaging any nearby enemies Patrol = Orders drone to patrol around their current location rather then follow their leader Sentry = Orders drones to stay put and only move to attack enemies that come near its area

"broadcast:Type" -set Type {antenna, beacon} Default = Beacon Antenna = stats will be broadcasted via antenna if you do not set this then the drones stats will be broadcasted via Beacon

"standing:type" -set type (passive, agressive) Default type = agressive

"orbitrange:Number" - set Number (whole positive) default value = based on mass/size Number = Sets the drones orbit range. must be a non-negative number, value will be rounded down if not a whole number

"mode:Number" - set type (whole positive)
default value = AtRange - the drones will by default orbit you
fighter = this makes the drone pick a static location in relation to you and stay there relitive to you.

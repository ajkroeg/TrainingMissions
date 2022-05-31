# TrainingMissions

**DependsOn IRBTModUtils**

## Training Missions

Allows "training missions" that auto-repair and restore your damaged and destroyed mechs after combat.
 
Functionally copies mechs dropped by the player, including their custom loadouts (not including contract-specific pilots' mechs, e.g. Training Day mission urbies or Escort VIP Griffins). Notably, this will <i>not</i> exclude mechs provided by "Test Drive" contracts, so maybe don't use it for those. Or do, but adjust the contract rewards since the player will be getting a mech out of the deal.

Player mechs are restored <i>after</i> the contract results have resolved. Thus, they will still see all the damage they incurred on the mission results screen, and will also recieve notifications for "Mech Destroyed and unable to be recovered", even if that mech will be restored.


## Swap Forces contracts

These contracts will swap your units with the opfors. Optionally can be designated a "simulation", where you can still get paid (per the contract def), but with no other lasting consequences good or bad (e.g. no injuries, but no reputation or salvage). If not designated a simulation, your mechs will be destroyed/salvaged like any other opfor, but you get to keep the mechs you got from the AI.

## Doppelganger contracts

These contracts duplicate your mechs and give them to the opfor. Optionally can be designated a "simulation", where you can still get paid (per the contract def), but with no other lasting consequences good or bad (e.g. no injuries, but no reputation or salvage). If not designated a simulation, your mechs will be recovered like normal, and you can salvage the "copies" as normal opfor mechs.

## Simulation hotkey

Holding shift while clicking the "Deploy" button designates any "normal" contract a "simulation", for which there will be no payment, no reputation change, no injuries taken, and no damage to mechs.

Settings:

```
"Settings": {
		"enableLogging": true,
		"showRestoreNotification": true,
		"enableSimulationHotKey": true,
		"TrainingContractIDs": {
			"SimpleBattle_TakeTheBait": "GOODFAITH",
     "ThreeWayBattle_TestDrive": "SUCCESS",
     "ThreeWayBattle_ShowTheFlag": "ALWAYS"
		},
		"SwapUnitsWithAIContractIDs": {
			"DuoDuel_SwapLance": "RECOVER",
			"DuoDuel_SwapLanceSIM": "SIMULATOR"
		},
		"DoppelgangerContractIDs": {
			"DuoDuel_Doppelganger": "SIMULATOR"
		},
		"DisallowedRecoveryTags": [
			"unit_noncontrollableTank"
		]
	},
```

`enableLogging` - bool, enables logging.

`showRestoreNotification` - bool, enable/disable post-contract notification telling user that their damaged/destroyed mechs have been restored to pre-contract state. If enabled, the "Mechs Restored" notification will appear in the same series of post-contract notifications as the "Mechwarrior Training available" and "Mech destroyed", etc notifications.

`enableSimulationHotKey` - bool, enables using Shift-Click when pressing the "Deploy" button in the lance configuration screen to designate that contract as a "simulation contract", where no mech damage or injuries will persist, and for which you will receive no salvage, payment, or reputation changes.

`TrainingContractIDs` - Dictionary<string, string> - Dictionary of contract ID Keys and mission outcome Value requirements for which pre-contract mechs will be copied and then restored to the player after the contract.

If mission outcome value requirement == `SUCCESS`, mechs will only be restored on a successful mission. If mission outcome value requirement == `GOODFAITH`, mechs will be restored on a successful mission or good faith effort. If mission outcome value == `ALWAYS`, mechs will <i>always</i> be restored, regardless of mission outcome.

Using the above settings, the contract "Show the Flag" will restore the players mechs regardless of mission outcome. "Take The Bait" will restore the players mechs if they achieve a "good faith effort" or success. "Test Drive" will restore the players mechs <i>only</i> on a successful mission.


`SwapUnitsWithAIContractIDs` - Dictionary<string, string> - Dictionary of contract ID Keys and string Values. For contracts with ID matching the Key, the players' chosen mechs will be swapped with the contracts chosen opfor mechs for the duration of the contract, and vice versa. If the corresponding Value == "SIMULATOR", the contract is assumed to be a simulation. Injuries and damage will not persist after the contract, and the player will recieve no reputation changes and no salvage, but <i>will</i> receive any negotiated contract payments (this will also be subject to drop costs/ammo costs if mods implementing those are used. Not much I can do about that). If the corresponding value == "RECOVER", the contract is assumed to be a "real" contract. The player will have to salvage their original (now-destroyed) mechs like any other opfor unit, but they will/won't recover their "new" (previously belonging to the OpFor) mechs like normal, subject to normal mech recovery rules.

`DoppelgangerContractIDs` - Dictionary<string, string> - Dictionary of contract ID Keys and string Values. For contracts with ID matching the Key, the players' chosen mechs will be duplicated and given to the Opfor. If the corresponding Value == "SIMULATOR", the contract is assumed to be a simulation. Injuries and damage will not persist after the contract, and the player will recieve no reputation changes and no salvage, but <i>will</i> receive any negotiated contract payments (this will also be subject to drop costs/ammo costs if mods implementing those are used. Not much I can do about that). If the corresponding value == "RECOVER", the contract is assumed to be a "real" contract. The player will be able to salvage their duplicated (now-destroyed) mechs like any other opfor unit, and they will/won't recover their own mechs like normal, subject to normal mech recovery rules.

### Adding Training Missions at runtime

In 1.0.3.0, external mods using TrainingMissions as a dependency can call `Util.AddTrainingContract(Contract contract, string repairRequirement)` to add individual training contracts at runtime. The `repairRequirement` parameter can be "SUCCESS", "GOODFAITH" or "ALWAYS" as described earlier.

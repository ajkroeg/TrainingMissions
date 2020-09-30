# TrainingMissions
 Allows "training missions" that auto-repair and restore your damaged mechs after combat. Functionally copies mechs dropped by the player, including their custom loadouts (not including contract-specific pilots' mechs, e.g. Training Day mission urbies or Escort VIP Griffins). Notably, this will <i>not</i> exclude mechs provided by "Test Drive" contracts, so maybe don't use it for those. Or do, but adjust the contract rewards since the player will be getting a mech out of the deal.

Player mechs are restored <i>after</i> the contract results have resolved. Thus, they will still see all the damage they incurred on the mission results screen, and will also recieve notifications for "Mech Destroyed and unable to be recovered", even if that mech will be restored.

Settings:

```
"Settings": {

"enableLogging": true,
"showRestoreNotification": true,
"TrainingContractIDs":
   {
     "SimpleBattle_TakeTheBait": 1,
     "ThreeWayBattle_TestDrive": 2,
     "ThreeWayBattle_ShowTheFlag": 0
   }
},
```

`enableLogging` - bool, enables logging.

`showRestoreNotification` - bool, enable/disable post-contract notification telling user that their damaged/destroyed mechs have been restored to pre-contract state. If enabled, the "Mechs Restored" notification will appear in the same series of post-contract notifications as the "Mechwarrior Training available" and "Mech destroyed", etc notifications.

`TrainingContractIDs` - `Dictionary<string, int>` - Dictionary of contract ID Keys and mission outcome Value requirements for which pre-contract mechs will be copied and then restored to the player after the contract.

If mission outcome value requirement == `2`, mechs will only be restored on a successful mission. If mission outcome value requirement == `1`, mechs will be restored on a successful mission or good faith effort. If mission outcome value == `0` (default) or any other value, mechs will <i>always</i> be restored, regardless of mission outcome.

Using the above settings, the contract "Show the Flag" will restore the players mechs regardless of mission outcome. "Take The Bait" will restore the players mechs if they achieve a "good faith effort" or success. "Test Drive" will restore the players mechs <i>only</i> on a successful mission.

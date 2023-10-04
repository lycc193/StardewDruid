﻿using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using Netcode;
using StardewDruid.Cast;
using StardewDruid.Map;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Quests;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Xml.Linq;
using System.Xml.Schema;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;
using static StardewValley.Menus.CharacterCustomization;

namespace StardewDruid
{
    public class Mod : StardewModdingAPI.Mod
    {

        private ModData Config;

        private ActiveData activeData;

        private StaticData staticData;

        private Map.Effigy druidEffigy;

        public Dictionary<int, string> TreeTypes;

        public Dictionary<string, List<Vector2>> earthCasts;

        private Dictionary<Type, List<Cast.Cast>> activeCasts;

        public string activeLockout;

        public Dictionary<string, Vector2> warpPoints;

        public Dictionary<string, Vector2> firePoints;

        public Dictionary<string, int> warpTotems;

        public List<string> warpCasts;

        public List<string> fireCasts;

        private Dictionary<string, Map.Quest> questIndex;

        private Dictionary<string, List<Monster>> monsterSpawns;

        private Queue<Rite> riteQueue;

        StardewValley.Tools.Pickaxe targetPick;

        StardewValley.Tools.Axe targetAxe;

        override public void Entry(IModHelper helper)
        {

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            helper.Events.GameLoop.SaveLoaded += SaveLoaded;

            helper.Events.Input.ButtonsChanged += OnButtonsChanged;

            helper.Events.GameLoop.OneSecondUpdateTicked += EverySecond;

            helper.Events.GameLoop.Saving += SaveUpdated;

        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {

            Config = Helper.ReadConfig<ModData>();

            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModData(),
                save: () => Helper.WriteConfig(Config)
            );

            configMenu.AddKeybindList(
                mod: ModManifest,
                name: () => "Rite Keybinds",
                tooltip: () => "Configure the list of keybinds to use for casting Rites",
                getValue: () => Config.riteButtons,
                setValue: value => Config.riteButtons = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Cast Buffs",
                tooltip: () => "Enables automatic berry, sashimi and cheese consumption when casting with critically low stamina. Enables magnetism buff when casting Rite of the Earth.",
                getValue: () => Config.masterStart,
                setValue: value => Config.masterStart = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Master Start",
                tooltip: () => "Start with all Rite levels unlocked (not recommended for immersion). Note that activating, saving in game, then deactivating afterwards, will wipe all your Rite Levels and Effigy Quests.",
                getValue: () => Config.castBuffs,
                setValue: value => Config.castBuffs = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Effigy tileX",
                tooltip: () => "Move the Effigy's position in the farmcave on the X Axis",
                getValue: () => Config.farmCaveStatueX,
                setValue: value => Config.farmCaveStatueX = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Effigy tileY",
                tooltip: () => "Move the Effigy's position in the farmcave on the Y Axis",
                getValue: () => Config.farmCaveStatueY,
                setValue: value => Config.farmCaveStatueY = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Approach tileX location",
                tooltip: () => "Move the position of the action tile used to trigger dialogue with the Effigy on the X Axis.",
                getValue: () => Config.farmCaveActionX,
                setValue: value => Config.farmCaveActionX = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Approach tileY location",
                tooltip: () => "Move the position of the action tile used to trigger dialogue with the Effigy on the Y Axis",
                getValue: () => Config.farmCaveActionY,
                setValue: value => Config.farmCaveActionY = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Hide Effigy",
                tooltip: () => "Hide the Effigy, and instead use the Rite Key anywhere on the farmcave map to converse with its disembodied voice",
                getValue: () => Config.farmCaveHideStatue,
                setValue: value => Config.farmCaveHideStatue = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Recess Effigy Space",
                tooltip: () => "Configure the backwall of the farmcave to situate the Effigy",
                getValue: () => Config.farmCaveMakeSpace,
                setValue: value => Config.farmCaveMakeSpace = value
            );

        }

        private void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {

            if(Context.IsMainPlayer) {

                staticData = Helper.Data.ReadSaveData<StaticData>("staticData");

            }

            staticData ??= new StaticData();

            if (Config.masterStart && !staticData.blessingList.ContainsKey("masterStart"))
            {

                staticData.questList = Config.questList;

                staticData.blessingList = Config.blessingList;

                staticData.blessingList["masterStart"] = 1;

                staticData.activeBlessing = "earth";

            }
            else if(staticData.blessingList.ContainsKey("masterStart"))
            {

                staticData = new StaticData();

            }

            questIndex = Map.QuestData.QuestList();

            if (staticData.questList.Count == 0)
            {

                string firstQuest = Map.QuestData.FirstQuest();

                staticData.questList[firstQuest] = false;

                ReassignQuest(firstQuest);

            }

            earthCasts = new();

            activeCasts = new();

            activeLockout = "none";

            riteQueue = new();

            /*if (staticData.blessingList != null)
            {

                if (!staticData.blessingList.ContainsKey("special"))
                {

                    staticData.blessingList["special"] = 1;

                }

                specialLimit = 2 + (staticData.blessingList["special"] * 2);

                specialCasts = specialLimit;

            }*/

            druidEffigy = new(
                this,
                Config.farmCaveStatueX,
                Config.farmCaveStatueY,
                Config.farmCaveHideStatue,
                Config.farmCaveMakeSpace
            );

            druidEffigy.ModifyCave();

            warpPoints = Map.WarpData.WarpPoints();

            warpTotems = Map.WarpData.WarpTotems();

            warpCasts = new();

            firePoints = Map.FireData.FirePoints();

            fireCasts = new();

            monsterSpawns = new();

            targetPick = new();

            targetPick.UpgradeLevel = 4;

            targetPick.DoFunction(Game1.player.currentLocation, 0, 0, 1, Game1.player);

            targetAxe = new();

            targetAxe.UpgradeLevel = 4;

            targetAxe.DoFunction(Game1.player.currentLocation, 0, 0, 1, Game1.player);

            return;

        }

        private void SaveUpdated(object sender, SavingEventArgs e)
        {
            if(Context.IsMainPlayer)
            {
                
                Helper.Data.WriteSaveData("staticData", staticData);

            }

            druidEffigy.lessonGiven = false;

            foreach (KeyValuePair<Type, List<Cast.Cast>> castEntry in activeCasts)
            {
                
                if (castEntry.Value.Count > 0)
                {
                    
                    foreach (Cast.Cast castInstance in castEntry.Value)
                    {
                        
                        castInstance.CastRemove();
                    
                    }
                
                }

            }

            activeLockout = "none";

            foreach (KeyValuePair<string, List<Monster>> monsterEntry in monsterSpawns)
            {
                
                if (monsterEntry.Value.Count > 0)
                {
                    
                    foreach (Monster monsterInstance in monsterEntry.Value)
                    {
                        
                        Game1.getLocationFromName(monsterEntry.Key).characters.Remove(monsterInstance);
                    
                    }
                
                }

            }

            activeData.castComplete = true;

            earthCasts = new();

            activeCasts = new();

            riteQueue = new();

            warpCasts = new();

            fireCasts = new();

            monsterSpawns = new();

        }

        private void EverySecond(object sender, OneSecondUpdateTickedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
            {

                return;

            }

            /*if (ChallengeLocations.ContainsKey(Game1.player.currentLocation.Name))
            {

                foreach (string Challenge in ChallengeLocations[Game1.player.currentLocation.Name])
                {

                    if (!staticData.ChallengeList[Challenge])
                    {

                        if (!ChallengeAnimations.ContainsKey(Challenge))
                        {

                            ChallengeAnimations[Challenge] = Map.Spawn.ChallengeAnimation(Challenge);

                        }

                    }

                }

            }*/

            if (activeCasts.Count == 0)
            {

                activeLockout = "none";

                return;

            }

            List<Cast.Cast> activeCast = new();

            List<Cast.Cast> removeCast = new();

            foreach (KeyValuePair<Type, List<Cast.Cast>> castEntry in activeCasts)
            {

                int entryCount = castEntry.Value.Count;

                if (castEntry.Value.Count > 0)
                {

                    int castIndex = 0;

                    foreach (Cast.Cast castInstance in castEntry.Value)
                    {

                        castIndex++;

                        if (castInstance.CastActive(castIndex, entryCount))
                        {

                            activeCast.Add(castInstance);

                        }
                        else
                        {

                            removeCast.Add(castInstance);

                        }


                    }


                }

            }

            foreach (Cast.Cast castInstance in removeCast)
            {

                castInstance.CastRemove();

                activeCasts[castInstance.GetType()].Remove(castInstance);

            }

            removeCast.Clear();

            bool exitAll = false;

            if (Game1.eventUp || Game1.fadeToBlack || Game1.currentMinigame != null || Game1.isWarping || Game1.killScreen)
            {
                exitAll = true;
            
            }

            foreach (Cast.Cast castInstance in activeCast)
            {
                if (exitAll)
                {

                    castInstance.CastRemove();

                }
                if (Game1.activeClickableMenu != null || Game1.player.freezePause > 0)
                {

                    castInstance.CastExtend();

                }
                else
                {

                    castInstance.CastTrigger();

                }

            }

            activeCast.Clear();

            return;

        }

        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
            {

                return;

            }

            activeData ??= new ActiveData();

            // ignore if player is busy with something else
            if (Game1.player.IsBusyDoingSomething())
            {

                activeData.castComplete = true;

                return;

            }

            // simulates interactions with the farm cave effigy
            if (Game1.currentLocation.Name == "FarmCave")
            {

                Vector2 playerLocation = Game1.player.getTileLocation();

                Vector2 cursorLocation = Game1.currentCursorTile;

                if (playerLocation.X >= Config.farmCaveStatueX-1 && playerLocation.X <= Config.farmCaveActionX+1 && playerLocation.Y == Config.farmCaveActionY)// && cursorLocation.X == 6 && (cursorLocation.Y == 2 || cursorLocation.Y == 3))
                {

                    foreach (SButton buttonPressed in e.Pressed)
                    {

                        if (buttonPressed.IsUseToolButton() || buttonPressed.IsActionButton())
                        {

                            Helper.Input.Suppress(buttonPressed);

                            druidEffigy.Approach(Game1.player);

                            return;

                            

                        }

                    }

                }

                if (Config.riteButtons.GetState() == SButtonState.Pressed)
                {
                    druidEffigy.Approach(Game1.player);

                    return;
                }

            }

            bool chargeEffect = false;

            // check for cast trigger
            foreach (SButton buttonPressed in e.Pressed)
            {
                if (buttonPressed.IsUseToolButton() || buttonPressed.IsActionButton())
                {

                    activeData.castComplete = true;

                    return;


                }

            }

            if(Config.riteButtons.GetState() == SButtonState.Pressed)
                //else if (buttonPressed.Equals(Config.riteButton))
                {

                    //Helper.Input.Suppress(buttonPressed);

                    // ignore if current tool isn't set
                    if (Game1.player.CurrentTool is null)
                    {

                        Game1.addHUDMessage(new HUDMessage("Requires a melee weapon or scythe to activate rite", 2));

                        return;

                    }

                    // ignore if current tool isn't weapon
                    if (Game1.player.CurrentTool.GetType().Name != "MeleeWeapon")
                    {

                        Game1.addHUDMessage(new HUDMessage("Requires a melee weapon or scythe to activate rite", 2));

                        return;

                    }

                    Vector2 playerVector = Game1.player.getTileLocation();

                    List<Map.Quest> triggeredQuests = new();

                    foreach (string castString in staticData.triggerList)
                    {

                        Map.Quest questData = questIndex[castString];

                        if (questData.triggerLocation == Game1.player.currentLocation.Name && activeLockout == "none")
                        {

                            bool triggerValid = true;

                            if (questData.startTime != 0)
                            {

                                if (Game1.timeOfDay < questData.startTime)
                                {

                                    triggerValid = false;

                                }

                            }

                            if (triggerValid)
                            {

                                Vector2 ChallengeVector = questData.triggerVector;

                                Vector2 ChallengeLimit = ChallengeVector + questData.triggerLimit;

                                if (
                                    playerVector.X >= ChallengeVector.X &&
                                    playerVector.Y >= ChallengeVector.Y &&
                                    playerVector.X < ChallengeLimit.X &&
                                    playerVector.Y < ChallengeLimit.Y
                                    )
                                {

                                    triggeredQuests.Add(questData);

                                }

                            }

                        }

                    }

                    foreach (Map.Quest questData in triggeredQuests)
                    {

                        ModUtility.AnimateHands(Game1.player, Game1.player.FacingDirection, 500);

                        Game1.player.currentLocation.playSound("yoba");

                        Cast.Cast castHandle;

                        if (questData.triggerType == "sword")
                        {
                            castHandle = new Cast.Sword(this, playerVector, new Rite(), questData);
                        }
                        else
                        {
                            castHandle = new Cast.Challenge(this, playerVector, new Rite(), questData);
                        }

                        RemoveTrigger(questData.name);

                        castHandle.CastQuest();

                        activeLockout = "trigger";

                        return;

                    }

                    // new cast configuration

                    string activeBlessing = staticData.activeBlessing;

                    if (activeBlessing == "none")
                    {

                        Game1.player.currentLocation.playSound("ghost");

                        Game1.addHUDMessage(new HUDMessage("Nothing happens", 2));

                        return;

                    }

                    switch (Game1.player.CurrentTool.InitialParentTileIndex)
                    {

                        case 15: // Forest Sword

                            if (staticData.blessingList.ContainsKey("earth"))
                            {

                                activeBlessing = "earth";

                            }

                            break;

                        case 14: // Neptune's Glaive

                            if (staticData.blessingList.ContainsKey("water"))
                            {

                                activeBlessing = "water";

                            }

                            break;

                        case 9: // Lava Katana

                            if (staticData.blessingList.ContainsKey("stars"))
                            {

                                activeBlessing = "stars";

                            }

                            break;

                    }

                    Dictionary<string, bool> spawnIndex = Map.SpawnData.SpawnIndex(Game1.player.currentLocation);

                    // ignore if game location has no spawn profile
                    if (spawnIndex.Count == 0)
                    {

                        if (activeBlessing != "none")
                        {

                            Game1.addHUDMessage(new HUDMessage("Unable to reach the otherworldly plane", 2));

                            //Monitor.Log($"{Game1.player.currentLocation.Name} {activeBlessing}",LogLevel.Debug);

                        }

                        return;

                    }

                    activeData = new ActiveData
                    {
                        activeCast = activeBlessing,

                        spawnIndex = spawnIndex

                    };


                    // initial charge effect
                    chargeEffect = true;

                }

            //}


            // ignore if current tool isn't set
            if (Game1.player.CurrentTool is null)
            {

                return;

            }

            // ignore if current tool isn't weapon
            if (Game1.player.CurrentTool.GetType().Name != "MeleeWeapon")
            {

                return;

            }

            // Ignore if cast complete and not retriggered
            if (activeData.castComplete)
            {

                return;

            }

            bool validPress = false;
            if (Config.riteButtons.GetState() == SButtonState.Held)
            {

                validPress = true;
            
            }
                // begin to count amount that cast button is held
                /*    foreach (SButton buttonHeld in e.Held)
                {

                    // Already in Active State
                    if (buttonHeld.Equals(Config.riteButton))
                    {

                        validPress = true;

                    }

                }

                if (!validPress)
                {

                    foreach (string buttonFire in buttonList)
                    {

                        SButton buttonEnum = (SButton) Enum.Parse(typeof(SButton), buttonFire);

                        if (Helper.Input.IsSuppressed(buttonEnum))
                        {

                            Monitor.Log($"{buttonEnum}",LogLevel.Debug);

                            validPress = true;

                        } 
                        else if (Helper.Input.IsDown(buttonEnum))
                        {

                            validPress = true;

                        }

                    }

                }*/
                //Helper.Input.Suppress(buttonPressed);

            if (validPress)
            {

                int staminaRequired;

                switch (activeData.activeCast)
                {

                    case "water":

                        staminaRequired = 48;

                        break;

                    case "stars":

                        staminaRequired = 24;

                        break;

                    case "earth":

                        staminaRequired = 12;

                        break;

                    default:

                        staminaRequired = 0;

                        break;

                };

                // check player has enough energy for eventual costs
                if (Game1.player.Stamina <= staminaRequired)
                {

                    bool sashimiPower = false;

                    if (Config.castBuffs)
                    {

                        StardewValley.Object sashimiObject = new(227, 1);

                        int sashimiIndex = Game1.player.getIndexOfInventoryItem(sashimiObject);

                        if (sashimiIndex >= 0 && sashimiIndex <= 11)
                        {

                            Game1.player.Items[sashimiIndex].Stack -= 1;

                            if (Game1.player.Items[sashimiIndex].Stack <= 0)
                            {
                                Game1.player.Items[sashimiIndex] = null;

                            }

                            float num2 = Game1.player.Stamina;

                            int num3 = Game1.player.health;

                            int num4 = @sashimiObject.staminaRecoveredOnConsumption();

                            Game1.player.Stamina = Math.Min(Game1.player.MaxStamina, Game1.player.Stamina + (float)num4);

                            Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + @sashimiObject.healthRecoveredOnConsumption());

                            Game1.addHUDMessage(new HUDMessage("Sashimi", 4));

                            sashimiPower = true;

                        }

                    }

                    if(!sashimiPower)
                    {

                        if (activeData.castLevel >= 1)
                        {
                            Game1.addHUDMessage(new HUDMessage("Not enough energy to continue rite", 3));

                        }
                        else
                        {
                            Game1.addHUDMessage(new HUDMessage("Not enough energy to perform rite", 3));

                        }

                        activeData.castComplete = true;

                        return;
                    }

                }

                if (activeData.chargeAmount == 0 || activeData.chargeAmount % 12 == 0)
                {

                    //ModUtility.AnimateHands(Game1.player, activeData.activeDirection, 225);

                }

                //activeData.activeKey = buttonHeld.ToString();

                chargeEffect = true;

                activeData.chargeAmount++;

                int chargeLevel = 4;

                if (activeData.chargeAmount <= 40)
                {

                    chargeLevel = 1;

                }
                else if (activeData.chargeAmount <= 80)
                {

                    chargeLevel = 2;

                }
                else if (activeData.chargeAmount <= 120)
                {

                    chargeLevel = 3;

                }
                else if (activeData.chargeAmount == 160)
                {

                    if (activeData.activeCast == "water")
                    {

                        activeData.activeDirection = -1;

                    }

                    activeData.chargeAmount = 0;

                    activeData.cycleLevel++;

                }

                if (activeData.chargeLevel != chargeLevel && activeData.activeCast != "water")
                {

                    activeData.activeDirection = -1;

                }

                activeData.chargeLevel = chargeLevel;

                // check direction player is facing
                if (activeData.activeDirection == -1)
                {

                    activeData.activeDirection = Game1.player.FacingDirection;

                    activeData.activeVector = Game1.player.getTileLocation();

                    //Monitor.Log($"{activeData.activeCast},{activeData.activeVector}", LogLevel.Debug);

                    if (activeData.activeCast == "water")
                    {

                        Dictionary<int, Vector2> vectorIndex = new()
                        {

                            [0] = activeData.activeVector + new Vector2(0, -5),// up
                            [1] = activeData.activeVector + new Vector2(5, 0), // right
                            [2] = activeData.activeVector + new Vector2(0, 5),// down
                            [3] = activeData.activeVector + new Vector2(-5, 0), // left

                        };

                        activeData.activeVector = vectorIndex[activeData.activeDirection];

                    }

                }

            }

            // Active charge requires engaged input
            if(!chargeEffect)
            {

                activeData.castComplete = true;

                return;

            }

            if (activeData.castLevel != activeData.chargeLevel)
            {

                activeData.castLevel = activeData.chargeLevel;

                Rite castRite = new()
                {

                    //castLevel = activeData.castLevel++,
                    castLevel = activeData.castLevel,

                    direction = activeData.activeDirection,

                };

                switch (activeData.activeCast)
                {

                    case "stars":

                        castRite.castType = "CastStars";

                        ModUtility.AnimateStarsCast(activeData.activeVector, activeData.chargeLevel, activeData.cycleLevel);

                        break;

                    case "water":

                        castRite.castType = "CastWater";

                        castRite.castVector = activeData.activeVector;

                        ModUtility.AnimateWaterCast(activeData.activeVector, activeData.chargeLevel, activeData.cycleLevel);

                        break;

                    default: //"earth"

                        ModUtility.AnimateEarthCast(activeData.activeVector,activeData.chargeLevel,activeData.cycleLevel);

                        break;

                }

                riteQueue.Enqueue(castRite);

                DelayedAction.functionAfterDelay(CastRite, 666);

            }

        }

        private void CastRite()
        { 
            
            if(riteQueue.Count != 0)
            {

                Rite rite = riteQueue.Dequeue();

                switch(rite.castType)
                {

                    case "CastStars":

                        CastStars(rite); break;

                    case "CastWater":

                        CastWater(rite); break;

                    default: //CastEarth

                        CastEarth(rite); break;
                }

            }
        
        }

        private void CastEarth(Rite riteData = null)
        {
            
            if(Config.castBuffs)
            {

                Buff earthBuff = new("Earthen Magnetism", 6000, "Rite of the Earth", 8);

                earthBuff.buffAttributes[8] = 8;

                earthBuff.which = 184651;

                //earthBuff.glow = Color.Green;

                if (!Game1.buffsDisplay.hasBuff(184651))
                {

                    Game1.buffsDisplay.addOtherBuff(earthBuff);

                }

            }

            int castCost = 0;

            riteData ??= new();

            List<Vector2> removeVectors = new();

            Dictionary<Vector2, Cast.Cast> effectCasts = new();

            Layer backLayer = riteData.castLocation.Map.GetLayer("Back");

            Layer buildingLayer = riteData.castLocation.Map.GetLayer("Buildings");

            int blessingLevel = staticData.blessingList["earth"];

            int rockfallChance = 12 - Math.Max(8, riteData.caster.MiningLevel);

            List<Vector2> clumpIndex;

            if (!earthCasts.ContainsKey(riteData.castLocation.Name))
            {
                earthCasts[riteData.castLocation.Name] = new();

            };

            int castRange;

            for (int i = 0; i < 2; i++)
            {

                castRange = (riteData.castLevel * 2) - 1 + i;

                List<Vector2> tileVectors = ModUtility.GetTilesWithinRadius(riteData.castLocation, riteData.castVector, castRange);

                if (riteData.castLocation is Farm)
                {
                    
                    foreach (Vector2 tileVector in tileVectors)
                    {

                        Microsoft.Xna.Framework.Rectangle tileRectangle = new((int)tileVector.X * 64 -16, (int)tileVector.Y * 64 -16, 96, 96);

                        Farm farmLocation = riteData.castLocation as Farm;

                        foreach (KeyValuePair<long, FarmAnimal> pair in farmLocation.animals.Pairs)
                        {

                            if(!pair.Value.wasPet.Value)
                            {
                                
                                if (pair.Value.GetBoundingBox().Intersects(tileRectangle) || pair.Value.nextPosition(pair.Value.getDirection()).Intersects(tileRectangle))
                                {

                                    effectCasts[tileVector] = new Cast.PetAnimal(this, tileVector, riteData, pair.Value);

                                    //npcCollision = true;

                                }
                            
                            }
                        
                        }

                    }
               
                }

                if(riteData.castLocation.characters.Count > 1)
                {

                    foreach (Vector2 tileVector in tileVectors)
                    {

                        Microsoft.Xna.Framework.Rectangle tileRectangle = new((int)tileVector.X * 64 - 16, (int)tileVector.Y * 64 - 16, 96, 96);

                        Farm farmLocation = riteData.castLocation as Farm;

                        foreach (NPC riteWitness in riteData.castLocation.characters)
                        {

                            if (riteWitness is Pet)
                            {

                                if (riteWitness.GetBoundingBox().Intersects(tileRectangle) || riteWitness.nextPosition(riteWitness.getDirection()).Intersects(tileRectangle))
                                {

                                    Pet petPet = (Pet)riteWitness;

                                    petPet.checkAction(riteData.caster, riteData.castLocation);

                                }

                            }
                            else if (riteWitness.isVillager())
                            {
                                
                                if (!riteData.caster.hasPlayerTalkedToNPC(riteWitness.Name))
                                {

                                    if (riteWitness.GetBoundingBox().Intersects(tileRectangle) || riteWitness.nextPosition(riteWitness.getDirection()).Intersects(tileRectangle))
                                    {

                                        effectCasts[tileVector] = new Cast.GreetVillager(this, tileVector, riteData, riteWitness);

                                        //npcCollision = true;

                                    }

                                }

                            }

                        }

                    }

                }

                foreach (Vector2 tileVector in tileVectors)
                {

                    if (earthCasts[riteData.castLocation.Name].Contains(tileVector) || removeVectors.Contains(tileVector)) // already served
                    {

                        continue;

                    }
                    else
                    {

                        earthCasts[riteData.castLocation.Name].Add(tileVector);

                    }

                    int tileX = (int)tileVector.X;

                    int tileY = (int)tileVector.Y;

                    Tile buildingTile = buildingLayer.PickTile(new Location(tileX * 64, tileY * 64), Game1.viewport.Size);

                    if (buildingTile != null)
                    {
                        
                        if (riteData.castLocation.Name.Contains("Beach"))
                        {

                            List<int> tidalList = new() { 60, 61, 62, 63, 77, 78, 79, 80, 94, 95, 96, 97, 104, 287, 288, 304, 305, 321, 362, 363 };

                            if (tidalList.Contains(buildingTile.TileIndex))
                            {

                                effectCasts[tileVector] = new Cast.Pool(this, tileVector, riteData);

                            }

                        }
                        
                        if (buildingTile.TileIndexProperties.TryGetValue("Passable", out _) == false)
                        {

                            continue;

                        }

                    }

                    if (riteData.castLocation.objects.Count() > 0)
                    {

                        if (riteData.castLocation.objects.ContainsKey(tileVector))
                        {

                            if (blessingLevel >= 1)
                            {

                                StardewValley.Object tileObject = riteData.castLocation.objects[tileVector];

                                if (tileObject.name.Contains("Stone"))
                                {

                                    if (Map.SpawnData.StoneIndex().Contains(tileObject.ParentSheetIndex))
                                    {

                                        effectCasts[tileVector] = new Cast.Weed(this, tileVector, riteData);

                                    }


                                }
                                else if (tileObject.name.Contains("Weeds") || tileObject.name.Contains("Twig"))
                                {

                                    effectCasts[tileVector] = new Cast.Weed(this, tileVector, riteData);

                                }

                            }

                            continue;

                        }

                    }

                    if (riteData.castLocation.terrainFeatures.ContainsKey(tileVector))
                    {

                        if (blessingLevel >= 2) {

                            TerrainFeature terrainFeature = riteData.castLocation.terrainFeatures[tileVector];

                            switch (terrainFeature.GetType().Name.ToString())
                            {

                                case "FruitTree":

                                    StardewValley.TerrainFeatures.FruitTree fruitFeature = terrainFeature as StardewValley.TerrainFeatures.FruitTree;

                                    if (fruitFeature.growthStage.Value >= 4)
                                    {

                                        effectCasts[tileVector] = new Cast.FruitTree(this, tileVector, riteData);

                                    }
                                    else if (staticData.blessingList["earth"] >= 4)
                                    {

                                        effectCasts[tileVector] = new Cast.FruitSapling(this, tileVector, riteData);

                                    }
                                    
                                    break;

                                case "Tree":

                                    StardewValley.TerrainFeatures.Tree treeFeature = terrainFeature as StardewValley.TerrainFeatures.Tree;

                                    if (treeFeature.growthStage.Value >= 5)
                                    {

                                        effectCasts[tileVector] = new Cast.Tree(this, tileVector, riteData);

                                    }
                                    else if(staticData.blessingList["earth"] >= 4 && treeFeature.fertilized.Value == false)
                                    {

                                        effectCasts[tileVector] = new Cast.Sapling(this, tileVector, riteData);

                                    }

                                    break;

                                case "Grass":

                                    /*Cast.Grass effectGrass = new(this, tileVector, riteData);

                                    if (!Game1.currentSeason.Equals("winter") && riteData.spawnIndex["cropseed"] && staticData.blessingList["earth"] >= 4)
                                    {

                                        effectGrass.activateSeed = true;

                                    }

                                    effectCasts[tileVector] = effectGrass;*/

                                    effectCasts[tileVector] = new Cast.Grass(this, tileVector, riteData);

                                    break;

                                case "HoeDirt":

                                    if (staticData.blessingList["earth"] >= 4)
                                    {

                                        if (!Game1.currentSeason.Equals("winter") && riteData.spawnIndex["cropseed"])
                                        {

                                            effectCasts[tileVector] = new Cast.Crop(this, tileVector, riteData);

                                        }

                                    }

                                    break;

                                default:

                                    break;

                            }

                        }

                        continue;

                    }

                    if (riteData.castLocation.resourceClumps.Count > 0)
                    {

                        bool targetClump = false;

                        clumpIndex = new()
                        {
                            tileVector,
                            tileVector + new Vector2(0,-1),
                            tileVector + new Vector2(-1,0),
                            tileVector + new Vector2(-1,-1)

                        };

                        foreach (ResourceClump resourceClump in riteData.castLocation.resourceClumps)
                        {

                            foreach (Vector2 originVector in clumpIndex)
                            {

                                if (resourceClump.tile.Value == originVector)
                                {

                                    if (blessingLevel >= 2)
                                    {

                                        switch (resourceClump.parentSheetIndex.Value)
                                        {

                                            case 600:
                                            case 602:

                                                effectCasts[tileVector] = new Cast.Stump(this, tileVector, riteData, resourceClump, "Farm");

                                                break;

                                            default:

                                                effectCasts[tileVector] = new Cast.Boulder(this, tileVector, riteData, resourceClump);

                                                break;

                                        }

                                    }

                                    Vector2 clumpVector = resourceClump.tile.Value;

                                    removeVectors.Add(new Vector2(clumpVector.X + 1, clumpVector.Y));

                                    removeVectors.Add(new Vector2(clumpVector.X, clumpVector.Y + 1));

                                    removeVectors.Add(new Vector2(clumpVector.X + 1, clumpVector.Y + 1));

                                    targetClump = true;

                                    continue;

                                }

                            }

                        }

                        if (targetClump)
                        {

                            continue;

                        }

                    }

                    if (riteData.castLocation is Woods)
                    {
                        if (blessingLevel >= 2)
                        {
                            Woods woodsLocation = riteData.castLocation as Woods;

                            foreach (ResourceClump resourceClump in woodsLocation.stumps)
                            {

                                if (resourceClump.tile.Value == tileVector)
                                {

                                    effectCasts[tileVector] = new Cast.Stump(this, tileVector, riteData, resourceClump, "Woods");

                                    Vector2 clumpVector = tileVector;

                                    removeVectors.Add(new Vector2(clumpVector.X + 1, clumpVector.Y));

                                    removeVectors.Add(new Vector2(clumpVector.X, clumpVector.Y + 1));

                                    removeVectors.Add(new Vector2(clumpVector.X + 1, clumpVector.Y + 1));

                                }

                            }

                        }

                    }

                    if (riteData.castLocation.largeTerrainFeatures.Count > 0)
                    {

                        bool targetTerrain = false;

                        Microsoft.Xna.Framework.Rectangle tileRectangle = new(tileX * 64 + 1, tileY * 64 + 1, 62, 62);

                        foreach (LargeTerrainFeature largeTerrainFeature in riteData.castLocation.largeTerrainFeatures)
                        {
                            if (largeTerrainFeature.getBoundingBox().Intersects(tileRectangle))
                            {

                                if (blessingLevel >= 2)
                                {

                                    effectCasts[tileVector] = new Cast.Bush(this, tileVector, riteData, largeTerrainFeature);

                                }

                                targetTerrain = true;

                                break;

                            }
                        }

                        if (targetTerrain)
                        {
                            continue;
                        }

                    }

                    foreach (Furniture item in riteData.castLocation.furniture)
                    {

                        if (item.boundingBox.Value.Contains(tileX * 64, tileY * 64))
                        {

                            continue;

                        }

                    }

                    if (riteData.castLocation is MineShaft)
                    {

                        MineShaft mineShaft = riteData.castLocation as MineShaft;

                        if (blessingLevel >= 5 && mineShaft.isTileOnClearAndSolidGround(tileX,tileY))
                        {
                            int probability = Game1.random.Next(rockfallChance);

                            if (probability == 0)
                            {

                                effectCasts[tileVector] = new Cast.Rockfall(this, tileVector, riteData);

                            }

                        }

                        continue;

                    }

                    Tile backTile = backLayer.PickTile(new Location(tileX * 64, tileY * 64), Game1.viewport.Size);

                    if (backTile != null)
                    {

                        if (backTile.TileIndexProperties.TryGetValue("Water", out _))
                        {

                            if (blessingLevel >= 2)
                            {

                                if (riteData.spawnIndex["fishup"])
                                {

                                    if(riteData.castLocation.Name.Contains("Farm"))
                                    {

                                        effectCasts[tileVector] = new Cast.Pool(this, tileVector, riteData);

                                    }
                                    else
                                    {

                                        effectCasts[tileVector] = new Cast.Water(this, tileVector, riteData);

                                    }

                                }

                            }

                            continue;

                        }

                        if (blessingLevel >= 3)
                        {
                            
                            if (backTile.TileIndexProperties.TryGetValue("Type", out var typeValue))
                            {

                                if (typeValue == "Dirt" || backTile.TileIndexProperties.TryGetValue("Diggable", out _))
                                {

                                    effectCasts[tileVector] = new Cast.Dirt(this, tileVector, riteData);

                                    continue;

                                }

                                if (typeValue == "Grass" && backTile.TileIndexProperties.TryGetValue("NoSpawn", out _) == false)
                                {

                                    effectCasts[tileVector] = new Cast.Lawn(this, tileVector, riteData);

                                    continue;

                                }

                            }

                        }

                    }

                }

                foreach (Vector2 tileVector in tileVectors)
                {

                    ModUtility.AnimateCastRadius(riteData.castLocation, tileVector, new Color(0.8f, 1f, 0.8f, 1), i);

                }

            }

            //-------------------------- fire effects

            List<Type> castLimits = new();

            if (effectCasts.Count != 0)
            {
                foreach (KeyValuePair<Vector2, Cast.Cast> effectEntry in effectCasts)
                {

                    if (removeVectors.Contains(effectEntry.Key)) // ignore tiles covered by clumps
                    {

                        continue;

                    }

                    
                    Cast.Cast effectHandle = effectEntry.Value;

                    Type effectType = effectHandle.GetType();

                    if (castLimits.Contains(effectType))
                    {

                        continue;

                    }

                    effectHandle.CastEarth();

                    if (effectHandle.castFire)
                    {

                        castCost += effectHandle.castCost;

                        //experienceGain++;

                    }

                    if (effectHandle.castLimit)
                    {

                        castLimits.Add(effectEntry.Value.GetType());

                    }

                    if (effectHandle.castActive)
                    {

                        ActiveCast(effectHandle);

                    }

                }

            }

            //-------------------------- effect on player

            if (castCost > 0)
            {

                float oldStamina = Game1.player.Stamina;

                float staminaCost = Math.Min(castCost, oldStamina - 1);

                //float staminaCost = 2;

                Game1.player.Stamina -= staminaCost;

                Game1.player.checkForExhaustion(oldStamina);

            }

            return;

        }

        private void CastWater(Rite riteData = null)
        {

            //-------------------------- tile effects

            int castCost = 0;

            riteData ??= new();

            Vector2 castVector = riteData.castVector;

            List <Vector2> tileVectors;

            List<Vector2> removeVectors = new();

            Dictionary<Vector2, Cast.Cast> effectCasts = new();

            Layer backLayer = riteData.castLocation.Map.GetLayer("Back");

            Layer buildingLayer = riteData.castLocation.Map.GetLayer("Buildings");

            int blessingLevel = staticData.blessingList["water"];

            string locationName = riteData.castLocation.Name.ToString();

            int castRange; 

            for (int i = 0; i < 2; i++) {

                castRange = (riteData.castLevel * 2) - 2 + i;

                tileVectors = ModUtility.GetTilesWithinRadius(riteData.castLocation, castVector, castRange);

                foreach (Vector2 tileVector in tileVectors)
                {

                    int tileX = (int)tileVector.X;

                    int tileY = (int)tileVector.Y;

                    if (blessingLevel >= 1)
                    {
                        if (warpPoints.ContainsKey(locationName))
                        {

                            if (warpPoints[locationName] == tileVector)
                            {

                                if (warpCasts.Contains(locationName))
                                {

                                    Game1.addHUDMessage(new HUDMessage($"Already extracted {locationName} warp power today", 3));

                                    continue;

                                }

                                int targetIndex = warpTotems[locationName];

                                effectCasts[tileVector] = new Cast.Totem(this, tileVector, riteData, targetIndex);

                                warpCasts.Add(locationName);

                                continue;

                            }

                        }

                    }

                    if (blessingLevel >= 1)
                    {
                        if (firePoints.ContainsKey(locationName))
                        {

                            if (firePoints[locationName] == tileVector)
                            {

                                if (fireCasts.Contains(locationName))
                                {

                                    Game1.addHUDMessage(new HUDMessage($"Already ignited {locationName} camp fire today", 3));

                                    continue;

                                }

                                effectCasts[tileVector] = new Cast.Campfire(this, tileVector, riteData);

                                fireCasts.Add(locationName);
                                
                                continue;

                            }

                        }

                    }

                    if (riteData.castLocation.objects.Count() > 0)
                    {

                        if (riteData.castLocation.objects.ContainsKey(tileVector))
                        {
                            
                            StardewValley.Object targetObject = riteData.castLocation.objects[tileVector];

                            if (riteData.castLocation.IsFarm && targetObject.bigCraftable.Value && targetObject.ParentSheetIndex == 9)
                            {

                                if (warpCasts.Contains("rod"))
                                {

                                    Game1.addHUDMessage(new HUDMessage("Already powered a lightning rod today", 3));

                                }
                                else if (blessingLevel >= 2)
                                {

                                    effectCasts[tileVector] = new Cast.Rod(this, tileVector, riteData);

                                    warpCasts.Add("rod");

                                }

                            }
                            else if (targetObject.Name.Contains("Campfire"))
                            {

                                string fireLocation = riteData.castLocation.Name;

                                if (fireCasts.Contains(fireLocation))
                                {

                                    //if (fireLocation != "Beach")
                                    //{
                                        Game1.addHUDMessage(new HUDMessage($"Already ignited {fireLocation} camp fire today", 3));

                                    //}

                                }
                                else if (blessingLevel >= 2)
                                {

                                    effectCasts[tileVector] = new Cast.Campfire(this, tileVector, riteData);

                                    fireCasts.Add(fireLocation);

                                }
                            }
                            else if (targetObject is Torch && targetObject.ParentSheetIndex == 93) // crafted candle torch
                            {
                                if (blessingLevel >= 5 && activeLockout != "trigger")
                                {
                                    if (riteData.spawnIndex["wilderness"])
                                    {

                                        effectCasts[tileVector] = new Cast.Portal(this, tileVector, riteData);

                                    }

                                }

                            }
                            else if (targetObject.IsScarecrow())
                            {
                                
                                string scid = "scarecrow_" + tileVector.X.ToString() + "_" + tileVector.Y.ToString();

                                if (blessingLevel >= 2 && !Game1.isRaining && !warpCasts.Contains(scid))
                                {
                                    
                                    effectCasts[tileVector] = new Cast.Scarecrow(this, tileVector, riteData);

                                    warpCasts.Add(scid);

                                }
                            
                            }

                            continue;

                        }

                    }

                    Tile buildingTile = buildingLayer.PickTile(new Location(tileX * 64, tileY * 64), Game1.viewport.Size);

                    if (buildingTile != null)
                    {
                        if (riteData.castLocation is Woods && !warpCasts.Contains("woods") && activeLockout == "none")
                        {

                            int tileIndex = buildingTile.TileIndex;

                            if ((uint)(tileIndex - 1140) <= 1u)
                            {

                                effectCasts[tileVector] = new Cast.WoodsStatue(this, tileVector, riteData);

                                warpCasts.Add("woods");

                                activeLockout = "trigger";

                            }

                        }

                        continue;
                    
                    }

                    if (riteData.castLocation.terrainFeatures.ContainsKey(tileVector))
                    {
                        
                        continue;

                    }

                    if(riteData.castLocation is Forest)
                    {

                        if (blessingLevel >= 1)
                        {

                            Forest forestLocation = riteData.castLocation as Forest;

                            if (forestLocation.log != null && forestLocation.log.tile.Value == tileVector)
                            {

                                effectCasts[tileVector] = new Cast.Stump(this, tileVector, riteData, forestLocation.log, "Log");

                                Vector2 clumpVector = tileVector;

                                removeVectors.Add(new Vector2(clumpVector.X + 1, clumpVector.Y));

                                removeVectors.Add(new Vector2(clumpVector.X, clumpVector.Y + 1));

                                removeVectors.Add(new Vector2(clumpVector.X + 1, clumpVector.Y + 1));

                            }

                        }

                    }

                    if(riteData.castLocation is Woods)
                    {
                        if (blessingLevel >= 1)
                        {
                            Woods woodsLocation = riteData.castLocation as Woods;

                            foreach (ResourceClump resourceClump in woodsLocation.stumps)
                            {

                                if (resourceClump.tile.Value == tileVector)
                                {

                                    effectCasts[tileVector] = new Cast.Stump(this, tileVector, riteData, resourceClump, "Woods");

                                    Vector2 clumpVector = tileVector;

                                    removeVectors.Add(new Vector2(clumpVector.X + 1, clumpVector.Y));

                                    removeVectors.Add(new Vector2(clumpVector.X, clumpVector.Y + 1));

                                    removeVectors.Add(new Vector2(clumpVector.X + 1, clumpVector.Y + 1));

                                }

                            }

                        }

                    }

                    if (riteData.castLocation.resourceClumps.Count > 0)
                    {

                        bool targetClump = false;

                        foreach (ResourceClump resourceClump in riteData.castLocation.resourceClumps)
                        {

                            if (resourceClump.tile.Value == tileVector)
                            {

                                if (blessingLevel >= 1)
                                {

                                    switch (resourceClump.parentSheetIndex.Value)
                                    {

                                        case 600:
                                        case 602:

                                            effectCasts[tileVector] = new Cast.Stump(this, tileVector, riteData, resourceClump, "Farm");

                                            break;

                                        default:

                                            effectCasts[tileVector] = new Cast.Boulder(this, tileVector, riteData, resourceClump);

                                            break;

                                    }

                                    Vector2 clumpVector = tileVector;

                                    removeVectors.Add(new Vector2(clumpVector.X + 1, clumpVector.Y));

                                    removeVectors.Add(new Vector2(clumpVector.X, clumpVector.Y + 1));

                                    removeVectors.Add(new Vector2(clumpVector.X + 1, clumpVector.Y + 1));

                                }

                                targetClump = true;

                                continue;

                            }

                        }

                        if (targetClump)
                        {

                            continue;

                        }

                    }

                    foreach (Furniture item in riteData.castLocation.furniture)
                    {

                        if (item.boundingBox.Value.Contains(tileX * 64, tileY * 64))
                        {

                            continue;

                        }

                    }

                    if (riteData.castLevel == 1 && riteData.spawnIndex["fishspot"])
                    { 
                        
                        Tile backTile = backLayer.PickTile(new Location(tileX * 64, tileY * 64), Game1.viewport.Size);

                        if (backTile != null)
                        {

                            if (backTile.TileIndexProperties.TryGetValue("Water", out _))
                            {
                                if (blessingLevel >= 3)
                                {

                                    Dictionary<int, Vector2> portalOffsets = new()
                                    {

                                        [0] = activeData.activeVector + new Vector2(0, -64),// up
                                        [1] = activeData.activeVector + new Vector2(64, 0), // right
                                        [2] = activeData.activeVector + new Vector2(0, 64),// down
                                        [3] = activeData.activeVector + new Vector2(-64, 0), // left

                                    };

                                    Cast.Water waterPortal = new(this, tileVector, riteData)
                                    {
                                        portalPosition = new Vector2(tileVector.X * 64, tileVector.Y * 64) + portalOffsets[riteData.direction]
                                    };

                                    effectCasts[tileVector] = waterPortal;

                                }

                                continue;

                            }

                        }

                    }

                }

                foreach (Vector2 tileVector in tileVectors) {

                    ModUtility.AnimateCastRadius(riteData.castLocation, tileVector, new Color(0.8f,0.8f,1f,1),i);

                }

            }

            if (blessingLevel >= 4)
            {
                Vector2 smiteVector = riteData.caster.getTileLocation();

                Microsoft.Xna.Framework.Rectangle castArea = new(
                    ((int)castVector.X - riteData.castLevel) * 64,
                    ((int)castVector.Y - riteData.castLevel) * 64,
                    (riteData.castLevel * 128) + 64,
                    (riteData.castLevel * 128) + 64
                );


                Microsoft.Xna.Framework.Rectangle smiteArea = new(
                    ((int)smiteVector.X - riteData.castLevel) * 64,
                    ((int)smiteVector.Y - riteData.castLevel) * 64,
                    (riteData.castLevel * 128) + 64,
                    (riteData.castLevel * 128) + 64
                );

                int smiteCount = 0;

                foreach (NPC nonPlayableCharacter in riteData.castLocation.characters)
                {

                    if (nonPlayableCharacter is Monster)
                    {

                        Monster monsterCharacter = nonPlayableCharacter as Monster;

                        if (monsterCharacter.Health > 0 && !monsterCharacter.IsInvisible && !monsterCharacter.isInvincible() && (monsterCharacter.GetBoundingBox().Intersects(castArea) || monsterCharacter.GetBoundingBox().Intersects(smiteArea)))
                        {
                            Vector2 monsterVector = monsterCharacter.getTileLocation();

                            effectCasts[monsterVector] = new Cast.Smite(this, monsterVector, riteData, monsterCharacter);

                            smiteCount++;

                        } 

                    }

                    if (smiteCount == 2)
                    {
                        break;
                    }

                }

            }

            //-------------------------- fire effects

            List<Type> castLimits = new();

            if (effectCasts.Count != 0)
            {
                foreach (KeyValuePair<Vector2, Cast.Cast> effectEntry in effectCasts)
                {

                    if (removeVectors.Contains(effectEntry.Key)) // ignore tiles covered by clumps
                    {

                        continue;

                    }

                    Cast.Cast effectHandle = effectEntry.Value;

                    Type effectType = effectHandle.GetType();

                    if (castLimits.Contains(effectType))
                    {

                        continue;

                    }

                    effectHandle.CastWater();

                    if (effectHandle.castFire)
                    {

                        castCost += effectEntry.Value.castCost;

                    }

                    if (effectHandle.castLimit)
                    {

                        castLimits.Add(effectEntry.Value.GetType());

                    }

                    if(effectHandle.castActive)
                    {

                        ActiveCast(effectHandle);

                    }

                }

            }

            //-------------------------- effect on player

            if (castCost > 0)
            {

                float oldStamina = Game1.player.Stamina;

                float staminaCost = Math.Min(castCost, oldStamina - 1);

                //staminaCost = 18;

                Game1.player.Stamina -= staminaCost;

                Game1.player.checkForExhaustion(oldStamina);

            }

        }

        private void CastStars(Rite riteData = null)
        {

            if (staticData.blessingList["stars"] < 1)
            {

                return;

            }

            //-------------------------- tile effects

            int castCost = 0;

            riteData ??= new();

            //-------------------------- cast sound

            Game1.currentLocation.playSound("fireball");

            Random randomIndex = new();

            Dictionary<Vector2, Cast.Meteor> effectCasts = new();

            int castAttempt = riteData.castLevel + 1;

            List<Vector2> castSelection = ModUtility.GetTilesWithinRadius(riteData.castLocation, riteData.castVector, 2 + castAttempt);

            int castSelect = castSelection.Count;

            if(castSelect != 0)
            {

                int castIndex;

                Vector2 newVector;

                int castSegment = castSelect / castAttempt;

                if(castAttempt == 5)
                {

                    castAttempt = 4;

                }

                for (int i = 0; i < castAttempt; i++)
                {

                    castIndex = randomIndex.Next(castSegment) + (i * castSegment);

                    newVector = castSelection[castIndex];

                    effectCasts[newVector] = new Cast.Meteor(this, newVector, riteData);

                }

            }
            //-------------------------- fire effects

            if (effectCasts.Count != 0)
            {
                foreach (KeyValuePair<Vector2, Cast.Meteor> effectEntry in effectCasts)
                {

                    Cast.Meteor effectHandle = effectEntry.Value;

                    effectHandle.CastStars();

                    if (effectHandle.castFire)
                    {

                        castCost += effectEntry.Value.castCost;

                    }

                }

            }

            //-------------------------- effect on player

            if (castCost > 0)
            {

                float oldStamina = Game1.player.Stamina;

                float staminaCost = Math.Min(castCost, oldStamina - 1);

                Game1.player.Stamina -= staminaCost;

                Game1.player.checkForExhaustion(oldStamina);

            }

        }

        /*public int SpecialLimit()
        {

            if (specialCasts == (specialLimit * 6))
            {

                Game1.addHUDMessage(new HUDMessage("Special power has almost completely dissipated", 2));

            }

            decimal limit = specialCasts / specialLimit;

            return (int) Math.Floor(limit);

        }

        public void SpecialIncrement()
        {

            specialCasts++;

            if (specialCasts == (specialLimit * 3))
            {

                Game1.addHUDMessage(new HUDMessage("Special power starts to wane", 2));

            }

        }*/

        public string ActiveBlessing()
        {

            return staticData.activeBlessing;

        }

        public Dictionary<string, int> BlessingList()
        {

            return staticData.blessingList;

        }

        public void UpdateBlessing(string blessing)
        {

            staticData.activeBlessing = blessing;

            if(!staticData.blessingList.ContainsKey(blessing))
            {

                staticData.blessingList[blessing] = 0;

            }

        }

        public void LevelBlessing(string blessing)
        {

            staticData.blessingList[blessing] += 1;

        }

        public void ActiveCast(Cast.Cast castHandle)
        {

            Type handleType = castHandle.GetType();

            if (!activeCasts.ContainsKey(handleType))
            {

                activeCasts[handleType] = new();

            }

            activeCasts[handleType].Add(castHandle);

        }

        public bool QuestComplete(string quest)
        {

            if(staticData.questList.ContainsKey(quest))
            {

                return staticData.questList[quest];

            }

            return false;

        }

        public List<string> QuestList()
        {

            List<string> received = new();

            foreach (KeyValuePair<string, bool> challenge in staticData.questList)
            {

                received.Add(challenge.Key);

            }

            return received;

        }

        public void NewQuest(string quest)
        {
            
            staticData.questList[quest] = false;

            ReassignQuest(quest);

        }

        public bool QuestGiven(string quest)
        {

            return staticData.questList.ContainsKey(quest);

        }

        public void UpdateQuest(string quest, bool update)
        {

            if(!update)
            {

                staticData.questList[quest] = false;

                ReassignQuest(quest);

            }
            else
            {

                staticData.questList[quest] = true;

                RemoveQuest(quest);

            }

        }

        public void ReassignQuest(string quest)
        {

            Map.Quest questData = questIndex[quest];

            if (questData.questId != 0)
            {

                if(!Game1.player.hasQuest(questData.questId))
                {

                    StardewValley.Quests.Quest newQuest = new();

                    newQuest.questType.Value = questData.questValue;
                    newQuest.id.Value = questData.questId;
                    newQuest.questTitle = questData.questTitle;
                    newQuest.questDescription = questData.questDescription;
                    newQuest.currentObjective = questData.questObjective;
                    newQuest.showNew.Value = true;
                    newQuest.moneyReward.Value = questData.questReward;

                    Game1.player.questLog.Add(newQuest);

                }

            }

            if (questData.triggerCast != null)
            {

                if(!staticData.triggerList.Contains(quest))
                {
                    
                    staticData.triggerList.Add(quest);

                }

            }

        }

        public void RemoveQuest(string quest)
        {

            Map.Quest questData = questIndex[quest];

            if (questData.questId != 0)
            {

                Game1.player.completeQuest(questData.questId);

            }

            if (questData.triggerCast != null)
            {

                staticData.triggerList.Remove(quest);


            }

            if (questData.updateEffigy == true)
            {

                druidEffigy.questCompleted = quest;

            }

        }

        public void RemoveTrigger(string trigger)
        {

            staticData.triggerList.Remove(trigger);

        }

        public void UpdateEarthCasts(GameLocation location, Vector2 vector, bool update)
        {
            
            if (!earthCasts.ContainsKey(location.Name))
            {

                earthCasts.Add(location.Name, new());
            
            }

            if (!earthCasts[location.Name].Contains(vector) && update)
            {

                earthCasts[location.Name].Add(vector);

            }

            if (earthCasts[location.Name].Contains(vector) && !update)
            {

                earthCasts[location.Name].Remove(vector);

            }

        }

        public string CastControl()
        {

            return Config.riteButtons.ToString();

        }

        public void ResetMagnetism()
        {

            if (activeData.castComplete || activeData.activeCast != "earth")
            {
                
                Game1.player.MagneticRadius -= 10;

            }

        }

        public void MonsterTrack(GameLocation location, Monster monster)
        {
            if (!monsterSpawns.ContainsKey(location.Name))
            {

                monsterSpawns[location.Name] = new();

            }

            monsterSpawns[location.Name].Add(monster);
        
        }

        public void UnlockAll()
        {

            staticData.blessingList = new()
            {
                ["earth"] = 5,
                ["water"] = 5,
                ["stars"] = 1,
            };

            staticData.activeBlessing = "earth";

        }

        public Axe RetrieveAxe() { return targetAxe; }

        public Pickaxe RetrievePick() { return targetPick; }

        public void ToggleEffect(string effect)
        {

            if (staticData.blessingList.ContainsKey(effect))
            {
                
                staticData.blessingList.Remove(effect);

            }
            else
            {

                staticData.blessingList[effect] = 1;

            }

        }

        public bool ForgotEffect(string effect)
        {

            if (staticData.blessingList.ContainsKey(effect))
            {

                return true;

            }

            return false;

        }

    }

}

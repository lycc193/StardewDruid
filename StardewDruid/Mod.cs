﻿using Microsoft.Xna.Framework;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Schema;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace StardewDruid
{
    public class Mod : StardewModdingAPI.Mod
    {

        private ModData Config;

        private ActiveData activeData;

        private StaticData staticData;

        private List<string> buttonList;

        //private List<string> craftList;

        private Map.Effigy druidEffigy;

        public Dictionary<int, string> TreeTypes;

        public Dictionary<string, List<Vector2>> earthCasts;

        private Dictionary<Type, List<Cast.Cast>> activeCasts;

        //private int specialCasts;

        //private int specialLimit;

        public Dictionary<string, Vector2> warpPoints;

        public Dictionary<string, int> warpTotems;

        public List<string> warpCasts;

        private Dictionary<string, Map.Quest> questIndex;

        private Dictionary<string, List<Monster>> monsterSpawns;

        private Queue<Rite> riteQueue;

        override public void Entry(IModHelper helper)
        {

            helper.Events.GameLoop.SaveLoaded += SaveLoaded;

            helper.Events.Input.ButtonsChanged += OnButtonsChanged;

            helper.Events.GameLoop.OneSecondUpdateTicked += EverySecond;

            helper.Events.GameLoop.Saving += SaveUpdated;

        }

        private void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {

            Config = Helper.ReadConfig<ModData>();

            staticData = Helper.Data.ReadSaveData<StaticData>("staticData");

            staticData ??= new StaticData();

            if (Config.startWithBlessing)
            {

                staticData.questList = Config.questList;

                staticData.blessingList = Config.blessingList;

                staticData.activeBlessing = "earth";

            }

            questIndex = Map.QuestData.QuestList();

            if (staticData.questList.Count == 0)
            {

                string firstQuest = Map.QuestData.FirstQuest();

                staticData.questList[firstQuest] = false;

                ReassignQuest(firstQuest);

            }

            buttonList = Config.buttonList;

            //craftList = Config.craftList;

            earthCasts = new();

            activeCasts = new();

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

            druidEffigy = new(this);

            druidEffigy.ModifyCave();

            warpPoints = Map.WarpData.WarpPoints();

            warpTotems = Map.WarpData.WarpTotems();

            warpCasts = new();

            monsterSpawns = new();

            return;

        }

        private void SaveUpdated(object sender, SavingEventArgs e)
        {

            Helper.Data.WriteSaveData("staticData", staticData);

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

            foreach (Cast.Cast castInstance in activeCast)
            {

                castInstance.CastTrigger();

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

                if (playerLocation.X >= 5 && playerLocation.X <= 7 && playerLocation.Y == 4 && cursorLocation.X == 6 && (cursorLocation.Y == 2 || cursorLocation.Y == 3))
                {

                    foreach (SButton buttonPressed in e.Pressed)
                    {
                        
                        if (buttonPressed.IsUseToolButton() || buttonPressed.IsActionButton())
                        {

                            druidEffigy.Approach(Game1.player);

                            Helper.Input.Suppress(buttonPressed);

                            return;

                        }

                    }

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
                else if (buttonList.Contains(buttonPressed.ToString())) 
                {

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

                        if (questData.triggerLocation == Game1.player.currentLocation.Name)
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

                        return;

                    }

                    // new cast configuration

                    string activeBlessing = staticData.activeBlessing;

                    if (activeBlessing == "none")
                    {

                        //ModUtility.AnimateHands(Game1.player, Game1.player.FacingDirection, 500);

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
                    if(spawnIndex.Count == 0)
                    {
                        
                        if(activeBlessing != "none")
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

            }


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

            // begin to count amount that cast button is held
            foreach (SButton buttonHeld in e.Held)
            {

                // Already in Active State
                if (buttonList.Contains(buttonHeld.ToString()))
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

                    if (activeData.chargeAmount == 0 || activeData.chargeAmount % 12 == 0)
                    {

                        //ModUtility.AnimateHands(Game1.player, activeData.activeDirection, 225);

                    }

                    activeData.activeKey = buttonHeld.ToString();

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

                        if(activeData.activeCast == "water")
                        {

                            activeData.activeDirection = -1;

                        }

                        activeData.chargeAmount = 0;

                        activeData.cycleLevel++;

                    }

                    if(activeData.chargeLevel != chargeLevel && activeData.activeCast != "water")
                    {

                        activeData.activeDirection = -1;

                    }

                    activeData.chargeLevel = chargeLevel;

                    // check direction player is facing
                    if (activeData.activeDirection == -1)
                    {

                        activeData.activeDirection = Game1.player.FacingDirection;

                        activeData.activeVector = Game1.player.getTileLocation();

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

                        AnimateStars();

                        break;

                    case "water":

                        castRite.castType = "CastWater";

                        castRite.castVector = activeData.activeVector;

                        AnimateWater();

                        break;

                    default: //"earth"

                        AnimateEarth();

                        break;

                }

                riteQueue.Enqueue(castRite);

                DelayedAction.functionAfterDelay(CastRite, 666);

            }

        }

        /*private void ReviewQuests()
        {

            if(staticData.questList.Count == 0)
            {

                string firstQuest = Map.Quest.FirstQuest();

                staticData.questList[firstQuest] = false;

                ReassignQuest(firstQuest,true);

            }
            else
            {

                foreach (KeyValuePair<string, bool> quest in staticData.questList)
                {

                    if (!quest.Value)
                    {

                        ReassignQuest(quest.Key);

                    }

                }

            }

        }*/

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

        private void AnimateEarth()
        {

            //-------------------------- cast variables

            int animationRow;

            Microsoft.Xna.Framework.Rectangle animationRectangle;

            float animationInterval;

            int animationLength;

            int animationLoops;

            bool animationFlip;

            float animationScale;

            Microsoft.Xna.Framework.Color animationColor;

            Vector2 animationPosition;

            TemporaryAnimatedSprite newAnimation;

            animationFlip = false;

            float colorIncrement = 1.2f - (0.2f * activeData.chargeLevel);

            animationColor = new(colorIncrement, 1, colorIncrement, 1);

            animationScale = 0.6f + (0.2f * activeData.chargeLevel);

            float vectorCastX = 12 - (6 * activeData.chargeLevel);

            float vectorCastY = 0 - 122 - (6 * activeData.chargeLevel);

            animationPosition = (activeData.activeVector * 64) + new Vector2(vectorCastX, vectorCastY);

            /*switch (activeData.chargeLevel)
            {

                case 1:
                    animationColor = new(uint.MaxValue); // deepens from white to green
                    animationScale = 0.8f;
                    animationPosition = Game1.player.Position + new Vector2(6f, -128f); // 2 tiles above player
                    break;
                case 2:
                    animationColor = new(0.8f, 1, 0.8f, 1);
                    animationScale = 1f;
                    animationPosition = Game1.player.Position + new Vector2(0f, -134f); // 2 tiles above player
                    break;
                default:
                    animationColor = new(0.6f, 1, 0.6f, 1);
                    animationScale = 1.2f;
                    animationPosition = Game1.player.Position + new Vector2(-6f, -140f); // 2 tiles above player
                    break;

            }*/

            //-------------------------- cast animation

            animationRow = 10;

            animationRectangle = new(0, animationRow * 64, 64, 64);

            animationInterval = 75f;

            animationLength = 8;

            animationLoops = 1;

            newAnimation = new("TileSheets\\animations", animationRectangle, animationInterval, animationLength, animationLoops, animationPosition, false, animationFlip, -1, 0f, animationColor, animationScale, 0f, 0f, 0f)
            {
                //motion = new Vector2(0f, -0.1f)

            };

            Game1.currentLocation.temporarySprites.Add(newAnimation);

            //-------------------------- hand animation

            //activeData.animateLevel = activeData.chargeLevel;

            int pitchLevel = activeData.cycleLevel % 4;

            if(activeData.chargeLevel == 1)
            {

                Game1.currentLocation.playSoundPitched("discoverMineral", 600 + (pitchLevel * 200));

            }

            if (activeData.chargeLevel == 3)
            {

                Game1.currentLocation.playSoundPitched("discoverMineral", 700 + (pitchLevel * 200));

            }

        }

        private void CastEarth(Rite riteData = null)
        {

            int castCost = 0;

            riteData ??= new();

            List<Vector2> removeVectors = new();

            Dictionary<Vector2, Cast.Cast> effectCasts = new();

            Layer backLayer = riteData.castLocation.Map.GetLayer("Back");

            Layer buildingLayer = riteData.castLocation.Map.GetLayer("Buildings");

            int blessingLevel = staticData.blessingList["earth"];

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

               // bool npcCollision = false;

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

                //if (npcCollision)
                //{

                    //tileVectors.Clear();

                //}

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

                                                effectCasts[tileVector] = new Cast.Stump(this, tileVector, riteData, resourceClump);

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
                            int probability = Game1.random.Next(10);

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

                    ModUtility.AnimateEarth(riteData.castLocation,effectHandle.targetVector);

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

        private void AnimateWater()
        {


            //-------------------------- cast variables

            int animationRow = 5;

            Microsoft.Xna.Framework.Rectangle animationRectangle = new(0, animationRow * 64, 64, 64);

            float animationInterval = 84f;

            int animationLength = 4;

            int animationLoops = 1;

            bool animationFlip = false;

            float animationScale;

            float animationDepth = activeData.activeVector.X * 1000 + activeData.activeVector.Y;

            Microsoft.Xna.Framework.Color animationColor;

            Vector2 animationPosition;

            //-------------------------- cast shadow

            animationColor = new(0, 0, 0, 0.5f);

            animationScale = 0.2f * activeData.chargeLevel;

            animationPosition = (activeData.activeVector * 64) + (new Vector2(6, 6) * (5 - activeData.chargeLevel));

            /*switch (activeData.chargeLevel)
            {

                case 1:
                    animationScale = 0.2f;
                    animationPosition = (activeData.activeVector * 64) + new Vector2(24,24);
                    break;
                case 2:
                    animationScale = 0.4f;
                    animationPosition = (activeData.activeVector * 64) + new Vector2(18, 18);
                    break;
                default: // 3
                    animationScale = 0.6f;
                    animationPosition = (activeData.activeVector * 64) + new Vector2(12, 12);
                    break;

            }*/


            TemporaryAnimatedSprite shadowAnimationOne = new("TileSheets\\animations", animationRectangle, animationInterval, animationLength, animationLoops, animationPosition, false, animationFlip, animationDepth+1, 0f, animationColor, animationScale, 0f, 0f, 0f);

            Game1.currentLocation.temporarySprites.Add(shadowAnimationOne);

            animationScale = 0.1f + (0.2f * activeData.chargeLevel);

            animationPosition = (activeData.activeVector * 64) + (new Vector2(6, 6) * (5 - activeData.chargeLevel)) - new Vector2(3,3);

            /*switch (activeData.chargeLevel)
            {

                case 1:
                    animationScale = 0.3f;
                    animationPosition = (activeData.activeVector * 64) + new Vector2(21, 21);
                    break;
                case 2:
                    animationScale = 0.5f;
                    animationPosition = (activeData.activeVector * 64) + new Vector2(15, 15);
                    break;
                default: // 3
                    animationScale = 0.7f;
                    animationPosition = (activeData.activeVector * 64) + new Vector2(9, 9);
                    break;

            }*/

            TemporaryAnimatedSprite shadowAnimationTwo = new("TileSheets\\animations", animationRectangle, animationInterval, animationLength, animationLoops, animationPosition, false, animationFlip, animationDepth+2, 0f, animationColor, animationScale, 0f, 0f, 0f)
            {

                delayBeforeAnimationStart = 336,

            };

            Game1.currentLocation.temporarySprites.Add(shadowAnimationTwo);

            //-------------------------- cast animation

            float colorIncrement = 1.2f - (0.2f * activeData.chargeLevel);

            animationColor = new(colorIncrement, colorIncrement, 1, 1); // deepens from white to blue

            animationScale = 0.2f + (0.4f * activeData.chargeLevel);

            float vectorCastX = 30 - (12 * activeData.chargeLevel);

            float vectorCastY = 0 - 96 - (32f * activeData.chargeLevel);

            animationPosition = (activeData.activeVector * 64) + new Vector2(vectorCastX, vectorCastY);

            /*switch (activeData.chargeLevel)
            {

                case 1:
                    animationColor = new(uint.MaxValue); // deepens from white to blue
                    animationScale = 0.6f;
                    animationPosition = (activeData.activeVector * 64) + new Vector2(18,-128f);
                    break;
                case 2:
                    animationColor = new(0.8f, 0.8f, 1, 1); // deepens from white to blue
                    animationScale = 1.0f;
                    animationPosition = (activeData.activeVector * 64) + new Vector2(6,-160f);
                    break;
                default: // 3
                    animationColor = new(0.6f, 0.6f, 1, 1);
                    animationFlip = true;
                    animationScale = 1.4f;
                    animationPosition = (activeData.activeVector * 64) + new Vector2(-6,-192f);
                    break;

            }*/

            TemporaryAnimatedSprite animationOne = new("TileSheets\\animations", animationRectangle, animationInterval, animationLength, animationLoops, animationPosition, false, animationFlip, animationDepth+3, 0f, animationColor, animationScale, 0f, 0f, 0f);

            Game1.currentLocation.temporarySprites.Add(animationOne);

            colorIncrement = 1.1f - (0.2f * activeData.chargeLevel);

            animationColor = new(colorIncrement, colorIncrement, 1, 1); // deepens from white to blue

            animationScale = 0.4f + (0.4f * activeData.chargeLevel);

            vectorCastX = 24 - (12 * activeData.chargeLevel);

            vectorCastY = 0 - 112 - (32f * activeData.chargeLevel);

            animationPosition = (activeData.activeVector * 64) + new Vector2(vectorCastX, vectorCastY);

            /*switch (activeData.chargeLevel)
            {
                case 1:
                    animationColor = new(0.9f, 0.9f, 1, 1);
                    animationScale = 0.8f;
                    animationPosition = (activeData.activeVector * 64) + new Vector2(12, -144f);
                    break;
                case 2:
                    animationColor = new(0.7f, 0.7f, 1, 1);
                    animationFlip = true;
                    animationScale = 1.2f;
                    animationPosition = (activeData.activeVector * 64) + new Vector2(0, -176f);
                    break;
                default:
                    animationColor = new(0.5f, 0.5f, 1, 1);
                    animationScale = 1.6f;
                    animationPosition = (activeData.activeVector * 64) + new Vector2(-12, -208f);
                    break;

            }*/

            TemporaryAnimatedSprite animationTwo = new("TileSheets\\animations", animationRectangle, animationInterval, animationLength, animationLoops, animationPosition, false, animationFlip, animationDepth+4, 0f, animationColor, animationScale, 0f, 0f, 0f)
            {

                delayBeforeAnimationStart = 336,

            };

            Game1.currentLocation.temporarySprites.Add(animationTwo);

            //-------------------------- hand animation

            //ModUtility.AnimateHands(Game1.player, activeData.activeDirection, 333);

            //activeData.animateLevel = activeData.chargeLevel;

            if (activeData.chargeLevel == 1)
            {

                Game1.currentLocation.playSoundPitched("thunder_small", 800);

            }

            if (activeData.chargeLevel == 3)
            {

                Game1.currentLocation.playSoundPitched("thunder_small", 1000);

            }

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

                    if (riteData.castLocation.objects.Count() > 0)
                    {

                        if (riteData.castLocation.objects.ContainsKey(tileVector))
                        {

                            StardewValley.Object targetObject = riteData.castLocation.objects[tileVector];

                            /*if (craftList.Contains(targetObject.Name.ToString()))
                            {
                                if (blessingLevel >= 2)
                                {
                                    if (targetObject.MinutesUntilReady > 0)
                                    {

                                        effectCasts[tileVector] = new Cast.Craft(this, tileVector, riteData);

                                    }

                                }

                            }
                            else*/
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

                                if (warpCasts.Contains("campfire"))
                                {

                                    Game1.addHUDMessage(new HUDMessage("Already powered a campfire today", 3));

                                }
                                else if (blessingLevel >= 2)
                                {

                                    effectCasts[tileVector] = new Cast.Campfire(this, tileVector, riteData);

                                    warpCasts.Add("campfire");

                                }
                            }
                            else if (targetObject is Torch && targetObject.ParentSheetIndex == 93) // crafted candle torch
                            {
                                if (blessingLevel >= 5)
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
                        continue;
                    }

                    if (riteData.castLocation.terrainFeatures.ContainsKey(tileVector))
                    {

                        continue;

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

                                            effectCasts[tileVector] = new Cast.Stump(this, tileVector, riteData, resourceClump);

                                            break;

                                        default:

                                            effectCasts[tileVector] = new Cast.Boulder(this, tileVector, riteData, resourceClump);

                                            break;

                                    }

                                    Vector2 clumpVector = resourceClump.tile.Value;

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

            }

            if (blessingLevel >= 4)
            {
                Vector2 smiteVector = riteData.caster.getTileLocation();

                Microsoft.Xna.Framework.Rectangle castArea = new(
                    ((int)castVector.X - riteData.castLevel - 1) * 64,
                    ((int)castVector.Y - riteData.castLevel - 1) * 64,
                    (riteData.castLevel * 128) + 192,
                    (riteData.castLevel * 128) + 192
                );


                Microsoft.Xna.Framework.Rectangle smiteArea = new(
                    ((int)smiteVector.X - riteData.castLevel - 1) * 64,
                    ((int)smiteVector.Y - riteData.castLevel - 1) * 64,
                    (riteData.castLevel * 128) + 192,
                    (riteData.castLevel * 128) + 192
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
        
        private void AnimateStars()
        {

            //activeData.animateLevel = activeData.chargeLevel;

            if(activeData.chargeLevel == 2)
            {

                Game1.currentLocation.playSound("Meteorite");

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

                //staminaCost = 8;

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

            string control = Config.buttonList[0];

            return control;

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

    }

}

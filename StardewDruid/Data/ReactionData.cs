﻿using Microsoft.VisualBasic;
using Netcode;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Intrinsics.X86;
using static StardewDruid.Data.IconData;
using xTile.Dimensions;

namespace StardewDruid.Data
{
    public static class ReactionData
    {

        /*
        $neutral	Switch the speaking character to their neutral portrait.
        $h	Switch the speaking character to their happy portrait.
        $s	Switch the speaking character to their sad portrait.
        $u	Switch the speaking character to their unique portrait.
        $l	Switch the speaking character to their love portrait.
        $a	Switch the speaking character to their angry portrait.
        
        row	index	purpose
        1	0–3	animation used to start an emote (I think)
        2	4–7	empty can
        3	8–11	question mark
        4	12–15	angry
        5	16–19	exclamation
        6	20–23	heart
        7	24–27	sleep
        8	28–31	sad
        9	32–35	happy
        10	36–39	x
        11	40–43	pause
        12	44–47	not used
        13	48–51	not used
        14	52–55	videogame
        15	56–59	music note
        16	60–63	blush 

         */

        public enum reactions
        {
            weald,
            stars,
            mists,
            fates,
            dragon,
            jester,

        }

        public enum portraits
        {
            neutral,
            happy,
            sad,
            unique,
            love,
            angry

        }

        public static void ReactTo(NPC NPC, reactions reaction, int friendship = 0, List<int> context = null)
        {

            Mod.instance.AddWitness(reaction, NPC.Name);

            List<string> stringList = VillagerData.CustomReaction(reaction, NPC.Name);

            if (stringList.Count > 0)
            {

                for (int index = stringList.Count - 1; index >= 0; --index)
                {

                    string str = stringList[index];

                    NPC.CurrentDialogue.Push(new StardewValley.Dialogue(NPC, "0", str));

                }

                return;

            }

            Dictionary<string, int> affinities = VillagerData.Affinity();

            int affinity = 3;

            if (affinities.ContainsKey(NPC.Name))
            {

                affinity = affinities[NPC.Name];

            }

            string place = "valley";

            if (NPC.currentLocation.Name.Contains("Island"))
            {

                place = "island";

            }

            Dictionary<portraits,string> shots = VillagerData.ReactionPortraits(NPC.Name);

            switch (reaction)
            {

                case reactions.dragon:

                    if (Game1.player.friendshipData.ContainsKey(NPC.Name))
                    {

                        if (Game1.player.friendshipData[NPC.Name].Points >= 1500)
                        {

                            NPC.doEmote(20, true);

                            switch (affinity)
                            {
                                case 0:

                                    stringList.Add("I read somewhere about the return of Dragons.");

                                    stringList.Add("I couldn't make sense of it, but I trust you. "+shots[portraits.happy]+"");

                                    break;

                                case 1:

                                    stringList.Add("It's great to see a friendly Dragon. "+shots[portraits.love]+"");

                                    break;

                                case 2:

                                    stringList.Add(" I think I saw you fly. You're brave to go so high! "+shots[portraits.happy]+"");

                                    break;

                                case 3:


                                    stringList.Add("Your scales glitter in the light of the " + place + ".");

                                    stringList.Add("It's really something! "+shots[portraits.love]+"");

                                    break;

                                case 4:

                                    stringList.Add("A Dragon warrior is a great boon to the forces of light.");

                                    stringList.Add("I count on you to protect the " + place + " from evil. "+shots[portraits.happy]+"");

                                    break;

                                default:

                                    stringList.Add("The Tyrant of Calico created a mystical kingdom on top of a nexus of ether");

                                    stringList.Add("He extended his reach towards the celestial plane, but was cast down by a powerful hero of the Stars.");

                                    stringList.Add("Or so the legend goes.");

                                    break;
                            }

                            break;

                        }

                        if (Game1.player.friendshipData[NPC.Name].Points >= 1000)
                        {

                            NPC.doEmote(32, true);

                            switch (affinity)
                            {

                                case 0:

                                    stringList.Add("I don't know if I'll ever get used to seeing you like this.");

                                    stringList.Add("I'm sure one day it will be weird to see you as a human. "+shots[portraits.happy]+"");

                                    break;

                                case 1:

                                    stringList.Add("Is it fun to be a Dragon?");

                                    stringList.Add("You must eat a lot of barbeque. "+shots[portraits.happy]+"");

                                    break;

                                case 2:

                                    stringList.Add("I thought I heard something massive stomping along the wayside.");

                                    stringList.Add("Please be careful when you are... enlarged.");

                                    break;

                                case 3:

                                    stringList.Add("Can you breath fire? Can you fly?");

                                    stringList.Add("Tell me everything! "+shots[portraits.happy]+"");

                                    break;

                                case 4:

                                    stringList.Add("Dragons were the masters of many legendary kingdoms.");

                                    stringList.Add("Perhaps you might find and explore their long forgotten secrets.");

                                    break;

                                default:

                                    stringList.Add("Can you see the flows of ether from the ethereal plane?");

                                    stringList.Add("It's effervescent nature is imperceptible to the limited senses of humans.");

                                    break;

                            }

                            break;

                        }

                    }
                    switch (affinity)
                    {
                        case 0:

                            List<string> alertListOne = new()
                            {
                                "Whoa",
                                "What The Flip?"
                            };

                            NPC.showTextAboveHead(alertListOne[new Random().Next(2)]);

                            stringList.Add("Oh... it's " + Game1.player.Name);

                            stringList.Add("How did you become a... big lizard? "+shots[portraits.sad]+"");

                            break;

                        case 1:

                            List<string> alertListTwo = new()
                            {
                                "Monster!",
                                "AHHH! DRAGON!!",
                                "OH NO NO NO NO",
                            };

                            NPC.showTextAboveHead(alertListTwo[new Random().Next(3)]);

                            stringList.Add("(" + NPC.Name + " trembles with fear and uncertainty). "+shots[portraits.sad]+"");

                            break;

                        case 2:

                            NPC.doEmote(16, true);

                            stringList.Add("Oh... hi there big friend...");

                            stringList.Add("If you're looking for a horde of treasure... maybe try the mountain... or even further away. "+shots[portraits.sad]+"");

                            break;

                        case 3:

                            NPC.doEmote(16, true);

                            stringList.Add(Game1.player.Name + "?");

                            stringList.Add("I can't believe this happened to you. It's like something out of a fairytale.");

                            break;

                        case 4:


                            List<string> alertList = new()
                            {
                                "Alert the Guild!",
                                "The town is under attack!",
                                "To Arms!",
                            };

                            NPC.showTextAboveHead(alertList[new Random().Next(3)]);

                            stringList.Add("Declare yourself, beast!");

                            break;

                        default:

                            NPC.doEmote(16, true);

                            stringList.Add("Ancient ones have not roamed the " + place + " in centuries");

                            stringList.Add("This might be an omen of troubling times to come. "+shots[portraits.sad]+"");

                            break;
                    }

                    break;
                case reactions.fates:

                    string trick = "levitation";

                    switch (context.First())
                    {

                        case 0:
                            trick = "random critters";
                            break;
                        case 1:
                            trick = "jumping fishes";
                            break;
                        case 2:
                            trick = "a burst of butterflies";
                            break;

                    }

                    if (friendship >= 75)
                    {

                        switch (affinity)
                        {
                            case 0:

                                NPC.doEmote(20, true);

                                stringList.Add("I just had an out of body experience. My perception of reality is all messed up. "+shots[portraits.happy]+"");

                                break;

                            case 1:
                                NPC.doEmote(20, true);

                                stringList.Add("I think " + trick + " is my new favourite trick. "+shots[portraits.love]+"");


                                break;

                            case 2:


                                NPC.showTextAboveHead("Bravo!");

                                stringList.Add("You've become a master entertainer! "+shots[portraits.love]+"");

                                break;

                            case 3:

                                NPC.doEmote(20, true);

                                stringList.Add("I really want to be able to do that kind of thing. "+shots[portraits.love]+"");

                                break;

                            case 4:

                                NPC.doEmote(20, true);

                                stringList.Add("Ah, so there's a little bit of magic in our town magician (wink). "+shots[portraits.love]+"");

                                break;

                            default:

                                NPC.doEmote(20, true);

                                stringList.Add("You've become a master manipulater of the chaos of chance.");

                                stringList.Add("I suspect a Fate watches over you. "+shots[portraits.happy]+"");

                                break;

                        }

                        break;

                    }

                    if (friendship >= 50)
                    {

                        switch (affinity)
                        {
                            case 0:

                                NPC.doEmote(32, true);

                                stringList.Add("Nice one " + Game1.player.Name + ". I can picture you in a big show!");

                                stringList.Add("I would go to see it. "+shots[portraits.happy]+"");

                                break;

                            case 1:

                                NPC.showTextAboveHead("Hehe");

                                stringList.Add("That was fun. Do it again for me sometime! "+shots[portraits.happy]+"");

                                break;

                            case 2:

                                NPC.doEmote(32, true);

                                stringList.Add(trick.ToUpper() + "! Now that was special."+shots[portraits.happy]+"");

                                break;

                            case 3:

                                NPC.doEmote(32, true);

                                stringList.Add("That was great! You've brightened my day."+shots[portraits.happy]+"");

                                break;

                            case 4:

                                NPC.doEmote(32, true);

                                stringList.Add("I can sense there's more to this than a simple trick. "+shots[portraits.happy]+"");

                                break;

                            default:

                                NPC.doEmote(32, true);

                                stringList.Add("The great magician, the one known as the Fool.");

                                stringList.Add("He suffered a legendary defeat at the throne of the King of Maples.");

                                stringList.Add("His cheer and magic was lost to the world. "+shots[portraits.sad]+"");

                                break;
                        }

                        break;

                    }

                    if (friendship >= 25)
                    {
                        switch (affinity)
                        {
                            case 0:

                                NPC.doEmote(24, true);

                                stringList.Add("I know you're trying.");

                                stringList.Add("But maybe you're trying too hard to be funny? Just be yourself.");
                                break;

                            case 1:

                                NPC.showTextAboveHead("What?");

                                stringList.Add("I dont get what happened.");

                                break;

                            case 2:

                                NPC.doEmote(24, true);

                                stringList.Add("Well that was an... experience. Now back to business.");

                                break;

                            case 3:

                                NPC.doEmote(24, true);

                                stringList.Add("Hmmm. Well, I think " + trick + " might not be your strongest trick. How about cake out of a hat?");

                                stringList.Add("I mean, who doesn't like cake? Maybe a couple of the older guys.");

                                break;

                            case 4:

                                NPC.doEmote(24, true);

                                stringList.Add("That may have some potential. It definitely needs a lot of work, and a good deal of luck to pull off.");

                                break;

                            default:

                                NPC.doEmote(32, true);

                                stringList.Add("You seem to be quite comfortable playing with the forces of chance.");

                                break;
                        }


                        break;

                    }

                    switch (affinity)
                    {

                        case 0:


                            NPC.doEmote(28, true);

                            if (NPC is Child)
                            {

                                stringList.Add("I don't think I like that trick. "+shots[portraits.sad]+"");

                            }
                            else
                            {

                                stringList.Add("Ughh. I think I'll be happy if " + trick + " never happen to me again."+shots[portraits.angry]+"");

                            }

                            break;

                        case 1:

                            NPC.doEmote(28, true);

                            stringList.Add("Let's not talk about this again."+shots[portraits.sad]+"");

                            stringList.Add("It's all in the past.");

                            break;

                        case 2:

                            NPC.showTextAboveHead("Eeek!");

                            stringList.Add("No thanks. I don't need any more mischief in my life."+shots[portraits.sad]+"");

                            break;

                        case 3:

                            NPC.doEmote(28, true);

                            stringList.Add("That isn't what I was hoping for. "+shots[portraits.sad]+"");

                            break;

                        case 4:

                            NPC.doEmote(28, true);

                            stringList.Add("Maybe you could practice a different kind of talent. Something that would benefit the town. "+shots[portraits.sad]+"");

                            break;

                        default:

                            NPC.doEmote(28, true);

                            stringList.Add("(" + NPC.Name + " grumbles). The Fates should not be trifled with! "+shots[portraits.angry]+"");

                            break;

                    }

                    break;

                case reactions.stars:

                    NPC.doEmote(8, true);

                    switch (affinity)
                    {

                        case 0:

                            stringList.Add("Well " + Game1.player.Name + ", I'm not sure how you did that, but it seemed dangerous.");

                            stringList.Add("Please dont put the town in peril.");

                            break;

                        case 1:

                            stringList.Add("I remember an old story about a star falling to the ground.");

                            stringList.Add("I think it was a love story, but it was a long time ago when I heard it. "+shots[portraits.happy]+"");

                            break;

                        case 2:

                            stringList.Add("By Yoba. The sky opened up, and, fire came down, and, and, I blanked out.");

                            break;

                        case 3:

                            stringList.Add("Wow " + Game1.player.Name + ", that was a quite the display.");

                            stringList.Add("Please use your power to keep the " + place + " safe.");

                            break;

                        case 4:

                            stringList.Add("Your ability reminds me of a tale of hubris and forlorn love.");

                            stringList.Add("The hero sought the favour of the Stars, and when they answered his call, it started a fire that hurt his entire family. "+shots[portraits.sad]+"");

                            break;

                        default:

                            stringList.Add("The servants of the Lord of the Deep will cover the land in shadow.");

                            stringList.Add("And the grief of the Sisters will overwhelm the heavens and shower the land in a hail of fire.");

                            stringList.Add("It used to be that most folk in the " + place + " knew those words.");

                            break;

                    }

                    break;

                case reactions.mists:

                    NPC.doEmote(16, true);

                    switch (affinity)
                    {
                        case 0:

                            stringList.Add("Was that a bolt of lightning? ");

                            stringList.Add("I hope you're careful using that around tall poles and bodies of water!");

                            break;

                        case 1:

                            if (NPC is Child)
                            {

                                stringList.Add("I always hear a scary thunder sound whenever you're around "+shots[portraits.sad]+"");

                            }
                            else
                            {

                                stringList.Add("Rain and thunder every day! "+shots[portraits.angry]+"");

                            }

                            break;

                        case 2:

                            stringList.Add("It's like the clouds follow you around. "+shots[portraits.sad]+"");

                            break;

                        case 3:

                            stringList.Add("Where did that come from! You're full of surprises, " + Game1.player.Name);

                            break;

                        case 4:

                            stringList.Add("That must be really useful for things that get in your way. Not so fun for bystanders though. "+shots[portraits.sad]+"");

                            break;

                        default:

                            stringList.Add("Many say thunder is the voice of the Lady Beyond the Shore.");

                            stringList.Add("Be careful, " + Game1.player.Name + ", you don't want to catch her ire. "+shots[portraits.sad]+"");

                            stringList.Add("She awaits the time of her revenge.");

                            break;

                    }

                    break;

                case reactions.weald:

                    if (Game1.player.friendshipData[NPC.Name].Points >= 1500)
                    {
                        NPC.doEmote(20, true);

                        switch (affinity)
                        {
                            case 0:

                                stringList.Add("I give up trying to figure out how you're doing all this gardening.");

                                stringList.Add("You have flower petals all over you. It's ridiculous, but neat. "+shots[portraits.happy]+"");

                                break;

                            case 1:

                                stringList.Add("You're really good with nature. "+shots[portraits.happy]+"");

                                break;

                            case 2:

                                stringList.Add("I should visit the farm sometime, and see all the fantastic things you've brought to life. "+shots[portraits.love]+"");

                                break;

                            case 3:


                                stringList.Add("The " + place + " has never felt more alive than since you moved here. "+shots[portraits.love]+"");

                                break;

                            case 4:

                                stringList.Add("I've noticed that you have a special connection to the life force of the " + place + ". That means you have a special connection to all of us. "+shots[portraits.love]+"");

                                break;

                            default:

                                stringList.Add("You have shown great aptitude for the sacred rites of the " + place + " Druids.");

                                stringList.Add("The town doesn't know how blessed it is to have you here. "+shots[portraits.love]+"");

                                break;

                        }

                    }
                    else if (Game1.player.friendshipData[NPC.Name].Points >= 750)
                    {

                        NPC.doEmote(32, true);

                        switch (affinity)
                        {
                            case 0:

                                stringList.Add("Trees are sprouting everywhere. It's a phenomenon that I can't explain.");

                                stringList.Add("It seems to coincide with your activity around town though. Do you have anything to say about that?");

                                break;
                            case 1:

                                stringList.Add("The old folk think there some kind of forest spirit at work.");

                                break;

                            case 2:

                                stringList.Add("There are gardens sprouting up everywhere this season. "+shots[portraits.happy]+"");

                                break;

                            case 3:

                                stringList.Add("It's as if nature sings when you walk into town. "+shots[portraits.happy]+"");

                                break;

                            case 4:

                                stringList.Add("I saw the way you caught dinner from the stream yesterday.");

                                stringList.Add("The fish must really love your voice.");

                                break;

                            default:

                                stringList.Add("The small creatures listen to the songs of the wild. "+shots[portraits.happy]+"");

                                break;

                        }

                    }
                    else
                    {

                        switch (affinity)
                        {
                            case 0:

                                NPC.showTextAboveHead("Huh?");

                                switch (new Random().Next(3))
                                {

                                    case 0:

                                        stringList.Add("Why are you speaking strange words and waving your hands like that. Is it some kind of farmer ritual?");

                                        break;

                                    case 1:

                                        stringList.Add("Strange, but, all the broadleaf weeds on the village green look like they contracted some kind of explosive disease. They're all mulch.");

                                        break;

                                    default:

                                        stringList.Add("I thought I saw someone throwing sticks and berries at you. But there was no one else around. Must have been my imagination.");

                                        break;
                                }

                                break;

                            case 1:

                                NPC.doEmote(8, true);

                                switch(new Random().Next(3))
                                {

                                    case 0:

                                        stringList.Add("The flowers and birds have come back. I think the " + place + " is happy. "+shots[portraits.happy]);

                                        break;

                                    case 1:

                                        stringList.Add("Mmm. I can smell sweet berries. I didn't know it was time to pick them." + shots[portraits.happy]);

                                        break;

                                    default:

                                        stringList.Add("The air is light and sparkly today. "+shots[portraits.happy]+"");

                                        break;
                                }

                                break;

                            case 2:

                                NPC.doEmote(8, true);

                                switch (new Random().Next(3))
                                {

                                    case 0:

                                        
                                        stringList.Add("I think we have a new gardener. Someone's been along the paths, picking up twigs and planting flowers. " + shots[portraits.happy]);

                                        break;

                                    case 1:

                                        stringList.Add("I heard your Grandfather talking about the backside of your farm one night");

                                        stringList.Add("Creepy caves, strange voices, and occultic symbols on the rocks and trees.");

                                        stringList.Add("Think he told tall tales to keep unwanted guests away from the place." + shots[portraits.happy]);

                                        break;

                                    default:

                                        stringList.Add("How many seeds do you have in your pocket?");

                                        stringList.Add("It seems you have an endless amount to throw around.");

                                        break;
                                }

                                break;

                            case 3:

                                NPC.doEmote(8, true);

                                switch (new Random().Next(3))
                                {

                                    case 0:

                                        stringList.Add("If I seem dazed, it's because for a while there was a dark thought on my mind." + shots[portraits.sad]);
                                        stringList.Add("Then you appeared, and all I can think about is how beautiful the day is. Wierd, huh."+ shots[portraits.happy]);

                                        break;

                                    case 1:

                                        stringList.Add("I grew up hearing some pretty fantastic stories about this valley from my parents. ");
                                        stringList.Add("Secret woodland groves, lakes of fire under the mountain, shrines of forbidden magic. " + shots[portraits.happy]);
                                        stringList.Add("I've only been able to explore some of it. Tell me if you come across anything truly special, " + Game1.player.Name);

                                        break;

                                    default:

                                        stringList.Add("Did you see that? I could swear the " + place + " shimmered for a moment.");

                                        break;
                                }

                                break;

                            case 4:

                                NPC.doEmote(8, true);

                                switch (new Random().Next(3))
                                {

                                    case 0:

                                        stringList.Add("Do you believe that the elementals of the world exist, "+Game1.player.Name + "?");

                                        stringList.Add("They only reveal themselves to those that are aware of their presence.");

                                        break;

                                    case 1:

                                        stringList.Add("There are energies that flow through the fields and woodlands of the valley.");

                                        stringList.Add("Sometimes in quiet moments I can hear their rustles and whispers." + shots[portraits.happy]);

                                        stringList.Add("It would be a true gift to be able to speak back to them.");

                                        break;

                                    default:

                                        stringList.Add("It's time someone took responsibility for the wild spaces.");

                                        stringList.Add("Most of us are very busy, and we've taken a lot for granted in our peaceful corner of the country.");

                                        break;
                                }

                                break;

                            default:

                                NPC.doEmote(8, true);

                                switch (new Random().Next(2))
                                {
                                    case 0:

                                        stringList.Add("I'm happy that the circle has returned to the valley." + shots[portraits.happy]);

                                        stringList.Add("Shadows have descended on the sacred spaces, and the phantoms of the past have begun to stir." + shots[portraits.unique]);

                                        break;

                                    default:

                                        stringList.Add("I thought I just saw you dance in the style of the Old Kings.");

                                        stringList.Add("They've gone to sleep now, at rest after a terrible war, never to frolic again. " + shots[portraits.sad]);

                                        stringList.Add("\"Until the crows come home for the golden bones...\"" + shots[portraits.unique]);

                                        break;
                                
                                }

                                break;

                        }


                    }

                    break;

                case reactions.jester:

                    NPC.doEmote(20, true);

                    stringList.Add("I feel like I just brushed up against a massive cat.");

                    break;

            }

            for (int index = stringList.Count - 1; index >= 0; --index)
            {

                string str = stringList[index];

                NPC.CurrentDialogue.Push(new StardewValley.Dialogue(NPC, "0", str));

            }

        }

    }

}

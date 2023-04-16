using Microsoft.Xna.Framework;
using Polyphemalus.Content.Items;
using Polyphemalus.Content.Items.Magic;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.PlayerDrawLayer;

namespace Polyphemalus.Content.NPCs
{
    // The main part of the boss, usually refered to as "body"
    [AutoloadBossHead] // This attribute looks for a texture called "ClassName_Head_Boss" and automatically registers it as the NPC boss head icon
    public class Exotrexia : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Exotrexia");

            // Add this in for bosses that have a summon item, requires corresponding code in the item (See MinionBossSummonItem.cs)
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            // Automatically group with other bosses
            NPCID.Sets.BossBestiaryPriority.Add(Type);

            // Specify the debuffs it is immune to
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Confused // Most NPCs have this
				}
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }

        public override void SetDefaults()
        {
            NPC.width = 110;
            NPC.height = 110;
            NPC.damage = 0;
            NPC.defense = 10;
            NPC.lifeMax = 17000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.value = Item.buyPrice(gold: 500);
            NPC.SpawnWithHigherTime(30);
            NPC.boss = true;
            NPC.npcSlots = 10f;
            NPC.aiStyle = -1;

        }

        public Dictionary<string, int> AIShare = new Dictionary<string, int>()
        {
            { "soloTimer", 0 },
            { "beenSolo", 1},
            { "shotTimer", 0 },
            { "index", 1 },
        };
        public ref float timer => ref NPC.localAI[0];
        public ref float phase => ref NPC.localAI[1];

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            cooldownSlot = ImmunityCooldownID.Bosses; // use the boss immunity cooldown counter, to prevent ignoring boss attacks by taking damage from other sources
            return true;
        }

        public override void AI()
        {
            // This should almost always be the first code in AI() as it is responsible for finding the proper player target
            if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
            {
                NPC.TargetClosest();
            }

            var eyesLeft = 0;
            AIShare["index"] = 0;
            NPC astigmadeddon = null;
            NPC cataractacomb = null;
            NPC conjunctivirus = null;
            Astigmageddon astigmageddonModNpc = null;
            Cataractacomb cataractacombModNpc = null;
            Conjunctivirus conjunctivirusModNpc = null;
            for (int k = 0; k < Main.maxNPCs; k++)
            {
                NPC target = Main.npc[k];
                if (target.type == ModContent.NPCType<Astigmageddon>() && target.active == true)
                {
                    astigmadeddon = target;
                    astigmageddonModNpc = astigmadeddon.ModNPC<Astigmageddon>();
                    AIShare["index"]++;
                    eyesLeft++;
                }
                if (target.type == ModContent.NPCType<Cataractacomb>() && target.active == true)
                {
                    cataractacomb = target;
                    cataractacombModNpc = cataractacomb.ModNPC<Cataractacomb>();
                    eyesLeft++;
                }
                if (target.type == ModContent.NPCType<Conjunctivirus>() && target.active == true)
                {

                    conjunctivirus = target;
                    conjunctivirusModNpc = conjunctivirus.ModNPC<Conjunctivirus>();
                    eyesLeft++;
                }
            }

            Player player = Main.player[NPC.target];

            if (player.dead)
            {
                // If the targeted player is dead, flee
                NPC.velocity.Y -= 0.04f;
                // This method makes it so when the boss is in "despawn range" (outside of the screen), it despawns in 10 ticks
                NPC.EncourageDespawn(10);
                return;
            }
            TurnTowards(player.Center);

            if (phase == 0)
            {
                timer++;

                var circlePos = CirclePos(player.Center, (float)(((timer % 180) * 2) * Math.PI / 180), 500f);
                if (Vector2.Distance(NPC.Center, circlePos) <= 48 * 4)
                {
                    NPC.Center = circlePos;
                    NPC.velocity = Vector2.Zero;
                }
                else
                {
                    MoveTowards(circlePos, 80, 5);
                }

                if (timer >= 60)
                {
                    if (timer % 32 == 0)
                    {
                        ShootCenter(ProjectileID.FrostWave, 0.5f, 10);
                    }
                }

                if (timer > 60 * 7)
                {
                    phase = 1;
                    timer = 0;

                    NPC.velocity = ((float)Math.PI / 180 * (90 * 2 + 30f)).ToRotationVector2() * 20;
                }


            }

            if (phase == 1)
            {
                var circleSpot = eyesLeft;
                if (conjunctivirusModNpc != null)
                {
                    if (conjunctivirusModNpc.AIShare["soloTimer"] > 0) circleSpot--;
                }
                if (cataractacombModNpc != null)
                {
                    if (cataractacombModNpc.AIShare["soloTimer"] > 0) circleSpot--;
                }
                if (astigmageddonModNpc != null)
                {
                    if (astigmageddonModNpc.AIShare["soloTimer"] > 0)
                    {
                        circleSpot--;
                        AIShare["index"]--;
                    }
                }
                TurnTowards(player.Center);
                timer++;
                var circlePos = CirclePos(player.Center, (float)(((timer % 360) * 2 + (360 / (circleSpot + 1)) * AIShare["index"]) * Math.PI / 180), 650f);

                if (Vector2.Distance(NPC.Center, circlePos) <= 48 * 4)
                {
                    NPC.Center = circlePos;
                    NPC.velocity = Vector2.Zero;
                }
                else
                {
                    MoveTowards(circlePos, 60, 10);
                }
                if (timer % 75 == 30)
                {
                    ShootCenter(ProjectileID.FrostWave, 3, 45);
                }
                if (timer % 60 == 45 && AIShare["beenSolo"] == 0)
                {
                    phase = 2;
                    timer = 0;
                    AIShare["soloTimer"] = 1;
                }

            }
            if (phase == 2)
            {
                timer++;
                var circle = CirclePos(CirclePos(player.Center, (float)(Math.PI / 180 * timer), 175), (float)(Math.PI / 180 * timer * 4), 300);
                var circlePredictive = CirclePos(CirclePos(player.Center, (float)(Math.PI / 180 * (timer + 1)), 175), (float)(Math.PI / 180 * (timer + 1) * 4), 300);
                if (Vector2.Distance(NPC.Center, circle) <= 48 * 4)
                {
                    NPC.Center = circle;
                    NPC.velocity = Vector2.Zero;
                    NPC.rotation = (circlePredictive - NPC.Center).ToRotation();
                    phase = 3;
                }
                else
                {
                    MoveTowards(circle, 40, 5);
                    NPC.rotation = NPC.velocity.ToRotation();
                }

                if (timer >= 360)
                {
                    timer = 0;
                    phase = 1;
                    ShootCenter(ProjectileID.CultistBossLightningOrb, 5, 50);
                    NPC.velocity *= -0.25f;
                }
            }
            if (phase == 3)
            {
                timer++;
                var circle = CirclePos(CirclePos(player.Center, (float)(Math.PI / 180 * timer), 175), (float)(Math.PI / 180 * timer*4), 300);
                var circlePredictive = CirclePos(CirclePos(player.Center, (float)(Math.PI / 180 * (timer + 1)), 175), (float)(Math.PI / 180 * (timer + 1)*4), 300);
                    NPC.Center = circle;
                    NPC.velocity = Vector2.Zero;
                    NPC.rotation = (circlePredictive - NPC.Center).ToRotation();
                if( timer % 20 == 0)
                {
                    ShootCenter(ProjectileID.FrostBlastHostile,1,20);
                }

                
                if (timer >= 360)
                {
                    timer = 0;
                    NPC.velocity = new Vector2(0, 10);
                    phase = -1;
                }
            }
            if (phase == -1)
            {
                timer++;
                NPC.velocity *= 0.95f;
                if (timer > 180)
                {
                    phase = 1;
                    timer = 0;
                    AIShare["beenSolo"] = 1;
                    AIShare["soloTimer"] = 0;
                    if (conjunctivirus != null)
                    {
                        timer = conjunctivirusModNpc.timer;
                        conjunctivirusModNpc.AIShare["beenSolo"] = 0;
                    }
                    else if (cataractacomb != null)
                    {
                        timer = cataractacombModNpc.timer;
                        cataractacombModNpc.AIShare["beenSolo"] = 0;
                    } else if (astigmadeddon != null)
                    {
                        timer = astigmageddonModNpc.timer;
                        astigmageddonModNpc.AIShare["beenSolo"] = 0;
                    }
                }
            }
        }

        public override void OnKill()
        {
            Astigmageddon astigmadeddon = null;
            Cataractacomb cataractacomb = null;
            Conjunctivirus conjunctivirus = null;
            for (int k = 0; k < Main.maxNPCs; k++)
            {
                NPC target = Main.npc[k];
                if (target.type == ModContent.NPCType<Astigmageddon>() && target.active == true)
                {
                    astigmadeddon = target.ModNPC<Astigmageddon>();
                }
                if (target.type == ModContent.NPCType<Cataractacomb>() && target.active == true)
                {
                    cataractacomb = target.ModNPC<Cataractacomb>();
                }
                if (target.type == ModContent.NPCType<Conjunctivirus>() && target.active == true)
                {

                    conjunctivirus = target.ModNPC<Conjunctivirus>();
                }
            }
            if (AIShare["beenSolo"] == 0)
            {
                if (conjunctivirus != null)
                {
                    conjunctivirus.AIShare["beenSolo"] = 0;
                }
                else if (astigmadeddon != null)
                {
                    astigmadeddon.AIShare["beenSolo"] = 0;
                }
                else if (cataractacomb != null)
                {
                    cataractacomb.AIShare["beenSolo"] = 0;
                }
            }
        }

        private void ShootCenter(int type, float velocityMod, int damage, float spread = 0)
        {
            if (Main.masterMode) damage /= 4;
            else if (Main.expertMode) damage /= 4;
            else damage /= 2;
            Vector2 position = NPC.Center + new Vector2(NPC.width / 2, 0).RotatedBy(NPC.rotation);
            Vector2 Velocity = NPC.rotation.ToRotationVector2() * velocityMod;
            Projectile.NewProjectile(NPC.GetSource_FromAI(), position, Velocity.RotatedBy(spread * Math.PI / 180), type, damage, 0f, Main.myPlayer);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            LeadingConditionRule lastLivingPoly = new LeadingConditionRule(new LastPolyBeaten());
            npcLoot.Add(lastLivingPoly);
            IItemDropRule dropItem = ItemDropRule.Common(ModContent.ItemType<Tetrachromancy>());
            lastLivingPoly.OnSuccess(dropItem);

        }
        private Vector2 CirclePos(Vector2 pos, float rotation, float distance)
        {
            return pos + (rotation).ToRotationVector2() * distance;
        }

        private void MoveTowards(Vector2 goal, float speed, float inertia)
        {
            Vector2 moveTo = (goal - NPC.Center).SafeNormalize(Vector2.UnitY) * speed / 1.5f;
            NPC.velocity = (NPC.velocity * (inertia - 1) + moveTo) / inertia;
        }

        private void TurnTowards(Vector2 goal, float offset = 0, float maxSpeed = 1)
        {
            float goal2 = (goal - NPC.Center).ToRotation() + offset;
            maxSpeed *= (float)Math.PI / 180f;
            float rad360 = (360 * (float)Math.PI / 180f);
            if (goal2 % rad360 + rad360 > NPC.rotation + rad360)
            {
                NPC.rotation += Math.Min((goal2 % rad360 + rad360) - NPC.rotation, maxSpeed + rad360);
            }
            if (goal2 % rad360 + rad360 < NPC.rotation + rad360)
            {
                NPC.rotation += Math.Min((goal2 % rad360 + rad360) - NPC.rotation, maxSpeed + rad360);
            }
        }

    }
}
﻿using RoR2;
using R2API;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using RiskyMod.Items.Uncommon;
using R2API.Utils;
using RiskyMod.Items.Legendary;
using RiskyMod.Items.Common;
using UnityEngine;

namespace RiskyMod.SharedHooks
{
    public class ModifyFinalDamage
    {
        public delegate void ModifyFinalDamageDelegate(DamageMult damageMult, DamageInfo damageInfo,
            HealthComponent victim, CharacterBody victimBody,
            CharacterBody attackerBody, Inventory attackerInventory);
        public static ModifyFinalDamageDelegate ModifyFinalDamageActions = ModifyFinalDamageMethod;
        private static void ModifyFinalDamageMethod(DamageMult damageMult, DamageInfo damageInfo,
            HealthComponent victim, CharacterBody victimBody,
            CharacterBody attackerBody, Inventory attackerInventory) {}

        public ModifyFinalDamage()
        {
			IL.RoR2.HealthComponent.TakeDamage += (il) =>
			{
				ILCursor c = new ILCursor(il);
				c.GotoNext(
					 x => x.MatchLdarg(1),
					 x => x.MatchLdfld<DamageInfo>("damage"),
                     x => x.MatchStloc(6)
					);
				c.Index ++;
                c.Remove();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<DamageInfo, HealthComponent, float>>((damageInfo, victimHealth) =>
                {
                    float newDamage = damageInfo.damage;
                    CharacterBody victimBody = victimHealth.body;
                    if (victimBody && damageInfo.attacker)
                    {
                        if (Chronobauble.enabled)
                        {
                            if (victimBody.HasBuff(RoR2Content.Buffs.Slow60))
                            {
                                newDamage *= 1.15f;
                            }
                        }
                        CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                        if (attackerBody)
                        {
                            Inventory attackerInventory = attackerBody.inventory;
                            if (attackerInventory)
                            {
                                DamageMult damageMult = new DamageMult();
                                ModifyFinalDamageActions.Invoke(damageMult, damageInfo, victimHealth, victimBody, attackerBody, attackerInventory);
                                newDamage *= damageMult.damageMult;
                            }
                        }
                    }
                    return newDamage;
                });
            };
		}
    }

    public class DamageMult
    {
        public float damageMult = 1f;
    }
}

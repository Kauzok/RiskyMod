﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskyMod.SharedHooks;
using RoR2;
using UnityEngine;

namespace RiskyMod.Items.Equipment
{
    public class SuperLeech
    {
        public static bool enabled = true;
        public SuperLeech()
        {
            if (!enabled) return;

            //Remove vanilla effect.
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += (il) =>
            {
                ILCursor c = new ILCursor(il);
                if(c.TryGotoNext(
                     x => x.MatchLdsfld(typeof(RoR2Content.Buffs), "LifeSteal")
                    ))
                {
                    c.Remove();
                    c.Emit<RiskyMod>(OpCodes.Ldsfld, nameof(RiskyMod.emptyBuffDef));
                }
                else
                {
                    UnityEngine.Debug.LogError("RiskyMod: SuperLeech IL Hook failed");
                }
            };

            TakeDamage.OnHpLostAttackerActions += HealOnHit;
        }

        private void HealOnHit(DamageInfo damageInfo, HealthComponent self, CharacterBody attackerBody, Inventory inventory, float hpLost)
        {
            if (attackerBody.HasBuff(RoR2Content.Buffs.LifeSteal))
            {
                hpLost = Mathf.Max(hpLost, damageInfo.damage);
                float toHeal = hpLost * 0.2f;
                attackerBody.healthComponent.Heal(toHeal * damageInfo.procCoefficient, damageInfo.procChainMask);
            }
        }
    }
}

﻿using System;
using EntityStates;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace RiskyMod.Items.Lunar
{
    public class Visions
    {
        public static bool enabled = true;

        //This could be simplified by simply hooking LunarPrimaryReplacementSkill.GetRechargeInterval, but this method ensures that it doesn't double-dip and benefit from both Attack Speed and Cooldown Reduction.
        public Visions()
        {
            if (!enabled) return;

            Content.Content.entityStates.Add(typeof(EntityStates.RiskyModStates.LunarReplacement.ReloadVisions));

            On.EntityStates.GlobalSkills.LunarNeedle.FireLunarNeedle.OnEnter += (orig, self) =>
            {
                orig(self);

                VisionsReloader visRel = self.GetComponent<VisionsReloader>();
                if (visRel) visRel.FireSkill();
            };

            RoR2.CharacterBody.onBodyInventoryChangedGlobal += CharacterBody_onBodyInventoryChangedGlobal;

            On.RoR2.Skills.LunarPrimaryReplacementSkill.GetRechargeInterval += (orig, self, skillSlot) =>
            {
                return 0f;
            };

            On.RoR2.Skills.LunarPrimaryReplacementSkill.GetRechargeStock += (orig, self, skillSlot) =>
            {
                return 0;
            };
        }

        private void CharacterBody_onBodyInventoryChangedGlobal(CharacterBody body)
        {
            if (body.inventory && body.inventory.GetItemCount(RoR2Content.Items.LunarPrimaryReplacement.itemIndex) > 0)
            {
                VisionsReloader visRel = body.GetComponent<VisionsReloader>();
                if (!visRel) body.gameObject.AddComponent<VisionsReloader>();
            }
        }
    }

    public class VisionsReloader : MonoBehaviour
    {
        public static float graceDuration = 0.4f;    //Used when there's still stocks in the mag
        public static float baseDuration = 2f;

        private CharacterBody body;
        private SkillLocator skills;

        private float reloadStopwatch;
        private float delayStopwatch;

        private void Awake()
        {
            body = base.GetComponent<CharacterBody>();
            skills = base.GetComponent<SkillLocator>();

            reloadStopwatch = 0f;
            delayStopwatch = 0f;
        }

        private void FixedUpdate()
        {
            //Destroy itself when the player no longer has Visions
            if (body.inventory && body.inventory.GetItemCount(RoR2Content.Items.LunarPrimaryReplacement) <= 0)
            {
                Destroy(this);
                return;
            }

            if (!skills.hasAuthority) return;

            if (skills.primary.stock < skills.primary.maxStock && skills.primary.skillDef == CharacterBody.CommonAssets.lunarPrimaryReplacementSkillDef)
            {
                if (skills.primary.stock <= 0) delayStopwatch = 0f;
                if (delayStopwatch > 0f)
                {
                    delayStopwatch -= Time.fixedDeltaTime;
                }
                else
                {
                    reloadStopwatch -= Time.fixedDeltaTime;
                    if (reloadStopwatch <= 0f)
                    {
                        skills.primary.stock = skills.primary.maxStock;
                    }
                }
            }
            else
            {
                reloadStopwatch = GetReloadStopwatch();
            }
        }

        private float GetStackMult()
        {
            float stackMult = 1f;
            if (body.inventory)
            {
                stackMult = Mathf.Max(1f, body.inventory.GetItemCount(RoR2Content.Items.LunarPrimaryReplacement));
            }
            return stackMult;
        }

        private float GetReloadStopwatch()
        {
            return (baseDuration / body.attackSpeed) * GetStackMult();
        }

        public void FireSkill()
        {
            delayStopwatch = graceDuration;  //Duration is already scaled to attack speed. InitialDelay is simply for inputs, and is ignored if the mag is empty.
            reloadStopwatch = GetReloadStopwatch();// + (skills.primary.stock <= 0 ? duration : 0f);
        }
    }
}

namespace EntityStates.RiskyModStates.LunarReplacement
{
    public class ReloadVisions : BaseState
    {
        public static float baseDuration = 2f;

		private float totalDuration;

        public override void OnEnter()
        {
            base.OnEnter();

            totalDuration = baseDuration;
            if (base.characterBody && base.characterBody.inventory)
            {
                int itemCount = Mathf.Max(1, base.characterBody.inventory.GetItemCount(RoR2Content.Items.LunarPrimaryReplacement));
                totalDuration *= itemCount;
            }
            totalDuration /= base.attackSpeedStat;
        }

        public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (base.isAuthority && base.fixedAge >= totalDuration)
			{
                if (base.skillLocator)
                {
                    base.skillLocator.primary.stock = base.skillLocator.primary.maxStock;
                }
				this.outer.SetNextStateToMain();
			}
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
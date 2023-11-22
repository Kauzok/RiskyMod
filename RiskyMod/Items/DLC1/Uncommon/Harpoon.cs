﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using R2API;

namespace RiskyMod.Items.DLC1.Uncommon
{
    public class Harpoon
    {
        public static bool enabled = true;
        private static GameObject effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MoveSpeedOnKillActivate");
        public static BuffDef ReturnsHarpoonBuff;
        public Harpoon()
        {
            if (!enabled) return;
            BuffDef originalHarpoonBuff = Addressables.LoadAssetAsync<BuffDef>("RoR2/DLC1/MoveSpeedOnKill/bdKillMoveSpeed.asset").WaitForCompletion();

            IL.RoR2.GlobalEventManager.OnCharacterDeath += (il) =>
            {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(
                     x => x.MatchLdsfld(typeof(DLC1Content.Items), "MoveSpeedOnKill")
                    ))
                {
                    c.Remove();
                    c.Emit<RiskyMod>(OpCodes.Ldsfld, nameof(RiskyMod.emptyItemDef));
                }
                else
                {
                    UnityEngine.Debug.LogError("RiskyMod: Harpoon IL Hook failed");
                }
            };

            ItemsCore.ModifyItemDefActions += ModifyItem;
            AssistManager.HandleAssistInventoryActions += OnKillEffect;

            ReturnsHarpoonBuff = SneedUtils.SneedUtils.CreateBuffDef(
                "RiskyMod_ReturnsHarpoonBuff",
                true,
                false,
                false,
                originalHarpoonBuff.buffColor,
                originalHarpoonBuff.iconSprite
                );

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.HasBuff(Harpoon.ReturnsHarpoonBuff))
            {
                args.moveSpeedMultAdd += 1.25f;
            }
        }

        private static void OnKillEffect(CharacterBody attackerBody, Inventory attackerInventory, CharacterBody victimBody, CharacterBody killerBody)
        {
            int itemCount = attackerInventory.GetItemCount(DLC1Content.Items.MoveSpeedOnKill);
            if (itemCount > 0)
            {
                int buffsToGive = itemCount;
                int currentBuffs = attackerBody.GetBuffCount(Harpoon.ReturnsHarpoonBuff);
                attackerBody.ClearTimedBuffs(Harpoon.ReturnsHarpoonBuff);

                for (int i = 0; i < Mathf.Min(25, buffsToGive + currentBuffs); i++)
                {
                    attackerBody.AddTimedBuff(Harpoon.ReturnsHarpoonBuff, 1f + i);
                }

                /*int stack = itemCount - 1;
                int num6 = 5;
                float totalDuration = 2f + (float)stack;
                attackerBody.ClearTimedBuffs(DLC1Content.Buffs.KillMoveSpeed);
                for (int i = 0; i < num6; i++)
                {
                    attackerBody.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, totalDuration * (float)(i + 1) / (float)num6);
                }*/

                EffectData effectData = new EffectData();
                effectData.origin = attackerBody.corePosition;
                CharacterMotor characterMotor = attackerBody.characterMotor;
                bool flag = false;
                if (characterMotor)
                {
                    Vector3 moveDirection = characterMotor.moveDirection;
                    if (moveDirection != Vector3.zero)
                    {
                        effectData.rotation = Util.QuaternionSafeLookRotation(moveDirection);
                        flag = true;
                    }
                }
                if (!flag)
                {
                    effectData.rotation = attackerBody.transform.rotation;
                }
                EffectManager.SpawnEffect(Harpoon.effectPrefab, effectData, true);
            }
        }

        private static void ModifyItem()
        {
            HG.ArrayUtils.ArrayAppend(ref ItemsCore.changedItemDescs, DLC1Content.Items.MoveSpeedOnKill);
        }
    }
}

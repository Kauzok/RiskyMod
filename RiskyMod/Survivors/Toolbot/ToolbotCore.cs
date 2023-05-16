﻿using RoR2;
using UnityEngine;
using R2API;
using MonoMod.Cil;
using System;
using RoR2.Skills;
using EntityStates;
using RiskyMod.SharedHooks;
using Mono.Cecil.Cil;
using RiskyMod.Survivors.Toolbot.Components;
using UnityEngine.AddressableAssets;

namespace RiskyMod.Survivors.Toolbot
{
    public class ToolbotCore
    {
        public static bool enabled = true;
        public static bool enableNailgunChanges = true;
        public static bool enableRebarChanges = true;
        public static bool enableScrapChanges = true;
        public static bool scrapICBM = true;

        public static bool sawPhysics = true;
        public static bool sawBarrierOnHit = true;
        public static bool sawHitbox = true;

        public static bool enableSecondarySkillChanges = true;

        public static bool enableRetoolChanges = true;
        public static bool enablePowerModeChanges = true;

        public static BuffDef PowerModeBuff;
        public static GameObject bodyPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterBodies/ToolbotBody");

        public ToolbotCore()
        {
            if (!enabled) return;

            ModifySkills(bodyPrefab.GetComponent<SkillLocator>());

            //Vanilla Titan kill times
            //Nailgun takes 26.14s
                //19s with Shift included
                //13.3s power mode

            //Rebar takes 65.8s
                //37.6 with quickswap

            //Scrap Launcher takes 37.8s
                //25.34s with quickswap
                //19.75s with power mode
        }

        private void ModifySkills(SkillLocator sk)
        {
            ModifyPrimaries(sk);
            ModifySecondaries(sk);
            ModifySpecials(sk);
        }

        private void ModifyPrimaries(SkillLocator sk)
        {
            NailgunChanges(sk);
            RebarChanges(sk);
            ScrapChanges(sk);
            SawChanges(sk);
        }

        private void NailgunChanges(SkillLocator sk)
        {
            if (!enableNailgunChanges) return;

            SkillDef nailgunDef = Addressables.LoadAssetAsync<SkillDef>("RoR2/Base/Toolbot/ToolbotBodyFireNailgun.asset").WaitForCompletion();

            IL.EntityStates.Toolbot.BaseNailgunState.FireBullet += (il) =>
            {
                ILCursor c = new ILCursor(il);
                if(c.TryGotoNext(
                     x => x.MatchCallvirt<BulletAttack>("Fire")
                     ))
                {
                    c.EmitDelegate<Func<BulletAttack, BulletAttack>>(bulletAttack =>
                    {
                        bulletAttack.radius = 0.2f;
                        bulletAttack.smartCollision = true;
                        return bulletAttack;
                    });
                }
                else
                {
                    UnityEngine.Debug.LogError("RiskyMod: Toolbot Nailgun IL Hook failed");
                }
            };
            new FixNailgunBurst();
        }

        private void RebarChanges(SkillLocator sk)
        {
            if (!enableRebarChanges) return;
            SkillDef railgunDef = Addressables.LoadAssetAsync<SkillDef>("RoR2/Base/Toolbot/ToolbotBodyFireSpear.asset").WaitForCompletion();
            railgunDef.skillDescriptionToken = "TOOLBOT_PRIMARY_ALT1_DESCRIPTION_RISKYMOD";
            SneedUtils.SneedUtils.SetEntityStateField("EntityStates.Toolbot.FireSpear", "damageCoefficient", "6.6");
        }

        private void ScrapChanges(SkillLocator sk)
        {
            if (!enableScrapChanges) return;
            bodyPrefab.AddComponent<GrenadeStockController>();
            SkillDef scrapDef = Addressables.LoadAssetAsync<SkillDef>("RoR2/Base/Toolbot/ToolbotBodyFireGrenadeLauncher.asset").WaitForCompletion();
            scrapDef.skillDescriptionToken = "TOOLBOT_PRIMARY_ALT2_DESCRIPTION_RISKYMOD";
            scrapDef.rechargeStock = 0;
            SneedUtils.SneedUtils.SetEntityStateField("EntityStates.Toolbot.FireGrenadeLauncher", "damageCoefficient", "3.9");
            SneedUtils.SneedUtils.SetEntityStateField("EntityStates.Toolbot.FireGrenadeLauncher", "maxSpread", "0");

            GameObject scrapProjectileModded = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Toolbot/ToolbotGrenadeLauncherProjectile.prefab").WaitForCompletion().InstantiateClone("RiskyModToolbotGrenadeProjectile", true);
            DamageAPI.ModdedDamageTypeHolderComponent mdc = scrapProjectileModded.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>();
            mdc.Add(SharedDamageTypes.SweetSpotModifier);
            Content.Content.projectilePrefabs.Add(scrapProjectileModded);

            On.EntityStates.Toolbot.FireGrenadeLauncher.OnEnter += (orig, self) =>
            {
                orig(self);
                GrenadeStockController gsc = self.gameObject.GetComponent<GrenadeStockController>();
                if (gsc)
                {
                    gsc.FireSkill(self.activatorSkillSlot, self.duration);
                }
            };

            if (scrapICBM)
            {
                //Not the best place to hook.
                On.EntityStates.Toolbot.FireGrenadeLauncher.ModifyProjectileAimRay += (orig, self, aimRay) =>
                {
                    if (self.characterBody && self.characterBody.inventory)
                    {
                        int icbmCount = self.characterBody.inventory.GetItemCount(DLC1Content.Items.MoreMissile);
                        int stackCount = icbmCount - 1;

                        if (icbmCount > 0)
                        {
                            float damageMult = 1f;
                            if (stackCount > 0) damageMult += 0.5f * stackCount;

                            self.damageCoefficient *= damageMult;


                            Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                            Vector3 axis = Vector3.Cross(aimRay.direction, rhs);

                            float currentSpread = 0f;
                            float angle = 0f;
                            float num2 = 0f;
                            num2 = UnityEngine.Random.Range(1f + currentSpread, 1f + currentSpread) * 3f;   //Bandit is x2
                            angle = num2 / 2f;  //3 - 1 rockets

                            Vector3 direction = Quaternion.AngleAxis(-num2 * 0.5f, axis) * aimRay.direction;
                            Quaternion rotation = Quaternion.AngleAxis(angle, axis);
                            Ray aimRay2 = new Ray(aimRay.origin, direction);
                            for (int i = 0; i < 3; i++)
                            {
                                if (i != 1) //Middle rocket is already fired by vanilla skill
                                {
                                    RoR2.Projectile.ProjectileManager.instance.FireProjectile(self.projectilePrefab,
                                        aimRay2.origin, Util.QuaternionSafeLookRotation(aimRay2.direction),
                                        self.gameObject,
                                        self.damageStat * self.damageCoefficient,
                                        self.force,
                                        self.RollCrit(),
                                        DamageColorIndex.Default,
                                        null,
                                        -1f);
                                }
                                aimRay2.direction = rotation * aimRay2.direction;
                            }
                        }
                    }

                    return orig(self, aimRay);
                };
            }
        }

        private void SawChanges(SkillLocator sk)
        {
            if (sawPhysics)
            {
                SneedUtils.SneedUtils.SetEntityStateField("EntityStates.Toolbot.FireBuzzsaw", "selfForceMagnitude", "0");
                IL.EntityStates.Toolbot.FireBuzzsaw.FixedUpdate += (il) =>
                {
                    ILCursor c = new ILCursor(il);
                    if (c.TryGotoNext(
                         x => x.MatchCallvirt<CharacterMotor>("ApplyForce")
                         ))
                    {
                        c.Emit(OpCodes.Ldarg_0);
                        c.EmitDelegate<Func<int, EntityStates.Toolbot.FireBuzzsaw, int>>((orig, self) =>
                        {
                            if (self.characterMotor && !self.characterMotor.isGrounded && self.characterMotor.velocity.y <= 0f)
                            {
                                //self.characterMotor.velocity.y = 0f;
                                self.SmallHop(self.characterMotor, 2.5f);
                            }
                            return orig;
                        });
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("RiskyMod: Toolbot SawPhysics IL Hook failed");
                    }
                };
            }

            SkillDef sawDef = Addressables.LoadAssetAsync<SkillDef>("RoR2/Base/Toolbot/ToolbotBodyFireBuzzsaw.asset").WaitForCompletion();
            sawDef.cancelSprintingOnActivation = false;

            if (sawHitbox)
            {
                HitBoxGroup[] hitboxes = ToolbotCore.bodyPrefab.GetComponentsInChildren<HitBoxGroup>();
                foreach (HitBoxGroup h in hitboxes)
                {
                    if (h.groupName.Contains("Buzzsaw"))
                    {
                        h.hitBoxes[0].transform.localScale *= 1.5f;
                    }
                }
            }

            if (sawBarrierOnHit)
            {
                On.EntityStates.Toolbot.FireBuzzsaw.OnEnter += (orig, self) =>
                {
                    orig(self);
                    self.attack.AddModdedDamageType(SharedDamageTypes.SawBarrier);
                };

                IL.EntityStates.Toolbot.FireBuzzsaw.FixedUpdate += (il) =>
                {
                    ILCursor c = new ILCursor(il);
                    if (c.TryGotoNext(MoveType.After,
                         x => x.MatchLdfld(typeof(EntityStates.Toolbot.FireBuzzsaw), "hitOverlapLastTick")
                         ))
                    {
                        c.Emit(OpCodes.Ldarg_0);
                        c.EmitDelegate<Func<bool, EntityStates.Toolbot.FireBuzzsaw, bool>>((hitEnemy, self) =>
                        {
                            if (hitEnemy && self.isAuthority && self.healthComponent)
                            {
                                self.healthComponent.AddBarrierAuthority(self.healthComponent.fullCombinedHealth * 0.003f);
                            }
                            return hitEnemy;
                        });
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("RiskyMod: Toolbot SawBarrier IL Hook failed");
                    }
                };

                sawDef.skillDescriptionToken = "TOOLBOT_PRIMARY_ALT3_DESCRIPTION_RISKYMOD";
            }
        }

        private void ModifySecondaries(SkillLocator sk)
        {
            if (!enableSecondarySkillChanges) return;

            GameObject nadePrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/CryoCanisterProjectile").InstantiateClone("RiskyModCryoCanister", true);
            DamageAPI.ModdedDamageTypeHolderComponent md = nadePrefab.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>();
            md.Add(SharedDamageTypes.AntiFlyingForce);
            Content.Content.projectilePrefabs.Add(nadePrefab);

            SneedUtils.SneedUtils.SetEntityStateField("EntityStates.Toolbot.AimStunDrone", "projectilePrefab", nadePrefab);
        }

        private void ModifySpecials(SkillLocator sk)
        {
            if (enableRetoolChanges)
            {
                On.EntityStates.Toolbot.ToolbotStanceSwap.OnEnter += (orig, self) =>
                {
                    orig(self);
                    if (self.isAuthority)
                    {
                        GenericSkill skill1 = self.GetPrimarySkill1();
                        if (skill1) skill1.stock = skill1.maxStock;

                        GenericSkill skill2 = self.GetPrimarySkill2();
                        if (skill2) skill2.stock = skill2.maxStock;
                    }
                };
            }

            if (enablePowerModeChanges)
            {
                SkillDef powerSkillDef = Addressables.LoadAssetAsync<SkillDef>("RoR2/Base/Toolbot/ToolbotDualWield.asset").WaitForCompletion();
                BuffDef powerDef = LegacyResourcesAPI.Load<BuffDef>("BuffDefs/SmallArmorBoost");

                powerSkillDef.skillDescriptionToken = "TOOLBOT_SPECIAL_ALT_DESCRIPTION_RISKYMOD";

                PowerModeBuff = SneedUtils.SneedUtils.CreateBuffDef(
                "RiskyMod_PowerModeBuff",
                false,
                false,
                false,
                new Color(0.839f, 0.788f, 0.227f),
                powerDef.iconSprite
                );

                RecalculateStatsAPI.GetStatCoefficients += HandlePowerMode;
                SneedUtils.SneedUtils.SetEntityStateField("EntityStates.Toolbot.ToolbotDualWieldBase", "bonusBuff", PowerModeBuff);
            }
        }

        private void HandlePowerMode(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.HasBuff(PowerModeBuff.buffIndex))
            {
                args.armorAdd += 60f;
            }
        }
    }
}

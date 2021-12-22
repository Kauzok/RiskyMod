﻿using RoR2;
using UnityEngine;
using RoR2.Projectile;
using MonoMod.Cil;
using System;
using R2API;
using Mono.Cecil.Cil;

namespace RiskyMod.Survivors.Croco
{
    public class ModifyM2Spit
    {
        private static GameObject spitProjectile;
        public ModifyM2Spit()
        {
            spitProjectile = Resources.Load<GameObject>("prefabs/projectiles/crocospit");

            IL.EntityStates.Croco.FireSpit.OnEnter += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                     x => x.MatchCallvirt<ProjectileManager>("FireProjectile")
                    );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<FireProjectileInfo, EntityStates.Croco.FireSpit, FireProjectileInfo>>((projectileInfo, self) =>
                {
                    if (self.projectilePrefab == spitProjectile)
                    {
                        projectileInfo.damageTypeOverride = DamageType.BlightOnHit;
                    }
                    return projectileInfo;
                });
            };
        }
    }
}
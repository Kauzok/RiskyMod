﻿using MonoMod.Cil;
using RoR2;
using UnityEngine;
using System;

namespace RiskyMod.Enemies.Mobs
{
    public class Golem
    {
        public static bool enabled = true;
        public Golem()
        {
            if (!enabled) return;
            //GameObject golemObject = Resources.Load<GameObject>("prefabs/characterbodies/golembody");
            RemoveClapFalloff();
        }

        private void RemoveClapFalloff()
        {
            IL.EntityStates.GolemMonster.ClapState.FixedUpdate += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                     x => x.MatchCallvirt<BlastAttack>("Fire")
                    );
                c.EmitDelegate<Func<BlastAttack, BlastAttack>>(blastAttack =>
                {
                    blastAttack.falloffModel = BlastAttack.FalloffModel.None;
                    return blastAttack;
                });
            };
        }
    }
}
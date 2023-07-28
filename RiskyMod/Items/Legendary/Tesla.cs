﻿using UnityEngine;
using RoR2;
using RoR2.Orbs;
using UnityEngine.Networking;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;

namespace RiskyMod.Items.Legendary
{
    public class Tesla
    {
        public static bool enabled = true;
        public Tesla()
        {
            if (!enabled) return;

            IL.RoR2.Items.ShockNearbyBodyBehavior.FixedUpdate += (il) =>
            {
                bool error = true;
                bool error2 = true;
                ILCursor c = new ILCursor(il);

                if (RiskyMod.disableProcChains)
                {
                    if (c.TryGotoNext(
                        x => x.MatchStfld<RoR2.Orbs.LightningOrb>("procCoefficient")
                       ))
                    {
                        c.Index--;
                        c.Next.Operand = 0f;
                        error = false;
                    }
                }

                if (c.TryGotoNext(
                     x => x.MatchStfld<RoR2.Orbs.LightningOrb>("range")
                    ))
                {
                    c.Index--;
                    c.Next.Operand = 25f;
                    error2 = false;
                }

                if (error || error2)
                {
                    UnityEngine.Debug.LogError("RiskyMod: Tesla IL Hook failed");
                }
            };
        }
    }
}

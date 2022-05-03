﻿using MonoMod.Cil;
using System;
using RoR2;

namespace RiskyMod.Tweaks.RunScaling
{
    public class PriceScaling
    {
        public static bool enabled = true;
        public PriceScaling()
        {
            if (!enabled) return;

            IL.RoR2.Run.GetDifficultyScaledCost_int_float += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchLdcR4(1.25f)
                    );
                c.Next.Operand = 1f;

                c.GotoNext(MoveType.After, x => x.MatchMul());
                c.EmitDelegate<Func<float, float>>(price =>
                {
                    return price * Run.instance.difficultyCoefficient * ((1 + 0.4f * Run.instance.difficultyCoefficient)/1.4f);
                });
            };
        }
    }
}
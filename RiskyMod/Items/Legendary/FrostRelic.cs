﻿using R2API;
using RoR2;
using UnityEngine;

namespace RiskyMod.Items.Legendary
{
    public class FrostRelic
    {
        public static bool enabled = true;

        public FrostRelic()
        {
            if (!enabled) return;
            ItemsCore.ModifyItemDefActions += ModifyItem;

            On.RoR2.IcicleAuraController.Awake += (orig, self) =>
            {
                orig(self);

                /*
                Debug.Log(self.baseIcicleAttackInterval);   //0.25
                Debug.Log(self.icicleBaseRadius);   //6
                Debug.Log(self.icicleRadiusPerIcicle);  //2
                Debug.Log(self.icicleDamageCoefficientPerTick); //3
                Debug.Log(self.icicleDamageCoefficientPerTickPerIcicle);    //0
                Debug.Log(self.icicleDuration); //5
                Debug.Log(self.icicleProcCoefficientPerTick);   //0.2
                Debug.Log(self.icicleCountOnFirstKill); //0
                Debug.Log(self.baseIcicleMax);  //6
                Debug.Log(self.icicleMaxPerStack);  //6
                */

                if (RiskyMod.disableProcChains)
                {
                    self.icicleProcCoefficientPerTick = 0f;
                }
                //self.icicleRadiusPerIcicle = 3f;
                self.icicleDuration = 6f;
            };

            On.RoR2.IcicleAuraController.FixedUpdate += (orig, self) =>
            {
                //Update damage based on stack
                int itemCount = 1;
                if (self.cachedOwnerInfo.characterBody && self.cachedOwnerInfo.characterBody.inventory)
                {
                    itemCount = self.cachedOwnerInfo.characterBody.inventory.GetItemCount(RoR2Content.Items.Icicle);
                }
                self.icicleDamageCoefficientPerTick = 2f + itemCount;   //was 1.5f + 1.5f * itemCount

                orig(self);
            };

            //Clamp max radius
            On.RoR2.IcicleAuraController.UpdateRadius += (orig, self) =>
            {
                if (self.owner)
                {
                    if (self.finalIcicleCount > 0)
                    {
                        float maxRadius = self.icicleBaseRadius + self.baseIcicleMax * self.icicleRadiusPerIcicle;
                        float calculatedRadius = self.cachedOwnerInfo.characterBody ? (self.cachedOwnerInfo.characterBody.radius + self.icicleBaseRadius + self.icicleRadiusPerIcicle * self.finalIcicleCount) : 0f;
                        self.actualRadius = Mathf.Min(maxRadius, calculatedRadius);
                        return;
                    }
                    self.actualRadius = 0f;
                }
            };
        }
        private static void ModifyItem()
        {
            HG.ArrayUtils.ArrayAppend(ref ItemsCore.changedItemDescs, RoR2Content.Items.Icicle);
        }
    }
}

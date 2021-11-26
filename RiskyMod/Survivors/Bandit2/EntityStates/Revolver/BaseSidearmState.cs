﻿using UnityEngine;

namespace EntityStates.RiskyMod.Bandit2.Revolver
{
	public class BaseSidearmState : BaseState
	{
		public virtual string exitAnimationStateName
		{
			get
			{
				return "BufferEmpty";
			}
		}

		public override void OnEnter()
		{
			base.OnEnter();
			LoadStats();
			this.animator = base.GetModelAnimator();
			this.duration = this.baseDuration / this.attackSpeedStat;
			if (this.animator)
			{
				this.bodySideWeaponLayerIndex = this.animator.GetLayerIndex("Body, SideWeapon");
				this.animator.SetLayerWeight(this.bodySideWeaponLayerIndex, 1f);
			}
			if (this.crosshairOverridePrefab)
			{
				this.originalCrosshairPrefab = base.characterBody.crosshairPrefab;
				base.characterBody.crosshairPrefab = this.crosshairOverridePrefab;
			}
			base.characterBody.SetAimTimer(3f);
		}

		public override void OnExit()
		{
			if (this.animator)
			{
				this.animator.SetLayerWeight(this.bodySideWeaponLayerIndex, 0f);
			}
			base.PlayAnimation("Gesture, Additive", this.exitAnimationStateName);
			if (this.crosshairOverridePrefab)
			{
				base.characterBody.crosshairPrefab = this.originalCrosshairPrefab;
			}
			Transform transform = base.FindModelChild("SpinningPistolFX");
			if (transform)
			{
				transform.gameObject.SetActive(false);
			}
			base.OnExit();
		}

		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.PrioritySkill;
		}

		public virtual void LoadStats() { }

		[SerializeField]
		public float baseDuration;
		[SerializeField]
		public GameObject crosshairOverridePrefab;
		protected float duration;
		private Animator animator;
		private int bodySideWeaponLayerIndex;
		private GameObject originalCrosshairPrefab;
	}
}

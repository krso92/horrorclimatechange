﻿#define SAFE_MODE

//#define DEBUG_ENABLED

using System;
using JetBrains.Annotations;

namespace Sisus
{
	[Serializable]
	public class TweenedBool
	{
		private static float tweenSpeed = 6f;
		
		private float tweenProgress;
		private bool targetValue;
		private IInspectorDrawer drawer;
		private Action<bool> onTweenFinished;

		public bool NowTweening
		{
			get
			{
				return drawer != null;
			}
		}

		public TweenedBool() { }

		public TweenedBool(bool flag)
		{
			tweenProgress = flag ? 1f : 0f;
			targetValue = flag;
		}
		
		public void SetTarget([NotNull]IInspectorDrawer inspectorDrawer, bool setTarget, Action<bool> onFinished = null)
		{
			#if DEV_MODE && DEBUG_ENABLED
			UnityEngine.Debug.Log(StringUtils.ToColorizedString("TweenedBool.SetTarget(", setTarget, ") with tweenProgress=", tweenProgress, ", targetValue=", targetValue, ", NowTweening=", NowTweening, ", onFinished=", onFinished));
			#endif

			#if SAFE_MODE || DEV_MODE
			if(inspectorDrawer == null)
			{
				UnityEngine.Debug.LogError("TweenedBool.SetTarget called with null inspectorDrawer. Use SetValueInstant instead!");

				targetValue = setTarget;
				tweenProgress = setTarget ? 1f : 0f;
				return;
			}
			#endif
			
			targetValue = setTarget;
			
			// stop any tweens in progress
			if(NowTweening)
			{
				StopTween();
			}

			// if already at target value, we don't need to do any tweening
			// and should call onFinished immediately
			if(setTarget ? tweenProgress >= 1f : tweenProgress <= 0f)
			{
				if(onFinished != null)
				{
					onFinished(targetValue);
				}
				return;
			}

			onTweenFinished += onFinished;
			drawer = inspectorDrawer;

			//cache tween speed
			tweenSpeed = InspectorUtility.Preferences.foldingAnimationSpeed;

			StartTween();
			
			// if tween speed is invalid value or extremely high value
			// then skip the animation completely
			if(tweenSpeed <= 0f || tweenSpeed >= 144f)
			{
				SetValueInstant(targetValue);
			}
		}

		private void StartTween()
		{
			#if DEV_MODE && DEBUG_ENABLED
			UnityEngine.Debug.Log(StringUtils.ToColorizedString("TweenedBool.StartTween with tweenProgress=", tweenProgress, ", targetValue=", targetValue, ", NowTweening=", NowTweening, ", onTweenFinished=", onTweenFinished));
			#endif

			drawer.OnUpdate += Update;

			//UPDATE: Set tweenProgress to value just above 0f immediately after tween is started
			//this makes it simpler to check if TweenedBool is true or in the process of
			//tweening by simply checking if its float value is > 0f
			if(targetValue && tweenProgress <= 0f)
			{
				tweenProgress = float.Epsilon;
			}
		}

		private void Update(float deltaTime)
		{
			drawer.Repaint();

			if(targetValue)
			{
				tweenProgress += deltaTime * tweenSpeed;
				if(tweenProgress >= 1f)
				{
					tweenProgress = 1f;
					StopTween();
				}
			}
			else
			{
				tweenProgress -= deltaTime * tweenSpeed;
				if(tweenProgress <= 0f)
				{
					tweenProgress = 0f;
					StopTween();
				}
			}
		}
		
		private void StopTween()
		{
			#if DEV_MODE && DEBUG_ENABLED
			UnityEngine.Debug.Log(StringUtils.ToColorizedString("TweenedBool.StopTween with tweenProgress=", tweenProgress, ", targetValue=", targetValue, ", NowTweening=", NowTweening));
			#endif

			drawer.OnUpdate -= Update;
			drawer = null;

			if(targetValue)
			{
				if(tweenProgress >= 1f)
				{
					if(onTweenFinished != null)
					{
						var callback = onTweenFinished;
						onTweenFinished = null;
						callback(targetValue);
					}
				}
			}
			else if(tweenProgress <= 0f)
			{
				if(onTweenFinished != null)
				{
					var callback = onTweenFinished;
					onTweenFinished = null;
					callback(targetValue);
				}
			}
		}

		public void SetValueInstant(bool setValue)
		{
			#if DEV_MODE && DEBUG_ENABLED
			UnityEngine.Debug.Log(StringUtils.ToColorizedString("TweenedBool.SetValueInstant(", setValue, ") with tweenProgress=", tweenProgress, ", targetValue=", targetValue, ", NowTweening=", NowTweening));
			#endif

			tweenProgress = setValue ? 1f : 0f;
			targetValue = setValue;
			if(NowTweening)
			{
				StopTween();
			}
		}
		
		public static implicit operator bool(TweenedBool tweenedBool)
		{
			return tweenedBool.targetValue;
		}

		public static implicit operator float(TweenedBool tweenedBool)
		{
			return tweenedBool.tweenProgress;
		}

		public void Dispose()
		{
			if(drawer != null)
			{
				drawer.OnUpdate -= Update;
				drawer = null;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LitMotion.Animation
{
    [Serializable]
    public abstract class LitMotionAnimationComponent
    {
        public LitMotionAnimationComponent()
        {
#if UNITY_EDITOR
            var type = GetType();
            var attribute = type.GetCustomAttribute<LitMotionAnimationComponentMenuAttribute>();
            displayName = attribute != null
                ? attribute.MenuName.Split('/').Last()
                : type.Name;
#endif
        }

        [SerializeField] string displayName;
        [SerializeField] bool enabled = true;

        public bool Enabled => enabled;
        public string DisplayName => displayName;

        public abstract MotionHandle Play();

        public virtual bool TryGetDuration(out float duration)
        {
            duration = 0.0f;
            return false;
        }

        internal virtual bool TryGetDuration(HashSet<LitMotionAnimation> calculatingAnimations, out float duration)
        {
            return TryGetDuration(out duration);
        }

        protected static bool TryCalculateDuration<TValue, TOptions>(SerializableMotionSettings<TValue, TOptions> settings, out float duration)
            where TValue : unmanaged
            where TOptions : unmanaged, IMotionOptions
        {
            duration = 0.0f;
            if (settings == null)
                return false;

            if (settings.Loops < 0)
            {
                duration = float.PositiveInfinity;
                return true;
            }

            duration = settings.Duration * settings.Loops;
            duration += settings.DelayType == DelayType.EveryLoop
                ? settings.Delay * settings.Loops
                : settings.Delay;
            return !float.IsNaN(duration);
        }

        public virtual void OnResume() { }
        public virtual void OnPause() { }
        public virtual void OnStop() { }

        public MotionHandle TrackedHandle { get; set; }
    }
}

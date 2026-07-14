using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LitMotion.Animation.Components
{
    [Serializable]
    [LitMotionAnimationComponentMenu("Control/Delay")]
    public sealed class DelayComponent : LitMotionAnimationComponent
    {
        [SerializeField] float delay;

        public override MotionHandle Play()
        {
            return LMotion.Create(0f, 1f, delay)
                .RunWithoutBinding();
        }

        public override bool TryGetDuration(out float duration)
        {
            duration = delay;
            return !float.IsNaN(duration);
        }

        public override void OnStop() { }
    }

    [Serializable]
    [LitMotionAnimationComponentMenu("Control/Event")]
    public sealed class EventComponent : LitMotionAnimationComponent
    {
        [Space(5f)]
        [SerializeField] UnityEvent onPlay;
        [SerializeField] UnityEvent onStop;

        public override MotionHandle Play()
        {
            onPlay.Invoke();
            return LMotion.Create(0f, 1f, 0f).RunWithoutBinding();
        }

        public override bool TryGetDuration(out float duration)
        {
            duration = 0.0f;
            return true;
        }

        public override void OnStop()
        {
            onStop.Invoke();
        }
    }

    [Serializable]
    [LitMotionAnimationComponentMenu("Control/Play LitMotion Animation")]
    public sealed class PlayLitMotionAnimationComponent : LitMotionAnimationComponent
    {
        [SerializeField] LitMotionAnimation target;

        public override MotionHandle Play()
        {
            target.Play();
            return LMotion.Create(0f, 1f, float.MaxValue)
                .Bind(this, (x, state) =>
                {
                    if (target == null) TrackedHandle.TryComplete();
                    if (!target.IsPlaying) TrackedHandle.TryComplete();
                });
        }

        public override bool TryGetDuration(out float duration)
        {
            if (target == null)
            {
                duration = 0.0f;
                return false;
            }

            return target.TryGetDuration(out duration);
        }

        internal override bool TryGetDuration(HashSet<LitMotionAnimation> calculatingAnimations, out float duration)
        {
            if (target == null)
            {
                duration = 0.0f;
                return false;
            }

            return target.TryGetDuration(calculatingAnimations, out duration);
        }

        public override void OnResume()
        {
            target.Play();
        }

        public override void OnPause()
        {
            target.Pause();
        }

        public override void OnStop()
        {
            target.Stop();
        }
    }
}

using System;
using System.Collections.Generic;
using LitMotion.Collections;
using UnityEngine;

namespace LitMotion.Animation
{
    [AddComponentMenu("LitMotion Animation")]
    public sealed class LitMotionAnimation : MonoBehaviour, ISerializationCallbackReceiver
    {
        public enum AutoPlayMode
        {
            None,
            OnStart,
            OnEnable
        }

        public enum AnimationMode
        {
            Parallel,
            Sequential
        }

        [SerializeField] AutoPlayMode autoPlayMode = AutoPlayMode.OnStart;
        [SerializeField] AnimationMode animationMode;

        [SerializeReference]
        LitMotionAnimationComponent[] components;

        readonly Queue<LitMotionAnimationComponent> queue = new();
        FastListCore<LitMotionAnimationComponent> playingComponents;

        [HideInInspector, SerializeField] bool playOnAwake = true;
        [HideInInspector, SerializeField] int version;

        HashSet<LitMotionAnimation> _Hash4CalcDuration = new HashSet<LitMotionAnimation>();

        public AutoPlayMode PlayMode { get => autoPlayMode; set => autoPlayMode = value; }
        public AnimationMode AnimMode { get => animationMode; set => animationMode = value; }
        public bool PlayOnAwake { get => playOnAwake; set => playOnAwake = value; }
        public IReadOnlyList<LitMotionAnimationComponent> Components => components;


        void OnEnable()
        {
            if (autoPlayMode == AutoPlayMode.OnEnable)
                Play();
        }

        void Start()
        {
            if (autoPlayMode == AutoPlayMode.OnStart)
                Play();
        }

        void MoveNextMotion()
        {
            if (queue.TryDequeue(out var queuedComponent))
            {
                try
                {
                    var handle = queuedComponent.Play();
                    var isActive = handle.IsActive();

                    if (isActive)
                    {
                        handle.Preserve();
                        MotionManager.GetManagedDataRef(handle, false).OnCompleteAction += MoveNextMotion;
                    }

                    queuedComponent.TrackedHandle = handle;
                    playingComponents.Add(queuedComponent);

                    if (!isActive)
                    {
                        MoveNextMotion();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, context: this);
                }
            }
        }

        public void Play()
        {
            var isPlaying = false;

            foreach (var component in playingComponents.AsSpan())
            {
                var handle = component.TrackedHandle;
                if (handle.IsActive())
                {
                    handle.PlaybackSpeed = 1f;
                    isPlaying = true;

                    component.OnResume();
                }
            }

            if (isPlaying) return;

            playingComponents.Clear();

            switch (animationMode)
            {
                case AnimationMode.Sequential:
                    foreach (var component in components)
                    {
                        if (component == null) continue;
                        if (!component.Enabled) continue;
                        queue.Enqueue(component);
                    }

                    MoveNextMotion();
                    break;
                case AnimationMode.Parallel:
                    foreach (var component in components)
                    {
                        if (component == null) continue;
                        if (!component.Enabled) continue;

                        try
                        {
                            var handle = component.Play();
                            component.TrackedHandle = handle;

                            if (handle.IsActive())
                            {
                                handle.Preserve();
                            }

                            playingComponents.Add(component);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex, context: this);
                        }
                    }
                    break;
            }
        }

        public void Pause()
        {
            foreach (var component in playingComponents.AsSpan())
            {
                var handle = component.TrackedHandle;
                if (handle.IsActive())
                {
                    handle.PlaybackSpeed = 0f;
                    component.OnPause();
                }
            }
        }

        public void Stop()
        {
            var span = playingComponents.AsSpan();
            span.Reverse();
            foreach (var component in span)
            {
                var handle = component.TrackedHandle;
                handle.TryCancel();
                component.OnStop();
                component.TrackedHandle = handle;
            }

            playingComponents.Clear();
            queue.Clear();
        }

        public void Restart()
        {
            Stop();
            Play();
        }

        public bool TryGetDuration(out float duration)
        {
			_Hash4CalcDuration.Clear();
            return TryGetDuration(_Hash4CalcDuration, out duration);
        }

        internal bool TryGetDuration(HashSet<LitMotionAnimation> calculatingAnimations, out float duration)
        {
            duration = 0.0f;
            if (!calculatingAnimations.Add(this))
                return false;

            try
            {
                if (components == null)
                    return true;

                foreach (LitMotionAnimationComponent component in components)
                {
                    if (component == null || !component.Enabled)
                        continue;

                    if (!component.TryGetDuration(calculatingAnimations, out float componentDuration))
                        return false;

                    switch (animationMode)
                    {
                        case AnimationMode.Parallel:
                            duration = Math.Max(duration, componentDuration);
                            break;
                        case AnimationMode.Sequential:
                            duration += componentDuration;
                            break;
                        default:
                            duration = 0.0f;
                            return false;
                    }
                }

                return !float.IsNaN(duration);
            }
            finally
            {
                calculatingAnimations.Remove(this);
            }
        }

        public bool IsActive
        {
            get
            {
                if (queue.Count > 0) return true;

                foreach (var component in playingComponents.AsSpan())
                {
                    var handle = component.TrackedHandle;
                    if (handle.IsActive()) return true;
                }

                return false;
            }
        }

        public bool IsPlaying
        {
            get
            {
                if (queue.Count > 0) return true;

                foreach (var component in playingComponents.AsSpan())
                {
                    var handle = component.TrackedHandle;
                    if (handle.IsPlaying()) return true;
                }

                return false;
            }
        }

        void OnDisable()
        {
            if (autoPlayMode == AutoPlayMode.OnEnable)
                Stop();
        }

        void OnDestroy()
        {
            Stop();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (version < 1)
            {
                autoPlayMode = playOnAwake ? AutoPlayMode.OnStart : AutoPlayMode.None;
                version = 1;
            }
        }
    }
}

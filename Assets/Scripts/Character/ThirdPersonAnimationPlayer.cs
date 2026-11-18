using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Character
{
    public sealed class ThirdPersonAnimationPlayer : IDisposable
    {
        private const int StateCount = 5;
        private readonly PlayableGraph _graph;
        private readonly AnimationMixerPlayable _mixer;
        private readonly AnimationClipPlayable[] _playables = new AnimationClipPlayable[StateCount];
        private readonly AnimationClip[] _clips;
        private readonly double[] _times = new double[StateCount];
        private readonly float[] _blendFrom = new float[StateCount];
        private readonly float _blendDuration;
        private readonly float _airbornePose;
        private readonly bool _holdJumpPoseOnFall;
        private ThirdPersonController.CharacterState _state;
        private float _blendElapsed;

        public ThirdPersonAnimationPlayer(Animator animator, AnimationClip idle, AnimationClip walk,
            AnimationClip run, AnimationClip jump, AnimationClip fall, float blendDuration, float airbornePose)
        {
            _clips = new[] { idle, walk, run, jump, fall != null ? fall : jump };
            _blendDuration = Mathf.Max(0f, blendDuration);
            _airbornePose = Mathf.Clamp01(airbornePose);
            _holdJumpPoseOnFall = fall == null;

            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            _graph = PlayableGraph.Create("Third Person Clip Animation");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _mixer = AnimationMixerPlayable.Create(_graph, StateCount);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Character", animator);
            output.SetSourcePlayable(_mixer);

            for (int i = 0; i < StateCount; i++)
            {
                _playables[i] = AnimationClipPlayable.Create(_graph, _clips[i]);
                _playables[i].SetApplyFootIK(false);
                _playables[i].SetSpeed(0d); // Sample time explicitly, including loops and held poses.
                _graph.Connect(_playables[i], 0, _mixer, i);
                _mixer.SetInputWeight(i, i == (int)ThirdPersonController.CharacterState.Idle ? 1f : 0f);
            }

            _state = ThirdPersonController.CharacterState.Idle;
            _blendElapsed = _blendDuration;
            _graph.Play();
        }

        public void SetState(ThirdPersonController.CharacterState state)
        {
            if (state == _state) return;

            for (int i = 0; i < StateCount; i++)
            {
                // Snapshot current weights so another state change can interrupt a blend smoothly.
                _blendFrom[i] = _mixer.GetInputWeight(i);
            }

            int target = (int)state;
            if (_blendFrom[target] < 0.001f || state == ThirdPersonController.CharacterState.Jump)
            {
                _times[target] = 0d;
            }

            _state = state;
            _blendElapsed = 0f;
        }

        public void Tick(float deltaTime, float jumpProgress)
        {
            if (!_graph.IsValid()) return;

            deltaTime = Mathf.Max(0f, deltaTime);
            _blendElapsed += deltaTime;
            float blend = _blendDuration > 0f ? Mathf.Clamp01(_blendElapsed / _blendDuration) : 1f;
            blend = Mathf.SmoothStep(0f, 1f, blend);

            for (int i = 0; i < StateCount; i++)
            {
                float targetWeight = i == (int)_state ? 1f : 0f;
                float weight = Mathf.Lerp(_blendFrom[i], targetWeight, blend);
                _mixer.SetInputWeight(i, weight);
                if (weight <= 0f) continue;

                double length = _clips[i].length;
                if (i == (int)ThirdPersonController.CharacterState.Jump)
                {
                    // Match takeoff to the physical ascent instead of playing landing poses in midair.
                    _times[i] = length * _airbornePose * Mathf.Clamp01(jumpProgress);
                }
                else if (i == (int)ThirdPersonController.CharacterState.Fall && _holdJumpPoseOnFall)
                {
                    _times[i] = length * _airbornePose;
                }
                else if (length > 0d)
                {
                    // Locomotion and an optional fall clip loop independently of import settings.
                    _times[i] = (_times[i] + deltaTime) % length;
                }

                _playables[i].SetTime(_times[i]);
            }

            _graph.Evaluate(deltaTime);
        }

        public void Dispose()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }
    }
}

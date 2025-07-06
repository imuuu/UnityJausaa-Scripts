// PathFollower.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using Game.SkillSystem;    // for SpeedUtility & SPEED_TYPE
using Game.PathSystem;
using Game.Extensions;    // adjust if your PathCreator lives elsewhere

namespace Game.PathSystem
{
    [RequireComponent(typeof(Transform))]
    public class PathFollower : MonoBehaviour
    {
        public enum FOLLOW_MODE { ONCE, LOOP, PING_PONG }

        [Header("Parameters")]
        [SerializeField] private PathFollowerParameters _parameters;

        // runtime state
        private float _traveledDistance;
        private int _direction = 1;
        private float _t = 0f;
        private bool _initialized;
        private List<float> _sampleTs;
        private List<float> _sampleLengths;
        private float _totalLength;
        private int _lastPointCount;
        private bool _lastClosed;
        private float _progress = 0f; // [0..1] progress along path, reverse is [1..0]
        private Vector3 _lastPos = Vector3.zero;

        public Action OnComplete { get; set; }

        public void SetParameters(PathFollowerParameters parameters)
        {
            _parameters = parameters;
            _initialized = false;
            _traveledDistance = 0f;
            _t = 0f;
            _direction = 1;
        }

        public void Update()
        {
            if(ManagerPause.IsPaused()) return;

            PathCreator path = _parameters.Path;
            if (path == null || path.NumPoints < 4) return;

            if (!_initialized
             || path.NumPoints != _lastPointCount
             || path.IsClosed() != _lastClosed)
            {
                _lastPointCount = path.NumPoints;
                _lastClosed = path.IsClosed();
                InitializeArcLength(path);
            }

            // compute progress [0..1] (will naturally go 1→0 if reverse)
            _progress = (_totalLength > 0f)
                      ? _traveledDistance / _totalLength
                      : 0f;
            _progress = Mathf.Clamp01(_progress);

            // unified speed (fixed or variable)
            float speed = (_parameters.SpeedType == SPEED_TYPE.FIXED)
                        ? _parameters.BaseSpeed
                        : SpeedUtility.GetSpeed(
                              _parameters.SpeedType,
                              _parameters.BaseSpeed,
                              _parameters.MaxSpeed,
                              _progress
                          );

            float deltaDist = speed * Time.deltaTime * _direction;
            _traveledDistance += deltaDist;
            WrapDistance();

            // lookup parameter from distance (no 1‑t flip here!)
            float raw = GetTForDistance(_traveledDistance);
            _t = raw;

            _lastPos = transform.position;
           
            transform.position = GetPoint(path, _t);

            if (_parameters.IsLookForward)
            {
                transform.LookAt(GetPoint(path, _t + (_parameters.IsReverse ? -0.05f : 0.05f)));
            }

            if(OnComplete == null) return;

            if ((_parameters.FollowMode == FOLLOW_MODE.ONCE && !_parameters.IsReverse && _progress >= 1f)
             || (_parameters.FollowMode == FOLLOW_MODE.ONCE && _parameters.IsReverse  && _progress <= 0f))
            {
                //Debug.Log("PathFollower: Completed path travel.");
                OnComplete?.Invoke();
                OnComplete = null;
            }
        }
        


        private void WrapParam(ref float p)
        {
            switch (_parameters.FollowMode)
            {
                case FOLLOW_MODE.LOOP:
                    p = Mathf.Repeat(p, 1f);
                    break;
                case FOLLOW_MODE.PING_PONG:
                    if (p > 1f) { p = 2f - p; _direction = -1; }
                    else if (p < 0f) { p = -p; _direction = 1; }
                    break;
                case FOLLOW_MODE.ONCE:
                    p = Mathf.Clamp01(p);
                    break;
            }
        }

        private void WrapDistance()
        {
            switch (_parameters.FollowMode)
            {
                case FOLLOW_MODE.LOOP:
                    if (_traveledDistance > _totalLength)
                        _traveledDistance -= _totalLength;
                    else if (_traveledDistance < 0f)
                        _traveledDistance += _totalLength;
                    break;
                case FOLLOW_MODE.PING_PONG:
                    if (_traveledDistance > _totalLength)
                    {
                        _traveledDistance = _totalLength
                                          - (_traveledDistance - _totalLength);
                        _direction = -_direction;
                    }
                    else if (_traveledDistance < 0f)
                    {
                        _traveledDistance = -_traveledDistance;
                        _direction = -_direction;
                    }
                    break;
                case FOLLOW_MODE.ONCE:
                    _traveledDistance = Mathf.Clamp(
                        _traveledDistance, 0f, _totalLength);
                    break;
            }
        }

        private void InitializeArcLength(PathCreator path)
        {
            int segCount = (path.NumPoints - 1) / 3
                         + (path.IsClosed() ? 1 : 0);
            _sampleTs = new List<float>(segCount * _parameters.SamplesPerSegment + 1);
            _sampleLengths = new List<float>(segCount * _parameters.SamplesPerSegment + 1);

            Vector3 prev = GetPoint(path, 0f);
            _totalLength = 0f;
            _sampleTs.Add(0f);
            _sampleLengths.Add(0f);

            for (int s = 0; s < segCount; s++)
            {
                for (int i = 1; i <= _parameters.SamplesPerSegment; i++)
                {
                    float u = (s + (i / (float)_parameters.SamplesPerSegment)) / segCount;
                    Vector3 pt = GetPoint(path, u);
                    _totalLength += Vector3.Distance(prev, pt);
                    _sampleTs.Add(u);
                    _sampleLengths.Add(_totalLength);
                    prev = pt;
                }
            }

            // _traveledDistance = Mathf.Clamp(
            //     _traveledDistance, 0f, _totalLength);

            _traveledDistance = _parameters.IsReverse
                      ? _totalLength
                      : 0f;
            _direction = _parameters.IsReverse
                               ? -1
                               : 1;
            _initialized = true;
            _lastPointCount = path.NumPoints;
            _lastClosed = path.IsClosed();

            if(_parameters.IsReverse) _progress = 1f;
        }

        private float GetTForDistance(float dist)
        {
            int idx = _sampleLengths.BinarySearch(dist);
            if (idx >= 0) return _sampleTs[idx];
            idx = ~idx;
            if (idx <= 0) return _sampleTs[0];
            if (idx >= _sampleLengths.Count) return _sampleTs[^1];

            float l0 = _sampleLengths[idx - 1], l1 = _sampleLengths[idx];
            float t0 = _sampleTs[idx - 1], t1 = _sampleTs[idx];
            float f = (dist - l0) / (l1 - l0);
            return Mathf.Lerp(t0, t1, f);
        }

        private Vector3 GetPoint(PathCreator path, float t)
        {
            t = Mathf.Clamp01(t);
            if (path.IsClosed())
            {
                t %= 1f;
                if (t < 0f) t += 1f;
            }
            int segCount = (path.NumPoints - 1) / 3
                         + (path.IsClosed() ? 1 : 0);
            float overall = t * segCount;
            int si = Mathf.Min(Mathf.FloorToInt(overall), segCount - 1);
            float u = overall - si;
            int i = si * 3;
            return Bezier.GetPoint(
                path.GetPoint(i + 0),
                path.GetPoint(i + 1),
                path.GetPoint(i + 2),
                path.GetPoint(i + 3),
                u
            );
        }

        private Vector3 GetDerivative(PathCreator path, float t)
        {
            if (path.IsClosed())
            {
                t %= 1f;
                if (t < 0f) t += 1f;
            }
            int segCount = (path.NumPoints - 1) / 3
                         + (path.IsClosed() ? 1 : 0);
            float overall = t * segCount;
            int si = Mathf.Min(Mathf.FloorToInt(overall), segCount - 1);
            float u = overall - si;
            int i = si * 3;
            return Bezier.GetFirstDerivative(
                path.GetPoint(i + 0),
                path.GetPoint(i + 1),
                path.GetPoint(i + 2),
                path.GetPoint(i + 3),
                u
            );
        }

        private float GetDerivativeMagnitude(PathCreator path, float t)
            => GetDerivative(path, t).magnitude;
    }
}

// Bezier helper extension:


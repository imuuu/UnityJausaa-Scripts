using System;
using System.Collections.Generic;
using System.Linq;
using DigitalRuby.ThunderAndLightning;
using Game.HitDetectorSystem;
using Game.PoolSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.SkillSystem
{
    /// <summary>
    /// Determines the starting direction for a lightning bolt.
    /// </summary>
    // public enum LightningOrigin
    // {
    //     /// <summary>
    //     /// The bolt is aimed toward the mouse position.
    //     /// </summary>
    //     //AimedAtMouse,
    //     /// <summary>
    //     /// The bolt starts in a random horizontal direction around the player.
    //     /// </summary>
    //     RandomAroundPlayer
    // }

    public class Ability_Lightning : Ability, IRecastSkill
    {   
        [Title("Lightning Prefab"), Required]
        public GameObject _lightningPrefab;

        [Title("Lightning Settings")]
        [Tooltip("Number of segments for the lightning bolt (1 segment = 2 points).")]
        public int Lines = 5;

        //[Tooltip("Length of each lightning segment.")]
        //public float SegmentLength = 5f;

        [Tooltip("Angle spread (in degrees) for each segment’s random deviation.")]
        public float AngleSpread = 30f;

        public float _offsetY = 1;

        [Title("Lightning Options")]
        // [Tooltip("Choose the origin option for the lightning bolt.")]
        // public LightningOrigin OriginOption = LightningOrigin.RandomAroundPlayer;

        // [Tooltip("How many lightning bolts to spawn at once.")]
        // public int LightningCount = 1;

        [Title("Lightning Branching Options(Experimental, not hit detection or visuals)")]
        [Tooltip("Enable extra branch lines to simulate real lightning.")]
        public bool EnableBranches = false;

        [Tooltip("Number of branch lines to attempt per main segment.")]
        [ShowIf("EnableBranches")]
        public int BranchesPerSegment = 1;

        [Tooltip("Branch length multiplier relative to the main segment length.")]
        [ShowIf("EnableBranches")]
        public float BranchLengthMultiplier = 0.5f;

        [Tooltip("Maximum angle deviation (in degrees) for branch lines relative to the main segment direction.")]
        [ShowIf("EnableBranches")]
        public float BranchAngleDeviation = 45f;

        [Tooltip("Recursive depth for branch generation. (0 = no sub-branches, 1 = one level, etc.)")]
        [ShowIf("EnableBranches")]
        public int BranchRecursionDepth = 2;

        [Title("Debug Options")]
        public bool IsDebug = false;

        private List<Vector3[]> _lightningBolts;
        private List<Vector3[]> _branchSegments;
        private List<Vector3[]> _finalPointsList;

        private Transform _transform => GetLaunchUser().transform;
        private bool _generated = false;
        private GameObject _lightningBolt_SECOND;

        private const int NEEDED_POINTS = 4;

        public override void AwakeSkill()
        {
            base.AwakeSkill();

            PoolOptions poolOptions = new PoolOptions();
            poolOptions.PoolType = POOL_TYPE.DYNAMIC;
            poolOptions.ReturnType = POOL_RETURN_TYPE.TIMED;
            poolOptions.ReturnDelay = GetDuration();
            poolOptions.Min = 1;

            _lightningBolt_SECOND = GameObject.Instantiate(_lightningPrefab);
            _lightningBolt_SECOND.name = "LightningBolt ENGINE";

            IOwner owner = _lightningBolt_SECOND.GetComponent<IOwner>();
            if(owner == null) _lightningBolt_SECOND.AddComponent<Owner>();

            HitDetector_Lines hitDetector = _lightningBolt_SECOND.GetComponent<HitDetector_Lines>();
            if (hitDetector != null) hitDetector.SetManualDestroy(true);

            LightningSplineScript lightningBoltPathScript = _lightningBolt_SECOND.GetComponent<LightningSplineScript>();
            List<GameObject> lightningPath = new ();
            for (int i = 0; i < GetPointCount(); i++)
            {
                GameObject point = new GameObject("Point");
                point.transform.SetParent(_lightningBolt_SECOND.transform);
                lightningPath.Add(point);
            }
            lightningBoltPathScript.LightningPath = lightningPath;

            ManagerPrefabPooler.Instance.CreatePrefabPool(_lightningBolt_SECOND, poolOptions);
            _lightningBolt_SECOND.SetActive(false);
        }

        public override void StartSkill()
        {
            Debug.Log("Starting Lightning skill");
            _lightningBolts = ListPool<Vector3[]>.Get();
            _finalPointsList = ListPool<Vector3[]>.Get();

            if(EnableBranches)
                _branchSegments = ListPool<Vector3[]>.Get();

            GenerateLightningBolts();

            foreach (Vector3[] points in _lightningBolts)
            {
                GameObject lightning = ManagerPrefabPooler.Instance.GetFromPool(_lightningBolt_SECOND);
                lightning.SetActive(true);
                lightning.GetComponent<IOwner>().SetOwner(GetOwnerType());
                ApplyStats(lightning);

                lightning.transform.position = _transform.position;

                LightningSplineScript lightningSplineScript = lightning.GetComponent<LightningSplineScript>();
                lightningSplineScript.Camera = Camera.main;

                if (lightningSplineScript == null)
                {
                    Debug.LogWarning("LightningSplineScript is null");
                    continue;
                }

                Vector3[] finalPoints = points;

                HitDetector_Lines hitDetector = lightning.GetComponent<HitDetector_Lines>();
                if (hitDetector != null)
                {
                    hitDetector.SetLinePoints(points);
                    hitDetector.PerformHitCheck(out _);
                    finalPoints = hitDetector.GetPoints();
                    hitDetector.SetManualDestroy(true);
                }

                if(finalPoints.Count() < NEEDED_POINTS)
                {
                    finalPoints = EnsureFourPoints(finalPoints);
                }

                List<GameObject> newObjList = lightningSplineScript.LightningPath;
                newObjList.Clear();
                for (int i = 0; i < finalPoints.Length; i++)
                {
                    GameObject obj = lightningSplineScript.transform.GetChild(i).gameObject;
                    newObjList.Add(obj);
                }

                lightningSplineScript.LightningPath = newObjList;

                for (int i = 0; i < lightningSplineScript.LightningPath.Count; i++)
                {
                    if(IsDebug) MarkHelper.DrawSphereTimed(finalPoints[i], 0.1f, GetDuration(), Color.cyan);
                    lightningSplineScript.LightningPath[i].transform.position = finalPoints[i];
                }

                _finalPointsList.Add(finalPoints);

                lightningSplineScript.Trigger(GetDuration() - 0.05f);
            }
        }

        public override void UpdateSkill()
        {
#if UNITY_EDITOR
            if (!_generated)
                return;

            if(!IsDebug)
                return;

            for (int i = 0; i < _finalPointsList.Count; i++)
            {
                Color boltColor = Color.Lerp(Color.red, Color.yellow, (float)i / Mathf.Max(1, _finalPointsList.Count - 1));
                PointsUtilities.DrawLines(_finalPointsList[i], boltColor);
                PointsUtilities.DrawMedianDirection(_finalPointsList[i], Color.magenta, 2f);
            }

            if (EnableBranches)
            {
                foreach (Vector3[] branch in _branchSegments)
                {
                    Debug.DrawLine(branch[0], branch[1], Color.cyan);
                }
            }
#endif
        }

        public override void EndSkill()
        {
            for(int i = 0; i < _lightningBolts.Count;  i++)
            {
                Vector3ArrayPool.ReturnArray(_lightningBolts[i]);
            }

            ListPool<Vector3[]>.Return(_lightningBolts);
            ListPool<Vector3[]>.Return(_finalPointsList);
            if(EnableBranches) ListPool<Vector3[]>.Return(_branchSegments);
        }

        /// <summary>
        /// Generates all lightning bolts based on LightningCount.
        /// </summary>
        private void GenerateLightningBolts()
        {
            _lightningBolts.Clear();
            
            if(EnableBranches) _branchSegments.Clear();

            int totalLightningCount = _baseStats.GetStat(StatSystem.STAT_TYPE.PROJECTILE_COUNT).GetValueInt();
            for (int i = 0; i < totalLightningCount; i++)
            {
                Vector3[] bolt = GenerateLightningBolt();
                _lightningBolts.Add(bolt);

                if (EnableBranches)
                {
                    GenerateBranches(bolt);
                }
            }
            _generated = true;
        }

        private int GetPointCount()
        {
            return Lines + 1;
        }

        private float GetSegmentLength()
        {
            return _baseStats.GetStat(StatSystem.STAT_TYPE.RANGE).GetValue();
        }

        /// <summary>
        /// Generates a single lightning bolt as an array of points.
        /// </summary>
        private Vector3[] GenerateLightningBolt()
        {
            int pointCount = GetPointCount();
            Vector3[] boltPoints = Vector3ArrayPool.GetArray(pointCount);

            Vector3 startPos = _transform.position;
            startPos.y = _offsetY;
            Vector3 baseDirection = GetDirection(_direction);

            // if (OriginOption == LightningOrigin.AimedAtMouse)
            // {
            //     Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //     Plane plane = new Plane(Vector3.up, _transform.position);
            //     if (plane.Raycast(ray, out float enter))
            //     {
            //         Vector3 mouseWorldPos = ray.GetPoint(enter);
            //         baseDirection = (mouseWorldPos - _transform.position).normalized;
            //     }
            // }
            // else if (OriginOption == LightningOrigin.RandomAroundPlayer)
            // {
            //     float randomAngle = UnityEngine.Random.Range(0f, 360f);
            //     baseDirection = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;
            // }

            boltPoints[0] = startPos;
            Vector3 currentDirection = baseDirection;

            for (int i = 1; i < pointCount; i++)
            {
                if (i > 1)
                {
                    Vector3 centroid = ComputeCentroid(boltPoints, i);
                    if (boltPoints[i - 1] != centroid)
                    {
                        currentDirection = (boltPoints[i - 1] - centroid).normalized;
                    }
                }
                float deviation = UnityEngine.Random.Range(-AngleSpread / 2f, AngleSpread / 2f);
                Vector3 newDir = Quaternion.AngleAxis(deviation, Vector3.up) * currentDirection;
                boltPoints[i] = boltPoints[i - 1] + newDir.normalized * GetSegmentLength();
            }

            return boltPoints;
        }

        /// <summary>
        /// Generates branch segments for the lightning bolt.
        /// </summary>
        private void GenerateBranches(Vector3[] bolt)
        {
            for (int seg = 1; seg < bolt.Length; seg++)
            {
                Vector3 segmentStart = bolt[seg - 1];
                Vector3 segmentEnd = bolt[seg];
                Vector3 mainDir = (segmentEnd - segmentStart).normalized;

                for (int b = 0; b < BranchesPerSegment; b++)
                {
                    float t = UnityEngine.Random.Range(0.3f, 0.7f);
                    Vector3 branchStart = Vector3.Lerp(segmentStart, segmentEnd, t);
                    float deviation = UnityEngine.Random.Range(-BranchAngleDeviation, BranchAngleDeviation);
                    Vector3 branchDir = Quaternion.AngleAxis(deviation, Vector3.up) * mainDir;
                    float branchLength = GetSegmentLength() * BranchLengthMultiplier;
                    GenerateBranchRecursive(branchStart, branchDir, branchLength, BranchAngleDeviation, BranchRecursionDepth);
                }
            }

            if (bolt.Length >= 2)
            {
                Vector3 lastDir = (bolt[bolt.Length - 1] - bolt[bolt.Length - 2]).normalized;
                float deviation = UnityEngine.Random.Range(-BranchAngleDeviation, BranchAngleDeviation);
                Vector3 branchDir = Quaternion.AngleAxis(deviation, Vector3.up) * lastDir;
                float branchLength = GetSegmentLength() * BranchLengthMultiplier;
                GenerateBranchRecursive(bolt[bolt.Length - 1], branchDir, branchLength, BranchAngleDeviation, BranchRecursionDepth);
            }
        }

        /// <summary>
        /// Recursively generates branch segments.
        /// </summary>
        private void GenerateBranchRecursive(Vector3 branchStart, Vector3 branchDirection, float branchLength, float branchAngleDeviation, int depth)
        {
            Vector3 branchEnd = branchStart + branchDirection.normalized * branchLength;
            _branchSegments.Add(new Vector3[] { branchStart, branchEnd });

            if (depth <= 0)
                return;

            for (int i = 0; i < BranchesPerSegment; i++)
            {
                float t = UnityEngine.Random.Range(0.3f, 0.7f);
                Vector3 subStart = Vector3.Lerp(branchStart, branchEnd, t);
                float subDeviation = UnityEngine.Random.Range(-branchAngleDeviation, branchAngleDeviation);
                Vector3 subDir = Quaternion.AngleAxis(subDeviation, Vector3.up) * branchDirection;
                float subLength = branchLength * BranchLengthMultiplier;
                GenerateBranchRecursive(subStart, subDir, subLength, branchAngleDeviation, depth - 1);
            }
        }

        /// <summary>
        /// Computes the centroid (average position) of the first 'count' points.
        /// </summary>
        private Vector3 ComputeCentroid(Vector3[] pts, int count)
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                sum += pts[i];
            }
            return sum / count;
        }

        /// <summary>
        /// If the input points array has less than 4 points, resample the polyline so that the returned array always has 4 points.
        /// The additional points are inserted between the original ones. If there are 4 or more points, the original array is returned.
        /// </summary>
        private Vector3[] EnsureFourPoints(Vector3[] points)
        {
            if (points == null || points.Length == 0)
                return new Vector3[NEEDED_POINTS];
            if (points.Length >= NEEDED_POINTS)
                return points;
            return ResamplePolyline(points, NEEDED_POINTS);
        }

        /// <summary>
        /// Resamples a polyline defined by 'points' to contain exactly 'desiredCount' points, evenly spaced along the polyline.
        /// </summary>
        private Vector3[] ResamplePolyline(Vector3[] points, int desiredCount)
        {
            int n = points.Length;
            Vector3[] resampled = new Vector3[desiredCount];
            if (n == 1)
            {
                for (int i = 0; i < desiredCount; i++)
                    resampled[i] = points[0];
                return resampled;
            }

            float[] cumulative = new float[n];
            cumulative[0] = 0f;
            for (int i = 1; i < n; i++)
            {
                cumulative[i] = cumulative[i - 1] + Vector3.Distance(points[i - 1], points[i]);
            }
            float totalLength = cumulative[n - 1];
            for (int i = 0; i < desiredCount; i++)
            {
                float t = (float)i / (desiredCount - 1);
                float targetDist = totalLength * t;
                int seg = 0;
                while (seg < n - 1 && cumulative[seg + 1] < targetDist)
                    seg++;
                float segmentLength = cumulative[seg + 1] - cumulative[seg];
                float localT = (targetDist - cumulative[seg]) / (segmentLength > 0 ? segmentLength : 1);
                resampled[i] = Vector3.Lerp(points[seg], points[seg + 1], localT);
            }
            return resampled;
        }
    }

}

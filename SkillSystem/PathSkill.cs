using System;
using System.Collections.Generic;
using Game.PathSystem;
using Game.Utility;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.SkillSystem
{
    public class PathSkill : MonoBehaviour 
    {   
        [Title("Path References")]
        [SerializeField] private PathFollowerParameters _followerParameters;

        private List<GameObject> _pathObjectHolders= new ();

        private Action _onCompleteAction = null;
        private SimpleTimer _timer = new SimpleTimer(0.1f);

        private void Reset()
        {
            _followerParameters._showBaseSpeedInspector = false;
        }

        private void Update()
        {
            if(ManagerPause.IsPaused()) return;

            _timer.UpdateTimer();

            if (!_timer.IsRoundCompleted) return;

            bool noChilds = false;
            foreach (var obj in _pathObjectHolders)
            {
                if (obj == null) continue;
                
                noChilds = false;
                
                if (obj.transform.childCount == 0)
                {
                    noChilds = true;
                }
            }

            if(_onCompleteAction != null && noChilds)
            {
                _onCompleteAction.Invoke();
                _onCompleteAction = null;
            }
        }

        public void AddObjectToPath(IProjectile projectile, Vector3 localPos, Action onComplete = null)
        {
            GameObject pathObjectHolder = GetHolderOrCreateNew();

            if(projectile is IEnabled enabledProjectile)
            {
                enabledProjectile.SetEnable(false);
            }

            Transform projectileTransform = projectile.GetTransform();
            projectileTransform.SetParent(pathObjectHolder.transform);
            projectileTransform.localPosition = localPos;

            if(_followerParameters.IsLookForward)
                projectileTransform.localRotation = Quaternion.Euler(0, 0, 0);

            PathFollower pathFollower = pathObjectHolder.GetOrAddComponent<PathFollower>();
            pathFollower.SetParameters(_followerParameters);

            //pathObjectHolder.GetOrAddComponent<MedianDirectionController>();
            
            if(onComplete != null)
            {
                pathFollower.OnComplete += onComplete;
            }
        }

        public void SetDirection(Vector3 direction)
        {
            _followerParameters.Path.SetLockedDirection(direction);
        }

        public void SetOnCompleteAction(Action onComplete)
        {
            _onCompleteAction = onComplete;
        }
       

        public GameObject GetHolderOrCreateNew()
        {
            GameObject holder = null;
            foreach (var obj in _pathObjectHolders)
            {
                if (!HasChildren(obj.transform))
                {
                    holder = obj;
                    break;
                }
            }

            if (holder == null)
            {
                holder = new GameObject("PathObjectHolder");
                holder.transform.SetParent(this.transform);
                _pathObjectHolders.Add(holder);
            }
            return holder;
        }

        private bool HasChildren(Transform parent)
        {
            return parent.childCount > 0;
        }
    }
}
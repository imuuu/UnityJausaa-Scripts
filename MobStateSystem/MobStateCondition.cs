using System;

namespace Game.MobStateSystem
{
    public abstract class MobStateCondition
    {
        public abstract void EnterState();
        public abstract void ExitState();
        public virtual void UpdateState() {}
        public abstract MOB_STATE GetState();
        public abstract bool IsConditionMet();

        public Action OnEnterState;

        public Action OnExitState;
    }

}
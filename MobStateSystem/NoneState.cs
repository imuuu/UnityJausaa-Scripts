namespace Game.MobStateSystem
{
    public class NoneState : MobStateCondition
    {
        public override void EnterState() { }

        public override void ExitState() { }

        public override MOB_STATE GetState() => MOB_STATE.NONE;

        public override bool IsConditionMet() => false;
    }

   
}
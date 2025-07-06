namespace Game.MobStateSystem
{
    public class FallenState : MobStateCondition
    {
        private MobStateMachine _machine;
        public FallenState(MobStateMachine machine)
        {
            _machine = machine;
        }
        public override void EnterState() { }

        public override void ExitState() { }
        public override MOB_STATE GetState() => MOB_STATE.FALLEN;

        public override bool IsConditionMet()
        {
            return _machine.GetTilt() > CONSTANTS.MOB_STAND_TILT_THRESHOLD && _machine.IsFalling();
        }
    }

   
}
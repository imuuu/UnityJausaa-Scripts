namespace Game.MobStateSystem
{
    public class UpSideDownState : MobStateCondition
    {
        private MobStateMachine _machine;

        public UpSideDownState(MobStateMachine machine)
        {
            _machine = machine;
        }
        public override void EnterState() 
        {
            //Debug.Log("======> MOB IS Upside Down");
        }

        public override void ExitState() { }

        public override void UpdateState()
        {
            base.UpdateState();

        }
        public override MOB_STATE GetState() => MOB_STATE.UP_SIDE_DOWN;

        public override bool IsConditionMet()
        {
            return _machine.GetTilt() >= (CONSTANTS.MOB_UPSIDE_DOWN_VALUE-CONSTANTS.MOB_UPSIDE_DOWN_THRESHOLD) 
            && _machine.GetTilt() <= (CONSTANTS.MOB_UPSIDE_DOWN_VALUE + CONSTANTS.MOB_UPSIDE_DOWN_THRESHOLD) && !_machine.IsMoving(_machine.GetMovementThreshold());
        }
    }

   
}
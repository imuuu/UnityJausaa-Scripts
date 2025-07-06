namespace Game.MobStateSystem
{
    public class StandingState : MobStateCondition
    {
        private MobStateMachine _machine;

        public StandingState(MobStateMachine machine)
        {
            _machine = machine;
        }

        public override void EnterState() 
        {
            _machine.ToggleRigidbodyConstraints(true);
        }

        public override void ExitState() { }

        public override MOB_STATE GetState()
        {
            return MOB_STATE.STAND;
        }

        public override bool IsConditionMet()
        {
            return _machine.GetTilt() <= CONSTANTS.MOB_STAND_TILT_THRESHOLD && !_machine.IsMoving(_machine.GetMovementThreshold());
        }
    }

   
}
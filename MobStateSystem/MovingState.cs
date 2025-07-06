namespace Game.MobStateSystem
{
    public class MovingState : MobStateCondition
    {
        private MobStateMachine _machine;

        public MovingState(MobStateMachine machine)
        {
            _machine = machine;
        }

        public override void EnterState() 
        {
            _machine.ToggleRigidbodyConstraints(true);
        }

        public override void ExitState() { }

        public override MOB_STATE GetState() => MOB_STATE.MOVING;

        public override bool IsConditionMet()
        {
            return _machine.IsGrounded() && _machine.IsMoving(_machine.GetMovementThreshold()) && _machine.GetTilt() <= CONSTANTS.MOB_STAND_TILT_THRESHOLD;
        }
    }

   
}
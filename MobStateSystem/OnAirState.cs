namespace Game.MobStateSystem
{
    public class OnAirState : MobStateCondition
    {
        private MobStateMachine _machine;

        public OnAirState(MobStateMachine machine)
        {
            _machine = machine;
        }

        public override void EnterState() { }

        public override void ExitState() { }
        public override MOB_STATE GetState() => MOB_STATE.ON_AIR;

        public override bool IsConditionMet()
        {
            return !_machine.IsGrounded() && _machine.IsAir();
        }
    }

   
}
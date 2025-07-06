namespace Game.MobStateSystem
{
    public class ReadyToFlyState : MobStateCondition
    {
        private MobStateMachine _machine;

       
        public ReadyToFlyState(MobStateMachine machine)
        {
            _machine = machine;
        }
        public override void EnterState() 
        {
            _machine.ToggleRigidbodyConstraints(false);
        }

        public override void ExitState() 
        {

        }
        public override MOB_STATE GetState() => MOB_STATE.READY_TO_FLY;

        public override bool IsConditionMet()
        {
            return false;
        }
    }

   
}

namespace Game.MobStateSystem
{
    public class MobGrabbingState : MobStateCondition
    {
        private MobStateMachine _machine;

        public MobGrabbingState(MobStateMachine machine)
        {
            _machine = machine;
        }

        public override void EnterState()
        {
            _machine.ToggleRigidbodyConstraints(false);
        }

        public override void ExitState() { }

        public override MOB_STATE GetState() => MOB_STATE.MOB_GRABBING;

        public override bool IsConditionMet()
        {
            return false;
        }
    }


}
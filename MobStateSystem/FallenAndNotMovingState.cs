using UnityEngine;
namespace Game.MobStateSystem
{
    public class FallenAndNotMovingState : MobStateCondition
    {
        private MobStateMachine _machine;
        private float _movementThreshold = 0.1f;
        private float _enteredFallenTime;

        public FallenAndNotMovingState(MobStateMachine machine)
        {
            _machine = machine;
        }
        public override void EnterState() { }

        public override void ExitState() { }
        public override MOB_STATE GetState() => MOB_STATE.FALLEN_NOT_MOVING;

        public override bool IsConditionMet()
        {
            if(_machine.GetTilt() <= CONSTANTS.MOB_STAND_TILT_THRESHOLD) return false;

            if (_machine.IsGrounded()) return false;

            if(_machine.GetTilt() < 45) return false;

            if (_machine.IsMoving(_movementThreshold))
            {
                _enteredFallenTime = Time.time;
                return false;
            }

            return Time.time - _enteredFallenTime >= _machine.GetFallenNotMovingStillThreshold();
        }

    }

   
}
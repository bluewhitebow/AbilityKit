using System;

namespace AbilityKit.Ability.FrameSync.Rollback
{
    public interface IRollbackCommand
    {
        void Rollback();
    }

    public sealed class DelegateRollbackCommand : IRollbackCommand
    {
        private readonly Action _rollback;

        public DelegateRollbackCommand(Action rollback)
        {
            _rollback = rollback ?? throw new ArgumentNullException(nameof(rollback));
        }

        public void Rollback()
        {
            _rollback();
        }
    }
}

namespace Game.SkillSystem
{
    // public interface IBlockEventContext : ISkillEventContext
    // {
    //     /// <summary>
    //     /// Gets the damage dealer.
    //     /// </summary>
    //     IDamageDealer DamageDealer { get; }

    //     /// <summary>
    //     /// Gets the damage receiver.
    //     /// </summary>
    //     IDamageReceiver DamageReceiver { get; }
    // }

    public class BlockEventContext : ISkillEventContext //could be ISkillEventContext<IDamageDealer, IDamageReceiver>, but simpler understanding without generics
    {
        /// <summary>
        /// Gets the damage dealer.
        /// </summary>
        public IDamageDealer DamageDealer { get; set; }

        /// <summary>
        /// Gets the damage receiver.
        /// </summary>
        public IDamageReceiver DamageReceiver { get; set; }

        public BlockEventContext(IDamageDealer dealer, IDamageReceiver receiver)
        {
            DamageDealer = dealer;
            DamageReceiver = receiver;
        }
    }
// EXAMPLE OF A BLOCK Event CONTEXT
// public class BlockEventContext : ISkillEventContext<IDamageDealer, IDamageReceiver>
// {
//     /// <summary>
//     /// Gets the damage dealer.
//     /// </summary>
//     public IDamageDealer DamageDealer { get; private set; }

    //     /// <summary>
    //     /// Gets the damage receiver.
    //     /// </summary>
    //     public IDamageReceiver DamageReceiver { get; private set; }

    //     public BlockEventContext(IDamageDealer dealer, IDamageReceiver receiver)
    //     {
    //         DamageDealer = dealer;
    //         DamageReceiver = receiver;
    //     }
    // }
}
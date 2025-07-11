namespace Game.SkillSystem
{
    /// <summary>
    /// Base interface for skill Event context.
    /// </summary>
    public interface ISkillEventContext
    {

    }

    public interface ISkillEventContext<T> : ISkillEventContext
    {
        /// <summary>
        /// Gets the skill Event context data.
        /// </summary>
        T Data { get; }
    }
    public interface ISkillEventContext<T1, T2> : ISkillEventContext
    {
        /// <summary>
        /// Gets the first skill Event context data.
        /// </summary>
        T1 Data1 { get; }

        /// <summary>
        /// Gets the second skill Event context data.
        /// </summary>
        T2 Data2 { get; }
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

public class CurrencyOrb : DropOrb
{
    public CURRENCY CurrencyType { get; private set; }
    
    public void Init(float amount, CURRENCY currencyType)
    {
        base.Init(amount);
        CurrencyType = currencyType;
    }
    public override void Init(float amount)
    {
        base.Init(amount);
        CurrencyType = CURRENCY.NONE;
    }

}

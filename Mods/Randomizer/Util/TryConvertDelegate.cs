namespace GRandomizer.Util
{
    public delegate bool TryConvert<TFrom, TTo>(TFrom value, out TTo result);
}

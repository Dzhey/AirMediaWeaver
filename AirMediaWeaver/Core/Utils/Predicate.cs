namespace AirMedia.Core.Utils
{
    public abstract class Predicate<T>
    {
        public abstract bool Apply(T input);
    }
}
using AirMedia.Core.Utils;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates
{
    public class AllInputCharsAreDigits : Predicate<string>
    {
        public override bool Apply(string input)
        {
            foreach (var ch in input)
            {
                if (char.IsDigit(ch) == false)
                    return false;
            }

            return false;
        }
    }
}
using AirMedia.Core.Utils;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates
{
    public class AllInputCharsIsLowercase : Predicate<string>
    {
        public override bool Apply(string input)
        {
            foreach (var ch in input)
            {
                if (char.IsUpper(ch))
                    return false;
            }

            return false;
        }
    }
}
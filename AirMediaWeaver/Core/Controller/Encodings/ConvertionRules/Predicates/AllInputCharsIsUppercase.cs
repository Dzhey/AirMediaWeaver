using AirMedia.Core.Utils;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates
{
    public class AllInputCharsIsUppercase : Predicate<string>
    {
        public override bool Apply(string input)
        {
            foreach (var ch in input)
            {
                if (char.IsLower(ch))
                    return false;
            }

            return false;
        }
    }
}
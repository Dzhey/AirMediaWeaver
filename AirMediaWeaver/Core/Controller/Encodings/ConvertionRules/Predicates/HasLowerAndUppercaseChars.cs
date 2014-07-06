using AirMedia.Core.Utils;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates
{
    public class HasLowerAndUppercaseChars : Predicate<string>
    {
        public override bool Apply(string input)
        {
            bool hasLowerCaseChar = false;
            bool hasUppercaseChar = false;
            foreach (var ch in input)
            {
                if (!hasLowerCaseChar && char.IsLower(ch))
                {
                    hasLowerCaseChar = true;
                }
                if (!hasUppercaseChar && char.IsUpper(ch))
                {
                    hasUppercaseChar = true;
                }

                if (hasLowerCaseChar && hasUppercaseChar)
                    return true;
            }

            return false;
        }
    }
}
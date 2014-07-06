using System.Linq;
using System.Text;
using AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.CyrillicRules
{
    public class Utf8ToCp866ConversionRule : BaseTreeRule
    {
        private const float TriggerThreshold = .5f;

        private static readonly Encoding SourceEncoding = Encoding.GetEncoding("utf-8");
        private static readonly Encoding TargetEncoding = Encoding.GetEncoding("cp866");

        private static readonly char[] AssumeChars = new[] { '\u2555', '\u2551', '\u2568' };
        private const char AssumeChar = '\u2592';

        private readonly InputIsPseudoGraphicsWithText _checkerRule;

        public Utf8ToCp866ConversionRule(params BaseTreeRule[] children) : base(children)
        {
            _checkerRule = new InputIsPseudoGraphicsWithText();
        }

        public override Encoding GetSourceEncoding()
        {
            return SourceEncoding;
        }

        public override Encoding GetTargetEncoding()
        {
            return TargetEncoding;
        }

        protected override bool IsRuleAppliableImpl(string input)
        {
            if (_checkerRule.Apply(input) == false)
                return false;

            int assumeCharsCount = 0;
            bool hasAssumeChar = false;
            bool hasUppercaseLetter = false;

            foreach (var ch in input)
            {
                if (AssumeChars.Contains(ch))
                {
                    assumeCharsCount++;
                }
                else if (AssumeChar == ch)
                {
                    assumeCharsCount++;
                    hasAssumeChar = true;
                }

                if (!hasUppercaseLetter && char.IsLower(ch) == false)
                {
                    hasUppercaseLetter = true;
                }
            }

            float threshold = ((float)assumeCharsCount) / input.Length;

            return hasAssumeChar 
                && hasUppercaseLetter
                && threshold > TriggerThreshold;
        }

        protected override string ApplyConversionImpl(string input)
        {
            var text = SourceEncoding.GetBytes(input);

            return TargetEncoding.GetString(text);
        }

    }
}
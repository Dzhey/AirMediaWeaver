using System.Linq;
using System.Text;
using AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.CyrillicRules
{
    public class Utf8ToKoi8RConversionRule : BaseTreeRule
    {
        private const float TriggerThreshold = .3f;

        private static readonly Encoding SourceEncoding = Encoding.GetEncoding("utf-8");
        private static readonly Encoding TargetEncoding = Encoding.GetEncoding("koi8-r");

        private static readonly char[] AssumeChars = new[] {'\u043F', '\u044F'};

        private readonly InputIsPseudoGraphicsWithText _checkerRule;

        public Utf8ToKoi8RConversionRule()
        {
            _checkerRule = new InputIsPseudoGraphicsWithText();
        }

        public Utf8ToKoi8RConversionRule(BaseTreeRule child) : base(child)
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

            int assumeCharsCount = AssumeChars.Sum(assumeChar => input.Count(ch => assumeChar == ch));
            float threshold = ((float)assumeCharsCount) / input.Length;

            return threshold > TriggerThreshold;
        }

        protected override string ApplyConversionImpl(string input)
        {
            var text = SourceEncoding.GetBytes(input);

            return TargetEncoding.GetString(text);
        }

    }
}
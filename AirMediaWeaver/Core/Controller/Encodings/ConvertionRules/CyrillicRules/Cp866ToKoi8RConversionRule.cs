using System.Text;
using AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.CyrillicRules
{
    public class Cp866ToKoi8RConversionRule : BaseTreeRule
    {
        private static readonly Encoding SourceEncoding = Encoding.GetEncoding("cp866");
        private static readonly Encoding TargetEncoding = Encoding.GetEncoding("koi8-r");

        private readonly InputIsPseudoGraphicsWithText _checkerRule;

        public Cp866ToKoi8RConversionRule()
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

            return true;
        }

        protected override string ApplyConversionImpl(string input)
        {
            var text = SourceEncoding.GetBytes(input);

            return TargetEncoding.GetString(text);
        }

    }
}
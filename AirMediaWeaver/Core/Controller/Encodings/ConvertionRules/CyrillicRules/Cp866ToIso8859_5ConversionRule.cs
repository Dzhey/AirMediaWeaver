using System.Text;
using AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.CyrillicRules
{
    public class Cp866ToIso8859_5ConversionRule : BaseTreeRule
    {
        private static readonly Encoding SourceEncoding = Encoding.GetEncoding("cp866");
        private static readonly Encoding TargetEncoding = Encoding.GetEncoding("iso-8859-5");

        private readonly AtLeastHalfUmlauts _checkerRule;

        public Cp866ToIso8859_5ConversionRule() : this(new BaseTreeRule[0])
        {
        }

        public Cp866ToIso8859_5ConversionRule(params BaseTreeRule[] children) : base(children)
        {
            _checkerRule = new AtLeastHalfUmlauts();
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
            if (_checkerRule.Apply(input))
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
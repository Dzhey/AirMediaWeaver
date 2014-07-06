using System.Text;
using AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.CyrillicRules
{
    public class Iso8859_5ToWin1252ConversionRule : BaseTreeRule
    {
        private static readonly Encoding SourceEncoding = Encoding.GetEncoding("iso-8859-5");
        private static readonly Encoding TargetEncoding = Encoding.GetEncoding("windows-1252");

        private readonly HasLowerAndUppercaseChars _ruleChecker;


        public Iso8859_5ToWin1252ConversionRule()
        {
            _ruleChecker = new HasLowerAndUppercaseChars(); ;
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
            return _ruleChecker.Apply(input);
        }

        protected override string ApplyConversionImpl(string input)
        {
            var text = SourceEncoding.GetBytes(input);

            return TargetEncoding.GetString(text);
        }

    }
}
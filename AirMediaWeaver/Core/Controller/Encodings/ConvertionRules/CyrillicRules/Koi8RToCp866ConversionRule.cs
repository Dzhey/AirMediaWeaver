using System.Linq;
using System.Text;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.CyrillicRules
{
    /// <summary>
    /// Appliable when all characters are pseude-graphic characters.
    /// </summary>
    public class Koi8RToCp866ConversionRule : BaseTreeRule
    {
        private static readonly Encoding SourceEncoding = Encoding.GetEncoding("koi8-r");
        private static readonly Encoding TargetEncoding = Encoding.GetEncoding("cp866");

        public Koi8RToCp866ConversionRule()
        {
        }

        public Koi8RToCp866ConversionRule(params BaseTreeRule[] children)
            : base(children)
        {
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
            foreach (var ch in input)
            {
                if (UnicodeCodepages.PseudographicsCodepage.Contains(ch) == false)
                    return false;
            }

            return true;
        }

        protected override string ApplyConversionImpl(string input)
        {
            var text = SourceEncoding.GetBytes(input);

            return TargetEncoding.GetString(text);
        }

    }
}
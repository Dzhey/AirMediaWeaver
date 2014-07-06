using System;
using AirMedia.Core.Controller.Encodings.ConvertionRules;
using AirMedia.Core.Controller.Encodings.ConvertionRules.CyrillicRules;
using AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates;

namespace AirMedia.Core.Controller.Encodings
{
    public static class CyrillicConverter
    {
        private static readonly BaseTreeRule RuleTree;

        static CyrillicConverter()
        {
            RuleTree = new Koi8RToCp866ConversionRule(
                new PredicateRule(
                    new InputIsPseudoGraphicsWithText(),

                    new Utf8ToKoi8RConversionRule(
                        new Utf8ToCp866ConversionRule(
                            new Iso8859_5ToCp866ConversionRule(
                                new Cp866ToKoi8RConversionRule()
                            )
                        )
                    )
                ),

                new Cp866ToIso8859_5ConversionRule(
                    new Cp866ToWin1251ConversionRule(
                        new Koi8RToWin1251ConversionRule(),
                        new Win1251ToWin1252ConversionRule(),
                        new Iso8859_5ToWin1252ConversionRule(),
                        new Iso8859_15ToWin1251ConversionRule()
                    )
                )
            );
        }

        public static BaseTreeRule[] FindApplicableConversionRules(string input)
        {
            return RuleTree.FindAppliableChildRules(input);
        }

        public static bool TryConvert(string input, out string output, 
            Predicate<string> confirmPredicate = null)
        {
            output = input;

            if (RuleTree.IsRuleApplicable(input) == false)
                return false;

            output = RuleTree.ApplyConversion(input, confirmPredicate);

            return output != null;
        }
    }
}
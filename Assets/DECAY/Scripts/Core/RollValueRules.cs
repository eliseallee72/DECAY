
using System;

namespace Decay
{
    public static class RollValueRules
    {
        public static bool IsValid(int value)
        {
            return value >= BattleRules.MinimumRollValue && value <= BattleRules.MaximumRollValue;
        }

        public static int Require(int value, string parameterName)
        {
            if (!IsValid(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    $"Roll value must be between {BattleRules.MinimumRollValue} and {BattleRules.MaximumRollValue}.");
            }

            return value;
        }
    }
}

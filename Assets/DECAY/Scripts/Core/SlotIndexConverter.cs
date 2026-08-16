
using System;

namespace Decay
{
    public static class SlotIndexConverter
    {
        public static int ToStorageIndex(SlotId slotId)
        {
            if (!slotId.IsValid)
            {
                throw new ArgumentException("A valid SlotId is required.", nameof(slotId));
            }

            return slotId.Number - BattleRules.FirstSlotNumber;
        }

        public static int ToStorageIndex(int slotNumber)
        {
            if (slotNumber < BattleRules.FirstSlotNumber || slotNumber > BattleRules.LastSlotNumber)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotNumber),
                    slotNumber,
                    $"Slot number must be between {BattleRules.FirstSlotNumber} and {BattleRules.LastSlotNumber}.");
            }

            return slotNumber - BattleRules.FirstSlotNumber;
        }

        public static SlotId FromStorageIndex(Side side, int storageIndex)
        {
            if (storageIndex < 0 || storageIndex >= BattleRules.SlotsPerSide)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(storageIndex),
                    storageIndex,
                    $"Storage index must be between 0 and {BattleRules.SlotsPerSide - 1}.");
            }

            return new SlotId(side, storageIndex + BattleRules.FirstSlotNumber);
        }
    }
}

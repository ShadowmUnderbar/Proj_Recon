namespace App.Common.Data
{
    public class SaveData
    {
        public bool IsSwitchableFocus = false;
        public HandType DominantHand = HandType.Right;
        public int Exp = 0;
        public int Level = 1;
        public int Cash = 0;
        public PlayerUnlockType UnlockType { get; private set; } = PlayerUnlockType.None;
    }
}
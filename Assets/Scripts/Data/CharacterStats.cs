using System;

namespace VoidBound.Data
{
    [Serializable]
    public struct CharacterStats
    {
        public int str;
        public int dex;
        public int vig;
        public int intel;

        public CharacterStats(int str, int dex, int vig, int intel)
        {
            this.str = str;
            this.dex = dex;
            this.vig = vig;
            this.intel = intel;
        }

        public static CharacterStats operator +(CharacterStats a, CharacterStats b)
        {
            return new CharacterStats(a.str + b.str, a.dex + b.dex, a.vig + b.vig, a.intel + b.intel);
        }
    }
}

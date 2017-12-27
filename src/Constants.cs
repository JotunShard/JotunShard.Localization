using System.Collections.Immutable;

namespace JotunShard.Localization
{
    internal static class Constants
    {
        public const string
            FlagsDelimeter = ", ";

        public static readonly ImmutableArray<char>
            FlagsSeparators = FlagsDelimeter.ToImmutableArray();
    }
}
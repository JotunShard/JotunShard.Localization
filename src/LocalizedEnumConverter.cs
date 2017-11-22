using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace JotunShard.Localization
{
    public class LocalizedEnumConverter<TResourceManager> : EnumConverter
        where TResourceManager : ResourceManager
    {
        private const string
            FlagsDelimeter = ", ";

        private static readonly char[]
            FlagsSeparators = FlagsDelimeter.ToCharArray();

        private static readonly Dictionary<CultureInfo, Dictionary<string, object>> localizations = new Dictionary<CultureInfo, Dictionary<string, object>>();

        private readonly ResourceManager res;

        private readonly Array flagValues;

        public LocalizedEnumConverter(Type EnumType) : base(EnumType)
        {
            res = new ResourceManager(typeof(TResourceManager));
            if (EnumType.GetTypeInfo().GetCustomAttributes(typeof(FlagsAttribute), true).Any())
                flagValues = Enum.GetValues(EnumType);
        }

        private string LocalizeValue(CultureInfo culture, object value)
        {
            var name = $"{EnumType.Name}_{value}";
            return res.GetString(name, culture) ?? name;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (!(value is string stringValue))
                return base.ConvertFrom(context, culture, value);
            culture = culture ?? CultureInfo.CurrentCulture;
            if (!localizations.TryGetValue(culture, out Dictionary<string, object> conversions))
                localizations.Add(
                    culture,
                    conversions = GetStandardValues(context)
                        .Cast<object>()
                        .ToDictionary(v => LocalizeValue(culture, v), v => v));
            conversions.TryGetValue(stringValue, out object result);
            return (flagValues?.Length ?? 0) == 0
                ? result
                : Enum.ToObject(
                    EnumType,
                    stringValue.Split(FlagsSeparators)
                        .Join(conversions, v => v, c => c.Key, (v, c) => c.Value)
                        .Aggregate(0u, (r, v) => r | Convert.ToUInt32(v)));
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != typeof(string))
                return base.ConvertTo(context, culture, value, destinationType);
            culture = culture ?? CultureInfo.CurrentCulture;
            if ((flagValues?.Length ?? 0) == 0 || Enum.IsDefined(EnumType, value))
                return LocalizeValue(culture, value);
            var valueFlags = Convert.ToUInt32(value);
            return string.Join(
                FlagsDelimeter,
                flagValues
                    .Cast<object>()
                    .Select(f => new
                    {
                        Flag = f,
                        Value = Convert.ToUInt32(f),
                    })
                    .Where(f => (f.Value & valueFlags) == f.Value)
                    .Select(f => LocalizeValue(culture, f.Flag)));
        }
    }
}
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace JotunShard.Localization
{
    public class LocalizedEnumConverter : EnumConverter
    {
        private static readonly Dictionary<CultureInfo, Dictionary<string, object>>
            localizations = new Dictionary<CultureInfo, Dictionary<string, object>>();

        private readonly IStringLocalizer localizer;

        private readonly Array flagValues;

        public LocalizedEnumConverter(Type EnumType, IStringLocalizerFactory localizerFactory, Type resourceSource = null) : base(EnumType)
        {
            localizer = localizerFactory.Create(resourceSource ?? EnumType);
            if (EnumType.GetTypeInfo().GetCustomAttributes(typeof(FlagsAttribute), true).Any())
                flagValues = Enum.GetValues(EnumType);
        }

        private string LocalizeValue(CultureInfo culture, object value)
        {
            var name = $"{EnumType.Name}_{value}";
            return localizer.GetString(name, culture) ?? name;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (!(value is string stringValue))
                return base.ConvertFrom(context, culture, value);
            culture = culture ?? CultureInfo.CurrentCulture;
            if (!localizations.TryGetValue(culture, out var conversions))
            {
                conversions = GetStandardValues(context)
                    .Cast<object>()
                    .ToDictionary(v => LocalizeValue(culture, v), v => v);
                localizations.Add(culture, conversions);
            }
            conversions.TryGetValue(stringValue, out var result);
            return (flagValues?.Length ?? 0) == 0
                ? result
                : Enum.ToObject(
                    EnumType,
                    stringValue.Split(Constants.FlagsSeparators.ToArray())
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
                Constants.FlagsDelimeter,
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
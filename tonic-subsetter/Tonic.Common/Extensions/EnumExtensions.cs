using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Tonic.Common.Extensions
{
    /// <summary>Provides functionality to enhance enumerations.</summary>
    public static class EnumExtension
    {
        private static readonly Type AttributeUsageAttributeType = typeof(AttributeUsageAttribute);

        //find all enum values based on attribute value
        public static List<TEnum> GetValuesFromAttribute<TEnum, TAttribute>(string value, Func<TAttribute, string> propGetter)
            where TEnum : struct, Enum
            where TAttribute : Attribute
        {
            var type = typeof(TEnum);
            if (!type.IsEnum) throw new ArgumentException("parameter must be an enum.", nameof(value));

            var foundEnums = new List<TEnum>(Enum.GetValues<TEnum>().Length);

            foreach (Enum enumValue in type.GetEnumValues())
            {
                if (enumValue.GetAttribute<TAttribute>() is TAttribute attribute)
                {
                    if (propGetter(attribute) == value) foundEnums.Add((TEnum) enumValue);
                }
                else
                {
                    if (string.Equals(enumValue.ToString(), value)) foundEnums.Add((TEnum) enumValue);
                }
            }

            return foundEnums;
        }


        //find enum value based on attribute value
        public static TEnum GetValueFromAttribute<TEnum, TAttribute>(string value, Func<TAttribute, string> propGetter)
            where TEnum : struct, Enum
            where TAttribute : Attribute
        {
            var type = typeof(TEnum);
            if (!type.IsEnum) throw new ArgumentException("parameter must be an enum.", nameof(value));

            foreach (Enum enumValue in type.GetEnumValues())
            {
                if (enumValue.GetAttribute<TAttribute>() is TAttribute attribute)
                {
                    if (propGetter(attribute) == value) return (TEnum) enumValue;
                }
                else
                {
                    if (string.Equals(enumValue.ToString(), value)) return (TEnum) enumValue;
                }
            }

            throw new ArgumentException("Not found.", nameof(value));
        }

        public static T? GetAttribute<T>(this Enum? enumValue, Func<T, bool>? predicate = null)
            where T : Attribute =>
            GetAttributes(enumValue, predicate).FirstOrDefault();

        public static T[] GetAttributes<T>(this Enum? enumValue, Func<T, bool>? predicate = null)
            where T : Attribute
        {
            if (!ReferenceEquals(enumValue, null))
            {
                var enumTypeInfo = enumValue.GetType().GetTypeInfo();
                var memberInfo = enumTypeInfo.GetDeclaredField(enumValue.ToString());
                if (!ReferenceEquals(memberInfo, null))
                {
                    var typeOfAttribute = typeof(T);

                    var attributes = memberInfo.GetCustomAttributes(typeOfAttribute);
                    var attributesOfT = attributes.Cast<T>().ToArray();
                    if (attributesOfT.Length > 0)
                    {
                        if (attributesOfT.Length > 1 &&
                            typeOfAttribute.GetCustomAttribute(AttributeUsageAttributeType) is AttributeUsageAttribute { AllowMultiple: false })
                        {
                            throw new Exception(
                                $"Too many {typeOfAttribute.Name}s exist on enum member '{enumTypeInfo.Name}.{enumValue}'.");
                        }

                        if (predicate != null)
                        {
                            return attributesOfT.Where(predicate).ToArray();
                        }

                        return attributesOfT;
                    }
                }
            }
            return Array.Empty<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetAttribute<T>(this Enum enumValue, [NotNullWhen(true)] out T? attribute)
            where T : Attribute
        {
            attribute = enumValue.GetAttribute<T>();
            return attribute != null;
        }
    }
}
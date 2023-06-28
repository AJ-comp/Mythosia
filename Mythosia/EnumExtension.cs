using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mythosia
{
    public static class EnumExtension
    {
        public static TEnum ToEnum<TEnum>(this sbyte value) where TEnum : Enum
        {
            int tValue = value;
            return tValue.ToEnum<TEnum>();
        }

        public static TEnum ToEnum<TEnum>(this byte value) where TEnum : Enum
        {
            int tValue = value;
            return tValue.ToEnum<TEnum>();
        }


        public static TEnum ToEnum<TEnum>(this short value) where TEnum : Enum
        {
            int tValue = value;
            return tValue.ToEnum<TEnum>();
        }


        public static TEnum ToEnum<TEnum>(this ushort value) where TEnum : Enum
        {
            uint tValue = value;
            return tValue.ToEnum<TEnum>();
        }


        public static TEnum ToEnum<TEnum>(this int value) where TEnum : Enum
        {
            if (Enum.IsDefined(typeof(TEnum), value))
            {
                return (TEnum)(object)value;
            }
            else
            {
                throw new ArgumentException($"The value '{value}' is not a valid representation of {typeof(TEnum).Name}.");
            }
        }

        public static TEnum ToEnum<TEnum>(this uint value) where TEnum : Enum
        {
            if (Enum.IsDefined(typeof(TEnum), value))
            {
                return (TEnum)(object)value;
            }
            else
            {
                throw new ArgumentException($"The value '{value}' is not a valid representation of {typeof(TEnum).Name}.");
            }
        }

        public static T GetEnumFromDescription<T>(this string description) where T : Enum
        {
            Type enumType = typeof(T);
            if (!enumType.IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            FieldInfo[] fields = enumType.GetFields();
            foreach (FieldInfo field in fields)
            {
                if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (attribute.Description == description)
                    {
                        return (T)field.GetValue(null);
                    }
                }
                else if (field.Name == description)
                {
                    return (T)field.GetValue(null);
                }
            }

            throw new ArgumentException($"Enum value not found for description: {description}");
        }


        public static string ToDescription(this Enum value)
        {
            // Get the Description attribute value for the enum value
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return (attributes.Length > 0) ? attributes[0].Description : value.ToString();
        }

        private static string GetDescription(object value)
        {
            // Get the Description attribute value for the enum value
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return (attributes.Length > 0) ? attributes[0].Description : value.ToString();
        }
    }
}

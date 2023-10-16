using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Mythosia
{
    public static class EnumExtension
    {
        /*******************************************************************************/
        /// <summary>
        /// Converts a signed byte value to the specified enum type.
        /// </summary>
        /// <typeparam name="TEnum">The enum type to convert to.</typeparam>
        /// <param name="value">The signed byte value to convert.</param>
        /// <returns>The converted enum value.</returns>
        /*******************************************************************************/
        public static TEnum ToEnum<TEnum>(this sbyte value) where TEnum : Enum
        {
            int tValue = value;
            return tValue.ToEnum<TEnum>();
        }


        /*******************************************************************************/
        /// <summary>
        /// Converts an unsigned byte value to the specified enum type.
        /// </summary>
        /// <typeparam name="TEnum">The enum type to convert to.</typeparam>
        /// <param name="value">The unsigned byte value to convert.</param>
        /// <returns>The converted enum value.</returns>
        /*******************************************************************************/
        public static TEnum ToEnum<TEnum>(this byte value) where TEnum : Enum
        {
            int tValue = value;
            return tValue.ToEnum<TEnum>();
        }


        /*******************************************************************************/
        /// <summary>
        /// Converts a short value to the specified enum type.
        /// </summary>
        /// <typeparam name="TEnum">The enum type to convert to.</typeparam>
        /// <param name="value">The short value to convert.</param>
        /// <returns>The converted enum value.</returns>
        /*******************************************************************************/
        public static TEnum ToEnum<TEnum>(this short value) where TEnum : Enum
        {
            int tValue = value;
            return tValue.ToEnum<TEnum>();
        }


        /*******************************************************************************/
        /// <summary>
        /// Converts an unsigned short value to the specified enum type.
        /// </summary>
        /// <typeparam name="TEnum">The enum type to convert to.</typeparam>
        /// <param name="value">The unsigned short value to convert.</param>
        /// <returns>The converted enum value.</returns>
        /*******************************************************************************/
        public static TEnum ToEnum<TEnum>(this ushort value) where TEnum : Enum
        {
            uint tValue = value;
            return tValue.ToEnum<TEnum>();
        }


        /*******************************************************************************/
        /// <summary>
        /// Converts an integer value to the specified enum type.
        /// </summary>
        /// <typeparam name="TEnum">The enum type to convert to.</typeparam>
        /// <param name="value">The integer value to convert.</param>
        /// <returns>The converted enum value.</returns>
        /*******************************************************************************/
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


        /*******************************************************************************/
        /// <summary>
        /// Converts an unsigned integer value to the specified enum type.
        /// </summary>
        /// <typeparam name="TEnum">The enum type to convert to.</typeparam>
        /// <param name="value">The unsigned integer value to convert.</param>
        /// <returns>The converted enum value.</returns>
        /*******************************************************************************/
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


        /*******************************************************************************/
        /// <summary>
        /// Gets the enum value associated with the specified description.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <param name="description">The description to search for.</param>
        /// <returns>The enum value associated with the description.</returns>
        /// <exception cref="ArgumentException">Thrown when the enum value is not found for the given description.</exception>
        /*******************************************************************************/
        public static T GetEnumFromDescription<T>(this string description) where T : Enum
        {
            // this logic is operate but need test
//            Enum.GetValues(typeof(T)).Cast<T>().FirstOrDefault(v => v.ToDescription() == description);

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


        public static T GetEnumFromName<T>(this string name) where T : Enum
            => (T)Enum.Parse(typeof(T), name);


        /*******************************************************************************/
        /// <summary>
        /// Gets the description associated with the specified enum value.
        /// </summary>
        /// <param name="value">The enum value.</param>
        /// <returns>The description associated with the enum value.</returns>
        /*******************************************************************************/
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

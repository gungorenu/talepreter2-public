using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace Talepreter.GUI.Common
{
    public class ConvertBackException : Exception
    {
        public object Converter { get; private set; }

        public ConvertBackException(object converter)
        {
            Converter = converter;
        }
    }

    /// <summary>
    /// Params: Value & Parameter
    /// * Value: bool
    /// * Parameter, Reverse [Optional]: bool
    /// if Value ^ Reverse then Visible otherwise Collapsed
    /// -- No ConvertBack
    /// </summary>
    public class Bool2VisibilityConverter : IValueConverter
    {
        public Visibility Invisible { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool reverse = false;
            if (parameter != null) reverse = System.Convert.ToBoolean(parameter);

            bool boolValue = System.Convert.ToBoolean(value);
            return boolValue ^ reverse ? Visibility.Visible : Invisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new ConvertBackException(this);
        }
    }

    public enum MultipleBool2VisibilityType
    {
        And,
        Or
    }

    /// <summary>
    /// Params: Value[] & Parameter
    /// * Value: bool[]
    /// * Parameter, Reverse [Optional]: bool
    /// if Value[] with operator, then set to Visible, can be reversed with optional Parameter
    /// -- No ConvertBack
    /// </summary>
    public class MultipleBool2VisibilityConverter : IMultiValueConverter
    {
        public MultipleBool2VisibilityType Operator { get; set; }

        public Visibility Invisible { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool reverse = false;
            if (parameter != null) reverse = System.Convert.ToBoolean(parameter);

            bool[] valuesAsBoolean = new bool[values.Length];
            for (int i = 0; i < values.Length; i++) valuesAsBoolean[i] = System.Convert.ToBoolean(values[i]);

            if (Operator == MultipleBool2VisibilityType.Or) return valuesAsBoolean.Any((b) => b) ^ reverse ? Visibility.Visible : Invisible;
            else return valuesAsBoolean.All((b) => b) ^ reverse ? Visibility.Visible : Invisible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new ConvertBackException(this);
        }
    }


    /// <summary>
    /// Params: Value & Parameter
    /// * Value: string
    /// * Parameter, Reverse [Optional]: bool
    /// if Value is not null or empty ^ Reverse then Visible otherwise Collapsed (Invisible)
    /// -- No ConvertBack
    /// </summary>
    public class EmptyText2VisibilityConverter : IValueConverter
    {
        public Visibility Invisible { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool reverse = false;
            if (parameter != null) reverse = System.Convert.ToBoolean(parameter);

            string strValue = value?.ToString() ?? "";
            return !string.IsNullOrEmpty(strValue) ^ reverse ? Visibility.Visible : Invisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new ConvertBackException(this);
        }
    }

    /// <summary>
    /// Params: Value
    /// * Value: double
    /// return the value with multiplying by 100
    /// -- No ConvertBack
    /// </summary>
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Truncate(100 * System.Convert.ToDouble(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new ConvertBackException(this);
        }
    }

    /// <summary>
    /// Params: Values
    /// return true if they are equal
    /// -- No ConvertBack
    /// </summary>
    public class AreEqualConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double value1 = System.Convert.ToDouble(values[0]);
            double value2 = System.Convert.ToDouble(values[1]);

            return Math.Ceiling(value1) == Math.Ceiling(value2);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new ConvertBackException(this);
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Talepreter.GUI.Common.Controls
{
    /// <summary>
    /// Interaction logic for NumericUpDown.xaml
    /// </summary>
    public partial class NumericUpDown : UserControl
    {
        #region Event Overrides

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            Border.BorderBrush = (Brush)FindResource("ControlFocusBrush");
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            Border.BorderBrush = (Brush)FindResource("ControlBorderBrush");
            base.OnMouseLeave(e);
        }

        private void OnTextBoxKeyDown(Object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (!IsReadOnly && DecreaseCommand.CanExecute()) DecreaseCommand.Execute();
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (!IsReadOnly && IncreaseCommand.CanExecute()) IncreaseCommand.Execute();
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        #endregion

        #region Dependency Property Definitions

        /// <summary>
        /// Minimum value property
        /// </summary>
        public static readonly DependencyProperty MinValueProperty;

        /// <summary>
        /// Maximum value property
        /// </summary>
        public static readonly DependencyProperty MaxValueProperty;

        /// <summary>
        /// Increment value property
        /// </summary>
        public static readonly DependencyProperty IncrementProperty;

        /// <summary>
        /// Value property
        /// </summary>
        public static readonly DependencyProperty ValueProperty;

        /// <summary>
        /// IsReadOnly property
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty;

        #endregion

        #region Dependency Property Properties

        /// <summary>
        /// Minimum value property
        /// </summary>
        public int MinValue
        {
            get { return (int)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }

        /// <summary>
        /// Maximum value property
        /// </summary>
        public int MaxValue
        {
            get { return (int)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        /// <summary>
        /// Increment value property
        /// </summary>
        public int Increment
        {
            get { return (int)GetValue(IncrementProperty); }
            set { SetValue(IncrementProperty, value); }
        }

        /// <summary>
        /// Value property
        /// </summary>
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// IsReadOnly property
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        #endregion

        #region Constructor
        /// <summary>
        /// Static constructor
        /// </summary>
        static NumericUpDown()
        {
            MinValueProperty = DependencyProperty.Register("MinValue", typeof(int), typeof(NumericUpDown), new PropertyMetadata(Int32.MinValue, UpdateCommands));
            MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(int), typeof(NumericUpDown), new PropertyMetadata(Int32.MaxValue, UpdateCommands));
            IncrementProperty = DependencyProperty.Register("Increment", typeof(int), typeof(NumericUpDown), new PropertyMetadata(1, UpdateCommands));
            ValueProperty = DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDown), new PropertyMetadata(0, UpdateCommands));
            IsReadOnlyProperty = DependencyProperty.RegisterAttached("IsReadOnly", typeof(bool), typeof(NumericUpDown), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public NumericUpDown()
        {
            IncreaseCommand = new BaseUICommand(() => SetCurrentValue(ValueProperty, Value + Increment), () => Value + Increment <= MaxValue);
            DecreaseCommand = new BaseUICommand(() => SetCurrentValue(ValueProperty, Value - Increment), () => Value - Increment >= MinValue);

            InitializeComponent();
        }
        #endregion

        #region Commands

        /// <summary>
        /// Increase command
        /// </summary>
        public BaseUICommand IncreaseCommand { get; init; }

        /// <summary>
        /// Decrease command
        /// </summary>
        public BaseUICommand DecreaseCommand { get; init; }

        #endregion

        private static void UpdateCommands(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NumericUpDown numUpDown)
            {
                if (numUpDown.Value < numUpDown.MinValue) numUpDown.SetCurrentValue(ValueProperty, numUpDown.MinValue);
                if (numUpDown.Value > numUpDown.MaxValue) numUpDown.SetCurrentValue(ValueProperty, numUpDown.MaxValue);

                numUpDown.IncreaseCommand.RaiseCanExecuteChanged();
                numUpDown.DecreaseCommand.RaiseCanExecuteChanged();
            }
        }
    }
}

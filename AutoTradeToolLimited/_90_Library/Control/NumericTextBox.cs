using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace AutoTradeTool._90_Library.Control
{
    class NumericTextBox : TextBox
    {
        public Decimal Step
        {
            private get
            {
                return (Decimal)GetValue(StepProperty);
            }
            set
            {
                SetValue(StepProperty, value);
            }
        }
        public static DependencyProperty StepProperty { get; } = DependencyProperty.Register(
            nameof(Step),
            typeof(Decimal),
            typeof(NumericTextBox),
            new PropertyMetadata(null)
        );

        public Decimal Max
        {
            private get
            {
                return (Decimal)GetValue(MaxProperty);
            }
            set
            {
                SetValue(MaxProperty, value);
            }
        }
        public static DependencyProperty MaxProperty { get; } = DependencyProperty.Register(
            nameof(Max),
            typeof(Decimal),
            typeof(NumericTextBox),
            new PropertyMetadata(null)
        );

        public Decimal Min
        {
            private get
            {
                return (Decimal)GetValue(MinProperty);
            }
            set
            {
                SetValue(MinProperty, value);
            }
        }
        public static DependencyProperty MinProperty { get; } = DependencyProperty.Register(
            nameof(Min),
            typeof(Decimal),
            typeof(NumericTextBox),
            new PropertyMetadata(null)
        );

        private Int32 _ParticleSize
        {
            get
            {
                return (Int32)Math.Log10((double)Step);
            }
        }
        private readonly string _NumericPattern = @"^\-?\d+(\.?\d+|)$";
        private string _PrevText;

        public NumericTextBox()
        {
            this.GotFocus += NumericTextBox_GotFocus;
            this.MouseWheel += NumericTextBox_MouseWheel;
            this.KeyDown += NumericTextBox_KeyDown;
            this.LostFocus += NumericTextBox_LostFocus;
            this.Text = "0";
            this.Step = 1M;
            this.Max = 100.0M;
            this.Min = -100.0M;
            this.Text = 0.0M.ToString("F" + (-_ParticleSize).ToString());
            this._PrevText = this.Text;
        }

        private void NumericTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IsReadOnly == true)
            {
                return;
            }
            CorrectText();
        }

        private void NumericTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (IsReadOnly == true)
            {
                return;
            }
            if (
                (e.Key == Key.Return) ||
                (e.Key == Key.Enter)
            )
            {
                CorrectText();
                e.Handled = true;
                Keyboard.ClearFocus();
            }
        }

        private void NumericTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (IsReadOnly == true)
            {
                return;
            }
            Decimal value = Convert.ToDecimal(this.Text);

            if (e.Delta > 0)
            {
                value += this.Step;
            }
            else
            {
                value -= this.Step;
            }
            value = Math.Max(value, this.Min);
            value = Math.Min(value, this.Max);
            this.Text = value.ToString("F" + (-_ParticleSize).ToString());
            ApplyBindingObject();
        }

        private void NumericTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            this._PrevText = this.Text;
        }

        private void CorrectText()
        {
            if (!Regex.IsMatch(Text, this._NumericPattern))
            {
                Text = _PrevText;
            }
            Decimal value = Convert.ToDecimal(Text);
            value = Math.Max(value, Min);
            value = Math.Min(value, Max);
            Text = value.ToString("F" + (-_ParticleSize).ToString());

            ApplyBindingObject();
        }

        private void ApplyBindingObject()
        {
            BindingExpression bindingExpression = this.GetBindingExpression(TextBox.TextProperty);

            if ((bindingExpression != null) && (this.DataContext != null))
            {
                var property = this.DataContext.GetType().GetProperty(bindingExpression.ResolvedSourcePropertyName);
                property?.SetValue(this.DataContext, this.Text);
            }
        }
    }
}

using System;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PWM_Control
{

    public partial class MainWindow : Window
    {
        static SerialPort serialPort;

        public MainWindow()
        {
            InitializeComponent();
            Start();
        }
        private void Start() => SetWidgets(CheckArduino());
        private void SetWidgets(bool ArduinoFinded) {
            SetVisibleAll(ArduinoFinded ? Visibility.Visible : Visibility.Hidden, slider, textBox, powerSwitch);
            SetVisibleAll(ArduinoFinded ? Visibility.Hidden : Visibility.Visible, ErrorTextBlock, recheckButton);
        }
        private void SetVisibleAll(Visibility visibility, params UIElement[] controls) {
            foreach (UIElement control in controls)
                control.Visibility = visibility;
        }
        
        private bool CheckArduino()
        {
            foreach (string port in SerialPort.GetPortNames()) {
                serialPort = new SerialPort(port, 115200);
                serialPort.ReadTimeout = 100;
                serialPort.Open();
                serialPort.Write($"[GET] Arduino\n");
                try { 
                    if (serialPort.ReadByte() == 1) {
                        InitializeArduino();
                        return true;
                    }
                }
                catch(TimeoutException e){  }
                serialPort.Close();
            }
            return false;
        }
        private void InitializeArduino() {

            serialPort.Write($"[GET] PWM_k\n");
            Thread.Sleep(10);
            slider.Value = (serialPort.ReadByte() - 255 * serialPort.ReadByte()) / 255d * 100;

            serialPort.Write($"[GET] isActivated\n");
            Thread.Sleep(10);
            powerSwitch.Content = serialPort.ReadByte() != 0 ? "OFF" : "ON";
        }
        private void RecheckButton_Click(object sender, RoutedEventArgs e) => Start();
        private void Button_Click(object sender, RoutedEventArgs e) {
            try { 
                serialPort.Write("[SET] ON/OFF\n");
                powerSwitch.Content = (string)powerSwitch.Content == "OFF" ? "ON" : "OFF";
            }
            catch {
                SetWidgets(false);
            }
        }
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox txtBox = (TextBox)sender;
            if (e.Key == Key.Return) {
                double.TryParse(txtBox.Text.Trim('%'), out double d);
                var eventToSlider = new RoutedPropertyChangedEventArgs<double>(slider.Value, d);
                Slider_ValueChanged(slider, eventToSlider);
            }
        }
        private void TextBox_TextChanged(object sender, TextCompositionEventArgs e) {
            if (!int.TryParse(e.Text, out int _) && (e.Text != "-" || ((TextBox)sender).Text.Contains('-')))
                e.Handled = true;
        }
        private void textChangedEventHandler(object sender, TextChangedEventArgs args) {
            TextBox txtBox = (TextBox)sender;
            string txt = txtBox.Text.Trim('%');
            if (txt.IndexOf('-') > 0)
                txt = txt.Remove(txt.IndexOf('-'), 1);
            if (int.TryParse(txt, out int val))
                txt = (val > 100 ? 100 : val < -100 ? -100 : val).ToString() + "%";
            txtBox.Text = txt;
        }
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sl = (Slider)sender;
            slider.Value = e.NewValue;
            if (e.NewValue > 0) {
                sl.SelectionStart = 0;
                sl.SelectionEnd = e.NewValue;
            }
            else {
                sl.SelectionStart = e.NewValue;
                sl.SelectionEnd = 0;
            }
            textBox.Text = $"{(int)e.NewValue}%";
            try { serialPort.Write($"{(int)(e.NewValue / 100d * 255)}\n"); }
            catch { SetWidgets(false); }
           
        }

    }
}

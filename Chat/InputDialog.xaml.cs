using System.Windows;
using System.Windows.Input;

namespace Chat
{
    public partial class InputDialog : Window
    {
        public string input;

        public InputDialog(string message)
        {
            InitializeComponent();
            textBlockMessage.Text = message;
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            input = textBoxInput.Text;
            DialogResult = true;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TextBoxInput_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    ButtonOK_Click(sender, e);
                    break;
                case Key.Escape:
                    ButtonCancel_Click(sender, e);
                    break;
            }
        }
    }
}

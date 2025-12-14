using System.Windows;

namespace kanzeed.Pages
{
    public partial class SimpleInputDialog : Window
    {
        public string Value { get; private set; }

        public SimpleInputDialog(string title, string prompt)
        {
            InitializeComponent();
            Title = title;
            PromptText.Text = prompt;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Value = InputBox.Text.Trim();

            if (string.IsNullOrEmpty(Value))
            {
                MessageBox.Show("Поле не может быть пустым",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

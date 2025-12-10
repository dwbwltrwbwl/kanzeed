using kanzeed.ApplicationData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace kanzeed.Pages
{
    /// <summary>
    /// Логика взаимодействия для Registration.xaml
    /// </summary>
    public partial class Registration : Page
    {
        public Registration()
        {
            InitializeComponent();
        }

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePasswordVisibility(PasswordBox, TogglePasswordButton);
        }

        private void ToggleConfirmPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePasswordVisibility(ConfirmPasswordBox, ToggleConfirmPasswordButton);
        }

        private void TogglePasswordVisibility(PasswordBox passwordBox, Button toggleButton)
        {
            try
            {
                var grid = passwordBox.Parent as Grid;
                if (grid == null) return;

                var textBox = grid.Children.OfType<TextBox>().FirstOrDefault();

                if (textBox == null)
                {
                    // Скрыть пароль (переключить на TextBox)
                    var password = passwordBox.Password;
                    passwordBox.Visibility = Visibility.Collapsed;

                    var newTextBox = new TextBox
                    {
                        Text = password,
                        FontSize = 14,
                        Foreground = Brushes.Black,
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Padding = new Thickness(12, 0, 75, 0)
                    };

                    grid.Children.Add(newTextBox);
                    Grid.SetColumn(newTextBox, 0);
                    Grid.SetRow(newTextBox, 0);
                    toggleButton.Content = "🚫";
                }
                else
                {
                    // Показать пароль (вернуть PasswordBox)
                    var password = textBox.Text;
                    grid.Children.Remove(textBox);

                    passwordBox.Password = password;
                    passwordBox.Visibility = Visibility.Visible;
                    toggleButton.Content = "👁";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка переключения пароля: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация данных
                if (!ValidateInput())
                    return;

                // Проверка уникальности email
                if (AppConnect.model01.CUSTOMERS.Any(x => x.email == EmailTextBox.Text))
                {
                    MessageBox.Show("Пользователь с таким email уже существует", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    EmailTextBox.Focus();
                    return;
                }

                // Создание нового клиента
                var newCustomer = new CUSTOMERS
                {
                    last_name = LastNameTextBox.Text.Trim(),
                    first_name = FirstNameTextBox.Text.Trim(),
                    middle_name = MiddleNameTextBox.Text.Trim(),
                    email = EmailTextBox.Text.Trim(),
                    phone = PhoneTextBox.Text.Trim(),
                    password = PasswordBox.Password,
                    id_role = 1 // Роль клиента
                };

                // Добавление в БД
                AppConnect.model01.CUSTOMERS.Add(newCustomer);
                AppConnect.model01.SaveChanges();

                MessageBox.Show("Регистрация прошла успешно!\nТеперь вы можете войти в систему.",
                    "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);

                // Переход на страницу авторизации
                NavigationService.Navigate(new Authorization());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Введите фамилию", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LastNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                MessageBox.Show("Введите имя", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FirstNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Введите email", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return false;
            }

            // Проверка формата email
            if (!IsValidEmail(EmailTextBox.Text))
            {
                MessageBox.Show("Введите корректный email адрес", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Введите пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return false;
            }

            // Проверка минимальной длины пароля
            if (PasswordBox.Password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return false;
            }

            // Проверка совпадения паролей
            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ConfirmPasswordBox.Focus();
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void LoginLink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            NavigationService.Navigate(new Authorization());
        }
    }
}
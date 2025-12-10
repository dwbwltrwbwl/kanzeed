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
    /// Логика взаимодействия для Authorization.xaml
    /// </summary>
    public partial class Authorization : Page
    {
        public Authorization()
        {
            InitializeComponent();
        }

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var grid = PasswordBox.Parent as Grid;
                if (grid == null) return;

                var textBox = grid.Children.OfType<TextBox>().FirstOrDefault();

                if (textBox == null)
                {
                    // Скрыть пароль (переключить на TextBox)
                    var password = PasswordBox.Password;
                    PasswordBox.Visibility = Visibility.Collapsed;

                    var newTextBox = new TextBox
                    {
                        Text = password,
                        FontSize = 14,
                        Foreground = Brushes.Black,
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Padding = new Thickness(15, 12, 45, 12)
                    };

                    grid.Children.Add(newTextBox);
                    Grid.SetColumn(newTextBox, 0);
                    Grid.SetRow(newTextBox, 0);
                    TogglePasswordButton.Content = "👁‍🗨"; // Перечеркнутый глаз
                }
                else
                {
                    // Показать пароль (вернуть PasswordBox)
                    var password = textBox.Text;
                    grid.Children.Remove(textBox);

                    PasswordBox.Password = password;
                    PasswordBox.Visibility = Visibility.Visible;
                    TogglePasswordButton.Content = "👁"; // Обычный глаз
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка переключения пароля: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GuestLoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Вход как гость (клиент с ролью 1)
            try
            {
                // Создаем временного гостевого пользователя
                var guestUser = new CUSTOMERS
                {
                    first_name = "Гость",
                    last_name = "",
                    middle_name = "",
                    id_role = 1 // Роль клиента
                };

                MessageBox.Show("Вы вошли как гость!", "Уведомление",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Переход на главную страницу для гостя
                NavigationService.Navigate(new DataOutput());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка на пустые поля
                if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                {
                    MessageBox.Show("Введите email", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    EmailTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    MessageBox.Show("Введите пароль", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return;
                }

                // Сначала ищем в клиентах
                var customer = AppConnect.model01.CUSTOMERS.FirstOrDefault(x =>
                    x.email == EmailTextBox.Text &&
                    x.password == PasswordBox.Password);

                // Если не нашли в клиентах, ищем в сотрудниках
                if (customer == null)
                {
                    var employee = AppConnect.model01.EMPLOYEES.FirstOrDefault(x =>
                        x.email == EmailTextBox.Text &&
                        x.password == PasswordBox.Password);

                    if (employee != null)
                    {
                        // Авторизация сотрудника
                        ProcessEmployeeLogin(employee);
                        return;
                    }

                    MessageBox.Show("Неверный email или пароль", "Ошибка авторизации",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Авторизация клиента
                ProcessCustomerLogin(customer);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка " + ex.Message.ToString() + " Критическая ошибка приложения!",
                    "Уведомление", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ProcessCustomerLogin(CUSTOMERS customer)
        {
            // Получаем роль клиента
            var role = AppConnect.model01.ROLES.FirstOrDefault(r => r.role_id == customer.id_role);
            string roleName = role?.role_name ?? "Неизвестная роль";

            // Формируем ФИО
            string fullName = $"{customer.last_name} {customer.first_name} {customer.middle_name}".Trim();

            switch (customer.id_role)
            {
                case 1: // Клиент
                    MessageBox.Show($"Здравствуйте, {fullName}! (Роль: {roleName})",
                        "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case 2: // Менеджер (если клиент может быть менеджером)
                    MessageBox.Show($"Здравствуйте, Менеджер {fullName}!",
                        "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                default:
                    MessageBox.Show($"Здравствуйте, {fullName}! (Роль: {roleName})",
                        "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }

            // Сохраняем данные пользователя в статическом классе (если нужно)
            AppData.CurrentUser = new UserData
            {
                Id = customer.customer_id,
                FullName = fullName,
                Email = customer.email,
                RoleId = customer.id_role,
                RoleName = roleName,
                IsEmployee = false
            };

            // Переход на главную страницу
            NavigationService.Navigate(new DataOutput());
        }

        private void ProcessEmployeeLogin(EMPLOYEES employee)
        {
            // Получаем роль сотрудника
            var role = AppConnect.model01.ROLES.FirstOrDefault(r => r.role_id == employee.id_role);
            string roleName = role?.role_name ?? "Неизвестная роль";

            // Формируем ФИО
            string fullName = $"{employee.last_name} {employee.first_name} {employee.middle_name}".Trim();

            switch (employee.id_role)
            {
                case 2: // Менеджер
                    MessageBox.Show($"Здравствуйте, Менеджер {fullName}!",
                        "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case 3: // Курьер
                    MessageBox.Show($"Здравствуйте, Курьер {fullName}!",
                        "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case 4: // Администратор
                    MessageBox.Show($"Здравствуйте, Администратор {fullName}!",
                        "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                default:
                    MessageBox.Show($"Здравствуйте, Сотрудник {fullName}! (Роль: {roleName})",
                        "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }

            // Сохраняем данные сотрудника
            AppData.CurrentUser = new UserData
            {
                Id = employee.employee_id,
                FullName = fullName,
                Email = employee.email,
                RoleId = employee.id_role,
                RoleName = roleName,
                IsEmployee = true
            };

            // Переход на главную страницу (может быть другой для сотрудников)
            NavigationService.Navigate(new DataOutput());
        }

        private void Register_MouseDown(object sender, MouseButtonEventArgs e)
        {
            NavigationService.Navigate(new Pages.Registration());
        }
    }

    // Вспомогательный класс для хранения данных пользователя
    public static class AppData
    {
        public static UserData CurrentUser { get; set; }
    }

    public class UserData
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsEmployee { get; set; }
    }
}
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using kanzeed.ApplicationData;

namespace kanzeed.Pages
{
    public partial class UserProfilePage : Page
    {
        public UserProfilePage()
        {
            InitializeComponent();
            LoadProfile();
        }

        private void LoadProfile()
        {
            var user = AppData.CurrentUser;
            if (user == null)
            {
                // Если гость — перекинуть на страницу авторизации
                MessageBox.Show("Пожалуйста, войдите в систему.", "Требуется авторизация", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.Navigate(new Authorization());
                return;
            }

            // Отобразить базовую информацию
            NameText.Text = user.FullName ?? "Пользователь";
            EmailText.Text = user.Email ?? "-";
            PhoneText.Text = ""; // если храните телефон, заполните
            RoleText.Text = user.RoleName ?? $"Роль: {user.RoleId}";

            // Показываем менеджерские/админ кнопки
            if (user.RoleId == 2 || user.RoleId == 4)
            {
                AdminActions.Visibility = Visibility.Visible;
            }
            else
            {
                AdminActions.Visibility = Visibility.Collapsed;
            }

            if (user.RoleId == 4)
            {
                ManageUsersBtn.Visibility = Visibility.Visible;
            }
            else
            {
                ManageUsersBtn.Visibility = Visibility.Collapsed;
            }

            // Загрузить историю заказов, если клиент (role 1) — либо для всех показываем их релевантно
            if (user.RoleId == 1)
            {
                var orders = AppConnect.model01.ORDERS
                    .Where(o => o.customer_id == user.Id)
                    .OrderByDescending(o => o.order_date)
                    .ToList();
                OrdersList.ItemsSource = orders;

                // адреса клиента
                var addresses = AppConnect.model01.CUSTOMER_ADDRESSES
                    .Where(a => a.customer_id == user.Id)
                    .ToList();
                AddressesList.ItemsSource = addresses;
            }
            else
            {
                // Для сотрудников можно показать пустой список / все заказы (если нужно)
                OrdersList.ItemsSource = Enumerable.Empty<object>();
                AddressesList.ItemsSource = Enumerable.Empty<object>();
            }
        }

        private void EditProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция редактирования профиля пока не реализована. Можно добавить форму редактирования.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            // Очистка текущего пользователя и корзины
            AppData.CurrentUser = null;
            AppData.CurrentCart?.Clear();

            // Вернуться на страницу авторизации
            NavigationService.Navigate(new Authorization());
        }

        private void SavePassword_Click(object sender, RoutedEventArgs e)
        {
            var pw = NewPasswordBox.Password;
            var pw2 = ConfirmPasswordBox.Password;
            if (string.IsNullOrWhiteSpace(pw) || pw.Length < 4)
            {
                MessageBox.Show("Пароль должен содержать как минимум 4 символа", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (pw != pw2)
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Сохранение пароля в таблице CUSTOMERS или EMPLOYEES, в зависимости от IsEmployee флага
            if (AppData.CurrentUser.IsEmployee)
            {
                var emp = AppConnect.model01.EMPLOYEES.FirstOrDefault(empRow => empRow.employee_id == AppData.CurrentUser.Id);

                if (emp != null)
                {
                    emp.password = pw;
                    AppConnect.model01.SaveChanges();
                    MessageBox.Show("Пароль обновлён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                var cust = AppConnect.model01.CUSTOMERS.FirstOrDefault(c => c.customer_id == AppData.CurrentUser.Id);
                if (cust != null)
                {
                    cust.password = pw;
                    AppConnect.model01.SaveChanges();
                    MessageBox.Show("Пароль обновлён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void OpenTables_Click(object sender, RoutedEventArgs e)
        {
            // Навигация на страницу просмотра таблиц (реализуйте свою страницу)
            MessageBox.Show("Перейти к просмотру таблиц (менеджер).", "Навигация", MessageBoxButton.OK, MessageBoxImage.Information);
            // NavigationService.Navigate(new TablesPage());
        }

        private void ManageUsers_Click(object sender, RoutedEventArgs e)
        {
            // Навигация к управлению пользователями (админ)
            MessageBox.Show("Перейти к управлению пользователями (администратор).", "Навигация", MessageBoxButton.OK, MessageBoxImage.Information);
            // NavigationService.Navigate(new ManageUsersPage());
        }
    }
}

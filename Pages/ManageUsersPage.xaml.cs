using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using kanzeed.ApplicationData;

namespace kanzeed.Pages
{
    public partial class ManageUsersPage : Page, INotifyPropertyChanged
    {
        // Коллекция пользователей, отображаемая в гриде
        public ObservableCollection<UserViewModel> Users { get; set; } = new ObservableCollection<UserViewModel>();

        // Список ролей для ComboBox (role_id, role_name)
        public ObservableCollection<ROLES> RolesList { get; set; } = new ObservableCollection<ROLES>();

        public ManageUsersPage()
        {
            InitializeComponent();

            // DataContext нужен для привязки ComboBox-ов к RolesList
            DataContext = this;

            // Проверка прав (только админ)
            if (AppData.CurrentUser == null || AppData.CurrentUser.RoleId != 4)
            {
                MessageBox.Show("Доступ запрещён. Необходима роль администратора.", "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.Navigate(new Authorization());
                return;
            }

            LoadRoles();
            LoadUsers();
        }

        private void LoadRoles()
        {
            RolesList.Clear();
            var roles = AppConnect.model01.ROLES.OrderBy(r => r.role_id).ToList();
            foreach (var r in roles)
            {
                RolesList.Add(r);
            }
            OnPropertyChanged(nameof(RolesList));
        }

        private void LoadUsers()
        {
            Users.Clear();

            // Загрузка клиентов (обычно роли 1-3)
            var customers = AppConnect.model01.CUSTOMERS.ToList();
            foreach (var c in customers)
            {
                var role = AppConnect.model01.ROLES.FirstOrDefault(r => r.role_id == c.id_role);
                Users.Add(new UserViewModel
                {
                    Id = c.customer_id,
                    IsEmployee = false,
                    FirstName = c.first_name,
                    LastName = c.last_name,
                    MiddleName = c.middle_name,
                    Email = c.email,
                    RoleId = c.id_role,
                    RoleName = role?.role_name ?? "",
                    PreviousRoleId = c.id_role,
                    PreviousIsEmployee = false
                });
            }

            // Загрузка сотрудников (роли 4-5)
            var employees = AppConnect.model01.EMPLOYEES.ToList();
            foreach (var emp in employees)
            {
                var role = AppConnect.model01.ROLES.FirstOrDefault(r => r.role_id == emp.id_role);
                Users.Add(new UserViewModel
                {
                    Id = emp.employee_id,
                    IsEmployee = true,
                    FirstName = emp.first_name,
                    LastName = emp.last_name,
                    MiddleName = emp.middle_name,
                    Email = emp.email,
                    RoleId = emp.id_role,
                    RoleName = role?.role_name ?? "",
                    PreviousRoleId = emp.id_role,
                    PreviousIsEmployee = true
                });
            }

            UsersGrid.ItemsSource = Users;
        }

        // Поиск
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = SearchBox.Text?.Trim().ToLower() ?? "";

            if (string.IsNullOrEmpty(q))
            {
                UsersGrid.ItemsSource = Users;
                return;
            }

            var filtered = Users.Where(uv =>
                uv.FullName != null && uv.FullName.ToLower().Contains(q) ||
                uv.Email != null && uv.Email.ToLower().Contains(q) ||
                uv.Id.ToString().Contains(q)).ToList();

            UsersGrid.ItemsSource = filtered;
        }

        // Сохранить роль для конкретного пользователя
        // Сохранить роль для конкретного пользователя
        // Сохранить роль для конкретного пользователя
        private void SaveRole_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || !(btn.Tag is UserViewModel uv)) return;

            try
            {
                // Просто обновляем роль в текущей таблице
                if (uv.IsEmployee)
                {
                    var emp = AppConnect.model01.EMPLOYEES
                        .FirstOrDefault(empRow => empRow.employee_id == uv.Id);
                    if (emp != null)
                    {
                        emp.id_role = uv.RoleId;
                    }
                }
                else
                {
                    var cust = AppConnect.model01.CUSTOMERS
                        .FirstOrDefault(custRow => custRow.customer_id == uv.Id);
                    if (cust != null)
                    {
                        cust.id_role = uv.RoleId;
                    }
                }

                // Сохраняем с обработкой ошибок валидации
                AppConnect.model01.SaveChanges();

                // Обновляем отображаемое имя роли
                var role = AppConnect.model01.ROLES.FirstOrDefault(r => r.role_id == uv.RoleId);
                uv.RoleName = role?.role_name ?? "";

                MessageBox.Show("Роль успешно сохранена", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                // Обработка ошибок валидации Entity Framework
                var errorMessages = new List<string>();
                foreach (var validationResult in ex.EntityValidationErrors)
                {
                    foreach (var error in validationResult.ValidationErrors)
                    {
                        errorMessages.Add($"Property: {error.PropertyName} - Error: {error.ErrorMessage}");
                    }
                }

                string fullErrorMessage = string.Join("\n", errorMessages);
                MessageBox.Show($"Ошибка валидации:\n{fullErrorMessage}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении роли: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Вспомогательный метод для безопасного получения свойства
        private object GetPropertyIfExists(object obj, string propertyName)
        {
            var prop = obj.GetType().GetProperty(propertyName);
            return prop?.GetValue(obj);
        }

        // Метод для определения, является ли роль "сотруднической"
        private bool IsEmployeeRole(int roleId)
        {
            // Предположим, что роли 4-5 - для сотрудников, 1-3 - для клиентов
            // Настройте это в соответствии с вашей структурой ролей
            return roleId >= 4; // или другая логика
        }

        // Открыть страницу профиля
        private void OpenProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || !(btn.Tag is UserViewModel uv)) return;

            // Навигация: если это текущий пользователь — открыть профиль, иначе показать сообщение
            if (AppData.CurrentUser != null && AppData.CurrentUser.Id == uv.Id && AppData.CurrentUser.IsEmployee == uv.IsEmployee)
            {
                NavigationService.Navigate(new UserProfilePage());
            }
            else
            {
                MessageBox.Show($"ID: {uv.Id}\nИмя: {uv.FullName}\nEmail: {uv.Email}\nРоль: {uv.RoleName}\nТип: {(uv.IsEmployee ? "Сотрудник" : "Клиент")}",
                    "Информация о пользователе", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRoles();
            LoadUsers();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion
    }
}
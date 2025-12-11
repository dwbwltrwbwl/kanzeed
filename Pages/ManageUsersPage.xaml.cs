using System;
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

        private CollectionViewSource usersViewSource;

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

            usersViewSource = new CollectionViewSource();
            usersViewSource.Source = Users;
            UsersGrid.ItemsSource = usersViewSource.View;

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

            // Загрузка клиентов
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
                    RoleName = role?.role_name ?? ""
                });
            }

            // Загрузка сотрудников
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
                    RoleName = role?.role_name ?? ""
                });
            }

            usersViewSource.View.Refresh();
        }

        // Поиск
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = SearchBox.Text?.Trim().ToLower() ?? "";

            usersViewSource.View.Filter = obj =>
            {
                if (!(obj is UserViewModel uv)) return false;
                if (string.IsNullOrEmpty(q)) return true;
                if (uv.FullName != null && uv.FullName.ToLower().Contains(q)) return true;
                if (uv.Email != null && uv.Email.ToLower().Contains(q)) return true;
                if (uv.Id.ToString().Contains(q)) return true;
                return false;
            };
        }

        // Сохранить роль для конкретного пользователя (кнопка Save)
        private void SaveRole_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || !(btn.Tag is UserViewModel uv)) return;

            try
            {
                if (uv.IsEmployee)
                {
                    var emp = AppConnect.model01.EMPLOYEES
                        .FirstOrDefault(empRow => empRow.employee_id == uv.Id);
                    if (emp != null)
                    {
                        emp.id_role = uv.RoleId;
                        AppConnect.model01.SaveChanges();
                    }
                }
                else
                {
                    var cust = AppConnect.model01.CUSTOMERS
                        .FirstOrDefault(custRow => custRow.customer_id == uv.Id);
                    if (cust != null)
                    {
                        cust.id_role = uv.RoleId;
                        AppConnect.model01.SaveChanges();
                    }
                }

                // Обновить отображаемое имя роли
                var role = AppConnect.model01.ROLES.FirstOrDefault(r => r.role_id == uv.RoleId);
                uv.RoleName = role?.role_name ?? "";
                MessageBox.Show("Роль сохранена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                usersViewSource.View.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении роли: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Открыть страницу профиля (перейти в UserProfilePage для выбранного пользователя, если нужно)
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
                // Покажем диалог с базовой информацией
                MessageBox.Show($"ID: {uv.Id}\nИмя: {uv.FullName}\nEmail: {uv.Email}\nРоль: {uv.RoleName}\nСотрудник: {uv.IsEmployee}",
                    "Пользователь", MessageBoxButton.OK, MessageBoxImage.Information);
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

    // ViewModel для общих пользователей (клиент / сотрудник)
    public class UserViewModel : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public bool IsEmployee { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }

        public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();

        public string Email { get; set; }

        private int _roleId;
        public int RoleId
        {
            get => _roleId;
            set
            {
                if (_roleId == value) return;
                _roleId = value;
                OnPropertyChanged(nameof(RoleId));
            }
        }

        private string _roleName;
        public string RoleName
        {
            get => _roleName;
            set
            {
                if (_roleName == value) return;
                _roleName = value;
                OnPropertyChanged(nameof(RoleName));
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion
    }
}

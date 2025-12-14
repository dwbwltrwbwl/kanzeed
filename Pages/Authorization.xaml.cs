using kanzeed.ApplicationData;
using System;
using System.Linq;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace kanzeed.Pages
{
    public partial class Authorization : Page
    {
        private TextBlock emailErrorTextBlock;
        private TextBlock passwordErrorTextBlock;

        public Authorization()
        {
            InitializeComponent();

            CreateInlineErrorControls();

            EmailTextBox.TextChanged += (s, e) => ClearEmailError();
            PasswordBox.PasswordChanged += (s, e) => ClearPasswordError();
        }

        #region Inline errors

        private void CreateInlineErrorControls()
        {
            try
            {
                // Email
                var emailGrid = EmailTextBox.Parent as Grid;
                var emailBorder = emailGrid?.Parent as Border;
                var emailStack = emailBorder?.Parent as StackPanel;

                if (emailStack != null)
                {
                    emailErrorTextBlock = new TextBlock
                    {
                        Foreground = Brushes.DarkRed,
                        FontSize = 12,
                        Margin = new Thickness(6, 6, 0, 0),
                        Visibility = Visibility.Collapsed
                    };
                    emailStack.Children.Add(emailErrorTextBlock);
                }

                // Password
                var pwdGrid = PasswordBox.Parent as Grid;
                var pwdBorder = pwdGrid?.Parent as Border;
                var pwdStack = pwdBorder?.Parent as StackPanel;

                if (pwdStack != null)
                {
                    passwordErrorTextBlock = new TextBlock
                    {
                        Foreground = Brushes.DarkRed,
                        FontSize = 12,
                        Margin = new Thickness(6, 6, 0, 0),
                        Visibility = Visibility.Collapsed
                    };
                    pwdStack.Children.Add(passwordErrorTextBlock);
                }
            }
            catch
            {
                // игнорируем — UI всё равно продолжит работать
            }
        }

        private void ShowEmailError(string text)
        {
            if (emailErrorTextBlock == null) return;
            emailErrorTextBlock.Text = text;
            emailErrorTextBlock.Visibility = Visibility.Visible;
            SetBorderBrush(EmailTextBox, "#E74C3C");
        }

        private void ShowPasswordError(string text)
        {
            if (passwordErrorTextBlock == null) return;
            passwordErrorTextBlock.Text = text;
            passwordErrorTextBlock.Visibility = Visibility.Visible;
            SetBorderBrush(PasswordBox, "#E74C3C");
        }

        private void ClearEmailError()
        {
            if (emailErrorTextBlock == null) return;
            emailErrorTextBlock.Visibility = Visibility.Collapsed;
            SetBorderBrush(EmailTextBox, "#DDDDDD");
        }

        private void ClearPasswordError()
        {
            if (passwordErrorTextBlock == null) return;
            passwordErrorTextBlock.Visibility = Visibility.Collapsed;
            SetBorderBrush(PasswordBox, "#DDDDDD");
        }

        private void SetBorderBrush(Control control, string color)
        {
            try
            {
                var grid = control.Parent as Grid;
                var border = grid?.Parent as Border;
                if (border != null)
                    border.BorderBrush = (Brush)new BrushConverter().ConvertFromString(color);
            }
            catch { }
        }

        #endregion

        #region LOGIN

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ClearEmailError();
            ClearPasswordError();

            string email = EmailTextBox.Text?.Trim() ?? "";
            string password = PasswordBox.Password ?? "";

            bool ok = true;

            // Email validation
            if (string.IsNullOrEmpty(email))
            {
                ShowEmailError("Введите email");
                ok = false;
            }
            else if (email.Length > 50)
            {
                ShowEmailError("Email не должен превышать 50 символов");
                ok = false;
            }
            else if (!IsValidEmail(email))
            {
                ShowEmailError("Некорректный email");
                ok = false;
            }

            // Password validation
            if (string.IsNullOrWhiteSpace(password))
            {
                ShowPasswordError("Введите пароль");
                ok = false;
            }
            else if (password.Length < 6)
            {
                ShowPasswordError("Минимум 6 символов");
                ok = false;
            }

            if (!ok) return;

            string normEmail = email.ToLowerInvariant();

            // Клиент
            var customer = AppConnect.model01.CUSTOMERS
                .FirstOrDefault(c =>
                    c.email.ToLower() == normEmail &&
                    c.password == password);

            if (customer != null)
            {
                ProcessCustomerLogin(customer);
                return;
            }

            // Сотрудник
            var employee = AppConnect.model01.EMPLOYEES
                .FirstOrDefault(emp =>
                    emp.email.ToLower() == normEmail &&
                    emp.password == password);

            if (employee != null)
            {
                ProcessEmployeeLogin(employee);
                return;
            }

            ShowEmailError("Неверный email или пароль");
            ShowPasswordError("");
        }

        private void ProcessCustomerLogin(CUSTOMERS customer)
        {
            var role = AppConnect.model01.ROLES
                .FirstOrDefault(r => r.role_id == customer.id_role);

            AppData.CurrentUser = new UserData
            {
                Id = customer.customer_id,
                FullName = $"{customer.last_name} {customer.first_name} {customer.middle_name}".Trim(),
                Email = customer.email,
                RoleId = customer.id_role,
                RoleName = role?.role_name,
                IsEmployee = false
            };

            AppData.CurrentCart = new System.Collections.Generic.Dictionary<int, int>();
            NavigationService.Navigate(new DataOutput());
        }

        private void ProcessEmployeeLogin(EMPLOYEES employee)
        {
            var role = AppConnect.model01.ROLES
                .FirstOrDefault(r => r.role_id == employee.id_role);

            AppData.CurrentUser = new UserData
            {
                Id = employee.employee_id,
                FullName = $"{employee.last_name} {employee.first_name} {employee.middle_name}".Trim(),
                Email = employee.email,
                RoleId = employee.id_role,
                RoleName = role?.role_name,
                IsEmployee = true
            };

            AppData.CurrentCart = new System.Collections.Generic.Dictionary<int, int>();
            NavigationService.Navigate(new DataOutput());
        }

        #endregion

        #region OTHER

        private void GuestLoginButton_Click(object sender, RoutedEventArgs e)
        {
            AppData.CurrentUser = null;
            AppData.CurrentCart = new System.Collections.Generic.Dictionary<int, int>();
            NavigationService.Navigate(new DataOutput());
        }

        private void Register_MouseDown(object sender, MouseButtonEventArgs e)
        {
            NavigationService.Navigate(new Registration());
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                return new MailAddress(email).Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}

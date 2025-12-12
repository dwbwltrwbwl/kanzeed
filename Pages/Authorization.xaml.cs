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
    /// <summary>
    /// Logic for Authorization.xaml
    /// </summary>
    public partial class Authorization : Page
    {
        // Динамически создаваемые TextBlock'ы для ошибок (чтобы не менять XAML)
        private TextBlock emailErrorTextBlock;
        private TextBlock passwordErrorTextBlock;

        // Вспомогательный TextBox для отображения пароля при "показать"
        private TextBox passwordRevealTextBox;

        public Authorization()
        {
            InitializeComponent();

            // Создаём и вставляем inline-подсказки под полями (не меняя XAML)
            CreateInlineErrorControls();

            // Подпишемся на события, чтобы очищать подсказки при вводе
            EmailTextBox.TextChanged += (s, e) => ClearEmailError();
            PasswordBox.PasswordChanged += (s, e) =>
            {
                // синхронизируем reveal textbox если он видим
                if (passwordRevealTextBox != null && passwordRevealTextBox.Visibility == Visibility.Visible)
                    passwordRevealTextBox.Text = PasswordBox.Password;

                ClearPasswordError();
            };

            // Если пользователь будет печатать в reveal textbox — синхронизируем обратно
            // (создаётся лениво при первом показе)
        }

        #region Inline UI helpers (вставляем подсказки в существующий XAML без правки XAML)

        private void CreateInlineErrorControls()
        {
            try
            {
                // Email: EmailTextBox -> parent Grid -> parent Border -> parent StackPanel (Grid.Row=1)
                var emailGrid = EmailTextBox.Parent as Grid;
                if (emailGrid != null)
                {
                    var emailBorder = emailGrid.Parent as Border;
                    var emailStack = emailBorder?.Parent as StackPanel;
                    if (emailStack != null)
                    {
                        emailErrorTextBlock = new TextBlock
                        {
                            Foreground = (Brush)new BrushConverter().ConvertFromString("#C0392B"),
                            FontSize = 12,
                            Margin = new Thickness(6, 6, 0, 0),
                            Visibility = Visibility.Collapsed,
                        };
                        emailStack.Children.Add(emailErrorTextBlock);
                    }
                }

                // Password: PasswordBox -> parent Grid -> parent Border -> parent StackPanel (Grid.Row=2)
                var pwdGrid = PasswordBox.Parent as Grid;
                if (pwdGrid != null)
                {
                    var pwdBorder = pwdGrid.Parent as Border;
                    var pwdStack = pwdBorder?.Parent as StackPanel;
                    if (pwdStack != null)
                    {
                        passwordErrorTextBlock = new TextBlock
                        {
                            Foreground = (Brush)new BrushConverter().ConvertFromString("#C0392B"),
                            FontSize = 12,
                            Margin = new Thickness(6, 6, 0, 0),
                            Visibility = Visibility.Collapsed,
                        };
                        pwdStack.Children.Add(passwordErrorTextBlock);
                    }
                }
            }
            catch
            {
                // В случае ошибки добавления подсказок — ничего критичного, продолжим без inline-подсказок.
                emailErrorTextBlock = null;
                passwordErrorTextBlock = null;
            }
        }

        private void ShowEmailError(string message)
        {
            if (emailErrorTextBlock != null)
            {
                emailErrorTextBlock.Text = message;
                emailErrorTextBlock.Visibility = Visibility.Visible;
            }
            // Также выделим бордер поля
            SetBorderBrushForControl(EmailTextBox, "#E74C3C");
        }

        private void ClearEmailError()
        {
            if (emailErrorTextBlock != null)
            {
                emailErrorTextBlock.Text = "";
                emailErrorTextBlock.Visibility = Visibility.Collapsed;
            }
            SetBorderBrushForControl(EmailTextBox, "#DDDDDD");
        }

        private void ShowPasswordError(string message)
        {
            if (passwordErrorTextBlock != null)
            {
                passwordErrorTextBlock.Text = message;
                passwordErrorTextBlock.Visibility = Visibility.Visible;
            }
            SetBorderBrushForControl(PasswordBox, "#E74C3C");
        }

        private void ClearPasswordError()
        {
            if (passwordErrorTextBlock != null)
            {
                passwordErrorTextBlock.Text = "";
                passwordErrorTextBlock.Visibility = Visibility.Collapsed;
            }
            SetBorderBrushForControl(PasswordBox, "#DDDDDD");
        }

        // Установить цвет Border для обёртывающего Border вокруг контролла (если найден)
        private void SetBorderBrushForControl(Control control, string hexBrush)
        {
            try
            {
                var parentGrid = control.Parent as Grid;
                if (parentGrid == null) return;
                var border = parentGrid.Parent as Border;
                if (border == null) return;
                border.BorderBrush = (Brush)new BrushConverter().ConvertFromString(hexBrush);
            }
            catch
            {
                // ignore
            }
        }

        #endregion

        #region Password reveal toggle (корректная работа без удаления контролов)

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Если reveal textbox ещё не создан — создаём и размещаем его поверх PasswordBox (в той же Grid: column 0)
                if (passwordRevealTextBox == null)
                {
                    var pwdGrid = PasswordBox.Parent as Grid;
                    if (pwdGrid == null) return;

                    passwordRevealTextBox = new TextBox
                    {
                        Visibility = Visibility.Collapsed,
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Padding = PasswordBox.Padding,
                        FontSize = PasswordBox.FontSize,
                        Foreground = PasswordBox.Foreground,
                        VerticalContentAlignment = VerticalAlignment.Center
                    };

                    // Подписываемся на события изменений текста
                    passwordRevealTextBox.TextChanged += PasswordRevealTextBox_TextChanged;

                    // Вставляем в Grid в ту же колонку (Grid.Column=0)
                    Grid.SetColumn(passwordRevealTextBox, 0);
                    pwdGrid.Children.Add(passwordRevealTextBox);
                }

                // Переключение видимости
                if (passwordRevealTextBox.Visibility == Visibility.Collapsed)
                {
                    // Показать: копируем пароль и показываем TextBox
                    passwordRevealTextBox.Text = PasswordBox.Password;
                    PasswordBox.Visibility = Visibility.Collapsed;
                    passwordRevealTextBox.Visibility = Visibility.Visible;
                    TogglePasswordButton.Content = "🙈"; // скрыть-иконка
                }
                else
                {
                    // Скрыть: скопировать из textbox обратно в passwordbox и показать PasswordBox
                    PasswordBox.Password = passwordRevealTextBox.Text;
                    passwordRevealTextBox.Visibility = Visibility.Collapsed;
                    PasswordBox.Visibility = Visibility.Visible;
                    TogglePasswordButton.Content = "👁"; // показать-иконка
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка переключения видимости пароля: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PasswordRevealTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // если пользователь редактирует видимый пароль — синхронизируем в PasswordBox (только значение, не видимость)
            try
            {
                if (passwordRevealTextBox != null)
                {
                    PasswordBox.Password = passwordRevealTextBox.Text;
                    ClearPasswordError();
                }
            }
            catch { /*ignore*/ }
        }

        #endregion

        #region Login logic + validation

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // очищаем старые подсказки
                ClearEmailError();
                ClearPasswordError();

                string rawEmail = EmailTextBox.Text ?? string.Empty;
                string email = rawEmail.Trim();
                string password = PasswordBox.Visibility == Visibility.Visible ? PasswordBox.Password : (passwordRevealTextBox?.Text ?? string.Empty);

                bool ok = true;

                // email: required
                if (string.IsNullOrEmpty(email))
                {
                    ShowEmailError("Введите email");
                    ok = false;
                }
                else if (email.Length > 50)
                {
                    ShowEmailError("Email должен содержать не более 50 символов");
                    ok = false;
                }
                else if (!IsValidEmail(email))
                {
                    ShowEmailError("Введите корректный email");
                    ok = false;
                }

                // password: required
                if (string.IsNullOrWhiteSpace(password))
                {
                    ShowPasswordError("Введите пароль");
                    ok = false;
                }
                else if (password.Length < 6)
                {
                    ShowPasswordError("Пароль должен содержать минимум 6 символов");
                    ok = false;
                }
                else if (password.Length > 50)
                {
                    ShowPasswordError("Пароль должен содержать не более 50 символов");
                    ok = false;
                }
                else if (password.Any(char.IsControl))
                {
                    ShowPasswordError("Пароль содержит недопустимые управляющие символы");
                    ok = false;
                }

                if (!ok) return;

                // Авторизация: нормализуем email
                string normalizedEmail = email.ToLowerInvariant();

                // сначала клиенты
                var customer = AppConnect.model01.CUSTOMERS.FirstOrDefault(x =>
                    x.email.ToLower() == normalizedEmail && x.password == password);

                if (customer != null)
                {
                    ProcessCustomerLogin(customer);
                    return;
                }

                // потом сотрудники
                var employee = AppConnect.model01.EMPLOYEES.FirstOrDefault(x =>
                    x.email.ToLower() == normalizedEmail && x.password == password);

                if (employee != null)
                {
                    ProcessEmployeeLogin(employee);
                    return;
                }

                // Если не найдено — показать inline-ошибку (под полями)
                ShowEmailError("Неверный email или пароль");
                ShowPasswordError(""); // подсветка пароля (без текста) — чтобы показать, что проблема и там
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка авторизации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProcessCustomerLogin(CUSTOMERS customer)
        {
            var role = AppConnect.model01.ROLES.FirstOrDefault(r => r.role_id == customer.id_role);
            string roleName = role?.role_name ?? "Неизвестная роль";
            string fullName = $"{customer.last_name} {customer.first_name} {customer.middle_name}".Trim();

            AppData.CurrentUser = new UserData
            {
                Id = customer.customer_id,
                FullName = fullName,
                Email = customer.email,
                RoleId = customer.id_role,
                RoleName = roleName,
                IsEmployee = false
            };

            // очистим корзину при новом логине
            AppData.CurrentCart = new System.Collections.Generic.Dictionary<int, int>();

            // переходим на DataOutput
            NavigationService?.Navigate(new DataOutput());
        }

        private void ProcessEmployeeLogin(EMPLOYEES employee)
        {
            var role = AppConnect.model01.ROLES.FirstOrDefault(r => r.role_id == employee.id_role);
            string roleName = role?.role_name ?? "Неизвестная роль";
            string fullName = $"{employee.last_name} {employee.first_name} {employee.middle_name}".Trim();

            AppData.CurrentUser = new UserData
            {
                Id = employee.employee_id,
                FullName = fullName,
                Email = employee.email,
                RoleId = employee.id_role,
                RoleName = roleName,
                IsEmployee = true
            };

            AppData.CurrentCart = new System.Collections.Generic.Dictionary<int, int>();

            NavigationService?.Navigate(new DataOutput());
        }

        #endregion

        #region Guest / Register

        private void GuestLoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Вход как гость — оставляем CurrentUser = null (или можно создать временный клиент)
            AppData.CurrentUser = null;
            AppData.CurrentCart = new System.Collections.Generic.Dictionary<int, int>();
            NavigationService?.Navigate(new DataOutput());
        }

        private void Register_MouseDown(object sender, MouseButtonEventArgs e)
        {
            NavigationService?.Navigate(new Pages.Registration());
        }

        #endregion

        #region Utilities

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}

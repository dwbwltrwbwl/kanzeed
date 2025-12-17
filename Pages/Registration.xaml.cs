using kanzeed.ApplicationData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace kanzeed.Pages
{
    /// <summary>
    /// Логика взаимодействия для Registration.xaml
    /// </summary>
    public partial class Registration : Page
    {
        // Inline error textblocks (добавляются динамически под соответствующие StackPanel)
        private TextBlock lastNameError;
        private TextBlock firstNameError;
        private TextBlock emailError;
        private TextBlock phoneError;
        private TextBlock passwordError;
        private TextBlock confirmPasswordError;

        // Для управления вводом телефона
        private bool _phoneUpdating;

        public Registration()
        {
            InitializeComponent();

            // Подготовка inline ошибок (не меняет XAML)
            CreateInlineErrorControls();

            // Подписки для очистки ошибок при вводе
            LastNameTextBox.TextChanged += (s, e) => ClearLastNameError();
            FirstNameTextBox.TextChanged += (s, e) => ClearFirstNameError();
            EmailTextBox.TextChanged += (s, e) => ClearEmailError();
            PhoneTextBox.TextChanged += PhoneTextBox_TextChanged;
            PasswordBox.PasswordChanged += (s, e) => ClearPasswordError();
            ConfirmPasswordBox.PasswordChanged += (s, e) => ClearConfirmPasswordError();

            // Обработчики для маски телефона (как на странице аккаунта)
            PhoneTextBox.PreviewTextInput += PhoneTextBox_PreviewTextInput;
        }

        #region Inline errors helpers

        private void CreateInlineErrorControls()
        {
            try
            {
                // LastNameTextBox -> Border -> StackPanel (Grid.Column=0 in your XAML)
                AddErrorBlockUnderControl(LastNameTextBox, out lastNameError);

                // FirstName
                AddErrorBlockUnderControl(FirstNameTextBox, out firstNameError);

                // Email
                AddErrorBlockUnderControl(EmailTextBox, out emailError);

                // Phone
                AddErrorBlockUnderControl(PhoneTextBox, out phoneError);

                // Password (under its StackPanel)
                AddErrorBlockUnderControl(PasswordBox, out passwordError);

                // ConfirmPassword
                AddErrorBlockUnderControl(ConfirmPasswordBox, out confirmPasswordError);
            }
            catch
            {
                // если что-то пошло не так — игнорируем (ошибки будут показываться MessageBox'ом)
            }
        }

        private void AddErrorBlockUnderControl(Control control, out TextBlock errorBlock)
        {
            errorBlock = null;
            try
            {
                // control.Parent == Grid -> parent Border -> parent StackPanel
                var grid = control.Parent as Grid;
                if (grid == null) return;
                var border = grid.Parent as Border;
                var stack = border?.Parent as StackPanel;
                if (stack == null) return;

                errorBlock = new TextBlock
                {
                    Foreground = (Brush)new BrushConverter().ConvertFromString("#C0392B"),
                    FontSize = 12,
                    Margin = new Thickness(6, 6, 0, 0),
                    Visibility = Visibility.Collapsed,
                    TextWrapping = TextWrapping.Wrap
                };

                stack.Children.Add(errorBlock);
            }
            catch
            {
                errorBlock = null;
            }
        }

        private void ShowError(TextBlock block, string message)
        {
            if (block == null)
            {
                // запасной вариант — MessageBox
                MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            block.Text = message;
            block.Visibility = Visibility.Visible;

            // подсветим Border вокруг поля (если есть)
            SetBorderBrushForControl(block, "#E74C3C");
        }

        private void ClearError(TextBlock block)
        {
            if (block == null) return;
            block.Text = "";
            block.Visibility = Visibility.Collapsed;

            // вернуть нормальный бордер (светло-серый)
            SetBorderBrushForControl(block, "#DDDDDD");
        }

        private void ClearLastNameError() => ClearError(lastNameError);
        private void ClearFirstNameError() => ClearError(firstNameError);
        private void ClearEmailError() => ClearError(emailError);
        private void ClearPhoneError() => ClearError(phoneError);
        private void ClearPasswordError() => ClearError(passwordError);
        private void ClearConfirmPasswordError() => ClearError(confirmPasswordError);

        // Устанавливает BorderBrush родительского Border для контрола, под которым лежит errorBlock
        private void SetBorderBrushForControl(TextBlock block, string hexBrush)
        {
            try
            {
                if (block == null) return;

                // block.Parent == StackPanel -> last child is block, previous child contains Border -> Grid -> control
                var stack = block.Parent as StackPanel;
                if (stack == null || stack.Children.Count == 0) return;

                // Найдём Border внутри StackPanel (первый элемент, обычно)
                Border border = null;
                foreach (var ch in stack.Children)
                {
                    if (ch is Border b)
                    {
                        border = b;
                        break;
                    }
                }

                if (border != null)
                {
                    border.BorderBrush = (Brush)new BrushConverter().ConvertFromString(hexBrush);
                }
            }
            catch { /* ignore */ }
        }

        #endregion

        #region Phone mask & input handling (как на странице аккаунта)

        // PHONE MASK - ТАК ЖЕ КАК НА СТРАНИЦЕ АККАУНТА
        private void PhoneTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_phoneUpdating) return;
            _phoneUpdating = true;

            PhoneTextBox.Text = FormatPhone(ExtractDigits(PhoneTextBox.Text));
            PhoneTextBox.CaretIndex = PhoneTextBox.Text.Length;

            _phoneUpdating = false;
            ClearPhoneError();
        }

        private string ExtractDigits(string text) =>
            new string(text.Where(char.IsDigit).ToArray());

        private string FormatPhone(string digits)
        {
            if (string.IsNullOrEmpty(digits))
                return "+7";

            // если начинается с 7 — убираем
            if (digits.StartsWith("7"))
                digits = digits.Substring(1);

            // максимум 10 цифр
            if (digits.Length > 10)
                digits = digits.Substring(0, 10);

            if (digits.Length == 0)
                return "+7";

            if (digits.Length <= 3)
                return "+7 (" + digits;

            if (digits.Length <= 6)
                return "+7 (" + digits.Substring(0, 3) + ") " +
                       digits.Substring(3);

            if (digits.Length <= 8)
                return "+7 (" + digits.Substring(0, 3) + ") " +
                       digits.Substring(3, 3) + "-" +
                       digits.Substring(6);

            return "+7 (" + digits.Substring(0, 3) + ") " +
                   digits.Substring(3, 3) + "-" +
                   digits.Substring(6, 2) + "-" +
                   digits.Substring(8);
        }

        // Helper to extract digits from phone for validation
        private string GetDigitsFromPhone()
        {
            return ExtractDigits(PhoneTextBox.Text);
        }

        #endregion

        #region Registration / validation

        private void LoginLink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            NavigationService?.Navigate(new Authorization());
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                // Дополнительная обработка: нормализация email, очистка телефона до цифр
                var email = EmailTextBox.Text.Trim();
                var phoneDigits = GetDigitsFromPhone();

                // Проверка уникальности телефона в таблице CUSTOMERS
                var phoneExistsInCustomers = AppConnect.model01.CUSTOMERS.Any(c => c.phone == phoneDigits);

                // Также проверяем в таблице EMPLOYEES (сотрудники тоже могут иметь телефоны)
                var phoneExistsInEmployees = AppConnect.model01.EMPLOYEES.Any(emp => emp.phone == phoneDigits);

                if (phoneExistsInCustomers || phoneExistsInEmployees)
                {
                    ShowError(phoneError, "Пользователь с таким телефоном уже существует");
                    return;
                }

                // Проверка уникальности email в обеих таблицах (перед созданием)
                var emailExistsInCustomers = AppConnect.model01.CUSTOMERS.Any(c => c.email.ToLower() == email.ToLower());
                var emailExistsInEmployees = AppConnect.model01.EMPLOYEES.Any(emp => emp.email.ToLower() == email.ToLower());

                if (emailExistsInCustomers || emailExistsInEmployees)
                {
                    ShowError(emailError, "Пользователь с таким email уже существует");
                    return;
                }

                // Создание нового клиента
                var newCustomer = new CUSTOMERS
                {
                    last_name = LastNameTextBox.Text.Trim(),
                    first_name = FirstNameTextBox.Text.Trim(),
                    middle_name = MiddleNameTextBox.Text.Trim(),
                    email = email,
                    phone = phoneDigits, // сохраняем только цифры
                    password = PasswordBox.Password,
                    id_role = 1 // роль клиента
                };

                // Добавление в БД
                AppConnect.model01.CUSTOMERS.Add(newCustomer);
                AppConnect.model01.SaveChanges();

                MessageBox.Show("Регистрация прошла успешно!\nТеперь вы можете войти в систему.", "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);

                // Навигация на страницу авторизации
                NavigationService?.Navigate(new Authorization());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            // Очистим предыдущие сообщения
            ClearLastNameError();
            ClearFirstNameError();
            ClearEmailError();
            ClearPhoneError();
            ClearPasswordError();
            ClearConfirmPasswordError();

            bool ok = true;

            // Фамилия
            var last = (LastNameTextBox.Text ?? "").Trim();
            if (string.IsNullOrEmpty(last))
            {
                ShowError(lastNameError, "Введите фамилию");
                ok = false;
            }
            else if (last.Length > 50)
            {
                ShowError(lastNameError, "Фамилия слишком длинная (макс. 50 символов)");
                ok = false;
            }

            // Имя
            var first = (FirstNameTextBox.Text ?? "").Trim();
            if (string.IsNullOrEmpty(first))
            {
                ShowError(firstNameError, "Введите имя");
                ok = false;
            }
            else if (first.Length > 50)
            {
                ShowError(firstNameError, "Имя слишком длинное (макс. 50 символов)");
                ok = false;
            }

            // Email
            var email = (EmailTextBox.Text ?? "").Trim();
            if (string.IsNullOrEmpty(email))
            {
                ShowError(emailError, "Введите email");
                ok = false;
            }
            else if (email.Length > 100) // Согласно структуре таблицы nvarchar(100)
            {
                ShowError(emailError, "Email слишком длинный (макс. 100 символов)");
                ok = false;
            }
            else if (!IsValidEmail(email))
            {
                ShowError(emailError, "Введите корректный email");
                ok = false;
            }

            // Телефон — обязательный, минимум 10 цифр (после +7)
            var digits = GetDigitsFromPhone();
            // Убираем ведущую 7 если есть (так как формат +7)
            if (digits.StartsWith("7"))
                digits = digits.Substring(1);

            if (string.IsNullOrEmpty(digits))
            {
                ShowError(phoneError, "Введите телефон");
                ok = false;
            }
            else if (digits.Length < 10)
            {
                ShowError(phoneError, "Телефон должен содержать 10 цифр (без +7)");
                ok = false;
            }
            else if (digits.Length > 20) // Согласно структуре таблицы nvarchar(20)
            {
                ShowError(phoneError, "Телефон слишком длинный");
                ok = false;
            }

            // Пароль: требования
            string password = PasswordBox.Password;
            string confirm = ConfirmPasswordBox.Password;

            if (string.IsNullOrEmpty(password))
            {
                ShowError(passwordError, "Введите пароль");
                ok = false;
            }
            else
            {
                if (password.Length < 6)
                {
                    ShowError(passwordError, "Пароль должен содержать минимум 6 символов");
                    ok = false;
                }
                else if (password.Length > 100) // Согласно структуре таблицы nvarchar(100)
                {
                    ShowError(passwordError, "Пароль слишком длинный (макс. 100 символов)");
                    ok = false;
                }
                else if (password.Contains(" "))
                {
                    ShowError(passwordError, "Пароль не должен содержать пробелов");
                    ok = false;
                }
                else
                {
                    // проверим наличие заглавной, строчной, цифры и спецсимвола
                    bool hasUpper = password.Any(char.IsUpper);
                    bool hasLower = password.Any(char.IsLower);
                    bool hasDigit = password.Any(char.IsDigit);
                    bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

                    if (!hasUpper || !hasLower || !hasDigit || !hasSpecial)
                    {
                        ShowError(passwordError, "Пароль должен содержать заглавную и строчную буквы, цифру и спецсимвол");
                        ok = false;
                    }
                }
            }

            // Подтверждение пароля
            if (string.IsNullOrEmpty(confirm))
            {
                ShowError(confirmPasswordError, "Подтвердите пароль");
                ok = false;
            }
            else if (password != confirm)
            {
                ShowError(confirmPasswordError, "Пароли не совпадают");
                ok = false;
            }

            return ok;
        }

        #endregion

        #region Utilities

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

        // Метод для проверки уникальности email в обеих таблицах
        private bool IsEmailUnique(string email)
        {
            // Приводим к нижнему регистру для case-insensitive проверки
            var emailLower = email.ToLower();

            // Проверяем в таблице CUSTOMERS
            bool existsInCustomers = AppConnect.model01.CUSTOMERS
                .Any(c => c.email.ToLower() == emailLower);

            // Проверяем в таблице EMPLOYEES
            bool existsInEmployees = AppConnect.model01.EMPLOYEES
                .Any(emp => emp.email.ToLower() == emailLower);

            // Email уникален, если его нет ни в одной таблице
            return !existsInCustomers && !existsInEmployees;
        }

        // Метод для проверки уникальности телефона в обеих таблицах
        private bool IsPhoneUnique(string phoneDigits)
        {
            // Проверяем в таблице CUSTOMERS
            bool existsInCustomers = AppConnect.model01.CUSTOMERS
                .Any(c => c.phone == phoneDigits);

            // Проверяем в таблице EMPLOYEES
            bool existsInEmployees = AppConnect.model01.EMPLOYEES
                .Any(emp => emp.phone == phoneDigits);

            // Телефон уникален, если его нет ни в одной таблице
            return !existsInCustomers && !existsInEmployees;
        }

        #endregion
    }
}
using kanzeed.ApplicationData;
using System;
using System.Linq;
using System.Text.RegularExpressions;
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
        // Inline error textblocks (добавляются динамически под соответствующие StackPanel)
        private TextBlock lastNameError;
        private TextBlock firstNameError;
        private TextBlock emailError;
        private TextBlock phoneError;
        private TextBlock passwordError;
        private TextBlock confirmPasswordError;

        // Reveal textboxes для отображения пароля (лениво создаются и переиспользуются)
        private TextBox revealPasswordBox;
        private TextBox revealConfirmPasswordBox;

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

            // Маска ввода телефона: разрешаем только цифры при вводе, обрабатываем вставку
            PhoneTextBox.PreviewTextInput += PhoneTextBox_PreviewTextInput;
            DataObject.AddPastingHandler(PhoneTextBox, PhoneTextBox_Paste);
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

        #region Phone mask & input handling

        private static readonly Regex digitsOnlyRegex = new Regex(@"\D", RegexOptions.Compiled);

        // Разрешаем ввод только цифр
        private void PhoneTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // допускаем только цифры
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }
        }

        // Обработка вставки: очищаем всё кроме цифр
        private void PhoneTextBox_Paste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.SourceDataObject.GetDataPresent(DataFormats.Text, true)) return;
            var text = e.SourceDataObject.GetData(DataFormats.Text) as string;
            if (string.IsNullOrEmpty(text)) return;

            var digits = digitsOnlyRegex.Replace(text, "");
            // отменяем оригинальную вставку и ставим очищённый вариант
            e.CancelCommand();
            var tb = sender as TextBox;
            if (tb != null)
            {
                var selectionStart = tb.SelectionStart;
                var newText = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength);
                newText = newText.Insert(selectionStart, digits);
                tb.Text = newText;
                tb.CaretIndex = selectionStart + digits.Length;
                FormatPhoneInTextBox(tb);
            }
        }

        // Format phone on text changed (progressive formatting)
        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            // Save caret
            int caret = tb.CaretIndex;
            FormatPhoneInTextBox(tb);
            // try to restore caret reasonably
            tb.CaretIndex = Math.Min(tb.Text.Length, caret);
            ClearPhoneError();
        }

        private void FormatPhoneInTextBox(TextBox tb)
        {
            // Получаем только цифры
            var digits = digitsOnlyRegex.Replace(tb.Text, "");

            // Если начинается с 8 или 7, используем 8 как префикс, иначе добавим 8
            // Сделаем формат: 8 (XXX) XXX-XX-XX
            if (digits.StartsWith("8") || digits.StartsWith("7"))
            {
                // normalize to start with 8
                if (digits.StartsWith("7")) digits = "8" + digits.Substring(1);
            }
            else if (digits.StartsWith("9") && digits.Length <= 10)
            {
                // номер без первого символа — предположим мобильный 9xxxxxxxxx -> добавим 8
                digits = "8" + digits;
            }

            string formatted = digits;
            if (digits.Length <= 1)
            {
                formatted = digits;
            }
            else
            {
                // ensure it starts with 8
                if (!digits.StartsWith("8")) digits = "8" + digits;
                // apply formatting progressively
                // 8 (AAA) BBB-CC-DD
                var p = digits;
                string a = p.Length > 1 ? p.Substring(1, Math.Min(3, Math.Max(0, p.Length - 1))) : "";
                string b = p.Length > 4 ? p.Substring(4, Math.Min(3, p.Length - 4)) : "";
                string c = p.Length > 7 ? p.Substring(7, Math.Min(2, p.Length - 7)) : "";
                string d = p.Length > 9 ? p.Substring(9, Math.Min(2, p.Length - 9)) : "";

                formatted = "8";
                if (!string.IsNullOrEmpty(a)) formatted += $" ({a}";
                if (a.Length == 3) formatted += $")";
                if (!string.IsNullOrEmpty(b)) formatted += $" {b}";
                if (!string.IsNullOrEmpty(c)) formatted += $"-{c}";
                if (!string.IsNullOrEmpty(d)) formatted += $"-{d}";
            }

            // Avoid infinite loop by only setting text if different
            if (tb.Text != formatted)
            {
                tb.Text = formatted;
            }
        }

        // Helper to extract digits from phone for validation / saving
        private string GetDigitsFromPhone()
        {
            return digitsOnlyRegex.Replace(PhoneTextBox.Text ?? "", "");
        }

        #endregion

        #region Password reveal helpers (не меняют дизайн)

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleRevealForPasswordBox(PasswordBox, ref revealPasswordBox, TogglePasswordButton);
        }

        private void ToggleConfirmPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleRevealForPasswordBox(ConfirmPasswordBox, ref revealConfirmPasswordBox, ToggleConfirmPasswordButton);
        }

        private void ToggleRevealForPasswordBox(PasswordBox pwdBox, ref TextBox revealBox, Button toggleButton)
        {
            try
            {
                var parentGrid = pwdBox.Parent as Grid;
                if (parentGrid == null) return;

                if (revealBox == null)
                {
                    // создаём reveal box в той же Grid, колонка 0
                    revealBox = new TextBox
                    {
                        Visibility = Visibility.Collapsed,
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Padding = pwdBox.Padding,
                        FontSize = pwdBox.FontSize,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Foreground = pwdBox.Foreground,
                        MaxLength = pwdBox.MaxLength
                    };

                    // Создаем локальную (не-ref) переменную для использования в лямбде
                    var capturedReveal = revealBox;

                    // подписываемся на событие, используя локальную переменную
                    capturedReveal.TextChanged += (s, e) =>
                    {
                        // при изменении видимого текста синхронизируем значение в PasswordBox
                        if (pwdBox.Visibility == Visibility.Visible)
                        {
                            pwdBox.Password = capturedReveal.Text;
                        }
                    };

                    Grid.SetColumn(revealBox, 0);
                    parentGrid.Children.Add(revealBox);
                }

                if (revealBox.Visibility == Visibility.Collapsed)
                {
                    // показать
                    revealBox.Text = pwdBox.Password;
                    pwdBox.Visibility = Visibility.Collapsed;
                    revealBox.Visibility = Visibility.Visible;
                    toggleButton.Content = "🙈";
                }
                else
                {
                    // скрыть
                    pwdBox.Password = revealBox.Text;
                    revealBox.Visibility = Visibility.Collapsed;
                    pwdBox.Visibility = Visibility.Visible;
                    toggleButton.Content = "👁";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка переключения видимости пароля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoginLink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            NavigationService?.Navigate(new Authorization());
        }



        #endregion

        #region Registration / validation

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                // Дополнительная обработка: нормализация email, очистка телефона до цифр
                var email = EmailTextBox.Text.Trim();
                var phoneDigits = GetDigitsFromPhone();

                // Создание нового клиента
                var newCustomer = new CUSTOMERS
                {
                    last_name = LastNameTextBox.Text.Trim(),
                    first_name = FirstNameTextBox.Text.Trim(),
                    middle_name = MiddleNameTextBox.Text.Trim(),
                    email = email,
                    phone = PhoneTextBox.Text.Trim(),
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
            else if (email.Length > 50)
            {
                ShowError(emailError, "Email слишком длинный (макс. 50 символов)");
                ok = false;
            }
            else if (!IsValidEmail(email))
            {
                ShowError(emailError, "Введите корректный email");
                ok = false;
            }
            else
            {
                // Проверка уникальности email
                var existsInCustomers = AppConnect.model01.CUSTOMERS.Any(c => c.email.ToLower() == email.ToLower());
                var existsInEmployees = AppConnect.model01.EMPLOYEES.Any(e => e.email.ToLower() == email.ToLower());
                if (existsInCustomers || existsInEmployees)
                {
                    ShowError(emailError, "Пользователь с таким email уже существует");
                    ok = false;
                }
            }

            // Телефон — обязательный? здесь будем требовать минимум 10 цифр (моб.номер)
            var digits = GetDigitsFromPhone();
            if (string.IsNullOrEmpty(digits))
            {
                ShowError(phoneError, "Введите телефон");
                ok = false;
            }
            else if (digits.Length < 10)
            {
                ShowError(phoneError, "Телефон должен содержать минимум 10 цифр");
                ok = false;
            }

            // Пароль: требования
            string password = PasswordBox.Visibility == Visibility.Visible ? PasswordBox.Password : (revealPasswordBox?.Text ?? "");
            string confirm = ConfirmPasswordBox.Visibility == Visibility.Visible ? ConfirmPasswordBox.Password : (revealConfirmPasswordBox?.Text ?? "");

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
                else if (password.Length > 50)
                {
                    ShowError(passwordError, "Пароль слишком длинный (макс. 50 символов)");
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

        #endregion
    }
}

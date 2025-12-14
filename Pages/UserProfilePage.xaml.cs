using kanzeed.ApplicationData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace kanzeed.Pages
{
    public partial class UserProfilePage : Page
    {
        private bool _phoneUpdating;

        public UserProfilePage()
        {
            InitializeComponent();
            ReloadProfile();
        }

        private void ReloadProfile()
        {
            var user = AppData.CurrentUser;
            var cust = AppConnect.model01.CUSTOMERS.First(c => c.customer_id == user.Id);

            LastNameBox.Text = cust.last_name;
            FirstNameBox.Text = cust.first_name;
            MiddleNameBox.Text = cust.middle_name;
            EmailBox.Text = cust.email;
            PhoneBox.Text = FormatPhone(cust.phone ?? "");

            OrdersList.ItemsSource = AppConnect.model01.ORDERS
                .Where(o => o.customer_id == cust.customer_id)
                .OrderByDescending(o => o.order_date)
                .ToList();

            AddressesList.ItemsSource = AppConnect.model01.CUSTOMER_ADDRESSES
                .Where(a => a.customer_id == cust.customer_id)
                .ToList();
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            var user = AppData.CurrentUser;
            if (user == null)
            {
                MessageBox.Show("Пользователь не авторизован");
                return;
            }

            try
            {
                // ===== БАЗОВАЯ ВАЛИДАЦИЯ =====
                if (string.IsNullOrWhiteSpace(FirstNameBox.Text) ||
                    string.IsNullOrWhiteSpace(LastNameBox.Text))
                {
                    MessageBox.Show("Имя и фамилия обязательны");
                    return;
                }

                if (string.IsNullOrWhiteSpace(EmailBox.Text) || !EmailBox.Text.Contains("@"))
                {
                    MessageBox.Show("Введите корректный email");
                    return;
                }

                // ===== СМЕНА ПАРОЛЯ (если пользователь что-то ввёл) =====
                bool changePassword = !string.IsNullOrWhiteSpace(NewPasswordBox.Password);

                if (changePassword)
                {
                    if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
                    {
                        MessageBox.Show("Новый пароль и подтверждение не совпадают");
                        return;
                    }

                    if (NewPasswordBox.Password.Length < 6)
                    {
                        MessageBox.Show("Пароль должен быть не короче 6 символов");
                        return;
                    }
                }

                // ===== СОХРАНЕНИЕ ДАННЫХ =====
                if (user.IsEmployee)
                {
                    var emp = AppConnect.model01.EMPLOYEES
                        .FirstOrDefault(em => em.employee_id == user.Id);

                    if (emp == null)
                        return;

                    // проверка текущего пароля
                    if (changePassword && emp.password != CurrentPasswordBox.Password)
                    {
                        MessageBox.Show("Неверный текущий пароль");
                        return;
                    }

                    emp.last_name = LastNameBox.Text.Trim();
                    emp.first_name = FirstNameBox.Text.Trim();
                    emp.middle_name = MiddleNameBox.Text.Trim();
                    emp.phone = ExtractDigits(PhoneBox.Text);
                    emp.email = EmailBox.Text.Trim();

                    if (changePassword)
                        emp.password = NewPasswordBox.Password;
                }
                else
                {
                    var cust = AppConnect.model01.CUSTOMERS
                        .FirstOrDefault(c => c.customer_id == user.Id);

                    if (cust == null)
                        return;

                    // проверка текущего пароля
                    if (changePassword && cust.password != CurrentPasswordBox.Password)
                    {
                        MessageBox.Show("Неверный текущий пароль");
                        return;
                    }

                    cust.last_name = LastNameBox.Text.Trim();
                    cust.first_name = FirstNameBox.Text.Trim();
                    cust.middle_name = MiddleNameBox.Text.Trim();
                    cust.phone = ExtractDigits(PhoneBox.Text);
                    cust.email = EmailBox.Text.Trim();

                    if (changePassword)
                        cust.password = NewPasswordBox.Password;
                }

                AppConnect.model01.SaveChanges();

                // ===== ОЧИСТКА ПОЛЕЙ ПАРОЛЯ =====
                CurrentPasswordBox.Password = "";
                NewPasswordBox.Password = "";
                ConfirmPasswordBox.Password = "";

                ReloadProfile();

                MessageBox.Show("Изменения сохранены", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void BackToCatalog_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new DataOutput());
        }

        // PHONE MASK
        private void PhoneBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void PhoneBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_phoneUpdating) return;
            _phoneUpdating = true;

            PhoneBox.Text = FormatPhone(ExtractDigits(PhoneBox.Text));
            PhoneBox.CaretIndex = PhoneBox.Text.Length;

            _phoneUpdating = false;
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


        // ADDRESSES
        private void AddAddress_Click(object sender, RoutedEventArgs e)
        {
            var user = AppData.CurrentUser;
            if (user == null || user.IsEmployee)
            {
                MessageBox.Show("Адреса доступны только клиентам");
                return;
            }

            try
            {
                var address = new CUSTOMER_ADDRESSES
                {
                    customer_id = user.Id,
                    city = CityBox.Text.Trim(),
                    postal_code = PostalCodeBox.Text.Trim(),
                    street = StreetBox.Text.Trim(),
                    house = int.Parse(HouseBox.Text),
                    floor = string.IsNullOrWhiteSpace(FloorBox.Text) ? (int?)null : int.Parse(FloorBox.Text),
                    apartment = int.Parse(ApartmentBox.Text),
                    porch = int.Parse(PorchBox.Text)
                };

                AppConnect.model01.CUSTOMER_ADDRESSES.Add(address);
                AppConnect.model01.SaveChanges();

                LoadAddresses();

                MessageBox.Show("Адрес добавлен", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления адреса: " + ex.Message);
            }
        }
        private void LoadAddresses()
        {
            AddressesList.ItemsSource = AppConnect.model01.CUSTOMER_ADDRESSES
                .Where(a => a.customer_id == AppData.CurrentUser.Id)
                .ToList();
        }


        private void DeleteAddress_Click(object sender, RoutedEventArgs e)
        {
            var id = (int)((Button)sender).Tag;
            var addr = AppConnect.model01.CUSTOMER_ADDRESSES.First(a => a.address_id == id);
            AppConnect.model01.CUSTOMER_ADDRESSES.Remove(addr);
            AppConnect.model01.SaveChanges();
            ReloadProfile();
        }
    }
}

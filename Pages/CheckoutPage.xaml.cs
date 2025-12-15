using kanzeed.ApplicationData;
using kanzeed.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace kanzeed.Pages
{
    public partial class CheckoutPage : Page
    {
        public CheckoutPage()
        {
            InitializeComponent();
            LoadCheckoutData();
        }

        private void LoadCheckoutData()
        {
            try
            {
                // Загружаем товары из корзины
                var cartItems = CartService.GetCartItems();
                CheckoutItemsList.ItemsSource = cartItems;

                // Рассчитываем суммы
                decimal itemsTotal = cartItems.Sum(i => i.LineTotal);
                decimal deliveryCost = itemsTotal >= 2000 ? 0 : 300; // Пример: доставка 300₽ если сумма < 2000
                decimal totalAmount = itemsTotal + deliveryCost;

                // Обновляем тексты
                ItemsSubtotalText.Text = $"{itemsTotal:N2} ₽";
                DeliveryCostText.Text = deliveryCost == 0 ? "Бесплатно" : $"{deliveryCost:N2} ₽";
                TotalAmountText.Text = $"{totalAmount:N2} ₽";

                // Загружаем адреса пользователя из таблицы CUSTOMER_ADDRESSES
                LoadUserAddresses();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserAddresses()
        {
            try
            {
                if (AppData.CurrentUser == null) return;

                // Загружаем адреса пользователя из таблицы CUSTOMER_ADDRESSES
                var addresses = AppConnect.model01.CUSTOMER_ADDRESSES
                    .Where(a => a.customer_id == AppData.CurrentUser.Id)
                    .OrderByDescending(a => a.address_id) // Сначала новые
                    .ToList();

                if (addresses.Any())
                {
                    // Скрываем сообщение об отсутствии адресов
                    NoAddressText.Visibility = Visibility.Collapsed;
                    AddressComboBox.IsEnabled = true;

                    // Очищаем и добавляем адреса в ComboBox
                    AddressComboBox.Items.Clear();

                    foreach (var address in addresses)
                    {
                        // Получаем название города из связанной таблицы CITIES
                        string cityName = address.CITIES?.city_name ?? "Неизвестный город";

                        // Форматируем адрес для отображения
                        string addressText = $"{cityName}, {address.street}, д. {address.house}";

                        // Добавляем дополнительные части адреса, если они есть
                        if (address.apartment.HasValue)
                            addressText += $", кв. {address.apartment.Value}";

                        if (address.porch.HasValue)
                            addressText += $", подъезд {address.porch.Value}";

                        if (address.floor.HasValue)
                            addressText += $", этаж {address.floor.Value}";

                        if (!string.IsNullOrEmpty(address.postal_code))
                            addressText += $", {address.postal_code}";

                        // Добавляем в ComboBox
                        AddressComboBox.Items.Add(addressText);
                    }

                    // Выбираем первый адрес по умолчанию
                    if (AddressComboBox.Items.Count > 0)
                    {
                        AddressComboBox.SelectedIndex = 0;
                    }
                }
                else
                {
                    // Если нет адресов, показываем сообщение
                    AddressComboBox.Items.Clear();
                    AddressComboBox.Items.Add("Нет доступных адресов");
                    AddressComboBox.SelectedIndex = 0;
                    AddressComboBox.IsEnabled = false;

                    NoAddressText.Visibility = Visibility.Visible;

                    // Делаем кнопку подтверждения неактивной
                    var confirmButton = this.FindName("ConfirmOrderButton") as Button;
                    if (confirmButton != null)
                        confirmButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки адресов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                // В случае ошибки тоже показываем сообщение
                AddressComboBox.Items.Clear();
                AddressComboBox.Items.Add("Ошибка загрузки адресов");
                AddressComboBox.SelectedIndex = 0;
                AddressComboBox.IsEnabled = false;
                NoAddressText.Visibility = Visibility.Visible;
            }
        }

        private void BackToCart_Click(object sender, RoutedEventArgs e)
        {
            // Возвращаемся на страницу корзины
            NavigationService.Navigate(new CartPage());
        }

        private void ConfirmOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем, есть ли у пользователя адреса
                if (AddressComboBox.SelectedItem == null ||
                    AddressComboBox.SelectedItem.ToString() == "Нет доступных адресов" ||
                    AddressComboBox.SelectedItem.ToString() == "Ошибка загрузки адресов")
                {
                    MessageBox.Show("Для оформления заказа необходимо добавить хотя бы один адрес доставки в личном кабинете.",
                        "Отсутствует адрес доставки", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Определяем способ оплаты
                int paymentMethodId = CashRadio.IsChecked == true ? 1 : 2; // 1 - наличные, 2 - карта

                // Получаем комментарий
                string comment = CommentTextBox.Text?.Trim();

                // Создаём заказ
                int orderId = CartService.Checkout(paymentMethodId);

                // Показываем сообщение об успехе
                string successMessage = $"Заказ №{orderId} успешно оформлен!\n\n" +
                                       $"Адрес доставки: {AddressComboBox.SelectedItem}\n" +
                                       $"Способ оплаты: {(paymentMethodId == 1 ? "Наличные" : "Карта")}\n" +
                                       $"Сумма заказа: {TotalAmountText.Text}";

                if (!string.IsNullOrEmpty(comment))
                {
                    successMessage += $"\nКомментарий: {comment}";
                }

                successMessage += "\n\nСпасибо за покупку!";

                MessageBox.Show(successMessage,
                    "Заказ оформлен", MessageBoxButton.OK, MessageBoxImage.Information);

                // Возвращаемся на главную страницу
                NavigationService.Navigate(new DataOutput());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка оформления заказа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
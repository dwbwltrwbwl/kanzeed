using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using kanzeed.Services;
using kanzeed.ApplicationData;

namespace kanzeed.Pages
{
    public partial class CartPage : Page
    {
        public CartPage()
        {
            InitializeComponent();
            LoadCart();
        }

        private void LoadCart()
        {
            var items = CartService.GetCartItems();
            CartListView.ItemsSource = items;
            TotalAmountText.Text = $"Итого: {CartService.GetTotalAmount():C}";
        }

        private void IncreaseQty_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || btn.Tag == null) return;
            int pid = (int)btn.Tag;
            try
            {
                // Проверка запасов: получить текущее количество и увеличить на 1, если есть запас
                var product = AppConnect.model01.PRODUCTS.FirstOrDefault(p => p.product_id == pid);
                if (product == null) { MessageBox.Show("Товар не найден"); return; }

                int currentQty = AppData.CurrentCart.ContainsKey(pid) ? AppData.CurrentCart[pid] : 0;
                if (product.stock_quantity <= currentQty)
                {
                    MessageBox.Show("Недостаточно на складе", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                CartService.SetQuantity(pid, currentQty + 1);
                LoadCart();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DecreaseQty_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || btn.Tag == null) return;
            int pid = (int)btn.Tag;
            int currentQty = AppData.CurrentCart.ContainsKey(pid) ? AppData.CurrentCart[pid] : 0;
            if (currentQty <= 1)
                CartService.RemoveFromCart(pid);
            else
                CartService.SetQuantity(pid, currentQty - 1);

            LoadCart();
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || btn.Tag == null) return;
            int pid = (int)btn.Tag;
            CartService.RemoveFromCart(pid);
            LoadCart();
        }

        private void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AppData.CurrentUser == null || AppData.CurrentUser.RoleId != 1)
                {
                    MessageBox.Show("Только авторизованный клиент может оформить заказ.", "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!AppData.CurrentCart.Any())
                {
                    MessageBox.Show("Корзина пуста.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Выполняем checkout
                int orderId = CartService.Checkout(1); // paymentMethodId = 1 по умолчанию
                MessageBox.Show($"Заказ #{orderId} успешно создан.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Обновляем счётчик корзины на главной странице (если нужно)
                // Если навигация назад на DataOutput, UpdateCartCount выполнится в его конструкторе/OnNavigatedTo по реализации.
                NavigationService.Navigate(new DataOutput());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка оформления заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}

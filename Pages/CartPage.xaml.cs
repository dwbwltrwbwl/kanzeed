using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using kanzeed.Services;
using kanzeed.ApplicationData;
using System.Diagnostics;

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

            if (!items.Any())
            {
                // Показываем сообщение о пустой корзине
                CartListView.Visibility = Visibility.Collapsed;
                EmptyCartMessage.Visibility = Visibility.Visible;
                TotalAmountText.Text = "Итого: 0 ₽";
                ItemsCountText.Text = "0 товаров";
                return;
            }

            // Показываем список товаров
            CartListView.Visibility = Visibility.Visible;
            EmptyCartMessage.Visibility = Visibility.Collapsed;

            CartListView.ItemsSource = items;

            // Обновляем информацию
            int totalItems = items.Sum(i => i.Quantity);
            decimal totalAmount = items.Sum(i => i.LineTotal);

            TotalAmountText.Text = $"Итого: {totalAmount:N2} ₽";
            ItemsCountText.Text = $"{totalItems} {GetItemWord(totalItems)}";

            // Информация о доставке
            if (totalAmount >= 2000)
            {
                DeliveryInfoText.Text = "✓ Бесплатная доставка";
                DeliveryInfoText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                decimal remaining = 2000 - totalAmount;
                DeliveryInfoText.Text = $"До бесплатной доставки: {remaining:N2} ₽";
                DeliveryInfoText.Foreground = System.Windows.Media.Brushes.Orange;
            }
        }

        private string GetItemWord(int count)
        {
            if (count % 10 == 1 && count % 100 != 11) return "товар";
            if (count % 10 >= 2 && count % 10 <= 4 &&
                (count % 100 < 10 || count % 100 >= 20)) return "товара";
            return "товаров";
        }

        private void IncreaseQty_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || btn.Tag == null) return;
            int pid = (int)btn.Tag;
            try
            {
                var product = AppConnect.model01.PRODUCTS.FirstOrDefault(p => p.product_id == pid);
                if (product == null)
                {
                    MessageBox.Show("Товар не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int currentQty = AppData.CurrentCart.ContainsKey(pid) ? AppData.CurrentCart[pid] : 0;
                if (product.stock_quantity <= currentQty)
                {
                    MessageBox.Show($"Недостаточно на складе. Доступно: {product.stock_quantity} шт.",
                                   "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                CartService.SetQuantity(pid, currentQty + 1);
                LoadCart();

                // Показываем уведомление
                ShowNotification($"Количество товара \"{product.name}\" увеличено до {currentQty + 1} шт.");
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
            {
                // Спрашиваем подтверждение удаления
                var product = AppConnect.model01.PRODUCTS.FirstOrDefault(p => p.product_id == pid);
                if (product != null)
                {
                    var result = MessageBox.Show($"Удалить товар \"{product.name}\" из корзины?",
                                               "Подтверждение",
                                               MessageBoxButton.YesNo,
                                               MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        CartService.RemoveFromCart(pid);
                        ShowNotification($"Товар \"{product.name}\" удалён из корзины");
                    }
                }
                else
                {
                    CartService.RemoveFromCart(pid);
                }
            }
            else
            {
                CartService.SetQuantity(pid, currentQty - 1);

                var product = AppConnect.model01.PRODUCTS.FirstOrDefault(p => p.product_id == pid);
                if (product != null)
                {
                    ShowNotification($"Количество товара \"{product.name}\" уменьшено до {currentQty - 1} шт.");
                }
            }

            LoadCart();
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || btn.Tag == null) return;
            int pid = (int)btn.Tag;

            // Спрашиваем подтверждение
            var product = AppConnect.model01.PRODUCTS.FirstOrDefault(p => p.product_id == pid);
            if (product != null)
            {
                var result = MessageBox.Show($"Удалить товар \"{product.name}\" из корзины?",
                                           "Подтверждение",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    CartService.RemoveFromCart(pid);
                    ShowNotification($"Товар \"{product.name}\" удалён из корзины");
                    LoadCart();
                }
            }
            else
            {
                CartService.RemoveFromCart(pid);
                LoadCart();
            }
        }

        private void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AppData.CurrentUser == null || AppData.CurrentUser.RoleId != 1)
                {
                    MessageBox.Show("Только авторизованный клиент может оформить заказ.",
                                   "Доступ запрещён",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Warning);
                    return;
                }

                if (!AppData.CurrentCart.Any())
                {
                    MessageBox.Show("Корзина пуста.",
                                   "Информация",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);
                    return;
                }

                // Переход на страницу оформления заказа
                NavigationService.Navigate(new CheckoutPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new DataOutput());
        }

        private void ShowNotification(string message)
        {
            // Можно реализовать Toast-уведомление
            // Пока просто показываем в статусной строке или ToolTip
            // Или используем Snackbar если есть
            Debug.WriteLine($"Cart Notification: {message}");
        }
    }
}
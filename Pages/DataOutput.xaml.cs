using kanzeed.ApplicationData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using kanzeed.Services;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace kanzeed.Pages
{
    /// <summary>
    /// Логика взаимодействия для DataOutput.xaml
    /// </summary>
    public partial class DataOutput : Page
    {
        private List<PRODUCTS> allProducts;
        private PRODUCTS selectedProduct;
        private List<string> categoriesList = new List<string> { "Все категории" };

        public DataOutput()
        {
            InitializeComponent();

            try
            {
                // Привязка контекста (для конвертеров/visibility)
                this.DataContext = AppData.CurrentUser;

                // Инициализация UI контролов (зададим дефолтные индексы, но ComboBox'ы будут заполнены в ReloadProducts)
                ComboFilter.SelectedIndex = 0;
                ComboSort.SelectedIndex = 0;

                // Первичная загрузка данных
                ReloadProducts();

                // Обновление статуса корзины
                UpdateCartCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Подпишемся на событие навигации, чтобы при возврате на эту страницу перезагружать данные
            // NavigationService может быть null в конструкторе в некоторых сценариях, поэтому проверяем
            if (this.NavigationService != null)
            {
                this.NavigationService.Navigated += NavigationService_Navigated;
            }

            // Отписаться при выгрузке страницы
            this.Unloaded += DataOutput_Unloaded;
        }

        private void NavigationService_Navigated(object sender, NavigationEventArgs e)
        {
            // Выполняем действия только если навигация вернула нас на эту страницу
            if (e.Content == this)
            {
                try
                {
                    // Обновляем контекст пользователя (на случай входа/выхода) и перезагружаем товары
                    this.DataContext = AppData.CurrentUser;
                    ReloadProducts();
                    UpdateCartCount();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"NavigationService_Navigated error: {ex}");
                }
            }
        }

        private void DataOutput_Unloaded(object sender, RoutedEventArgs e)
        {
            // Отписываемся от событий, чтобы избежать утечек памяти и лишних вызовов
            if (this.NavigationService != null)
                this.NavigationService.Navigated -= NavigationService_Navigated;

            this.Unloaded -= DataOutput_Unloaded;
        }

        /// <summary>
        /// Полная перезагрузка товаров, категорий и обновление UI.
        /// </summary>
        private void ReloadProducts()
        {
            try
            {
                // Загружаем товары напрямую из БД, чтобы всегда иметь актуальные данные
                allProducts = AppConnect.model01.PRODUCTS
                    .OrderBy(p => p.name)
                    .ToList();

                // Привязка к ListView
                listProducts.ItemsSource = allProducts;

                // Перезагрузка списка категорий (для фильтра)
                categoriesList = new List<string> { "Все категории" };
                var categories = AppConnect.model01.CATEGORIES.OrderBy(c => c.name).ToList();
                foreach (var category in categories)
                {
                    categoriesList.Add(category.name);
                }
                ComboFilter.ItemsSource = categoriesList;

                // Установим элементы ComboBox'ов в дефолтные значения, если они не установлены
                if (ComboFilter.SelectedIndex < 0) ComboFilter.SelectedIndex = 0;
                if (ComboSort.SelectedIndex < 0) ComboSort.SelectedIndex = 0;

                // Обновления счётчиков и статистики
                UpdateFoundCount(allProducts.Count);
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при перезагрузке товаров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCartCount()
        {
            if (AppData.CurrentUser == null)
            {
                CartCountText.Text = "Корзина (0)";
                return;
            }

            try
            {
                int count = CartService.GetTotalQuantity();
                CartCountText.Text = $"Корзина ({count})";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateCartCount error: {ex}");
                CartCountText.Text = "Корзина (0)";
            }
        }

        private void UpdateStatistics()
        {
            if (allProducts == null) return;

            int total = allProducts.Count;
            int expensive = allProducts.Count(p => p.price > 1000);
            int lowStock = allProducts.Count(p => p.stock_quantity < 10);

            StatisticsText.Text = $"Всего: {total} | Дорогих: {expensive} | Мало на складе: {lowStock}";
        }

        private void listProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedProduct = listProducts.SelectedItem as PRODUCTS;
            if (selectedProduct != null)
            {
                Debug.WriteLine($"Выбран товар: {selectedProduct.name}");
            }
        }

        private void ComboFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateProductList();
        }

        private void ComboSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateProductList();
        }

        private void ApplySearch_Click(object sender, RoutedEventArgs e)
        {
            UpdateProductList();
        }

        private void TextSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Обновление при вводе текста (можно настроить с задержкой)
            UpdateProductList();
        }

        private void ResetSearch_Click(object sender, RoutedEventArgs e)
        {
            TextSearch.Text = string.Empty;
            ComboFilter.SelectedIndex = 0;
            ComboSort.SelectedIndex = 0;
            ShowOnlyExpensive.IsChecked = false;
            ShowOnlyDiscount.IsChecked = false;
            ShowLowStockWarning.IsChecked = false;
            UpdateProductList();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (listProducts.SelectedItem is PRODUCTS selectedProduct)
            {
                try
                {
                    // Рекомендуется получить объект из контекста, чтобы EF отслеживал изменения
                    var trackedProduct = AppConnect.model01.PRODUCTS
                        .FirstOrDefault(p => p.product_id == selectedProduct.product_id);

                    // Если по какой-то причине не найден — всё равно передадим выбранный объект
                    var productToEdit = trackedProduct ?? selectedProduct;

                    NavigationService.Navigate(new ProductEditPage(productToEdit));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось открыть страницу редактирования: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите товар для редактирования",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ProductEditPage());
        }

        private void UpdateProductList()
        {
            try
            {
                string searchText = TextSearch.Text?.ToLower() ?? string.Empty;
                string selectedCategory = ComboFilter.SelectedItem?.ToString() ?? "Все категории";

                // Получаем реальный текст выбранного пункта сортировки
                string selectedSort = null;
                if (ComboSort.SelectedItem is ComboBoxItem cbi)
                    selectedSort = cbi.Content?.ToString();
                else
                    selectedSort = ComboSort.SelectedItem?.ToString();

                if (string.IsNullOrEmpty(selectedSort))
                    selectedSort = "По названию (А-Я)";

                if (allProducts == null)
                {
                    UpdateFoundCount(0);
                    return;
                }

                // Фильтрация
                var filteredProducts = allProducts.Where(product =>
                    product != null &&
                    product.name != null &&
                    (product.name.ToLower().Contains(searchText) ||
                     (product.description != null && product.description.ToLower().Contains(searchText)) ||
                     (product.sku != null && product.sku.ToLower().Contains(searchText))) &&
                    (selectedCategory == "Все категории" ||
                     (product.CATEGORIES != null && product.CATEGORIES.name == selectedCategory)) &&
                    (!ShowOnlyExpensive.IsChecked.HasValue || !ShowOnlyExpensive.IsChecked.Value || product.price > 1000) &&
                    (!ShowLowStockWarning.IsChecked.HasValue || !ShowLowStockWarning.IsChecked.Value || product.stock_quantity < 10))
                    .ToList();

                // Сортировка — аккуратно с null-ами
                List<PRODUCTS> sortedProducts;
                switch (selectedSort)
                {
                    case "По названию (А-Я)":
                        sortedProducts = filteredProducts.OrderBy(product => product.name ?? string.Empty).ToList();
                        break;
                    case "По названию (Я-А)":
                        sortedProducts = filteredProducts.OrderByDescending(product => product.name ?? string.Empty).ToList();
                        break;
                    case "По цене (возрастание)":
                        sortedProducts = filteredProducts.OrderBy(product => product.price).ToList();
                        break;
                    case "По цене (убывание)":
                        sortedProducts = filteredProducts.OrderByDescending(product => product.price).ToList();
                        break;
                    // учитываем обе возможные строки (с опечаткой и без)
                    case "По количеству на складу":
                    case "По количеству на складе":
                        sortedProducts = filteredProducts.OrderBy(product => product.stock_quantity).ToList();
                        break;
                    default:
                        sortedProducts = filteredProducts;
                        break;
                }

                listProducts.ItemsSource = sortedProducts;
                UpdateFoundCount(sortedProducts.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления списка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateFoundCount(int count)
        {
            TextFoundCount.Text = $"Найдено: {count}";
        }

        private void AddToCartButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProduct == null)
            {
                MessageBox.Show("Выберите товар!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AppData.CurrentUser == null || AppData.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("Только авторизованный клиент может добавлять товары в корзину. Войдите в систему.", "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                CartService.AddToCart(selectedProduct.product_id, 1);
                MessageBox.Show($"Товар '{selectedProduct.name}' добавлен в корзину", "Корзина", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateCartCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления в корзину: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewCartButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CartPage());
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var product = listProducts.SelectedItem as PRODUCTS;
            if (product == null)
            {
                MessageBox.Show("Выберите товар для удаления",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Только администратор
            if (AppData.CurrentUser == null || AppData.CurrentUser.RoleId != 4)
            {
                MessageBox.Show("Удаление товаров доступно только администратору",
                    "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Вы действительно хотите удалить товар «{product.name}»?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                var trackedProduct = AppConnect.model01.PRODUCTS
                    .FirstOrDefault(p => p.product_id == product.product_id);

                if (trackedProduct == null)
                {
                    MessageBox.Show("Товар не найден в базе данных",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверка: используется ли товар в заказах
                bool usedInOrders = AppConnect.model01.ORDER_ITEMS
                    .Any(oi => oi.product_id == trackedProduct.product_id);

                if (usedInOrders)
                {
                    MessageBox.Show(
                        "Нельзя удалить товар, так как он используется в заказах",
                        "Удаление запрещено",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                AppConnect.model01.PRODUCTS.Remove(trackedProduct);
                AppConnect.model01.SaveChanges();

                ReloadProducts();

                MessageBox.Show("Товар успешно удалён",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления товара: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void FilterCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateProductList();
        }

        private void PrevPageButton_Click(object sender, RoutedEventArgs e)
        {
            // Реализация пагинации (упрощенная версия)
            MessageBox.Show("Переход на предыдущую страницу", "Пагинация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            // Реализация пагинации (упрощенная версия)
            MessageBox.Show("Переход на следующую страницу", "Пагинация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UserProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppData.CurrentUser == null)
            {
                // Если гость — показать окно авторизации
                NavigationService.Navigate(new Authorization());
            }
            else
            {
                NavigationService.Navigate(new UserProfilePage());
            }
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Очистка пользователя и корзины
            AppData.CurrentUser = null;
            AppData.CurrentCart?.Clear();

            // Переход на страницу авторизации
            NavigationService.Navigate(new Authorization());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Дополнительная инициализация при загрузке (если нужно подписаться здесь на NavigationService, можно сделать)
            if (this.NavigationService != null)
            {
                // гарантируем, что подписка есть (на случай, если в конструкторе NavigationService был null)
                this.NavigationService.Navigated -= NavigationService_Navigated;
                this.NavigationService.Navigated += NavigationService_Navigated;
            }
        }

        private void ViewUsersButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ManageUsersPage());
        }

        private void ViewTablesButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new TablesPage());
        }
    }
}

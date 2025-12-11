using kanzeed.ApplicationData;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using kanzeed.Services;

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
                // Инициализация фильтров и сортировки
                ComboFilter.SelectedIndex = 0;
                ComboSort.SelectedIndex = 0;

                // Загрузка всех товаров
                allProducts = AppConnect.model01.PRODUCTS.ToList();
                listProducts.ItemsSource = allProducts;

                // Загрузка категорий для фильтра
                var categories = AppConnect.model01.CATEGORIES.ToList();
                foreach (var category in categories)
                {
                    categoriesList.Add(category.name);
                }

                // Заполнение ComboBox категорий
                ComboFilter.ItemsSource = categoriesList;

                UpdateFoundCount(allProducts.Count);
                UpdateStatistics();

                // Обновление статуса пользователя
                UpdateCartCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            this.DataContext = AppData.CurrentUser;
        }

        private void UpdateCartCount()
        {
            if (AppData.CurrentUser == null)
            {
                CartCountText.Text = "Корзина (0)";
                return;
            }

            int count = CartService.GetTotalQuantity();
            CartCountText.Text = $"Корзина ({count})";
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
                // Проверка прав доступа через AppConnect
                //if (AppConnect.IsAdmin() || AppConnect.IsManager())
                //{
                //    // Открытие окна редактирования
                //    MessageBox.Show($"Редактирование товара: {selectedProduct.name}",
                //        "Редактирование", MessageBoxButton.OK, MessageBoxImage.Information);
                //    // NavigationService.Navigate(new EditProductPage(selectedProduct));
                //}
                //else
                //{
                //    MessageBox.Show("У вас недостаточно прав для редактирования товаров",
                //        "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                //}
            }
            else
            {
                MessageBox.Show("Выберите товар для редактирования",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка прав доступа через AppConnect
            //if (AppConnect.IsManager() || AppConnect.IsAdmin())
            //{
            //    // Создание нового товара
            //    MessageBox.Show("Добавление нового товара",
            //        "Добавление", MessageBoxButton.OK, MessageBoxImage.Information);
            //    // NavigationService.Navigate(new EditProductPage(new PRODUCTS()));
            //}
            //else
            //{
            //    MessageBox.Show("У вас недостаточно прав для добавления товаров",
            //        "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
            //}
        }

        private void UpdateProductList()
        {
            try
            {
                string searchText = TextSearch.Text.ToLower();
                string selectedCategory = ComboFilter.SelectedItem?.ToString() ?? "Все категории";
                string selectedSort = ComboSort.SelectedItem?.ToString() ?? "По названию (А-Я)";

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

                // Сортировка
                List<PRODUCTS> sortedProducts;
                switch (selectedSort)
                {
                    case "По названию (А-Я)":
                        sortedProducts = filteredProducts.OrderBy(product => product.name).ToList();
                        break;
                    case "По названию (Я-А)":
                        sortedProducts = filteredProducts.OrderByDescending(product => product.name).ToList();
                        break;
                    case "По цене (возрастание)":
                        sortedProducts = filteredProducts.OrderBy(product => product.price).ToList();
                        break;
                    case "По цене (убывание)":
                        sortedProducts = filteredProducts.OrderByDescending(product => product.price).ToList();
                        break;
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
            if (listProducts.SelectedItem is PRODUCTS selectedProduct)
            {
                // Проверка прав доступа через AppConnect
            //    if (AppConnect.IsAdmin())
            //    {
            //        var result = MessageBox.Show($"Вы уверены, что хотите удалить товар '{selectedProduct.name}'?",
            //            "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            //        if (result == MessageBoxResult.Yes)
            //        {
            //            try
            //            {
            //                // Проверка на наличие товара в заказах
            //                if (selectedProduct.ORDER_ITEMS.Any())
            //                {
            //                    MessageBox.Show("Невозможно удалить товар, так как он присутствует в одном или нескольких заказах.",
            //                        "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
            //                    return;
            //                }

            //                // Удаление товара
            //                AppConnect.model01.PRODUCTS.Remove(selectedProduct);
            //                AppConnect.model01.SaveChanges();

            //                // Обновление списка
            //                allProducts = AppConnect.model01.PRODUCTS.ToList();
            //                UpdateProductList();
            //                UpdateStatistics();

            //                MessageBox.Show("Товар успешно удален", "Успех",
            //                    MessageBoxButton.OK, MessageBoxImage.Information);
            //            }
            //            catch (Exception ex)
            //            {
            //                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
            //                    MessageBoxButton.OK, MessageBoxImage.Error);
            //            }
            //        }
            //    }
            //    else
            //    {
            //        MessageBox.Show("Только администратор может удалять товары",
            //            "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    }
            //}
            //else
            //{
            //    MessageBox.Show("Выберите товар для удаления",
            //        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Дополнительная инициализация при загрузке
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
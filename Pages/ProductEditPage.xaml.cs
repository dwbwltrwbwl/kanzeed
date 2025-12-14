using kanzeed.ApplicationData;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace kanzeed.Pages
{
    /// <summary>
    /// Логика для ProductEditPage.xaml
    /// </summary>
    public partial class ProductEditPage : Page
    {
        private PRODUCTS editingProduct;
        private bool isNew = false;

        public ProductEditPage(PRODUCTS product = null)
        {
            InitializeComponent();

            // Загружаем категории / поставщиков
            LoadCategories();
            LoadSuppliers();

            if (product == null)
            {
                isNew = true;
                editingProduct = new PRODUCTS();
                TitleText.Text = "Добавление товара";
                DeleteButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                isNew = false;
                editingProduct = product;
                TitleText.Text = "Редактирование товара";
                PopulateFields();
                // Показать кнопку удаления только для админа
                if (AppData.CurrentUser != null && AppData.CurrentUser.RoleId == 4)
                    DeleteButton.Visibility = Visibility.Visible;
            }
        }

        private void LoadCategories()
        {
            try
            {
                var cats = AppConnect.model01.CATEGORIES.OrderBy(c => c.name).ToList();
                CategoryComboBox.ItemsSource = cats;
                CategoryComboBox.DisplayMemberPath = "name";
                CategoryComboBox.SelectedValuePath = "category_id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSuppliers()
        {
            try
            {
                var sup = AppConnect.model01.SUPPLIERS.OrderBy(s => s.name).ToList();
                SupplierComboBox.ItemsSource = sup;
                SupplierComboBox.DisplayMemberPath = "name";
                SupplierComboBox.SelectedValuePath = "supplier_id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки поставщиков: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateFields()
        {
            if (editingProduct == null) return;

            NameTextBox.Text = editingProduct.name;
            SkuTextBox.Text = editingProduct.sku;
            DescriptionTextBox.Text = editingProduct.description;
            PriceTextBox.Text = editingProduct.price.ToString();
            StockTextBox.Text = editingProduct.stock_quantity.ToString();

            // Select category / supplier
            try
            {
                CategoryComboBox.SelectedValue = editingProduct.category_id;
                SupplierComboBox.SelectedValue = editingProduct.supplier_id;
            }
            catch { }

            // Image
            if (!string.IsNullOrEmpty(editingProduct.image))
            {
                ImagePathTextBox.Text = editingProduct.image;
                TryLoadImageFromPath(editingProduct.image);
            }
        }

        private void TryLoadImageFromPath(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) { ProductImage.Source = null; return; }

                // If path exists as absolute, use it; else try relative to app directory / images folder
                if (File.Exists(path))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.EndInit();
                    ProductImage.Source = bitmap;
                }
                else
                {
                    // try relative to application folder
                    var appPath = AppDomain.CurrentDomain.BaseDirectory;
                    var candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                    if (File.Exists(candidate))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(candidate, UriKind.Absolute);
                        bitmap.EndInit();
                        ProductImage.Source = bitmap;
                    }
                    else
                    {
                        ProductImage.Source = null;
                    }
                }
            }
            catch
            {
                ProductImage.Source = null;
            }
        }

        private void ChooseImageButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };

            if (dlg.ShowDialog() != true)
                return;

            string imagesDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Images");

            if (!Directory.Exists(imagesDir))
                Directory.CreateDirectory(imagesDir);

            string fileName =
                Path.GetFileNameWithoutExtension(dlg.FileName)
                + "_" + Guid.NewGuid().ToString("N").Substring(0, 6)
                + Path.GetExtension(dlg.FileName);

            string destPath = Path.Combine(imagesDir, fileName);
            File.Copy(dlg.FileName, destPath, true);

            string relativePath = Path.Combine("Images", fileName);

            ImagePathTextBox.Text = relativePath;
            ProductImage.Source = new BitmapImage(new Uri(destPath, UriKind.Absolute));
        }

        private void ClearImageButton_Click(object sender, RoutedEventArgs e)
        {
            // очищаем путь
            ImagePathTextBox.Text = string.Empty;

            // убираем превью
            ProductImage.Source = null;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                NavigationService.Navigate(new DataOutput());
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    MessageBox.Show("Введите название товара", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    NameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(SkuTextBox.Text))
                {
                    MessageBox.Show("Введите артикул (SKU)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SkuTextBox.Focus();
                    return;
                }

                if (!decimal.TryParse(PriceTextBox.Text.Replace(',', '.'), out decimal price))
                {
                    MessageBox.Show("Введите корректную цену (например 123.45)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PriceTextBox.Focus();
                    return;
                }

                if (!int.TryParse(StockTextBox.Text, out int stock))
                {
                    MessageBox.Show("Введите корректное количество на складе (целое число)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StockTextBox.Focus();
                    return;
                }

                if (CategoryComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите категорию товара", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SupplierComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите поставщика", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверки уникальности SKU при добавлении (и при редактировании — если изменили sku)
                var existingSku = AppConnect.model01.PRODUCTS.FirstOrDefault(p => p.sku == SkuTextBox.Text && p.product_id != editingProduct.product_id);
                if (existingSku != null)
                {
                    MessageBox.Show("Товар с таким артикулом (SKU) уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SkuTextBox.Focus();
                    return;
                }

                // Заполнение модели
                editingProduct.name = NameTextBox.Text.Trim();
                editingProduct.sku = SkuTextBox.Text.Trim();
                editingProduct.description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim();
                editingProduct.price = price;
                editingProduct.stock_quantity = stock;
                editingProduct.category_id = (int)CategoryComboBox.SelectedValue;
                editingProduct.supplier_id = (int)SupplierComboBox.SelectedValue;
                editingProduct.image = string.IsNullOrWhiteSpace(ImagePathTextBox.Text) ? null : ImagePathTextBox.Text.Trim();

                if (isNew)
                {
                    AppConnect.model01.PRODUCTS.Add(editingProduct);
                }
                else
                {
                    // при редактировании EF отслеживает объект, если он был получен из контекста;
                    // однако если продукт пришёл как внешний объект, лучше присоединить/обновить
                    var tracked = AppConnect.model01.PRODUCTS.FirstOrDefault(p => p.product_id == editingProduct.product_id);
                    if (tracked != null)
                    {
                        // обновляем поля tracked
                        tracked.name = editingProduct.name;
                        tracked.sku = editingProduct.sku;
                        tracked.description = editingProduct.description;
                        tracked.price = editingProduct.price;
                        tracked.stock_quantity = editingProduct.stock_quantity;
                        tracked.category_id = editingProduct.category_id;
                        tracked.supplier_id = editingProduct.supplier_id;
                        tracked.image = editingProduct.image;
                    }
                    else
                    {
                        // Если по какой-то причине не найден — добавим/attach
                        AppConnect.model01.PRODUCTS.Attach(editingProduct);
                        AppConnect.model01.Entry(editingProduct).State = System.Data.Entity.EntityState.Modified;
                    }
                }

                AppConnect.model01.SaveChanges();

                MessageBox.Show("Товар успешно сохранён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Возврат обратно к списку
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
                else
                    NavigationService.Navigate(new DataOutput());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (editingProduct == null || isNew)
                {
                    MessageBox.Show("Нечего удалять", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Проверка прав
                if (!(AppData.CurrentUser != null && AppData.CurrentUser.RoleId == 4))
                {
                    MessageBox.Show("Удалять товары может только администратор", "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var res = MessageBox.Show($"Вы уверены, что хотите удалить товар '{editingProduct.name}'?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes) return;

                // Проверим бизнес-ограничения: товар не должен быть в ORDER_ITEMS
                var used = AppConnect.model01.ORDER_ITEMS.Any(oi => oi.product_id == editingProduct.product_id);
                if (used)
                {
                    MessageBox.Show("Невозможно удалить товар — он присутствует в заказах.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Удаление
                var tracked = AppConnect.model01.PRODUCTS.FirstOrDefault(p => p.product_id == editingProduct.product_id);
                if (tracked != null)
                {
                    AppConnect.model01.PRODUCTS.Remove(tracked);
                    AppConnect.model01.SaveChanges();
                }

                MessageBox.Show("Товар удалён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
                else
                    NavigationService.Navigate(new DataOutput());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

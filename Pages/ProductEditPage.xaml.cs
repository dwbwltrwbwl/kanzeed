using kanzeed.ApplicationData;
using Microsoft.Win32;
using System;
using System.Diagnostics;
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
        private static readonly string ProductImagesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProductImages");

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
            PriceTextBox.Text = editingProduct.price.ToString("F2");
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

            // Скидка
            if (editingProduct.discount.HasValue && editingProduct.discount.Value > 0)
            {
                IsDiscountedCheckBox.IsChecked = true;
                DiscountPanel.Visibility = Visibility.Visible;
                DiscountTextBox.Text = editingProduct.discount.Value.ToString("0.##");
            }
            else
            {
                IsDiscountedCheckBox.IsChecked = false;
                DiscountPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void TryLoadImageFromPath(string relativePath)
        {
            try
            {
                if (string.IsNullOrEmpty(relativePath))
                {
                    ProductImage.Source = null;
                    return;
                }

                // Полный путь к изображению
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

                // Если файл не найден, ищем в папке Images
                if (!File.Exists(fullPath))
                {
                    string fileName = Path.GetFileName(relativePath);
                    fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", fileName);
                }

                if (!File.Exists(fullPath))
                {
                    ProductImage.Source = null;
                    return;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                bitmap.EndInit();

                ProductImage.Source = bitmap;
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
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title = "Выберите изображение товара"
            };

            if (dlg.ShowDialog() != true)
                return;

            // Папка Images в проекте (рядом с .csproj)
            string projectImagesFolder = Path.Combine(
                Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName,
                "Images");

            // Папка Images в выходной директории (bin/Debug или bin/Release)
            string outputImagesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

            // Создаём обе папки, если их нет
            if (!Directory.Exists(projectImagesFolder))
                Directory.CreateDirectory(projectImagesFolder);
            if (!Directory.Exists(outputImagesFolder))
                Directory.CreateDirectory(outputImagesFolder);

            // Генерируем уникальное имя файла на основе SKU
            string fileName;
            if (!string.IsNullOrEmpty(SkuTextBox.Text))
            {
                string cleanSku = SkuTextBox.Text.Trim()
                    .Replace(" ", "_")
                    .Replace("/", "_")
                    .Replace("\\", "_")
                    .Replace(":", "_")
                    .Replace("*", "_")
                    .Replace("?", "_")
                    .Replace("\"", "_")
                    .Replace("<", "_")
                    .Replace(">", "_")
                    .Replace("|", "_");

                fileName = $"product_{cleanSku}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(dlg.FileName)}";
            }
            else
            {
                fileName = $"product_{Guid.NewGuid():N}{Path.GetExtension(dlg.FileName)}";
            }

            // Убираем оставшиеся недопустимые символы
            fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

            try
            {
                // 1. Копируем в папку проекта (для исходного кода)
                string projectImagePath = Path.Combine(projectImagesFolder, fileName);
                File.Copy(dlg.FileName, projectImagePath, true);

                // 2. Копируем в папку bin (для запуска)
                string outputImagePath = Path.Combine(outputImagesFolder, fileName);
                File.Copy(dlg.FileName, outputImagePath, true);

                // 3. В БД сохраняем относительный путь: Images/имя_файла
                ImagePathTextBox.Text = Path.Combine("Images", fileName);

                // 4. Показываем превью
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(outputImagePath, UriKind.Absolute);
                bitmap.EndInit();

                ProductImage.Source = bitmap;

                MessageBox.Show($"Изображение сохранено в папку Images", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при копировании изображения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                if (!decimal.TryParse(PriceTextBox.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal price))
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

                decimal? discount = null;

                if (IsDiscountedCheckBox.IsChecked == true)
                {
                    if (!decimal.TryParse(DiscountTextBox.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal discountValue))
                    {
                        MessageBox.Show("Введите корректный процент скидки", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        DiscountTextBox.Focus();
                        return;
                    }

                    if (discountValue <= 0 || discountValue >= 90)
                    {
                        MessageBox.Show("Скидка должна быть в диапазоне от 1 до 90%", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        DiscountTextBox.Focus();
                        return;
                    }

                    discount = discountValue;
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
                editingProduct.discount = discount;

                if (isNew)
                {
                    AppConnect.model01.PRODUCTS.Add(editingProduct);
                }
                else
                {
                    // при редактировании EF отслеживает объект, если он был получен из контекста;
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
                        tracked.discount = editingProduct.discount;
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

                // Удаление изображения из папки, если оно существует
                if (!string.IsNullOrEmpty(editingProduct.image))
                {
                    try
                    {
                        string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, editingProduct.image);
                        if (File.Exists(imagePath))
                        {
                            File.Delete(imagePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Логируем ошибку, но не прерываем удаление товара
                        Debug.WriteLine($"Не удалось удалить изображение: {ex.Message}");
                    }
                }

                // Удаление товара из БД
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

        private void IsDiscountedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            DiscountPanel.Visibility = Visibility.Visible;
        }

        private void IsDiscountedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DiscountPanel.Visibility = Visibility.Collapsed;
            DiscountTextBox.Text = string.Empty;
        }

    }
}
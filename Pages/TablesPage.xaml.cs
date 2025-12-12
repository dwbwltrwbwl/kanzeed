using kanzeed.ApplicationData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace kanzeed.Pages
{
    public partial class TablesPage : Page
    {
        // Текущая выбранная "логическая" таблица
        private string currentTable = null;

        public TablesPage()
        {
            InitializeComponent();
            LoadTablesList();
        }

        #region Загрузка списка таблиц
        private void LoadTablesList()
        {
            // Явно перечисляем таблицы, которые хотим показать. sysdiagrams — исключаем.
            var tables = new List<string>
            {
                "CATEGORIES",
                "CUSTOMERS",
                "CUSTOMER_ADDRESSES",
                "EMPLOYEES",
                "DELIVERIES",
                "DELIVERY_METHODS",
                "ORDER_ITEMS",
                "ORDERS",
                "ORDER_STATUSES",
                "PAYMENT_METHODS",
                "PRODUCTS",
                "SUPPLIERS"
            };

            TablesList.ItemsSource = tables;
            TablesList.SelectedIndex = 0;
        }

        private void RefreshTables_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadTablesList();
            MessageBox.Show("Список таблиц обновлён", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region Selection / Refresh data
        private void TablesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TablesList.SelectedItem == null) return;
            var tableName = TablesList.SelectedItem.ToString();
            LoadTableData(tableName);
        }

        private void RefreshData_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentTable))
            {
                LoadTableData(currentTable);
                MessageBox.Show("Данные обновлены", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        #region Main loader (проекции для читаемости)
        private void LoadTableData(string tableName)
        {
            currentTable = tableName;
            CurrentTableName.Text = tableName;

            try
            {
                switch (tableName)
                {
                    case "CATEGORIES":
                        LoadCategories();
                        break;
                    case "CUSTOMERS":
                        LoadCustomers();
                        break;
                    case "CUSTOMER_ADDRESSES":
                        LoadCustomerAddresses();
                        break;
                    case "EMPLOYEES":
                        LoadEmployees();
                        break;
                    case "PRODUCTS":
                        LoadProducts();
                        break;
                    case "ORDERS":
                        LoadOrders();
                        break;
                    case "ORDER_ITEMS":
                        LoadOrderItems();
                        break;
                    case "DELIVERIES":
                        LoadDeliveries();
                        break;
                    case "DELIVERY_METHODS":
                        LoadDeliveryMethods();
                        break;
                    case "ORDER_STATUSES":
                        LoadOrderStatuses();
                        break;
                    case "PAYMENT_METHODS":
                        LoadPaymentMethods();
                        break;
                    case "SUPPLIERS":
                        LoadSuppliers();
                        break;
                    default:
                        TableDataGrid.ItemsSource = null;
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки таблицы {tableName}: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCategories()
        {
            // Убираем навигационные свойства (Products) — показываем только id/name
            var data = AppConnect.model01.CATEGORIES
                .Select(c => new
                {
                    ID = c.category_id,
                    Категория = c.name
                })
                .ToList();

            TableDataGrid.ItemsSource = data;
        }

        private void LoadCustomers()
        {
            var data = AppConnect.model01.CUSTOMERS
                .Select(c => new
                {
                    ID = c.customer_id,
                    Фамилия = c.last_name,
                    Имя = c.first_name,
                    Отчество = c.middle_name,
                    Email = c.email,
                    Телефон = c.phone,
                    Роль = c.ROLES != null ? c.ROLES.role_name : "—"
                })
                .ToList();

            TableDataGrid.ItemsSource = data;
        }

        private void LoadCustomerAddresses()
        {
            // Заменяем customer_id на читабельное ФИО; убираем навигационные CUSTOMERS и DELIVERIES
            var data = AppConnect.model01.CUSTOMER_ADDRESSES
                .Select(a => new
                {
                    ID = a.address_id,
                    Клиент = a.CUSTOMERS != null ? (a.CUSTOMERS.last_name + " " + a.CUSTOMERS.first_name).Trim() : ("ID:" + a.customer_id),
                    Город = a.city,
                    Почтовый_код = a.postal_code,
                    Улица = a.street,
                    Дом = a.house,
                    Этаж = a.floor,
                    Квартира = a.apartment,
                    Подъезд = a.porch
                })
                .ToList();

            TableDataGrid.ItemsSource = data;
        }

        private void LoadEmployees()
        {
            var data = AppConnect.model01.EMPLOYEES
                .Select(e => new
                {
                    ID = e.employee_id,
                    Фамилия = e.last_name,
                    Имя = e.first_name,
                    Отчество = e.middle_name,
                    Email = e.email,
                    Телефон = e.phone,
                    Роль = e.ROLES != null ? e.ROLES.role_name : "—"
                })
                .ToList();

            TableDataGrid.ItemsSource = data;
        }

        private void LoadProducts()
        {
            var data = AppConnect.model01.PRODUCTS
                .Select(p => new
                {
                    ID = p.product_id,
                    Товар = p.name,
                    Артикул = p.sku,
                    Описание = p.description,
                    Цена = p.price,
                    Категория = p.CATEGORIES != null ? p.CATEGORIES.name : "—",
                    Поставщик = p.SUPPLIERS != null ? p.SUPPLIERS.name : "—",
                    На_складе = p.stock_quantity,
                    Изображение = p.image
                })
                .ToList();

            TableDataGrid.ItemsSource = data;
        }

        private void LoadOrders()
        {
            var data = AppConnect.model01.ORDERS
                .Select(o => new
                {
                    ID = o.order_id,
                    Клиент = o.CUSTOMERS != null ? (o.CUSTOMERS.last_name + " " + o.CUSTOMERS.first_name).Trim() : ("ID:" + o.customer_id),
                    Дата = o.order_date,
                    Сумма = o.total_amount,
                    Статус = o.ORDER_STATUSES != null ? o.ORDER_STATUSES.status_name : "—",
                    Оплата = o.PAYMENT_METHODS != null ? o.PAYMENT_METHODS.method_name : "—"
                })
                .ToList();

            TableDataGrid.ItemsSource = data;
        }

        private void LoadOrderItems()
        {
            var data = AppConnect.model01.ORDER_ITEMS
                .Select(i => new
                {
                    ID = i.order_item_id,
                    Заказ = i.order_id,
                    Товар = i.PRODUCTS != null ? i.PRODUCTS.name : ("ID:" + i.product_id),
                    Количество = i.quantity
                })
                .ToList();

            TableDataGrid.ItemsSource = data;
        }

        private void LoadDeliveries()
        {
            var data = AppConnect.model01.DELIVERIES
                .Select(d => new
                {
                    ID = d.delivery_id,
                    Заказ = d.ORDERS != null ? d.ORDERS.order_id : d.order_id,
                    Метод = d.DELIVERY_METHODS != null ? d.DELIVERY_METHODS.method_name : "—",
                    Адрес = d.CUSTOMER_ADDRESSES != null ? (d.CUSTOMER_ADDRESSES.city + ", " + d.CUSTOMER_ADDRESSES.street + " " + d.CUSTOMER_ADDRESSES.house) : "—",
                    Стоимость = d.delivery_cost,
                    Курьер = d.EMPLOYEES != null ? (d.EMPLOYEES.last_name + " " + d.EMPLOYEES.first_name) : "—",
                    Ожидаемая_дата = d.estimated_date
                })
                .ToList();

            TableDataGrid.ItemsSource = data;
        }

        private void LoadDeliveryMethods()
        {
            var data = AppConnect.model01.DELIVERY_METHODS
                .Select(m => new
                {
                    ID = m.delivery_method_id,
                    Метод = m.method_name
                })
                .ToList();

            TableDataGrid.ItemsSource = data;
        }

        private void LoadOrderStatuses()
        {
            var data = AppConnect.model01.ORDER_STATUSES
                .Select(s => new
                {
                    ID = s.status_id,
                    Статус = s.status_name
                })
                .ToList();

            TableDataGrid.ItemsSource = data;
        }

        private void LoadPaymentMethods()
        {
            var data = AppConnect.model01.PAYMENT_METHODS
                .Select(p => new
                {
                    ID = p.payment_method_id,
                    Метод = p.method_name
                })
                .ToList();

            TableDataGrid.ItemsSource = data;
        }

        private void LoadSuppliers()
        {
            var data = AppConnect.model01.SUPPLIERS
                .Select(s => new
                {
                    ID = s.supplier_id,
                    Поставщик = s.name
                })
                .ToList();

            TableDataGrid.ItemsSource = data;
        }
        #endregion

        #region Open / Export / Back
        private void OpenInEditor_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentTable))
            {
                MessageBox.Show("Выберите таблицу.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selected = TableDataGrid.SelectedItem;
            if (selected == null)
            {
                MessageBox.Show("Выберите строку для открытия.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Специально обрабатываем PRODUCTS — переходим на ProductEditPage
                if (currentTable == "PRODUCTS")
                {
                    int id = GetIdFromAnonymous(selected);
                    if (id > 0)
                    {
                        var product = AppConnect.model01.PRODUCTS.FirstOrDefault(p => p.product_id == id);
                        if (product != null)
                        {
                            NavigationService.Navigate(new ProductEditPage(product));
                            return;
                        }
                    }

                    MessageBox.Show("Не удалось получить продукт для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Для остальных таблиц — показать подробности в всплывающем окне (можно заменить на полноценный редактор)
                var details = BuildDetailsStringFromObject(selected);
                MessageBox.Show(details, "Детали записи", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия записи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var items = TableDataGrid.ItemsSource as IEnumerable<object>;
            if (items == null)
            {
                MessageBox.Show("Нет данных для экспорта.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), $"kanzeed_{currentTable}_{DateTime.Now:yyyyMMddHHmmss}.csv");
                ExportToCsv(items, tempPath);
                MessageBox.Show($"Данные экспортированы в:\n{tempPath}", "Экспорт завершён", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
            else
                MessageBox.Show("Нет страницы для возврата.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region Helpers
        // Попытка получить поле ID из анонимного объекта (если есть свойство "ID" или "<имя>_id")
        private int GetIdFromAnonymous(object anon)
        {
            if (anon == null) return -1;
            var t = anon.GetType();
            var idProp = t.GetProperty("ID") ?? t.GetProperties().FirstOrDefault(p => p.Name.Equals("ID", StringComparison.OrdinalIgnoreCase)
                                                                                 || p.Name.EndsWith("_id", StringComparison.OrdinalIgnoreCase)
                                                                                 || p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
            if (idProp != null)
            {
                var v = idProp.GetValue(anon);
                if (v != null && int.TryParse(v.ToString(), out int id)) return id;
            }

            // Также попробуем найти первичный ключ со знакомым названием
            var fallback = t.GetProperties().FirstOrDefault(p => p.Name.ToLower().Contains("id"));
            if (fallback != null)
            {
                var v = fallback.GetValue(anon);
                if (v != null && int.TryParse(v.ToString(), out int id)) return id;
            }

            return -1;
        }

        // Собрать удобочитаемую строку с полями объекта (анонимного / проекции)
        private string BuildDetailsStringFromObject(object obj)
        {
            if (obj == null) return string.Empty;
            var sb = new StringBuilder();
            var t = obj.GetType();
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                var name = p.Name;
                var val = p.GetValue(obj);
                sb.AppendLine($"{name}: {val}");
            }
            return sb.ToString();
        }

        // Экспорт произвольного ItemsSource (списка анонимных объектов) в CSV
        private void ExportToCsv(IEnumerable<object> items, string filePath)
        {
            var first = items.FirstOrDefault();
            if (first == null)
            {
                File.WriteAllText(filePath, string.Empty, Encoding.UTF8);
                return;
            }

            var props = first.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var sb = new StringBuilder();

            // Заголовки (используем имена свойств)
            sb.AppendLine(string.Join(",", props.Select(p => QuoteCsv(p.Name))));

            foreach (var item in items)
            {
                var values = props.Select(p =>
                {
                    var v = p.GetValue(item);
                    return QuoteCsv(v?.ToString() ?? string.Empty);
                });
                sb.AppendLine(string.Join(",", values));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private string QuoteCsv(string s)
        {
            if (s == null) return "\"\"";
            // Экранируем двойные кавычки
            var esc = s.Replace("\"", "\"\"");
            return $"\"{esc}\"";
        }
        #endregion
    }
}

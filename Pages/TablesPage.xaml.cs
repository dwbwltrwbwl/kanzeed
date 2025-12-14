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
        private string currentTable;

        public TablesPage()
        {
            InitializeComponent();
            LoadTablesList();
        }

        // =====================================================
        // СПИСОК ТАБЛИЦ
        // =====================================================
        private void LoadTablesList()
        {
            var tables = new List<string>
            {
                "CATEGORIES",
                "PRODUCTS",
                "ORDERS",
                "ORDER_ITEMS",
                "DELIVERIES",
                "CUSTOMER_ADDRESSES",
                "SUPPLIERS",
                "PAYMENT_METHODS",
                "DELIVERY_METHODS",
                "ORDER_STATUSES"
            };

            // ⭐ Закрепляем ORDERS для менеджера
            if (AppData.CurrentUser?.RoleId == 2)
            {
                tables.Remove("ORDERS");
                tables.Insert(0, "⭐ ORDERS");
            }

            TablesList.ItemsSource = tables;
            TablesList.SelectedIndex = 0;
        }


        private void TablesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TablesList.SelectedItem == null) return;

            string table = TablesList.SelectedItem.ToString();

            // убираем звёздочку
            table = table.Replace("⭐ ", "");

            LoadTableData(table);
        }

        // =====================================================
        // ЗАГРУЗКА ДАННЫХ (READ — ВСЁ НА МЕСТЕ)
        // =====================================================
        private void LoadTableData(string table)
        {
            currentTable = table;
            CurrentTableName.Text = table;

            switch (table)
            {
                case "CATEGORIES":
                    TableDataGrid.ItemsSource = AppConnect.model01.CATEGORIES
                        .Select(c => new { ID = c.category_id, Название = c.name }).ToList();
                    break;

                case "PRODUCTS":
                    TableDataGrid.ItemsSource = AppConnect.model01.PRODUCTS
                        .Select(p => new
                        {
                            ID = p.product_id,
                            Товар = p.name,
                            Цена = p.price,
                            Категория = p.CATEGORIES.name,
                            НаСкладе = p.stock_quantity
                        }).ToList();
                    break;

                case "ORDERS":
                    TableDataGrid.ItemsSource = AppConnect.model01.ORDERS
                        .Select(o => new
                        {
                            ID = o.order_id,
                            Клиент = o.CUSTOMERS.last_name,
                            Дата = o.order_date,
                            Сумма = o.total_amount,
                            Статус = o.ORDER_STATUSES.status_name
                        }).ToList();
                    break;

                case "ORDER_ITEMS":
                    TableDataGrid.ItemsSource = AppConnect.model01.ORDER_ITEMS
                        .Select(i => new
                        {
                            ID = i.order_item_id,
                            Заказ = i.order_id,
                            Товар = i.PRODUCTS.name,
                            Количество = i.quantity
                        }).ToList();
                    break;

                case "DELIVERIES":
                    TableDataGrid.ItemsSource = AppConnect.model01.DELIVERIES
                        .Select(d => new
                        {
                            ID = d.delivery_id,
                            Заказ = d.order_id,
                            Метод = d.DELIVERY_METHODS.method_name,
                            Стоимость = d.delivery_cost,
                            Дата = d.estimated_date
                        }).ToList();
                    break;

                case "CUSTOMER_ADDRESSES":
                    TableDataGrid.ItemsSource = AppConnect.model01.CUSTOMER_ADDRESSES
                        .Select(a => new
                        {
                            ID = a.address_id,
                            Клиент = a.CUSTOMERS.last_name,
                            Город = a.city,
                            Улица = a.street,
                            Дом = a.house
                        }).ToList();
                    break;

                case "SUPPLIERS":
                    TableDataGrid.ItemsSource = AppConnect.model01.SUPPLIERS
                        .Select(s => new { ID = s.supplier_id, Поставщик = s.name }).ToList();
                    break;

                case "PAYMENT_METHODS":
                    TableDataGrid.ItemsSource = AppConnect.model01.PAYMENT_METHODS
                        .Select(p => new { ID = p.payment_method_id, Метод = p.method_name }).ToList();
                    break;

                case "DELIVERY_METHODS":
                    TableDataGrid.ItemsSource = AppConnect.model01.DELIVERY_METHODS
                        .Select(d => new { ID = d.delivery_method_id, Метод = d.method_name }).ToList();
                    break;

                case "ORDER_STATUSES":
                    TableDataGrid.ItemsSource = AppConnect.model01.ORDER_STATUSES
                        .Select(s => new { ID = s.status_id, Статус = s.status_name }).ToList();
                    break;
            }
        }

        // =====================================================
        // КНОПКИ ОБНОВЛЕНИЯ (ВОТ ЧЕГО НЕ ХВАТАЛО)
        // =====================================================
        private void RefreshTables_Click(object sender, RoutedEventArgs e)
        {
            LoadTablesList();
        }

        private void RefreshData_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentTable))
                LoadTableData(currentTable);
        }

        // =====================================================
        // ADD — ТОЛЬКО СПРАВОЧНИКИ
        // =====================================================
        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AppData.CurrentUser == null || AppData.CurrentUser.RoleId < 2)
            {
                MessageBox.Show("Недостаточно прав для добавления",
                    "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 🔴 СРАЗУ проверяем — можно ли добавлять
            bool canAdd =
                currentTable == "CATEGORIES" ||
                currentTable == "SUPPLIERS" ||
                currentTable == "PAYMENT_METHODS" ||
                currentTable == "DELIVERY_METHODS" ||
                currentTable == "ORDER_STATUSES";

            if (!canAdd)
            {
                MessageBox.Show(
                    $"Добавление запрещено для таблицы {currentTable}",
                    "Операция недоступна",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // ✅ Только теперь показываем окно ввода
            string value = Prompt("Добавление", "Введите значение:");
            if (string.IsNullOrWhiteSpace(value))
                return;

            switch (currentTable)
            {
                case "CATEGORIES":
                    AppConnect.model01.CATEGORIES.Add(new CATEGORIES { name = value });
                    break;

                case "SUPPLIERS":
                    AppConnect.model01.SUPPLIERS.Add(new SUPPLIERS { name = value });
                    break;

                case "PAYMENT_METHODS":
                    AppConnect.model01.PAYMENT_METHODS.Add(new PAYMENT_METHODS { method_name = value });
                    break;

                case "DELIVERY_METHODS":
                    AppConnect.model01.DELIVERY_METHODS.Add(new DELIVERY_METHODS { method_name = value });
                    break;

                case "ORDER_STATUSES":
                    AppConnect.model01.ORDER_STATUSES.Add(new ORDER_STATUSES { status_name = value });
                    break;
            }

            AppConnect.model01.SaveChanges();
            LoadTableData(currentTable);
        }


        // =====================================================
        // DELETE — ТОЛЬКО АДМИН
        // =====================================================
        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AppData.CurrentUser == null || AppData.CurrentUser.RoleId != 4)
                return;

            if (TableDataGrid.SelectedItem == null) return;

            int id = (int)TableDataGrid.SelectedItem.GetType()
                .GetProperty("ID").GetValue(TableDataGrid.SelectedItem);

            switch (currentTable)
            {
                case "CATEGORIES":
                    AppConnect.model01.CATEGORIES.Remove(
                        AppConnect.model01.CATEGORIES.First(x => x.category_id == id));
                    break;
                case "SUPPLIERS":
                    AppConnect.model01.SUPPLIERS.Remove(
                        AppConnect.model01.SUPPLIERS.First(x => x.supplier_id == id));
                    break;
                default:
                    MessageBox.Show("Удаление запрещено для этой таблицы");
                    return;
            }

            AppConnect.model01.SaveChanges();
            LoadTableData(currentTable);
        }

        // =====================================================
        // ПРОСМОТР / ЭКСПОРТ / НАЗАД
        // =====================================================
        private void OpenInEditor_Click(object sender, RoutedEventArgs e)
        {
            if (TableDataGrid.SelectedItem == null) return;
            MessageBox.Show(BuildDetails(TableDataGrid.SelectedItem),
                "Просмотр записи", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var items = TableDataGrid.ItemsSource as IEnumerable<object>;
            if (items == null) return;

            var props = items.First().GetType().GetProperties();
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", props.Select(p => p.Name)));

            foreach (var item in items)
                sb.AppendLine(string.Join(",", props.Select(p => p.GetValue(item))));

            string path = Path.Combine(Path.GetTempPath(), $"{currentTable}.csv");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

            MessageBox.Show($"CSV сохранён:\n{path}");
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        // =====================================================
        // HELPERS
        // =====================================================
        private string BuildDetails(object obj)
        {
            var sb = new StringBuilder();
            foreach (var p in obj.GetType().GetProperties())
                sb.AppendLine($"{p.Name}: {p.GetValue(obj)}");
            return sb.ToString();
        }

        private string Prompt(string title, string label)
        {
            var win = new Window
            {
                Title = title,
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };

            var tb = new TextBox { Margin = new Thickness(10) };
            var btn = new Button { Content = "OK", Margin = new Thickness(10), IsDefault = true };
            btn.Click += (_, __) => win.DialogResult = true;

            win.Content = new StackPanel
            {
                Children = { new TextBlock { Text = label }, tb, btn }
            };

            return win.ShowDialog() == true ? tb.Text : null;
        }
    }
}

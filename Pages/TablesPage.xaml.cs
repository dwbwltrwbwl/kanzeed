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
                "CITIES",
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
                            Клиент = a.CUSTOMERS.last_name + " " + a.CUSTOMERS.first_name,
                            Город = a.CITIES.city_name,
                            Улица = a.street,
                            Дом = a.house,
                            Квартира = a.apartment,
                            Этаж = a.floor,
                            Подъезд = a.porch,
                            Индекс = a.postal_code
                        })
                        .ToList();
                break;

                case "CITIES":
                    TableDataGrid.ItemsSource = AppConnect.model01.CITIES
                        .Select(c => new
                        {
                            ID = c.city_id,
                            Город = c.city_name
                        })
                        .ToList();
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
                MessageBox.Show("Недостаточно прав",
                    "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            switch (currentTable)
            {
                // ===== ПРОСТЫЕ СПРАВОЧНИКИ =====
                case "CATEGORIES":
                    AddSimple("Введите название категории",
                        v => AppConnect.model01.CATEGORIES.Add(new CATEGORIES { name = v }));
                    break;

                case "SUPPLIERS":
                    AddSimple("Введите название поставщика",
                        v => AppConnect.model01.SUPPLIERS.Add(new SUPPLIERS { name = v }));
                    break;

                case "CITIES":
                    AddSimple("Введите название города",
                        v => AppConnect.model01.CITIES.Add(new CITIES { city_name = v }));
                    break;

                case "PAYMENT_METHODS":
                    AddSimple("Введите способ оплаты",
                        v => AppConnect.model01.PAYMENT_METHODS.Add(new PAYMENT_METHODS { method_name = v }));
                    break;

                case "DELIVERY_METHODS":
                    AddSimple("Введите способ доставки",
                        v => AppConnect.model01.DELIVERY_METHODS.Add(new DELIVERY_METHODS { method_name = v }));
                    break;

                case "ORDER_STATUSES":
                    AddSimple("Введите статус заказа",
                        v => AppConnect.model01.ORDER_STATUSES.Add(new ORDER_STATUSES { status_name = v }));
                    break;

                // ===== СЛОЖНЫЕ ТАБЛИЦЫ =====
                case "PRODUCTS":
                    NavigationService.Navigate(new ProductEditPage());
                    return;

                case "CUSTOMER_ADDRESSES":
                    MessageBox.Show(
                        "Адреса добавляются пользователями через личный кабинет",
                        "Информация",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;

                case "ORDERS":
                case "ORDER_ITEMS":
                case "DELIVERIES":
                    MessageBox.Show(
                        "Добавление записей выполняется автоматически системой",
                        "Информация",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;

                default:
                    MessageBox.Show("Добавление не реализовано");
                    return;
            }

            AppConnect.model01.SaveChanges();
            LoadTableData(currentTable);
        }


        // =====================================================
        // DELETE — ТОЛЬКО АДМИН
        // =====================================================
        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AppData.CurrentUser == null)
                return;

            // ===== МЕНЕДЖЕР =====
            if (AppData.CurrentUser.RoleId == 2)
            {
                MessageBox.Show(
                    "Удаление доступно только администратору",
                    "Доступ запрещён",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // ===== ТОЛЬКО АДМИН =====
            if (AppData.CurrentUser.RoleId != 4)
                return;

            if (TableDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для удаления");
                return;
            }

            int id = (int)TableDataGrid.SelectedItem.GetType()
                .GetProperty("ID")
                .GetValue(TableDataGrid.SelectedItem);

            var confirm = MessageBox.Show(
                "Вы уверены, что хотите удалить выбранную запись?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                bool isUsed = false;

                switch (currentTable)
                {
                    case "ORDER_ITEMS":
                        {
                            var item = AppConnect.model01.ORDER_ITEMS
                                .SingleOrDefault(x => x.order_item_id == id);

                            if (item == null) return;

                            AppConnect.model01.ORDER_ITEMS.Remove(item);
                            break;
                        }

                    case "DELIVERIES":
                        {
                            var delivery = AppConnect.model01.DELIVERIES
                                .SingleOrDefault(x => x.delivery_id == id);

                            if (delivery == null) return;

                            AppConnect.model01.DELIVERIES.Remove(delivery);
                            break;
                        }

                    case "CUSTOMER_ADDRESSES":
                        {
                            var address = AppConnect.model01.CUSTOMER_ADDRESSES
                                .SingleOrDefault(x => x.address_id == id);

                            if (address == null) return;

                            AppConnect.model01.CUSTOMER_ADDRESSES.Remove(address);
                            break;
                        }

                    // ===== СПРАВОЧНИКИ С ПРОВЕРКОЙ =====

                    case "CITIES":
                        if (AppConnect.model01.CUSTOMER_ADDRESSES.Any(a => a.city_id == id))
                        {
                            MessageBox.Show("Город используется в адресах клиентов");
                            return;
                        }
                        AppConnect.model01.CITIES.Remove(
                            AppConnect.model01.CITIES.Single(c => c.city_id == id));
                        break;

                    case "PRODUCTS":
                        if (AppConnect.model01.ORDER_ITEMS.Any(oi => oi.product_id == id))
                        {
                            MessageBox.Show("Товар используется в заказах");
                            return;
                        }
                        AppConnect.model01.PRODUCTS.Remove(
                            AppConnect.model01.PRODUCTS.Single(p => p.product_id == id));
                        break;

                    case "ORDERS":
                        if (AppConnect.model01.ORDER_ITEMS.Any(oi => oi.order_id == id) ||
                            AppConnect.model01.DELIVERIES.Any(d => d.order_id == id))
                        {
                            MessageBox.Show("Заказ используется в доставках или позициях");
                            return;
                        }
                        AppConnect.model01.ORDERS.Remove(
                            AppConnect.model01.ORDERS.Single(o => o.order_id == id));
                        break;

                    case "CATEGORIES":
                        if (AppConnect.model01.PRODUCTS.Any(p => p.category_id == id))
                        {
                            MessageBox.Show("Категория используется в товарах");
                            return;
                        }
                        AppConnect.model01.CATEGORIES.Remove(
                            AppConnect.model01.CATEGORIES.Single(c => c.category_id == id));
                        break;

                    case "SUPPLIERS":
                        if (AppConnect.model01.PRODUCTS.Any(p => p.supplier_id == id))
                        {
                            MessageBox.Show("Поставщик используется в товарах");
                            return;
                        }
                        AppConnect.model01.SUPPLIERS.Remove(
                            AppConnect.model01.SUPPLIERS.Single(s => s.supplier_id == id));
                        break;

                    case "PAYMENT_METHODS":
                        {
                            if (AppConnect.model01.ORDERS.Any(o => o.payment_method_id == id))
                            {
                                MessageBox.Show("Метод оплаты используется в заказах");
                                return;
                            }

                            var pm = AppConnect.model01.PAYMENT_METHODS
                                .SingleOrDefault(x => x.payment_method_id == id);

                            if (pm == null) return;

                            AppConnect.model01.PAYMENT_METHODS.Remove(pm);
                            break;
                        }

                    case "DELIVERY_METHODS":
                        {
                            if (AppConnect.model01.DELIVERIES.Any(d => d.delivery_method_id == id))
                            {
                                MessageBox.Show("Метод доставки используется");
                                return;
                            }

                            var dm = AppConnect.model01.DELIVERY_METHODS
                                .SingleOrDefault(x => x.delivery_method_id == id);

                            if (dm == null) return;

                            AppConnect.model01.DELIVERY_METHODS.Remove(dm);
                            break;
                        }

                    case "ORDER_STATUSES":
                        {
                            if (AppConnect.model01.ORDERS.Any(o => o.status_id == id))
                            {
                                MessageBox.Show("Статус используется в заказах");
                                return;
                            }

                            var st = AppConnect.model01.ORDER_STATUSES
                                .SingleOrDefault(x => x.status_id == id);

                            if (st == null) return;

                            AppConnect.model01.ORDER_STATUSES.Remove(st);
                            break;
                        }
                }

                if (isUsed)
                {
                    MessageBox.Show(
                        "Невозможно удалить запись, так как она используется в других таблицах",
                        "Удаление запрещено",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                AppConnect.model01.SaveChanges();
                LoadTableData(currentTable);

                MessageBox.Show("Запись успешно удалена",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка удаления:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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
        private void AddSimple(string label, Action<string> addAction)
        {
            string value = Prompt("Добавление", label);
            if (string.IsNullOrWhiteSpace(value))
                return;

            addAction(value.Trim());
        }
        private void ChangeOrderStatus_Click(object sender, RoutedEventArgs e)
        {
            // Только менеджер и админ
            if (AppData.CurrentUser == null ||
                (AppData.CurrentUser.RoleId != 2 && AppData.CurrentUser.RoleId != 4))
            {
                MessageBox.Show("Недостаточно прав");
                return;
            }

            if (currentTable != "ORDERS")
            {
                MessageBox.Show("Статус можно менять только у заказов");
                return;
            }

            if (TableDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите заказ");
                return;
            }

            int orderId = (int)TableDataGrid.SelectedItem
                .GetType()
                .GetProperty("ID")
                .GetValue(TableDataGrid.SelectedItem);

            var order = AppConnect.model01.ORDERS
                .SingleOrDefault(o => o.order_id == orderId);

            if (order == null)
            {
                MessageBox.Show("Заказ не найден");
                return;
            }

            // ===== ОКНО ВЫБОРА СТАТУСА =====
            var statuses = AppConnect.model01.ORDER_STATUSES.ToList();

            var combo = new ComboBox
            {
                ItemsSource = statuses,
                DisplayMemberPath = "status_name",
                SelectedValuePath = "status_id",
                SelectedValue = order.status_id,
                Margin = new Thickness(10)
            };

            var btn = new Button
            {
                Content = "Сохранить",
                Margin = new Thickness(10),
                IsDefault = true
            };

            var win = new Window
            {
                Title = $"Изменение статуса заказа №{order.order_id}",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                Content = new StackPanel
                {
                    Children =
            {
                new TextBlock
                {
                    Text = "Выберите новый статус:",
                    Margin = new Thickness(10)
                },
                combo,
                btn
            }
                }
            };

            btn.Click += (_, __) =>
            {
                if (combo.SelectedValue == null)
                {
                    MessageBox.Show("Выберите статус");
                    return;
                }

                order.status_id = (int)combo.SelectedValue;
                AppConnect.model01.SaveChanges();
                win.DialogResult = true;
            };

            if (win.ShowDialog() == true)
            {
                LoadTableData("ORDERS");

                MessageBox.Show(
                    "Статус заказа успешно изменён",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}

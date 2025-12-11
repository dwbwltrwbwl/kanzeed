using kanzeed.ApplicationData;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace kanzeed.Pages
{
    /// <summary>
    /// Логика взаимодействия для TablesPage.xaml
    /// </summary>
    public partial class TablesPage : Page
    {
        // Список имён таблиц, которые нужно исключить из просмотра (пользователи и роли)
        private readonly HashSet<string> excludedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CUSTOMERS",
            "EMPLOYEES",
            "ROLES"
        };

        // Слова для отображения: свойство DbSetName -> display friendly name (опционально)
        private Dictionary<string, string> displayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public TablesPage()
        {
            InitializeComponent();

            // Защита доступа: только менеджер(2) и админ(4)
            if (AppData.CurrentUser == null || (AppData.CurrentUser.RoleId != 2 && AppData.CurrentUser.RoleId != 4))
            {
                MessageBox.Show("Доступ запрещён. Требуется роль менеджера или администратора.", "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.Navigate(new Authorization());
                return;
            }

            PrepareDisplayNames();
            LoadTableList();
        }

        private void PrepareDisplayNames()
        {
            // Можно подправить читаемые имена таблиц
            displayNames["PRODUCTS"] = "Товары (PRODUCTS)";
            displayNames["CATEGORIES"] = "Категории (CATEGORIES)";
            displayNames["ORDER_ITEMS"] = "Позиции заказа (ORDER_ITEMS)";
            displayNames["ORDERS"] = "Заказы (ORDERS)";
            displayNames["DELIVERIES"] = "Доставки (DELIVERIES)";
            displayNames["DELIVERY_METHODS"] = "Методы доставки (DELIVERY_METHODS)";
            displayNames["PAYMENT_METHODS"] = "Методы оплаты (PAYMENT_METHODS)";
            displayNames["SUPPLIERS"] = "Поставщики (SUPPLIERS)";
            displayNames["CUSTOMER_ADDRESSES"] = "Адреса клиентов (CUSTOMER_ADDRESSES)";
            // добавляйте другие, если нужно
        }

        private void LoadTableList()
        {
            TablesList.Items.Clear();

            // Находим все публичные свойства DbSet в AppConnect.model01 через reflection
            var ctxType = AppConnect.model01.GetType();
            var props = ctxType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var p in props)
            {
                // выбираем свойства, которые представляют коллекции сущностей: тип должен быть IEnumerable<T> или DbSet<T>
                var propType = p.PropertyType;
                // Пропускаем навигационные свойства и т.д.
                if (!propType.IsGenericType) continue;

                var genericName = propType.GetGenericArguments()[0].Name; // например PRODUCTS -> entity class name like PRODUCTS
                var propName = p.Name; // имя свойства DbSet, например PRODUCTS

                // Пропускаем исключённые таблицы
                if (excludedTables.Contains(propName.ToUpperInvariant())) continue;

                // Отображаем новое имя, если есть
                string display = displayNames.ContainsKey(propName.ToUpperInvariant()) ? displayNames[propName.ToUpperInvariant()] : propName;
                TablesList.Items.Add(propName);
            }

            if (TablesList.Items.Count > 0)
            {
                TablesList.SelectedIndex = 0;
            }
        }

        private void TablesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TablesList.SelectedItem == null) return;
            string tableName = TablesList.SelectedItem.ToString();
            LoadTableData(tableName);
        }

        private void LoadTableData(string dbSetName)
        {
            try
            {
                CurrentTableName.Text = displayNames.ContainsKey(dbSetName.ToUpperInvariant()) ? displayNames[dbSetName.ToUpperInvariant()] : dbSetName;

                // Получаем свойство DbSet по имени
                var prop = AppConnect.model01.GetType().GetProperty(dbSetName);
                if (prop == null)
                {
                    MessageBox.Show($"Не найден DbSet '{dbSetName}' в контексте.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Получаем данные: вызываем ToList() на DbSet
                var dbSet = prop.GetValue(AppConnect.model01, null);
                if (dbSet == null)
                {
                    TableDataGrid.ItemsSource = null;
                    return;
                }

                // Используем reflection, чтобы вызвать метод IQueryable.Cast<object>().ToList()
                var asQueryable = dbSet as System.Linq.IQueryable;
                if (asQueryable == null)
                {
                    // Попробуем как IEnumerable
                    var asEnum = dbSet as System.Collections.IEnumerable;
                    if (asEnum == null)
                    {
                        TableDataGrid.ItemsSource = null;
                        return;
                    }
                    var list = new List<object>();
                    foreach (var item in asEnum) list.Add(item);
                    TableDataGrid.ItemsSource = list;
                    return;
                }

                // Превращаем IQueryable в List<object> безопасно
                var enumerator = asQueryable.GetEnumerator();
                var listObjects = new List<object>();
                while (enumerator.MoveNext()) listObjects.Add(enumerator.Current);

                TableDataGrid.ItemsSource = listObjects;

                // Подсказка: столбцы генерируются автоматически (AutoGenerateColumns = true)
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshTables_Click(object sender, RoutedEventArgs e)
        {
            LoadTableList();
        }

        private void RefreshData_Click(object sender, RoutedEventArgs e)
        {
            if (TablesList.SelectedItem != null)
            {
                LoadTableData(TablesList.SelectedItem.ToString());
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (TableDataGrid.ItemsSource == null) return;

            try
            {
                var items = TableDataGrid.ItemsSource.Cast<object>().ToList();
                if (!items.Any())
                {
                    MessageBox.Show("Нет данных для экспорта.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Формируем CSV заголовки по свойствам первого объекта
                var first = items.First();
                var props = first.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var sb = new StringBuilder();
                sb.AppendLine(string.Join(";", props.Select(p => p.Name)));

                foreach (var it in items)
                {
                    var values = props.Select(p =>
                    {
                        var v = p.GetValue(it);
                        if (v == null) return "";
                        var s = v.ToString();
                        return s.Contains(";") ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
                    });
                    sb.AppendLine(string.Join(";", values));
                }

                var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"{TablesList.SelectedItem}.csv");
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

                MessageBox.Show($"Экспорт завершён. Файл сохранён на рабочем столе: {path}", "Экспорт CSV", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenInEditor_Click(object sender, RoutedEventArgs e)
        {
            // Показываем детальную информацию о выбранной строке
            var item = TableDataGrid.SelectedItem;
            if (item == null)
            {
                MessageBox.Show("Выберите запись для просмотра.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Формируем читаемое представление
            var props = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var sb = new StringBuilder();
            foreach (var p in props)
            {
                var v = p.GetValue(item);
                sb.AppendLine($"{p.Name}: {v}");
            }

            MessageBox.Show(sb.ToString(), "Детали записи", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}

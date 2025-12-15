using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace kanzeed.Pages
{
    public class RoleToVisibilityConverter : IValueConverter
    {
        // Маппинг ролей в вашей БД:
        // Guest (неавторизованный) -> RoleId = 0 (или AppData.CurrentUser == null)
        // 1 - Клиент (пользователь)
        // 2 - Менеджер
        // 3 - Курьер (можно учитывать отдельно)
        // 4 - Администратор

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // parameter - строка-метка действия
            string action = (parameter as string) ?? string.Empty;

            // безопасно получить roleId
            int roleId = 0; // по умолчанию гость
            try
            {
                // WPF может передать DependencyProperty.UnsetValue
                if (value == null || value == System.Windows.DependencyProperty.UnsetValue)
                {
                    roleId = 0;
                }
                else if (value is int)
                {
                    roleId = (int)value;
                }
                else
                {
                    // ожидаем объект UserData / AppData.CurrentUser
                    var prop = value.GetType().GetProperty("RoleId");
                    if (prop != null)
                    {
                        var v = prop.GetValue(value);
                        if (v is int) roleId = (int)v;
                    }
                    else
                    {
                        roleId = 0;
                    }
                }
            }
            catch
            {
                roleId = 0;
            }

            // Правила видимости (с учётом вашего описания):
            // Гость: только регистрация/вход (то есть скрываем каталог/корзину/CRUD)
            // Клиент (1): просмотр каталога, поиск, фильтрация, корзина, оформление заказов, история заказов
            // Менеджер (2): все права клиента + просмотр таблиц, CRUD (без удаления)
            // Админ (4): все права менеджера + управление пользователями и удаление

            switch (action)
            {
                // Основной каталог — только авторизованные
                case "ViewCatalog":
                case "Search":
                case "Filters":
                    return Visibility.Visible;

                // Корзина — ТОЛЬКО клиент
                case "Cart":
                case "AddToCart":
                case "ViewOrders":
                    return roleId == 1 ? Visibility.Visible : Visibility.Collapsed;

                // Менеджер и админ
                case "ViewTables":
                case "EditProduct":
                case "AddProduct":
                    return roleId == 2 || roleId == 4
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                // Только админ
                case "ManageUsers":
                case "DeleteProduct":
                case "DeleteAny":
                    return roleId == 4
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                case "GuestOnly":
                    return roleId == 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                // 🔴 ВОТ ЭТОГО НЕ ХВАТАЛО
                case "Logout":
                    return roleId >= 1
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                default:
                    return Visibility.Collapsed;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

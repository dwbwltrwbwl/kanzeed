using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using kanzeed.ApplicationData;
using kanzeed.Pages; // чтобы обращаться к AppData напрямую

namespace kanzeed.Services
{
    public static class CartService
    {
        // Добавить товар в корзину (увеличить количество)
        public static void AddToCart(int productId, int quantity = 1)
        {
            if (AppData.CurrentUser == null || AppData.CurrentUser.RoleId != 1)
                throw new InvalidOperationException("Только авторизованный клиент может добавлять товары в корзину.");

            if (quantity <= 0) quantity = 1;

            // Проверка наличия товара в БД
            var product = AppConnect.model01.PRODUCTS.FirstOrDefault(p => p.product_id == productId);
            if (product == null) throw new InvalidOperationException("Товар не найден.");
            if (product.stock_quantity < quantity) throw new InvalidOperationException("Недостаточно товаров на складе.");

            // Добавление / обновление в памяти
            if (AppData.CurrentCart.ContainsKey(productId))
                AppData.CurrentCart[productId] += quantity;
            else
                AppData.CurrentCart[productId] = quantity;
        }

        // Установить количество (в корзине)
        public static void SetQuantity(int productId, int quantity)
        {
            if (AppData.CurrentCart.ContainsKey(productId))
            {
                if (quantity <= 0) AppData.CurrentCart.Remove(productId);
                else AppData.CurrentCart[productId] = quantity;
            }
        }

        // Удалить товар из корзины
        public static void RemoveFromCart(int productId)
        {
            if (AppData.CurrentCart.ContainsKey(productId))
                AppData.CurrentCart.Remove(productId);
        }

        // Получить модель для отображения корзины
        // Получить модель для отображения корзины
        public static List<CartItemViewModel> GetCartItems()
        {
            var result = new List<CartItemViewModel>();
            var productIds = AppData.CurrentCart.Keys.ToList();
            if (!productIds.Any()) return result;

            var products = AppConnect.model01.PRODUCTS.Where(p => productIds.Contains(p.product_id)).ToList();

            foreach (var pid in productIds)
            {
                var product = products.FirstOrDefault(p => p.product_id == pid);
                if (product == null) continue;
                var qty = AppData.CurrentCart[pid];
                result.Add(new CartItemViewModel
                {
                    ProductId = pid,
                    ProductName = product.name,
                    UnitPrice = product.price,
                    Quantity = qty,
                    ProductImage = product.CurrentPhoto // Добавляем фото
                });
            }

            return result;
        }

        // Общее количество товаров (сумма qty)
        public static int GetTotalQuantity()
        {
            return AppData.CurrentCart.Values.Sum();
        }

        // Общая сумма
        public static decimal GetTotalAmount()
        {
            var items = GetCartItems();
            return items.Sum(i => i.LineTotal);
        }

        // Оформление заказа: создаёт запись ORDERS + ORDER_ITEMS и уменьшает запасы.
        // Возвращает созданный order_id
        public static int Checkout(int paymentMethodId = 1)
        {
            if (AppData.CurrentUser == null || AppData.CurrentUser.RoleId != 1)
                throw new InvalidOperationException("Только авторизованный клиент может оформить заказ.");

            var cartItems = GetCartItems();
            if (!cartItems.Any()) throw new InvalidOperationException("Корзина пуста.");

            // Проверки запасов
            foreach (var item in cartItems)
            {
                var product = AppConnect.model01.PRODUCTS.FirstOrDefault(p => p.product_id == item.ProductId);
                if (product == null) throw new InvalidOperationException($"Товар {item.ProductName} не найден.");
                if (product.stock_quantity < item.Quantity) throw new InvalidOperationException($"Недостаточно на складе: {item.ProductName}");
            }

            // Используем транзакцию EF (BeginTransaction)
            using (var transaction = AppConnect.model01.Database.BeginTransaction())
            {
                try
                {
                    // Создать заказ
                    var order = new ORDERS
                    {
                        customer_id = AppData.CurrentUser.Id,
                        order_date = DateTime.Now,
                        total_amount = GetTotalAmount(),
                        status_id = 1, // "Новый"
                        payment_method_id = paymentMethodId
                    };

                    AppConnect.model01.ORDERS.Add(order);
                    AppConnect.model01.SaveChanges();

                    // Добавить позиции
                    foreach (var item in cartItems)
                    {
                        var orderItem = new ORDER_ITEMS
                        {
                            order_id = order.order_id,
                            product_id = item.ProductId,
                            quantity = item.Quantity
                        };
                        AppConnect.model01.ORDER_ITEMS.Add(orderItem);

                        // Уменьшить запас продукта
                        var product = AppConnect.model01.PRODUCTS.First(p => p.product_id == item.ProductId);
                        product.stock_quantity -= item.Quantity;
                    }

                    AppConnect.model01.SaveChanges();
                    transaction.Commit();

                    // Очистить корзину
                    AppData.CurrentCart.Clear();

                    return order.order_id;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
}

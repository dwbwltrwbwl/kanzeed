using kanzeed.ApplicationData;
using System;
using System.Windows.Media.Imaging;
using System.Linq;

namespace kanzeed.Services
{
    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        public decimal LineTotal => UnitPrice * Quantity;

        // Добавляем свойство для фото
        public BitmapImage ProductImage { get; set; }

        // Метод для загрузки фото
        public static BitmapImage GetProductImage(int productId)
        {
            try
            {
                var product = AppConnect.model01.PRODUCTS.FirstOrDefault(p => p.product_id == productId);
                if (product != null)
                {
                    return product.CurrentPhoto; // Используем существующее свойство из PRODUCTS
                }
            }
            catch
            {
                // Игнорируем ошибки загрузки изображения
            }
            return null;
        }
    }
}
namespace kanzeed.ApplicationData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.IO;
    using System.Windows.Media;
    using System.Windows;
    using System.Windows.Media.Imaging;

    public partial class PRODUCTS
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PRODUCTS()
        {
            this.ORDER_ITEMS = new HashSet<ORDER_ITEMS>();
        }

        public int product_id { get; set; }
        public string name { get; set; }
        public string sku { get; set; }
        public string description { get; set; }
        public decimal price { get; set; }
        public int category_id { get; set; }
        public int supplier_id { get; set; }
        public int stock_quantity { get; set; }
        public string image { get; set; }
        public Nullable<decimal> discount { get; set; }

        public decimal OldPrice => price;
        public decimal DiscountPercent => discount ?? 0m;
        public bool HasDiscount => DiscountPercent > 0;
        public decimal PriceWithDiscount =>
            HasDiscount
                ? Math.Round(price * (1 - DiscountPercent / 100m), 2)
                : price;

        [NotMapped]
        public BitmapImage CurrentPhoto
        {
            get
            {
                try
                {
                    // ПЕРВЫЙ ПРИОРИТЕТ: если есть своё изображение в БД
                    if (!string.IsNullOrEmpty(image))
                    {
                        // Преобразуем относительный путь в абсолютный
                        string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, image);

                        // Убираем дублирование "Images/Images" если есть
                        imagePath = imagePath.Replace("Images\\Images", "Images").Replace("Images//Images", "Images");

                        if (File.Exists(imagePath))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                            bitmap.EndInit();
                            return bitmap;
                        }

                        // Если файл не найден по сохранённому пути, ищем только по имени файла в Images
                        string fileName = Path.GetFileName(image);
                        string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", fileName);

                        if (File.Exists(fallbackPath))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(fallbackPath, UriKind.Absolute);
                            bitmap.EndInit();
                            return bitmap;
                        }
                    }

                    // ВТОРОЙ ПРИОРИТЕТ: изображение по умолчанию nofoto.png в папке Images
                    string nofotoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "nofoto.png");

                    if (File.Exists(nofotoPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(nofotoPath, UriKind.Absolute);
                        bitmap.EndInit();
                        return bitmap;
                    }

                    // Если nofoto.png не найден - создаём программную заглушку
                    return CreateFallbackImage();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                    return CreateFallbackImage();
                }
            }
        }

        private BitmapImage CreateFallbackImage()
        {
            try
            {
                // Размеры изображения (80x80 как в вашем XAML)
                int width = 80;
                int height = 80;

                var drawingVisual = new System.Windows.Media.DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    // Светло-серый фон как в дизайне
                    drawingContext.DrawRectangle(
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 249, 250)), // #F8F9FA
                        null,
                        new Rect(0, 0, width, height));

                    // Рамка как в дизайне
                    drawingContext.DrawRectangle(
                        null,
                        new Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(238, 238, 238)), 1), // #EEEEEE
                        new Rect(0, 0, width, height));

                    // Текст "Нет фото" - УВЕЛИЧИВАЕМ и делаем ЖИРНЫМ
                    var text = new FormattedText(
                        "Нет фото",
                        System.Globalization.CultureInfo.CurrentCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new Typeface(new System.Windows.Media.FontFamily("Arial"),
                                    System.Windows.FontStyles.Normal,
                                    System.Windows.FontWeights.Bold, // ЖИРНЫЙ
                                    System.Windows.FontStretches.Normal),
                        16, // Размер шрифта (можно увеличить до 18-20 если помещается)
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136)), // #888888
                        1.0);

                    // Центрируем текст
                    double textWidth = text.Width;
                    double textHeight = text.Height;
                    double x = (width - textWidth) / 2;
                    double y = (height - textHeight) / 2;

                    drawingContext.DrawText(text, new System.Windows.Point(x, y));
                }

                // Рендерим
                var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                renderTarget.Render(drawingVisual);

                var bitmapImage = new BitmapImage();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                }

                return bitmapImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка создания fallback: {ex.Message}");
                return null;
            }
        }

        [NotMapped]
        public bool HasPhoto => !string.IsNullOrEmpty(image);

        public virtual CATEGORIES CATEGORIES { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ORDER_ITEMS> ORDER_ITEMS { get; set; }
        public virtual SUPPLIERS SUPPLIERS { get; set; }
    }
}
namespace AMZN.Models.Product
{
    public class ProductGalleryEditorViewModel
    {
        // Заголовок драгндроп блока галереи в UI
        public string Title { get; set; } = "Gallery images";

        // Имя file-input поля, которое должно попасть в model binding
        // Create -> Form.Images
        // Edit   -> Form.NewGalleryImages
        public string NewFilesInputName { get; set; } = null!;

        // Имя поля, для которого надо показать validation message под компонентом
        // Обычно совпадает с NewFilesInputName
        // Create -> Form.Images
        // Edit   -> Form.NewGalleryImages
        public string? ValidationMessageFieldName { get; set; }


        // Префикс hidden-полей с порядком existing изображений
        // Нужен только в Edit
        public string? ExistingIdsFieldPrefix { get; set; }

        // Уже сохранённые изображения
        // Для Create пустой список
        public List<ExistingProductImageViewModel> ExistingImages { get; set; } = new();

        // Разрешать drag-and-drop сортировку уже существующих изображений
        // Edit -> true || Create -> false
        public bool AllowExistingReorder { get; set; }

        // Режим компонента
        // true  -> Edit || false -> Create
        public bool IsEditMode { get; set; }
    }
}

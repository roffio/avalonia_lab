// Файл: DummyComponentItem.cs (или, например, Models/DummyComponentItem.cs)
// Убедитесь, что namespace соответствует вашему проекту и расположению файла.
// Если корневой namespace проекта 'avalonia_test', то этот код подойдет.
// Если вы создали папку Models, то namespace может быть 'avalonia_test.Models',
// и тогда в XAML нужно будет указать xmlns:local="clr-namespace:avalonia_test.Models"
// и x:DataType="local:DummyComponentItem"

namespace avalonia_test 
{
    public class DummyComponentItem
    {
        public string? Name { get; set; }
        public string? Status { get; set; }
        public string? Value { get; set; }
        public string? Description { get; set; }
    }
}
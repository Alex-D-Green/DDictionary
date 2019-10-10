
namespace DDictionary.Presentation.ViewModels
{
    public sealed class CheckBoxItem<T>
    {
        public bool IsSelected { get; set; }
        public string Text { get; set; }
        public T ItemValue { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}

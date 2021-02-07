using System.Collections.Generic;

namespace MainApp.Dto
{
    public class RelationDto<T>
    {
        public RelationDto() { }
        public RelationDto(T Parent, IEnumerable<T> Childrens)
        {
            this.Parent = Parent;
            this.Childrens = Childrens as List<T>;
        }
        public T Parent { get; set; }
        public List<T> Childrens { get; set; }
    }
}

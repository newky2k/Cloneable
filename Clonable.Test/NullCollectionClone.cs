using LoDaTek.Cloneable.Core;

namespace Cloneable.Sample
{
    [Cloneable]
    public partial class NullCollectionClone
    {
        public string A { get; set; }
        public List<int> Ints { get; set; }
        public List<SimpleClone> Simples { get; set; }
        public List<List<SimpleClone>> Nested { get; set; }
        public Dictionary<int, SimpleClone> Dict { get; set; }
        public SimpleClone[] Arr { get; set; }
        public IEnumerable<SimpleClone> Enumerable { get; set; }
    }
}

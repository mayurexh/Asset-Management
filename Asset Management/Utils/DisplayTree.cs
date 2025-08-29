using Asset_Management.Models;

namespace Asset_Management.Utils
{
    public static class DisplayTree
    {
        public static void Display(Asset root)
        {
            foreach(var child in root.Children)
            {
                Console.WriteLine($"Parent {root.Name}, Child {child.Name}");
                Display(child);
            }
        }
    }
}

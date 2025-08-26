using Asset_Management.Models;

namespace Asset_Management.Utils
{
    public static class PopulateParentIds
    {
        public static void AssignParentIds(Asset node, string? parentId = null)
        {
            if (node == null) return;

            // Set parentId for current node
            node.ParentId = parentId;

            // Recurse into children, passing current node’s Id as their parentId
            foreach (var child in node.Children)
            {
                AssignParentIds(child, node.Id);
            }
        }
    }
}

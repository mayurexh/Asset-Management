using Asset_Management.Interfaces;
using Asset_Management.Models;
using System.Xml;
using System.Xml.Serialization;

namespace Asset_Management.Services
{
    public class XmlAssetStorageService : IAssetStorageService
    {
        private readonly string _datafile;

        public XmlAssetStorageService(IWebHostEnvironment env)
        {
            _datafile = Path.Combine(env.ContentRootPath, "assets.xml");
        }
        public Asset LoadTree()
        {
            if (!File.Exists(_datafile))
            {
                var _root = new Asset { Id = "root", Name = "Root" };
                SaveTree(_root);
                return _root;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(Asset));
            using var reader = new StreamReader(_datafile);
            return (Asset)serializer.Deserialize(reader);

        }

        public void SaveTree(Asset root)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Asset));
            using var writer = new StreamWriter(_datafile);
            serializer.Serialize(writer, root);

        }
        public void DeleteTreeFile()
        {
            if (File.Exists(_datafile))
            {
                File.Delete(_datafile);
            }
        }
    }
}

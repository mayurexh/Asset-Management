using Asset_Management.Interfaces;
using Asset_Management.Models;
using System.Xml;
using System.Xml.Serialization;

namespace Asset_Management.Services
{
    //custom xml reader theat overrides how names are read (to include case insensitivity)
    public class CaseInsensitiveXmlReader : XmlTextReader
    {
        private readonly HashSet<string> _targetElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Asset", "asset" // Add the elements we want to normalize
        };

        public CaseInsensitiveXmlReader(TextReader reader) : base(reader)
        {
        }

        public override string LocalName
        {
            get
            {
                string localName = base.LocalName;
                // Only normalize specific elements
                if (_targetElements.Contains(localName))
                {
                    return "asset"; // Always return lowercase for Asset elements
                }
                return localName;
            }
        }

        public override string Name
        {
            get
            {
                string name = base.Name;
                // Only normalize specific elements  
                if (_targetElements.Contains(name))
                {
                    return "asset"; // Always return lowercase for Asset elements
                }
                return name;
            }
        }

        public override string GetAttribute(string name)
        {
            // Try both original and lowercase versions
            return base.GetAttribute(name) ?? base.GetAttribute(name.ToLowerInvariant());
        }
    }


    public class XmlAssetStorageService : IAssetStorageService
    {
        private readonly string _datafile;

        public XmlAssetStorageService(IWebHostEnvironment env)
        {
            _datafile = Path.Combine(env.ContentRootPath, "assets.xml");
        }
        public Asset ParseTree(string content)
        {
            try
            {
                using var reader = new StringReader(content);
                using var xmlReader = new CaseInsensitiveXmlReader(reader);
                XmlSerializer xml = new XmlSerializer(typeof(Asset));

                bool hasUnexpected = false;

                xml.UnknownAttribute += (sender, e) =>
                {
                    hasUnexpected = true;
                };

                xml.UnknownElement += (sender, e) =>
                {
                    hasUnexpected = true;
                };

                var newRoot = (Asset)xml.Deserialize(xmlReader);


                if (hasUnexpected)
                {
                    throw new InvalidOperationException("Invalid Xml format");
                }
                return newRoot;
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidFileFormatException("Invalid XML file format", ex);
            }
            


        }
        
        public string GetVersionedFileName()
        {
            return "hello";
        }

        public Asset LoadTree()
        {
            if (!File.Exists(_datafile))
            {
                var _root = new Asset { Id = "root", Name = "Root" };
                SaveTree(_root);
                return _root;
            }

            using var reader = new StreamReader(_datafile); //reader the contents

            using var xmlReader = new CaseInsensitiveXmlReader(reader); //pass the reader to custom CaseInsensitiveXmlReader

            XmlSerializer serializer = new XmlSerializer(typeof(Asset));
            return (Asset)serializer.Deserialize(xmlReader);

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

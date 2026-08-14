using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RAGNPCDialogue.Editor
{
    public class LoreDocumentLoader
    {
        private const string LoreFolder = "Assets/RAGNPCDialogue/Lore";

        public List<LoreDocument> LoadDocuments()
        {
            List<LoreDocument> documents = new List<LoreDocument>();
            string absoluteLoreFolder = Path.Combine(Application.dataPath, "RAGNPCDialogue/Lore");

            if (!Directory.Exists(absoluteLoreFolder))
            {
                return documents;
            }

            string[] files = Directory.GetFiles(absoluteLoreFolder, "*.txt", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                string content = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                documents.Add(new LoreDocument
                {
                    sourceFile = Path.GetFileName(file),
                    content = content.Trim()
                });
            }

            return documents;
        }

        public string GetLoreFolder()
        {
            return LoreFolder;
        }
    }
}

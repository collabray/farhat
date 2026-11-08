using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Farhat.Data;

namespace Farhat.EditorTools
{
    /// <summary>
    /// The story gate: a release build (scripting define RELEASE_BUILD) fails while any chapter included in the build is not tagged Verified.
    /// This makes the bible's source tags enforceable: no unchecked story ships.
    /// </summary>
    public class StoryGate : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public static List<string> FindUnverified(ChapterCatalog catalog)
        {
            List<string> bad = new List<string>();
            if (catalog == null) return bad;
            for (int i = 0; i < catalog.chapters.Count; i++)
            {
                ChapterDefinition c = catalog.chapters[i];
                if (c != null && c.includedInBuild && c.sourceTag != SourceTag.Verified)
                {
                    bad.Add("Chapter " + c.number + " (" + c.episodeTitle + ") is tagged " + c.sourceTag + ". Check the episode first.");
                }
            }
            return bad;
        }

        public void OnPreprocessBuild(BuildReport report)
        {
#if RELEASE_BUILD
            List<string> bad = FindUnverified(AssetDatabase.LoadAssetAtPath<ChapterCatalog>(ChapterCatalogImporter.CatalogPath));
            if (bad.Count > 0)
            {
                throw new BuildFailedException("Story gate failed:\n - " + string.Join("\n - ", bad.ToArray()));
            }
#endif
        }

        [MenuItem("Farhat/Content/Story Report")]
        public static void StoryReport()
        {
            ChapterCatalog catalog = AssetDatabase.LoadAssetAtPath<ChapterCatalog>(ChapterCatalogImporter.CatalogPath);
            StringBuilder sb = new StringBuilder("number,title,tag,bibleTag,tier,included\n");
            for (int i = 0; catalog != null && i < catalog.chapters.Count; i++)
            {
                ChapterDefinition c = catalog.chapters[i];
                sb.AppendLine(c.number + ",\"" + c.episodeTitle + "\"," + c.sourceTag + "," + c.bibleTag + "," + c.costTier + "," + c.includedInBuild);
            }
            Debug.Log(sb.ToString());
        }
    }
}

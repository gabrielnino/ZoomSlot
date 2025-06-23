using System.Reflection;
using PdfSharp.Fonts;

namespace Services
{
    public class VerdanaFontResolver : IFontResolver
    {
        public static readonly VerdanaFontResolver Instance = new VerdanaFontResolver();

        public string DefaultFontName => "Verdana";

        public byte[] GetFont(string faceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            switch (faceName)
            {
                case "Verdana#":
                    return LoadFontData(assembly, "Services.Fonts.verdana.ttf");
                case "Verdana-Bold#":
                    return LoadFontData(assembly, "Services.Fonts.verdanab.ttf");
                default:
                    throw new NotImplementedException($"Font face '{faceName}' not implemented.");
            }
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            if (familyName.Equals("Verdana", StringComparison.OrdinalIgnoreCase))
            {
                if (isBold && isItalic) return new FontResolverInfo("Verdana-Bold#"); // Update if you add BoldItalic
                if (isBold) return new FontResolverInfo("Verdana-Bold#");
                if (isItalic) return new FontResolverInfo("Verdana#"); // Update if you add Italic
                return new FontResolverInfo("Verdana#");
            }

            return null;
        }

        private byte[] LoadFontData(Assembly assembly, string resourceName)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) throw new Exception($"Resource '{resourceName}' not found.");
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}

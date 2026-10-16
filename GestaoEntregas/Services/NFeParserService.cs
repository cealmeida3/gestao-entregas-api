using System.Xml.Linq;

namespace GestaoEntregas.API.Services;

public class NFeParserDto
{
    public string ChaveAcesso { get; set; } = string.Empty;
    public string NumeroNota { get; set; } = string.Empty;
    public string EmitenteNome { get; set; } = string.Empty;
    public string DestinatarioNome { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
}

public class NFeParserService
{
    public NFeParserDto ParseXml(Stream xmlStream)
    {
        var doc = XDocument.Load(xmlStream);

        // Namespace padrão da NFe brasileira
        XNamespace ns = "http://www.portalfiscal.inf.br/nfe";

        var infNFe = doc.Descendants(ns + "infNFe").FirstOrDefault()
                     ?? doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "infNFe");

        if (infNFe == null)
            throw new Exception("Tag infNFe não encontrada no XML. Verifique se o arquivo enviado é uma NFe válida.");

        var chNFe = infNFe.Attribute("Id")?.Value?.Replace("NFe", "") ?? string.Empty;

        var ide = infNFe.Element(ns + "ide") ?? infNFe.Elements().FirstOrDefault(e => e.Name.LocalName == "ide");
        var nNF = ide?.Element(ns + "nNF")?.Value ?? ide?.Elements().FirstOrDefault(e => e.Name.LocalName == "nNF")?.Value ?? "";

        var emit = infNFe.Element(ns + "emit") ?? infNFe.Elements().FirstOrDefault(e => e.Name.LocalName == "emit");
        var xNomeEmit = emit?.Element(ns + "xNome")?.Value ?? emit?.Elements().FirstOrDefault(e => e.Name.LocalName == "xNome")?.Value ?? "";

        var dest = infNFe.Element(ns + "dest") ?? infNFe.Elements().FirstOrDefault(e => e.Name.LocalName == "dest");
        var xNomeDest = dest?.Element(ns + "xNome")?.Value ?? dest?.Elements().FirstOrDefault(e => e.Name.LocalName == "xNome")?.Value ?? "";

        var total = infNFe.Element(ns + "total") ?? infNFe.Elements().FirstOrDefault(e => e.Name.LocalName == "total");
        var ICMSTot = total?.Element(ns + "ICMSTot") ?? total?.Elements().FirstOrDefault(e => e.Name.LocalName == "ICMSTot");
        var vNFStr = ICMSTot?.Element(ns + "vNF")?.Value ?? ICMSTot?.Elements().FirstOrDefault(e => e.Name.LocalName == "vNF")?.Value ?? "0";

        decimal.TryParse(vNFStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var valorTotal);

        return new NFeParserDto
        {
            ChaveAcesso = chNFe,
            NumeroNota = nNF,
            EmitenteNome = xNomeEmit,
            DestinatarioNome = xNomeDest,
            ValorTotal = valorTotal
        };
    }
}
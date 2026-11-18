using System.Globalization;
using System.Text;

namespace AuraScent.Services;

public static class NoteTranslator
{
    private static readonly Dictionary<string, string> Notes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bergamot"] = "Bergamota", ["calabrian bergamot"] = "Bergamota da Calábria",
        ["lemon"] = "Limão-siciliano", ["sicilian lemon"] = "Limão-siciliano", ["lime"] = "Limão",
        ["orange"] = "Laranja", ["sweet orange"] = "Laranja doce", ["bitter orange"] = "Laranja amarga",
        ["blood orange"] = "Laranja sanguínea", ["mandarin"] = "Tangerina", ["mandarin orange"] = "Tangerina",
        ["tangerine"] = "Tangerina", ["grapefruit"] = "Toranja", ["pink grapefruit"] = "Toranja rosa",
        ["yuzu"] = "Yuzu", ["citron"] = "Cidra", ["neroli"] = "Neroli", ["petitgrain"] = "Petitgrain",
        ["orange blossom"] = "Flor de laranjeira", ["citruses"] = "Cítricos", ["citrus"] = "Cítricos",
        ["lemon verbena"] = "Verbena-limão", ["verbena"] = "Verbena", ["lemongrass"] = "Capim-limão",

        ["apple"] = "Maçã", ["green apple"] = "Maçã verde", ["pear"] = "Pera", ["peach"] = "Pêssego",
        ["apricot"] = "Damasco", ["plum"] = "Ameixa", ["cherry"] = "Cereja", ["black cherry"] = "Cereja negra",
        ["raspberry"] = "Framboesa", ["strawberry"] = "Morango", ["blackberry"] = "Amora",
        ["black currant"] = "Cassis", ["blackcurrant"] = "Cassis", ["cassis"] = "Cassis",
        ["red berries"] = "Frutas vermelhas", ["berries"] = "Frutas vermelhas", ["fig"] = "Figo",
        ["fig leaf"] = "Folha de figueira", ["pineapple"] = "Abacaxi", ["coconut"] = "Coco",
        ["mango"] = "Manga", ["passionfruit"] = "Maracujá", ["passion fruit"] = "Maracujá",
        ["lychee"] = "Lichia", ["litchi"] = "Lichia", ["melon"] = "Melão", ["watermelon"] = "Melancia",
        ["pomegranate"] = "Romã", ["quince"] = "Marmelo", ["date"] = "Tâmara", ["dates"] = "Tâmaras",
        ["banana"] = "Banana", ["grape"] = "Uva", ["rhubarb"] = "Ruibarbo", ["fruity notes"] = "Notas frutadas",
        ["fruits"] = "Frutas",

        ["rose"] = "Rosa", ["damask rose"] = "Rosa damascena", ["bulgarian rose"] = "Rosa búlgara",
        ["turkish rose"] = "Rosa turca", ["may rose"] = "Rosa de maio", ["rose de mai"] = "Rosa de maio",
        ["jasmine"] = "Jasmim", ["sambac jasmine"] = "Jasmim sambac", ["jasmine sambac"] = "Jasmim sambac",
        ["tuberose"] = "Tuberosa", ["iris"] = "Íris", ["orris"] = "Íris", ["orris root"] = "Raiz de íris",
        ["violet"] = "Violeta", ["violet leaf"] = "Folha de violeta", ["lily"] = "Lírio",
        ["lily of the valley"] = "Lírio-do-vale", ["lily-of-the-valley"] = "Lírio-do-vale",
        ["peony"] = "Peônia", ["magnolia"] = "Magnólia", ["gardenia"] = "Gardênia", ["freesia"] = "Frésia",
        ["ylang-ylang"] = "Ylang-ylang", ["ylang ylang"] = "Ylang-ylang", ["lavender"] = "Lavanda",
        ["geranium"] = "Gerânio", ["heliotrope"] = "Heliotrópio", ["carnation"] = "Cravo (flor)",
        ["mimosa"] = "Mimosa", ["osmanthus"] = "Osmanthus", ["honeysuckle"] = "Madressilva",
        ["lotus"] = "Lótus", ["water lily"] = "Ninfeia", ["orchid"] = "Orquídea", ["hyacinth"] = "Jacinto",
        ["narcissus"] = "Narciso", ["daffodil"] = "Narciso", ["chamomile"] = "Camomila",
        ["cyclamen"] = "Cíclame", ["lilac"] = "Lilás", ["frangipani"] = "Frangipani", ["tiare flower"] = "Flor de tiaré",
        ["white flowers"] = "Flores brancas", ["floral notes"] = "Notas florais", ["flowers"] = "Flores",
        ["immortelle"] = "Imortelle", ["cherry blossom"] = "Flor de cerejeira", ["acacia"] = "Acácia",

        ["pink pepper"] = "Pimenta-rosa", ["black pepper"] = "Pimenta-preta", ["pepper"] = "Pimenta",
        ["cardamom"] = "Cardamomo", ["cinnamon"] = "Canela", ["clove"] = "Cravo-da-índia",
        ["cloves"] = "Cravo-da-índia", ["nutmeg"] = "Noz-moscada", ["ginger"] = "Gengibre",
        ["saffron"] = "Açafrão", ["cumin"] = "Cominho", ["coriander"] = "Coentro", ["anise"] = "Anis",
        ["star anise"] = "Anis-estrelado", ["licorice"] = "Alcaçuz", ["liquorice"] = "Alcaçuz",
        ["juniper"] = "Zimbro", ["juniper berries"] = "Bagas de zimbro", ["elemi"] = "Elemi",
        ["spices"] = "Especiarias", ["spicy notes"] = "Notas especiadas", ["basil"] = "Manjericão",
        ["mint"] = "Menta", ["peppermint"] = "Hortelã-pimenta", ["spearmint"] = "Hortelã",
        ["rosemary"] = "Alecrim", ["sage"] = "Sálvia", ["clary sage"] = "Sálvia esclareia",
        ["thyme"] = "Tomilho", ["artemisia"] = "Artemísia", ["tarragon"] = "Estragão",
        ["bay leaf"] = "Louro", ["laurel"] = "Louro", ["galbanum"] = "Gálbano", ["tea"] = "Chá",
        ["green tea"] = "Chá verde", ["black tea"] = "Chá preto", ["mate"] = "Mate", ["fennel"] = "Erva-doce",
        ["aromatic notes"] = "Notas aromáticas", ["herbs"] = "Ervas", ["green notes"] = "Notas verdes",
        ["grass"] = "Grama", ["ivy"] = "Hera", ["tomato leaf"] = "Folha de tomate",

        ["sandalwood"] = "Sândalo", ["cedar"] = "Cedro", ["cedarwood"] = "Cedro", ["atlas cedar"] = "Cedro do Atlas",
        ["virginia cedar"] = "Cedro da Virgínia", ["vetiver"] = "Vetiver", ["patchouli"] = "Patchouli",
        ["oud"] = "Oud", ["agarwood"] = "Oud", ["agarwood (oud)"] = "Oud", ["guaiac wood"] = "Madeira de guaiaco",
        ["birch"] = "Bétula", ["cypress"] = "Cipreste", ["pine"] = "Pinho", ["fir"] = "Abeto",
        ["oakmoss"] = "Musgo de carvalho", ["oak moss"] = "Musgo de carvalho", ["moss"] = "Musgo",
        ["cashmeran"] = "Cashmeran", ["cashmere wood"] = "Madeira de caxemira", ["ebony"] = "Ébano",
        ["rosewood"] = "Pau-rosa", ["teak"] = "Teca", ["oak"] = "Carvalho", ["papyrus"] = "Papiro",
        ["woody notes"] = "Notas amadeiradas", ["woods"] = "Madeiras", ["wood"] = "Madeira",
        ["driftwood"] = "Madeira flutuante", ["cypriol"] = "Cypriol", ["haitian vetiver"] = "Vetiver do Haiti",

        ["amber"] = "Âmbar", ["ambergris"] = "Âmbar cinza", ["ambroxan"] = "Ambroxan", ["labdanum"] = "Ládano",
        ["benzoin"] = "Benjoim", ["incense"] = "Incenso", ["frankincense"] = "Olíbano", ["olibanum"] = "Olíbano",
        ["myrrh"] = "Mirra", ["opoponax"] = "Opopânax", ["tolu balsam"] = "Bálsamo-de-tolu",
        ["peru balsam"] = "Bálsamo-do-peru", ["styrax"] = "Estoraque", ["resins"] = "Resinas",
        ["balsamic notes"] = "Notas balsâmicas",

        ["vanilla"] = "Baunilha", ["madagascar vanilla"] = "Baunilha de Madagascar", ["bourbon vanilla"] = "Baunilha bourbon",
        ["tonka bean"] = "Fava tonka", ["tonka"] = "Fava tonka", ["caramel"] = "Caramelo", ["honey"] = "Mel",
        ["chocolate"] = "Chocolate", ["cacao"] = "Cacau", ["cocoa"] = "Cacau", ["coffee"] = "Café",
        ["praline"] = "Praliné", ["almond"] = "Amêndoa", ["bitter almond"] = "Amêndoa amarga",
        ["hazelnut"] = "Avelã", ["pistachio"] = "Pistache", ["chestnut"] = "Castanha", ["sugar"] = "Açúcar",
        ["brown sugar"] = "Açúcar mascavo", ["cotton candy"] = "Algodão-doce", ["milk"] = "Leite",
        ["cream"] = "Creme", ["whipped cream"] = "Chantilly", ["rum"] = "Rum", ["cognac"] = "Conhaque",
        ["whiskey"] = "Uísque", ["whisky"] = "Uísque", ["wine"] = "Vinho", ["champagne"] = "Champanhe",
        ["gin"] = "Gim", ["meringue"] = "Merengue", ["marshmallow"] = "Marshmallow", ["bread"] = "Pão",
        ["rice"] = "Arroz", ["gourmand notes"] = "Notas gourmand",

        ["musk"] = "Almíscar", ["white musk"] = "Almíscar branco", ["musks"] = "Almíscares",
        ["leather"] = "Couro", ["suede"] = "Camurça", ["tobacco"] = "Tabaco", ["tobacco leaf"] = "Folha de tabaco",
        ["civet"] = "Civeta", ["castoreum"] = "Castóreo", ["animalic notes"] = "Notas animálicas",
        ["powdery notes"] = "Notas atalcadas", ["iso e super"] = "Iso E Super", ["aldehydes"] = "Aldeídos",
        ["sea notes"] = "Notas marinhas", ["marine notes"] = "Notas marinhas", ["sea salt"] = "Sal marinho",
        ["salt"] = "Sal", ["water notes"] = "Notas aquáticas", ["aquatic notes"] = "Notas aquáticas",
        ["ozonic notes"] = "Notas ozônicas", ["ambrette"] = "Ambrette", ["ambrette seed"] = "Semente de ambrette",
        ["smoke"] = "Fumaça", ["earthy notes"] = "Notas terrosas", ["metallic notes"] = "Notas metálicas",
        ["mineral notes"] = "Notas minerais", ["calone"] = "Calone", ["seaweed"] = "Alga marinha",
        ["hedione"] = "Hedione", ["violet accord"] = "Acorde de violeta",

        ["woody"] = "Amadeirado", ["floral"] = "Floral", ["fresh"] = "Fresco", ["fruity"] = "Frutado",
        ["sweet"] = "Doce", ["spicy"] = "Especiado", ["warm spicy"] = "Especiado quente",
        ["fresh spicy"] = "Especiado fresco", ["aromatic"] = "Aromático", ["powdery"] = "Atalcado",
        ["green"] = "Verde", ["aquatic"] = "Aquático", ["marine"] = "Marinho", ["ozonic"] = "Ozônico",
        ["balsamic"] = "Balsâmico", ["earthy"] = "Terroso", ["smoky"] = "Defumado", ["animalic"] = "Animálico",
        ["musky"] = "Almiscarado", ["leathery"] = "Couro", ["gourmand"] = "Gourmand", ["oriental"] = "Oriental",
        ["white floral"] = "Floral branco", ["yellow floral"] = "Floral amarelo", ["rosy"] = "Rosado",
        ["citrusy"] = "Cítrico", ["herbal"] = "Herbal", ["tropical"] = "Tropical", ["mossy"] = "Musgoso",
        ["resinous"] = "Resinoso", ["soft spicy"] = "Especiado suave", ["lactonic"] = "Lactônico",
        ["boozy"] = "Alcoólico", ["metallic"] = "Metálico", ["mineral"] = "Mineral", ["salty"] = "Salgado",
        ["chypre"] = "Chipre", ["fougere"] = "Fougère", ["fougère"] = "Fougère", ["ambery"] = "Ambarado",
        ["nutty"] = "Amendoado", ["coffee accord"] = "Café", ["vanilla accord"] = "Baunilha",
    };

    public static string? TryTranslate(string note)
    {
        if (string.IsNullOrWhiteSpace(note)) return null;
        var key = Normalize(note);
        return Notes.TryGetValue(key, out var pt) ? pt : null;
    }

    public static string Translate(string note) => TryTranslate(note) ?? Capitalize(note.Trim());

    private static readonly Lazy<Dictionary<string, string>> ReverseNotes = new(() =>
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (en, pt) in Notes)
        {
            var ptKey = RemoveDiacritics(Normalize(pt));
            if (!map.TryGetValue(ptKey, out var existing) || en.Length < existing.Length)
                map[ptKey] = en;
        }
        return map;
    });

    public static string ToEnglishSearchTerm(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return query ?? string.Empty;
        var trimmed = query.Trim();
        var key = RemoveDiacritics(Normalize(trimmed));
        return ReverseNotes.Value.TryGetValue(key, out var en) ? en : trimmed;
    }

    public static bool ContainsIgnoringDiacritics(string source, string value)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(value)) return false;
        return RemoveDiacritics(source).Contains(RemoveDiacritics(value), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string note)
    {
        var s = note.Trim().ToLowerInvariant();
        return string.Join(' ', s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], CultureInfo.GetCultureInfo("pt-BR")) + s[1..];
}

using System.Collections.Generic;

namespace RozgrywkaKoncowa.Resources
{
    public static class Strings
    {
        private static readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            ["pl"] = new Dictionary<string, string>
            {
                // Wspólne
                ["AppTitle"] = "Rozgrywka Końcowa",
                ["Calculate"] = "Oblicz",
                ["Calculating"] = "Obliczam...",

                // Menu
                ["MenuHome"] = "Strona główna",
                ["MenuStrategyEval"] = "Ewaluacja strategii NS",
                ["MenuStrategyTest"] = "Test strategii",

                // Formularz ewaluacji
                ["FormCardsNorth"] = "Karty N (piki, np. AQT lub A10)",
                ["FormCardsSouth"] = "Karty S (piki, np. 234)",
                ["FormTargetTricks"] = "Oczekiwana liczba lew",
                ["PlaceholderNorth"] = "np. AQT, AKJ, A10",
                ["PlaceholderSouth"] = "np. 234, KQJ, 10987",

                // Walidacja
                ["ErrorInvalidCards"] = "Niepoprawne karty. Użyj: A, K, Q, J, T (lub 10), 9-2",
                ["ErrorEmptyField"] = "Pole nie może być puste",
                ["ErrorDuplicatesNorth"] = "Karty się powtarzają u North",
                ["ErrorDuplicatesSouth"] = "Karty się powtarzają u South",
                ["ErrorDuplicatesNS"] = "Karty powtarzają się między North i South",
                ["ErrorInvalidCardsServer"] = "Podaj poprawne karty dla N i S (np. AQT lub A10, 234). Dozwolone: A, K, Q, J, T (lub 10), 9-2.",
                ["ErrorDuplicatesNorthServer"] = "Karty North się powtarzają. Każda karta może wystąpić tylko raz.",
                ["ErrorDuplicatesSouthServer"] = "Karty South się powtarzają. Każda karta może wystąpić tylko raz.",
                ["ErrorDuplicatesNSServer"] = "Karty powtarzają się między North i South: {0}",
                ["ErrorTargetTricks"] = "Wprowadź liczbę od 1 wzwyż",

                // Wyniki
                ["Deal"] = "Rozdanie",
                ["SortCriterion"] = "Kryterium sortowania: największa szansa na wzięcie",
                ["AtLeastTricks"] = "co najmniej {0}",
                ["Tricks"] = "lew",
                ["Trick"] = "lewę",
                ["BestStrategy"] = "Najlepsza strategia",
                ["ExpectedTricks"] = "Oczekiwana liczba lew",
                ["ChanceAtLeast"] = "Szansa na co najmniej {0}",
                ["ChanceExactly"] = "Szansa na dokładnie {0}",
                ["ControlSum"] = "Suma kontrolna",
                ["NoData"] = "Brak danych do wyświetlenia",
                ["Strategy"] = "Strategia",
                ["Details"] = "Szczegóły",
                ["WestDist"] = "Rozdanie W",
                ["EastDist"] = "Rozdanie E",
                ["Probability"] = "Prawdopodobieństwo",
                ["Result"] = "Wynik",

                // Kompresja strategii
                ["CompressBySequences"] = "Kompresuj według sekwensów",
                ["ExpandGroup"] = "Rozwiń grupę",
                ["CollapseGroup"] = "Zwiń grupę",
                ["Variants"] = "wariantów",
            },
            ["en"] = new Dictionary<string, string>
            {
                // Common
                ["AppTitle"] = "End Game",
                ["Calculate"] = "Calculate",
                ["Calculating"] = "Calculating...",

                // Menu
                ["MenuHome"] = "Home",
                ["MenuStrategyEval"] = "NS Strategy Evaluation",
                ["MenuStrategyTest"] = "Strategy Test",

                // Evaluation form
                ["FormCardsNorth"] = "N cards (spades, e.g., AQT or A10)",
                ["FormCardsSouth"] = "S cards (spades, e.g., 234)",
                ["FormTargetTricks"] = "Target number of tricks",
                ["PlaceholderNorth"] = "e.g., AQT, AKJ, A10",
                ["PlaceholderSouth"] = "e.g., 234, KQJ, 10987",

                // Validation
                ["ErrorInvalidCards"] = "Invalid cards. Use: A, K, Q, J, T (or 10), 9-2",
                ["ErrorEmptyField"] = "Field cannot be empty",
                ["ErrorDuplicatesNorth"] = "North cards contain duplicates",
                ["ErrorDuplicatesSouth"] = "South cards contain duplicates",
                ["ErrorDuplicatesNS"] = "Cards duplicate between North and South",
                ["ErrorInvalidCardsServer"] = "Please provide valid cards for N and S (e.g., AQT or A10, 234). Allowed: A, K, Q, J, T (or 10), 9-2.",
                ["ErrorDuplicatesNorthServer"] = "North cards contain duplicates. Each card can appear only once.",
                ["ErrorDuplicatesSouthServer"] = "South cards contain duplicates. Each card can appear only once.",
                ["ErrorDuplicatesNSServer"] = "Cards duplicate between North and South: {0}",
                ["ErrorTargetTricks"] = "Enter a number from 1 onwards",

                // Results
                ["Deal"] = "Deal",
                ["SortCriterion"] = "Sort criterion: highest chance of taking",
                ["AtLeastTricks"] = "at least {0}",
                ["Tricks"] = "tricks",
                ["Trick"] = "trick",
                ["BestStrategy"] = "Best strategy",
                ["ExpectedTricks"] = "Expected number of tricks",
                ["ChanceAtLeast"] = "Chance for at least {0}",
                ["ChanceExactly"] = "Chance for exactly {0}",
                ["ControlSum"] = "Control sum",
                ["NoData"] = "No data to display",
                ["Strategy"] = "Strategy",
                ["Details"] = "Details",
                ["WestDist"] = "West distribution",
                ["EastDist"] = "East distribution",
                ["Probability"] = "Probability",
                ["Result"] = "Result",

                // Strategy compression
                ["CompressBySequences"] = "Compress by sequences",
                ["ExpandGroup"] = "Expand group",
                ["CollapseGroup"] = "Collapse group",
                ["Variants"] = "variants",
            }
        };

        private static string _currentLanguage = "pl";

        public static void SetLanguage(string language)
        {
            if (_translations.ContainsKey(language))
                _currentLanguage = language;
        }

        public static string Get(string key)
        {
            if (_translations.TryGetValue(_currentLanguage, out var lang) && 
                lang.TryGetValue(key, out var value))
                return value;
            return key; // Fallback to key if translation not found
        }

        public static string Get(string key, params object[] args)
        {
            var template = Get(key);
            return string.Format(template, args);
        }
    }
}

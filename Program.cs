﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HtmlAgilityPack;
using System.IO;
using System.IO.Compression;
using System.Drawing;
using System.Text.RegularExpressions;

public class Card
{
    public string Name, Cost, Img, Type, NumberOfSet, Stats, Rarity, NameENG, stylesheet, FileImg, Color, 
                    RuleText, FlavorText, loyalty, Illustrator, Power, Toughness, TextBoxes, NumberCoordinates, DividerCoordinates, SpecialText;
    public Int32 Number;
    public List<string> LoyaltyCost = new List<string>();
    public List<string> LevelText = new List<string>();

    static void PrintNotNull(String str, String preStr) { if ((str != "")&&!(str is null)) Console.WriteLine(preStr + ": " + str); }

    public void Print()
    {
        PrintNotNull(Name, "Имя");
        PrintNotNull(NameENG, "Имя (eng)");
        PrintNotNull(Cost, "Стоимость");
        PrintNotNull(Img, "Изображение");
        PrintNotNull(FileImg, "Файл изображения");
        PrintNotNull(Type, "Тип");
        PrintNotNull(NumberOfSet, "Номер в сете");
        PrintNotNull(Stats, "Статс");
        PrintNotNull(Rarity, "Раритетность");
        PrintNotNull(stylesheet, "Стиль");
        PrintNotNull(Color, "Цвет");
        PrintNotNull(RuleText, "Правила");
        PrintNotNull(FlavorText, "Художественный текст");
        PrintNotNull(loyalty, "Лояльность");
        PrintNotNull(Illustrator, "Иллюстратор");
        PrintNotNull(Power, "Сила");
        PrintNotNull(Toughness, "Выносливость");
        PrintNotNull(TextBoxes, "Текстбокс");        
        PrintNotNull(NumberCoordinates, "Координаты номера О.о");        
        PrintNotNull(DividerCoordinates, "Координаты Дивидера о.О");        
        PrintNotNull(SpecialText, "Специальный текст");
        foreach (string x in LoyaltyCost) PrintNotNull(x, "Цена лояльности");
        foreach (string x in LevelText) PrintNotNull(x, "Текст уровня");
    }
}

public class Set
{
    /// <summary>
    /// Код сэта
    /// </summary>
    public String setCode;
    /// <summary>
    /// Ссылка на изображения карт на https://magic.wizards.com/
    /// </summary>
    public String urlWizards;
    /// <summary>
    /// Количество карт в сете
    /// </summary>
    public Int16 maxCardInSet;
    /// <summary>
    /// Инвертировать символ common в белый?
    /// </summary>
    public String invertedCommonSymbol = "yes";
    /// <summary>
    /// Список карт сета
    /// </summary>
    public List<Card> cards = new List<Card>();
    /// <summary>
    /// Вывести в консоль информацию по сету
    /// </summary>
    public void Print()
    {
        Console.WriteLine("Код сета: " + setCode);
        foreach (Card card in cards)
        {
            Console.WriteLine();
            card.Print();
        }
    }
}

namespace mtg
{
    class Program
    {
        public static List<String> GetUrlCardImage(string url, string rusName)
        {
            WebClient WZ = new WebClient();
            HtmlDocument htmlDocumentWZ = new HtmlDocument();
            var urlCardImage = new List<String>();
            htmlDocumentWZ.LoadHtml(WZ.DownloadString(url));

            foreach (HtmlNode x in htmlDocumentWZ.DocumentNode.SelectNodes("//img"))
                if (WebUtility.HtmlDecode(x.GetAttributeValue("alt", "")) == rusName)
                    urlCardImage.Add(WebUtility.HtmlDecode(x.GetAttributeValue("src", "")));

            return urlCardImage;
        }
        
        public static String GetNumberOfSet(int number, int maxSetCard)
        {
            if (number <= 9)
                return "00" + number + "/" + maxSetCard;
            else if (number <= 99)
                return "0" + number + "/" + maxSetCard;
            else
                return number + "/" + maxSetCard;
        }
        
        public static Card GetCard(Set set, String path, Int32 number)
        {
            Card card = new Card();

            string urlCardMtgRu = "http://www.mtg.ru/cards/search.phtml?Grp=" + set.setCode + "&Number=";
            WebClient mtgWebClient = new WebClient();
            HtmlDocument htmlDocumentMTG = new HtmlDocument();
            htmlDocumentMTG.LoadHtml(mtgWebClient.DownloadString(urlCardMtgRu + number));

            // Собираем базовые данные карты
            try
            {
                var N = htmlDocumentMTG.DocumentNode.SelectSingleNode("//table[@class='NoteDiv U shadow']" + "//div[@class='SearchCardInfoDIV shadow']" + "//h2").InnerText;
                card.Name = (N.Substring(N.IndexOf("//") + 3, N.Length - N.IndexOf("//") - 3)).Trim(new Char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' }).Trim();
                card.NameENG = (N.Substring(0, N.IndexOf("//"))).Trim().Trim(new Char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' }).Trim();

                card.Number = number;
                card.NumberOfSet = GetNumberOfSet(number, set.maxCardInSet);

                Console.WriteLine("Обрабатывается карта: " + card.Name + " | " + card.NumberOfSet);
            } catch { Console.WriteLine(1); }

            // Получаем тип карты
            try
            {
                card.Type = (htmlDocumentMTG.DocumentNode.SelectSingleNode("//table[@class='NoteDiv U shadow']" + "//div[@class='SearchCardInfoDIV shadow']" + "//span[@class='rus']").InnerText).Substring(2);
            } catch { Console.WriteLine("Не разобрал тип"); }

            // Получаем иллюстратора
            try
            { 
                card.Illustrator = htmlDocumentMTG.DocumentNode.SelectSingleNode("//table[@class='NoteDiv U shadow']" + "//div[@class='SearchCardInfoDIVsub shadow']//table[@class='NoteDivWidth']//tr//td").InnerText.Split(':')[1].Trim();
            } catch { Console.WriteLine("Не разобрал иллюстратора"); }

            // Получаем раритетность
            try
            {
                var mtgRuRarity = htmlDocumentMTG.DocumentNode.SelectNodes("//table[@class='NoteDiv U shadow']" + "//div[@class='SearchCardInfoDIV shadow']")[4];
                if (mtgRuRarity.InnerText.Trim() == "Редкость:")
                    card.Rarity = "common";
                else if (!mtgRuRarity.InnerText.Contains("Базовая земля"))
                    switch (mtgRuRarity.InnerText.Split('-')[1].Trim())
                    {
                        case ("Необычная"):
                            card.Rarity = "uncommon";
                            break;
                        case ("Редкая"):
                            card.Rarity = "rare";
                            break;
                        case ("Раритетная"):
                            card.Rarity = "mythic rare";
                            break;
                        default:
                            card.Rarity = "common";
                            break;
                    }
                else
                    card.Rarity = "common";
            } catch { Console.WriteLine("Не обработал редкость"); }

            // Формируем стоимость
            try
            {
                if (htmlDocumentMTG.DocumentNode.SelectNodes("//table[@class='NoteDiv U shadow']" + "//div[@class='SearchCardInfoDIV shadow']" + "//img[@class='Mana']") != null)
                {
                    foreach (HtmlNode c in htmlDocumentMTG.DocumentNode.SelectNodes("//table[@class='NoteDiv U shadow']" + "//div[@class='SearchCardInfoDIV shadow']" + "//img[@class='Mana']"))
                    {
                        var mana = c.GetAttributeValue("alt", "нету")[0].ToString();
                        if (c.GetAttributeValue("alt", "нету").Length > 1)
                            for (var i = 1; i < c.GetAttributeValue("alt", "не").Length; i++)
                                mana += "/" + c.GetAttributeValue("alt", "не")[i];
                        card.Cost += mana;
                    }
                }
                else
                    card.Cost = ""; 
            } catch { Console.WriteLine("Не обработал стоимость"); }
                        
            // Проверяем изображение и, при необходимости, скачиваем
            try
            {
                var urlCardImage = GetUrlCardImage(set.urlWizards, card.Name);
                if (!File.Exists(path + set.setCode + @"\" + number + ".jpg"))
                {
                    if (urlCardImage.Count == 1)
                        card.Img = urlCardImage[0];
                    else if (urlCardImage.Count > 1)
                    {
                        var count = 0;
                        foreach (var uc in urlCardImage)
                        {
                            WebClient myWebClient = new WebClient();
                            var img = new Bitmap(myWebClient.OpenRead(uc));
                            img.Clone(new Rectangle(21, 42, 223, 164), img.PixelFormat).Save(path + set.setCode + "/" + (number + count) + ".jpg");
                            count++;
                        }
                        card.Img = "";
                        card.FileImg = number + ".jpg";
                    }

                }
                else card.Img = "";
                               
                if (card.Img != "")
                {
                    WebClient myWebClient = new WebClient();
                    var img = new Bitmap(myWebClient.OpenRead(card.Img));
                    if (!card.Type.Contains("Сага"))
                    {
                        img.Clone(new Rectangle(21, 42, 223, 164), img.PixelFormat).Save(path + set.setCode + "/" + number + ".jpg");
                        card.FileImg = number + ".jpg";
                    }
                    else
                    {
                        img.Clone(new Rectangle(132, 42, 111, 268), img.PixelFormat).Save(path + set.setCode + "/" + number + ".jpg");
                        card.FileImg = number + ".jpg";
                    }
                }
                else card.FileImg = number + ".jpg";
            } catch { Console.WriteLine("Не обработалось изображение"); }
            
            // Выбираем шаблон и данные, специфичные для типов
            try
            { 
                if (card.Type.Contains("Сага")) //если Сага
                {
                    card.stylesheet = "m15-saga";
                    card.RuleText = "<i-auto>(При выходе этой Саги и после вашего шага взятия карты добавьте один жетон знаний. Пожертвуйте после III.)</i-auto>";
                    card.FlavorText = null;

                    string str = htmlDocumentMTG.DocumentNode.SelectSingleNode("//div[@class='SearchCardInfoText']//span[@class='rus']").InnerHtml.Trim();

                    switch (Regex.Split(str, "<p>").Count() - 1) // выбираем количество уровней саги
                    {
                        case 2:
                            card.TextBoxes = "two";
                            card.NumberCoordinates = "183,223,329,";
                            card.DividerCoordinates = "296,";
                            break;
                        case 3:
                            card.TextBoxes = "three";
                            card.NumberCoordinates = "185,279,373,";
                            card.DividerCoordinates = "249,343,";
                            break;
                        case 4:
                            card.TextBoxes = "four";
                            card.NumberCoordinates = "185,279,373,450,";
                            card.DividerCoordinates = "225,296,367,";
                            break;
                    }

                    card.SpecialText = Environment.NewLine + "\t\t" + card.RuleText;

                    string LevelText = htmlDocumentMTG.DocumentNode.SelectSingleNode("//div[@class='SearchCardInfoText']//span[@class='rus']").InnerHtml.Trim();
                    for (int k = 1; k < Regex.Split(str, "<p>").Count(); k++)
                    {
                        card.LevelText.Add(Regex.Split(Regex.Split(LevelText, "<p>")[k], " — ")[1]);
                        card.SpecialText = card.SpecialText + Environment.NewLine + "\t\t" + Regex.Split(LevelText, "<p>")[k];
                    }
                }
                else if (card.Type.Contains("Planeswalker")) // Если Мироходец
                {
                    card.stylesheet = "m15-planeswalker";
                    card.loyalty = htmlDocumentMTG.DocumentNode.SelectNodes("//table[@class='NoteDiv U shadow']" + "//div[@class='SearchCardInfoDIV shadow']")[3].InnerText.Split(':')[1].Trim();

                    if (htmlDocumentMTG.DocumentNode.SelectSingleNode("//div[@class='SearchCardInfoText']//span[@class='rus']") != null)
                    {
                        card.RuleText = "";
                        string str = htmlDocumentMTG.DocumentNode.SelectSingleNode("//div[@class='SearchCardInfoText']//span[@class='rus']").InnerHtml.Trim();

                        for (int k = 0; k < Regex.Split(str, "<p>").Count(); k++)
                        {
                            if (k == 0)
                            {
                                card.RuleText += Regex.Split(Regex.Split(str, "<p>")[k], ": ")[1];
                            }
                            else
                            {
                                if (Regex.Split(Regex.Split(str, "<p>")[k], ": ").Count() > 0)
                                {
                                    card.RuleText += Environment.NewLine + "\t\t" + Regex.Split(Regex.Split(str, "<p>")[k], ": ")[1];
                                }
                            }
                            card.LoyaltyCost.Add(Regex.Split(Regex.Split(str, "<p>")[k], ": ")[0]);
                        }
                        card.FlavorText = null;
                    }
                }
                else // для всех остальных
                {
                    if (htmlDocumentMTG.DocumentNode.SelectSingleNode("//div[@class='SearchCardInfoText']//span[@class='rus']") != null)
                    {
                        card.RuleText = htmlDocumentMTG.DocumentNode.SelectSingleNode("//div[@class='SearchCardInfoText']//span[@class='rus']").InnerHtml;
                        foreach (Match rt in Regex.Matches(card.RuleText, "<img src=\".*? class=\"Mana\">"))
                        {
                            HtmlDocument a = new HtmlDocument();
                            a.LoadHtml(rt + "");
                            if (a.DocumentNode.SelectSingleNode("//img").GetAttributeValue("alt", "") == "TAP")
                            {
                                card.RuleText = card.RuleText.Replace(rt + "", "<sym>T</sym>");
                            }
                            else
                            {
                                if (a.DocumentNode.SelectSingleNode("//img").GetAttributeValue("alt", "").Length == 1)
                                    card.RuleText = card.RuleText.Replace(rt + "", "<sym>" + a.DocumentNode.SelectSingleNode("//img").GetAttributeValue("alt", "") + "</sym>");
                                else
                                {
                                    var str = "";
                                    for (var i = 0; i < a.DocumentNode.SelectSingleNode("//img").GetAttributeValue("alt", "").Length; i++)
                                        if (i != a.DocumentNode.SelectSingleNode("//img").GetAttributeValue("alt", "").Length - 1)
                                            str += a.DocumentNode.SelectSingleNode("//img").GetAttributeValue("alt", "")[i] + "/";
                                        else
                                            str += a.DocumentNode.SelectSingleNode("//img").GetAttributeValue("alt", "")[i];
                                    card.RuleText = card.RuleText.Replace(rt + "", "<sym>" + str + "</sym>");
                                }
                            }
                        }
                        card.RuleText = card.RuleText.Replace("<p>", "\n\t\t");
                        card.RuleText = card.RuleText.Replace("<i>", "");
                        card.RuleText = card.RuleText.Replace("</i>", "");
                        card.RuleText = card.RuleText.Replace("<nobr>", "");
                        card.RuleText = card.RuleText.Replace("</nobr>", "");
                        card.RuleText = card.RuleText.Replace("<u>", "");
                        card.RuleText = card.RuleText.Replace("</u>", "");
                        card.RuleText = card.RuleText.Replace("o", "");
                    }
                    if (htmlDocumentMTG.DocumentNode.SelectSingleNode("//div[@class='SearchCardInfoDIVsub SearchCardTextDiv shadow']//span[@class='rus']") != null)
                    {
                        card.FlavorText = htmlDocumentMTG.DocumentNode.SelectSingleNode("//div[@class='SearchCardInfoDIVsub SearchCardTextDiv shadow']//span[@class='rus']").InnerHtml;
                        card.FlavorText = card.FlavorText.Replace("<p>", "\n\t\t");
                        card.FlavorText = card.FlavorText.Replace("<i>", "");
                        card.FlavorText = card.FlavorText.Replace("</i>", "");
                        card.FlavorText = card.FlavorText.Replace("<nobr>", "");
                        card.FlavorText = card.FlavorText.Replace("</nobr>", "");
                        card.FlavorText = card.FlavorText.Replace("<u>", "");
                        card.FlavorText = card.FlavorText.Replace("</u>", "");
                    }

                    if (card.Type.Contains("Легендарн")) // если легендарка
                    {
                        card.stylesheet = "m15-legendary";
                    }
                    else // если не легендарка
                    {
                        card.stylesheet = "m15-flavor-bar";
                    }

                    if (card.Type.Contains("Существо")) // добавляем признак существа (если существо)
                    {
                        string PowerToughness = htmlDocumentMTG.DocumentNode.SelectNodes("//table[@class='NoteDiv U shadow']" + "//div[@class='SearchCardInfoDIV shadow']")[3].InnerText.Split(':')[1].Trim();
                        card.Power = PowerToughness.Split('/')[0].Trim();
                        card.Toughness = PowerToughness.Split('/')[1].Trim();
                    }
                }
            } catch
            {
                Console.WriteLine("Не обработалось...");
            }

            // Формируем цвет
            try
            {
                if ((card.Cost == null) || (card.Cost == ""))
                {
                    card.Color = "land";
                    if (card.RuleText != null)
                    {
                        if (card.RuleText.Contains("W")) card.Color += ", white";
                        if (card.RuleText.Contains("U")) card.Color += ", blue";
                        if (card.RuleText.Contains("R")) card.Color += ", red";
                        if (card.RuleText.Contains("B")) card.Color += ", black";
                        if (card.RuleText.Contains("G")) card.Color += ", green";

                        var countColor = card.Color.Count(x => x == ',');

                        if (countColor == 2) { card.Color += ", hybrid, horizontal"; }
                        if (countColor > 2) { card.Color = "land, multicolor"; }
                    }

                    if (card.Type.Split('-')[0].Trim() == "Базовая Земля")
                    {
                        if (card.Type.Split('-')[1].Trim() == "Равнина") card.Color += ", white"; 
                        if (card.Type.Split('-')[1].Trim() == "Остров") card.Color += ", blue"; 
                        if (card.Type.Split('-')[1].Trim() == "Гора") card.Color += ", red"; 
                        if (card.Type.Split('-')[1].Trim() == "Болото") card.Color += ", black"; 
                        if (card.Type.Split('-')[1].Trim() == "Лес") card.Color += ", green"; 
                    }
                }
                else if (card.Cost.Trim(new Char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', 'X', 'Y', 'Z' }).Distinct().Count() == 2)
                {
                    if (card.Cost.Contains("W")) card.Color += "white, ";
                    if (card.Cost.Contains("U")) card.Color += "blue, ";
                    if (card.Cost.Contains("R")) card.Color += "red, ";
                    if (card.Cost.Contains("B")) card.Color += "black, ";
                    if (card.Cost.Contains("G")) card.Color += "green, ";
                    card.Color += "multicolor";
                }
                else if (card.Cost.Trim(new Char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', 'X', 'Y', 'Z' }).Distinct().Count() <= 1)
                {
                    if (card.Cost.Contains("W")) card.Color = "white";
                    else if (card.Cost.Contains("U")) card.Color = "blue";
                    else if (card.Cost.Contains("R")) card.Color = "red";
                    else if (card.Cost.Contains("B")) card.Color = "black";
                    else if (card.Cost.Contains("G")) card.Color = "green";
                    else card.Color = "artifact";
                }
                else if ((card.Cost.Trim(new Char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', 'X', 'Y', 'Z' }).Distinct().Count() > 1) && (card.Cost.Contains("/")))
                {
                    if (card.Cost.Contains("W")) card.Color += "white, ";
                    if (card.Cost.Contains("U")) card.Color += "blue, ";
                    if (card.Cost.Contains("R")) card.Color += "red, ";
                    if (card.Cost.Contains("B")) card.Color += "black, ";
                    if (card.Cost.Contains("G")) card.Color += "green, ";
                    card.Color += (card.Cost.Contains("/")) ? "hybrid, " : "multicolor, ";
                    card.Color += "horizontal";
                }
                else { card.Color = "multicolor"; }
            }
            catch { Console.WriteLine("Не обработал цвет"); }

            return card;
        }

        public static Set CreateSet(Set set, string path)
        {
            if (File.Exists(path + set.setCode + @"\set"))
                for (var i = 0; i < set.maxCardInSet; i++)
                    set.cards.Add(GetCard(set, path, i + 1));
            else
                Console.WriteLine("Файл сета не найден");
            return set;
        }
        
        public static void SaveSet(Set set, string path)
        {
            var SetLocation = path + set.setCode + @"\set";
            if (File.Exists(path + set.setCode + @"\set"))
               foreach (var card in set.cards)
               {
                    Console.WriteLine("Сохраняю карту: " + card.Name + " | " + card.NumberOfSet);
                    var cardTextAll = "";
                    cardTextAll += "card:" + Environment.NewLine + "\tstylesheet: " + card.stylesheet + Environment.NewLine;
                    cardTextAll += "\thas styling: true" + Environment.NewLine + "\tstyling data:" + Environment.NewLine;
                    if (!card.Type.Contains("Planeswalker") && !card.Type.Contains("Сага")) { cardTextAll += "\t\tchop top: 7" + Environment.NewLine; }
                    if (!card.Type.Contains("Planeswalker") && !card.Type.Contains("Сага")) { cardTextAll += "\t\tchop bottom: 7" + Environment.NewLine; }
                    //if (!card.Type.Contains("Planeswalker") && !card.Type.Contains("Сага")) { cardTextAll += "\t\tflavor bar offset: 40" + Environment.NewLine; }
                    if (card.Type.Contains("Сага")) { cardTextAll += "\t\tchapter textboxes: " + card.TextBoxes + Environment.NewLine; }
                    if (card.Type.Contains("Сага")) { cardTextAll += "\t\tchapter number coordinates: " + "208,329,370," + Environment.NewLine; }
                    cardTextAll += "\t\ttext box mana symbols: magic-mana-small.mse-symbol-font" + Environment.NewLine;
                    if ((card.Rarity == "common") && (set.invertedCommonSymbol == "no"))
                        cardTextAll += "\t\tinverted common symbol: no" + Environment.NewLine;
                    else
                        cardTextAll += "\t\tinverted common symbol: yes" + Environment.NewLine;
                    cardTextAll += "\t\toverlay:" + Environment.NewLine;
                    cardTextAll += "\tnotes: Создано автоматически" + Environment.NewLine;
                    cardTextAll += "\ttime created: " + DateTime.Now.ToString("u").Remove(DateTime.Today.ToString("u").Length - 1) + Environment.NewLine;
                    cardTextAll += "\ttime modified: " + DateTime.Now.ToString("u").Remove(DateTime.Today.ToString("u").Length - 1) + Environment.NewLine;
                    if (card.Type.Contains("Сага")) { cardTextAll += "\textra data:" + Environment.NewLine + "\t\tmagic-m15-saga: chapter text: " + card.RuleText + Environment.NewLine; }
                    if (card.Type.Contains("Земля") && (card.stylesheet == "m15-flavor-bar"))
                        cardTextAll += "\textra data:" + Environment.NewLine +
                            "\t\tmagic-m15-flavor-bar:" + Environment.NewLine +
                                "\t\t\tstamp: " + card.Color + Environment.NewLine;
                    cardTextAll += "\tcard color: " + card.Color + Environment.NewLine;
                    cardTextAll += "\tname: <b>" + card.Name + "</b>" + Environment.NewLine;
                    cardTextAll += "\tcasting cost: " + card.Cost + Environment.NewLine;
                    cardTextAll += "\timage: " + card.FileImg + Environment.NewLine;
                    cardTextAll += "\tsuper type: <b>" + card.Type.Split('-')[0].Trim() + "</b>" + Environment.NewLine;
                    if (card.Type.Split('-').Length > 1) { cardTextAll += "\tsub type: <b>" + card.Type.Split('-')[1].Trim() + "</b>" + Environment.NewLine; }
                    cardTextAll += "\trarity: " + card.Rarity + Environment.NewLine;
                    cardTextAll += "\trule text:\r\n\t\t" + card.RuleText + Environment.NewLine;
                    if (!card.Type.Contains("Planeswalker") && !card.Type.Contains("Сага")) { cardTextAll += "\tflavor text:\r\n\t\t" + card.FlavorText + Environment.NewLine; }
                    if (card.Type.Contains("Существо")) { cardTextAll += "\tpower: " + card.Power + Environment.NewLine; }
                    if (card.Type.Contains("Существо")) { cardTextAll += "\ttoughness: " + card.Toughness + Environment.NewLine; }
                    if (card.Type.Contains("Planeswalker")) { cardTextAll += "\tloyalty: " + card.loyalty + Environment.NewLine; }
                    for (int lc = 0; lc < card.LoyaltyCost.Count(); lc++)
                        cardTextAll += "\tloyalty cost " + (lc + 1) + ": " + card.LoyaltyCost[lc] + Environment.NewLine;
                    if (card.Type.Split('-')[0].Trim() == "Базовая Земля")
                    {
                        if (card.Type.Split('-')[1].Trim() == "Равнина") cardTextAll += "\twatermark: mana symbol white" + Environment.NewLine; ;
                        if (card.Type.Split('-')[1].Trim() == "Остров") cardTextAll += "\twatermark: mana symbol blue" + Environment.NewLine; 
                        if (card.Type.Split('-')[1].Trim() == "Гора") cardTextAll += "\twatermark: mana symbol red" + Environment.NewLine; 
                        if (card.Type.Split('-')[1].Trim() == "Болото") cardTextAll += "\twatermark: mana symbol black" + Environment.NewLine;
                        if (card.Type.Split('-')[1].Trim() == "Лес") cardTextAll += "\twatermark: mana symbol green" + Environment.NewLine;
                    }
                    cardTextAll += "\tcustom card number: " + card.NumberOfSet + Environment.NewLine;
                    cardTextAll += "\tillustrator: " + card.Illustrator + Environment.NewLine;
                    if (card.Type.Contains("Сага")) { cardTextAll += "\tspecial text: " + card.SpecialText + Environment.NewLine; }
                    for (int lc = 0; lc < card.LevelText.Count(); lc++)
                        cardTextAll += "\tlevel " + (lc + 1) + " text: " + card.LevelText[lc] + Environment.NewLine;                    

                    File.AppendAllText(SetLocation, cardTextAll);
               }
            else
                Console.WriteLine("Файл сета не найден");
            if (File.Exists(path + "/" + set.setCode + ".mse-set"))
                File.Delete(path + "/" + set.setCode + ".mse-set");
            ZipFile.CreateFromDirectory(path + set.setCode + "/", path + set.setCode + ".mse-set", CompressionLevel.NoCompression, false);

            Console.WriteLine("Сет сохранен");
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                // Отладка
                Set set = new Set
                {
                    setCode = "IKO",
                    invertedCommonSymbol = "no",
                    maxCardInSet = 274,
                    urlWizards = @"https://magic.wizards.com/ru/articles/archive/card-image-gallery/ikoria-lair-behemoths"
                };
                for (var i = 243; i <= 274; i++)
                    set.cards.Add(GetCard(set, @"E:\OneDrive\MTG\Sets\", i));
                SaveSet(set, @"E:\OneDrive\MTG\Sets\");
            }
            else
            {
                Console.WriteLine("Путь, Код, no/yes: черные/белые комона, Количество, Wizards");
                String path = args[0];
                Set set = new Set
                {
                    setCode = args[1], //"C20",
                    invertedCommonSymbol = args[2],
                    maxCardInSet = Int16.Parse(args[3]), //322,
                    urlWizards = args[4] //"https://magic.wizards.com/ru/articles/archive/card-image-gallery/commander-2020-edition"
                };
                CreateSet(set, path);
                SaveSet(set, path);
            }            
            Console.WriteLine("Тыкни любую пимпу...");
            Console.ReadKey();
        }
    }
}

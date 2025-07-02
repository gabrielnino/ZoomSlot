using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Models;
using Services.Interfaces;

namespace Services
{
    public class PromptGenerator(IOpenAIClient openAIClient, ILogger<DocumentParse> logger) : IPromptGenerator
    {
        private readonly IOpenAIClient _openAIClient = openAIClient;
        private readonly ILogger<DocumentParse> _logger = logger;

        public async Task ExecuteChain()
        {

            int maxLengh = 450;
            // Step 1: Product Manager
            var task1 = new AIPromptBuilder
            {
                Role = "Data Engineer with a strong focus on data cleaning, transformation, and quality assurance. Skilled in building robust data pipelines that extract, cleanse, and structure raw data into reliable and consistent formats, ready for downstream analytics and machine learning applications.\r\n\r\nExperienced in handling large-scale datasets from diverse sources (CSV, JSON, APIs, databases, cloud storage), detecting anomalies, handling missing or inconsistent values, deduplicating records, and applying normalization techniques. Proficient in Python, SQL, and ETL tools such as Apache Airflow, dbt, and Pandas, with hands-on experience in data validation frameworks and automated data quality checks.\r\n\r\nAdept at collaborating with data scientists, analysts, and product teams to understand business logic and ensure data accuracy and usability. Familiar with cloud platforms like AWS and Azure, and leveraging services such as S3, Redshift, Glue, and Azure Data Factory to implement scalable and automated data cleaning pipelines.",
                Context = $"You are working with a Python module designed to group and normalize a list of skill names. The grouping process is based on semantic similarity and predefined technical or non-technical categories. The script consists of several key components:\r\n\r\nInitial Configuration:\r\n\r\nDefines constants: MIN_GROUP_SIZE and SIMILARITY_THRESHOLD.\r\n\r\nEstablishes MAIN_CATEGORIES and NON_TECH_CATEGORIES with associated keywords.\r\n\r\nFunction normalize_skill(skill_name):\r\n\r\nConverts skills to lowercase, removes spaces and special characters.\r\n\r\nPerforms standard replacements (e.g., .NET → dotnet, C# → csharp).\r\n\r\nRemoves irrelevant words and very short terms.\r\n\r\nFunction is_technical(skill):\r\n\r\nClassifies a skill as technical or non-technical based on keywords.\r\n\r\nFunction should_group_together(a, b):\r\n\r\nNormalizes both skills.\r\n\r\nChecks if one contains the other or if they share the first 4 characters.\r\n\r\nUses SequenceMatcher to compute similarity and compare it to the threshold.\r\n\r\nFunction determine_primary_category(skill_name):\r\n\r\nAssigns a category by checking against non-technical and then main categories.\r\n\r\nIf no match, labels as OTHER_TECH or OTHER_NON_TECH.\r\n\r\nFunction create_initial_groups(skills):\r\n\r\nSorts skills by length.\r\n\r\nFor each skill, finds a compatible group using should_group_together.\r\n\r\nVerifies category match and either adds to a group or starts a new one.\r\n\r\nFunction consolidate_groups(groups):\r\n\r\nMerges smaller groups by category.\r\n\r\nPicks representative skill, removes duplicates, and sorts alphabetically.",
                Task = "Develop a Python module (SkillNormalizer.py) with:\r\n\r\nComprehensive skill normalization logic\r\n\r\nReal-time progress tracking (tqdm progress bar)\r\n\r\nDetailed execution logging\r\n\r\nGrouping by semantic similarity and category",
                Format = "Complete Python script named SkillNormalizer.py with:\r\n\r\nType hints for all functions\r\n\r\nDocstrings following Google style\r\n\r\nProgress bars for batch operations\r\n\r\nException handling with logging\r\n\r\nPEP-8 compliant formatting\r\n\r\nTone: Professional, technical precision\r\nStyle: Concise yet fully documented implementation",
                MaxLength = maxLengh
            };
            /*
             * 
            string exampleText = "What’s the story behind your restaurant?\r\nWhen I was a teenager, I had serious problems with my parents—to the point where they even suggested I move out. At the time, I was unemployed, and I thought it would be a good idea to start selling empanadas.\r\n\r\nWho is your ideal customer, and how do you want them to feel when they experience your restaurant?\r\nMy ideal customer is someone who loves to eat empanadas. I want them to feel like they’re at home, surrounded by a warm, welcoming atmosphere and traditional Colombian flavors.\r\n\r\nWhat makes your restaurant truly different from others in your area or cuisine?\r\nWe always use fresh ingredients, and we have a special recipe that makes our empanadas—and especially our sauces—completely unique.\r\n\r\nWhat do you want people to say about your restaurant after they leave or see it online?\r\nI want people to say that our empanadas are the best they’ve ever had—and that they can enjoy them quickly and at an affordable price.";

            string restaurantName = "empanadas con sabor..";
            int maxLengh = 250;
            // Step 1: Product Manager
            var task1 = new AIPromptBuilder
            {
                Role = "Brand Strategist specializing in restaurants and food startups. I help define your brand personality, tone, and positioning so your restaurant connects emotionally with the right audience—from your visual identity to the language used in your menu and social media.",
                Context = $"You are working for the owner of a restaurant named {restaurantName}, with the following description: {exampleText}",
                Task = "Define the brand personality, tone, and positioning for the restaurant based on the provided description. Deliver a professionally structured document that includes:\r\n\r\n" +
                       "- Brand Vision & Mission\r\n" +
                       "- Core Values\r\n" +
                       "- Brand Personality (e.g., bold, friendly, elegant)\r\n" +
                       "- Brand Story / Origin Narrative\r\n" +
                       "- Brand Tone of Voice (e.g., formal, playful, nostalgic)\r\n" +
                       "- Messaging Framework (how to communicate the brand in different contexts)\r\n" +
                       "- Customer Promise",
                Format = "Clear markdown with headers and bullet points.\r\n\r\nConcise, professional tone (max 150 words per section).\r\n\r\nFocus: Emotional connection through storytelling and consistency.",
                MaxLength = maxLengh
            };


            task1.AddExample("Brand Personality:\r\n\r\nFriendly: Feels like a family kitchen.\r\n\r\nNostalgic: Celebrates tradition and personal history.\r\n\r\nHumble: Focused on genuine flavors, not pretension.");
            task1.AddConstraint("Avoid generic terms—tie everything back to the owner’s story.");
            task1.AddConstraint("Prioritize differentiation (e.g., \"unique sauces,\" \"homemade warmth\").");
            task1.AddParameter("Focus", "Leverage the owner’s journey (from struggle to success) to humanize the brand.");

            var task2 = new AIPromptBuilder
            {
                Role = "Act as a Brand Strategist specializing in restaurants and food startups. Your expertise is defining brand personality, tone, and positioning to help businesses connect emotionally with their audience—from visual identity to menu language and social media.",
                Context = $"You’re working with the owner of  {restaurantName}, with this background: {AIPromptBuilder.StepTag}",
                Task = " Define the brand’s personality, tone, and positioning using the provided details. Deliver a structured document with:\r\n\r\nBrand Vision & Mission (1–2 sentences each)\r\n\r\nCore Values (4–5 bullet points)\r\n\r\nBrand Personality (e.g., humble, warm, passionate)\r\n\r\nBrand Story / Origin Narrative (2–3 sentences)\r\n\r\nBrand Tone of Voice (e.g., conversational, nostalgic)\r\n\r\nMessaging Framework (examples for social media, menu, community engagement)\r\n\r\nCustomer Promise (1–2 sentences)",
                Format = $"Markdown or plain text with headers and bullet points.\r\n\r\nTone: Professional yet warm.\r\n\r\nStyle: Concise, avoid jargon.\r\n\r\nMax Length: {maxLengh} words total.",
                MaxLength = maxLengh
            };
            task2.AddConstraint("Align with the owner’s story and values (e.g., authenticity, affordability).");
            task2.AddConstraint("Emphasize emotional connection (nostalgia, comfort, warmth).");
            task2.AddConstraint("Avoid generic or overly corporate language.");
            task2.AddExample("### Brand Vision & Mission  \r\n- Vision: To be the most beloved empanada spot...  \r\n- Mission: Serving authentic, freshly made...  \r\n\r\n### Core Values  \r\n- Authenticity: Traditional recipes, humble roots.  \r\n- Warmth: \"Feels like family.\"  ");
            task1.NextTask = task2;

            var task3 = new AIPromptBuilder
            {
                Role = "I am a passionate Brand Identity Designer dedicated to crafting memorable and impactful visual identities for businesses, startups, and creatives. With a keen eye for detail and a deep understanding of branding principles, I transform ideas into cohesive brand experiences that resonate with audiences.\r\n\r\nMy expertise lies in logo design, typography, color theory, and brand strategy, ensuring every element aligns with a company’s vision and values. Whether it’s a startup looking to establish its presence or an established brand seeking a refresh, I create designs that are unique, timeless, and strategically effective.",

                Context = $"You're a part of a team working with the owner of  {restaurantName}, with this background: ###ResultPreviousStep##",

                Task = $"Define the brand’s a bran identiry ✔ Logo & Symbol Design\r\n✔ Brand Guidelines & Visual Identity Systems\r\n✔ Typography & Custom Lettering\r\n✔ Color Psychology & Palette Selection\r\n✔ Packaging & Collateral Design\r\n✔ Creative Strategy & Brand Positioning with this background: {AIPromptBuilder.StepTag}",

                Format = $"✔ Logo & Symbol Design\r\nExample: A hand-drawn logo of a steaming empanada shaped like a heart, symbolizing warmth and love. The steam subtly forms the map of Colombia to reflect its roots.\r\n\r\n✔ Brand Guidelines & Visual Identity Systems\r\nExample: A PDF brand book that includes:\r\n\r\nLogo usage rules (do's and don’ts)\r\n\r\nMinimum sizes\r\n\r\nPlacement guides\r\n\r\nSocial media profile templates\r\n\r\nTypography hierarchy\r\n\r\nColor codes (Pantone, HEX, CMYK, RGB)\r\n\r\n✔ Typography & Custom Lettering\r\nExample: A custom, rounded, slightly rustic typeface inspired by hand-painted Colombian street signs. Used for the logo and headers to evoke nostalgia and authenticity.\r\n\r\n✔ Color Psychology & Palette Selection\r\nExample:\r\n\r\nWarm yellow: evokes fried corn and joy\r\n\r\nDeep red: passion, sauce, and love\r\n\r\nEarthy brown: homemade, natural, comforting\r\n\r\nTurquoise accent: freshness and coastal Colombian vibes\r\n\r\nEach color includes a short note explaining its emotional role in the brand experience.\r\n\r\n✔ Packaging & Collateral Design\r\nExample:\r\n\r\nEco-friendly cardboard boxes with the logo stamped in red, featuring a short story about the founder.\r\n\r\nSauce containers labeled with custom illustrations.\r\n\r\nTakeaway bags printed with playful messages like “Más que una empanada, un abrazo.”\r\n\r\n✔ Creative Strategy & Brand Positioning\r\nExample:\r\nPositioning Statement:\r\n\"Empanadas con Sabor is the taste of Colombian comfort—where every bite brings warmth, tradition, and a sense of belonging to Latinos abroad and food lovers everywhere.\"",

                MaxLength = maxLengh
            };
            task2.NextTask = task3;

            var task4 = new AIPromptBuilder
            {
                Role = "boundary-pushing graphic designer known for blending modern minimalism with emotional artistry. Their work disrupts traditional aesthetics with clean lines, bold negative space, and refined simplicity that evokes powerful visual storytelling. Every composition is intentional—stripped down to its essence—yet layered with meaning and emotion.\r\n\r\nStyle & Philosophy:\r\n\r\nMinimalist yet expressive: Believes less is more, but never less than impactful.\r\n\r\nDraws from fine art, architecture, and contemporary culture, fusing structure with soul.\r\n\r\nPrioritizes clarity, balance, and rhythm in design, letting typography and form breathe.\r\n\r\nKnown for visual tension—juxtaposing softness and strength, order and chaos.\r\n\r\nSignature Traits:\r\n\r\nMonochrome palettes with strategic bursts of color\r\n\r\nElegant typography with brutalist undertones\r\n\r\nDeconstruction of traditional grid systems\r\n\r\nUse of whitespace as a storytelling device\r\n\r\nStrong editorial design roots and cinematic mood boards\r\n\r\nIdeal Projects:\r\n\r\nHigh-concept branding for fashion, design, or cultural institutions\r\n\r\nExperimental editorial layouts for print and digital\r\n\r\nAlbum artwork, gallery identities, or avant-garde campaigns\r\n\r\nDesign systems that challenge conventions and shift perspectives",

                Context = $"You are working with an existing frontend application composed of index.html, app.js, and styles.css. The app currently loads job offers from processed_parse_jobs.json and allows users to browse and select jobs. You are now tasked with extending this application by also loading data from resume.json and computing the similarity (or "match percentage") between the resume and the selected job offer.

This percentage should be visually represented as a circular gauge, styled similarly to the reference image fortunes-graphic-dark.png, and integrated seamlessly into the UI. The gauge should dynamically update as the user navigates through different job offers.

The similarity algorithm can be based on keyword overlap between resume skills and job requirements (e.g., Key Skills Required, Essential Qualifications, etc.).

You must ensure that:

The UI is professional and visually consistent with the current dark theme.

The implementation uses vanilla JavaScript.

The new elements (HTML/CSS) are modular and do not break the existing layout.",

                Task = $"Create svg log with this background: {AIPromptBuilder.StepTag}",

                Format = $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 500 300\" width=\"500\" height=\"300\">\r\n  <!-- Background -->\r\n  <rect width=\"500\" height=\"300\" fill=\"#FFF\" />\r\n  \r\n  <!-- Heart-shaped empanada -->\r\n  <path d=\"M250,120 C270,80 320,70 340,100 C360,130 340,170 250,210 C160,170 140,130 160,100 C180,70 230,80 250,120 Z\" fill=\"#FFD166\" stroke=\"#6B4B3E\" stroke-width=\"3\" />\r\n  \r\n  <!-- Crust texture (hand-drawn effect) -->\r\n  <path d=\"M240,130 C260,100 300,90 330,120\" fill=\"none\" stroke=\"#D62839\" stroke-width=\"1.5\" stroke-linecap=\"round\" opacity=\"0.7\" />\r\n  <path d=\"M230,150 C250,130 290,130 320,160\" fill=\"none\" stroke=\"#D62839\" stroke-width=\"1.5\" stroke-linecap=\"round\" opacity=\"0.7\" />\r\n  \r\n  <!-- Steam as Colombian map (simplified) -->\r\n  <path d=\"M250,80 C245,60 260,40 280,50 C290,30 310,40 300,70 C330,60 340,80 320,90 C340,100 330,120 300,110\" fill=\"none\" stroke=\"#2EC4B6\" stroke-width=\"2\" stroke-linecap=\"round\" opacity=\"0.8\" />\r\n  \r\n  <!-- Wordmark: Custom rustic script -->\r\n  <text x=\"250\" y=\"250\" font-family=\"'Brush Script MT', cursive\" font-size=\"40\" fill=\"#D62839\" text-anchor=\"middle\" font-weight=\"bold\">Empanadas con Sabor</text>\r\n  <text x=\"250\" y=\"280\" font-family=\"Arial, sans-serif\" font-size=\"16\" fill=\"#6B4B3E\" text-anchor=\"middle\" font-style=\"italic\">Hecho con amor y sazón</text>\r\n</svg>",

                MaxLength = maxLengh
            };
            task3.NextTask = task4;

            */

            var currentTask = task1;
            string? result = null;
            Prompt? prompt;
            while (currentTask != null)
            {
                Console.WriteLine($"➡️ Step Task:{currentTask.Step}\n");
                string promptChain = currentTask.BuildPrompt();
                prompt = currentTask.BuildPromptObject(result);
                var texPrompt = currentTask.BuildPrompt();
                result = await _openAIClient.GetChatCompletionAsync(prompt);               
                Console.WriteLine("📋 Task Chain Prompt:\n");
                Console.WriteLine(promptChain);
                currentTask = currentTask.NextTask;
            }

            prompt = currentTask.BuildPromptObject(result);
            Console.WriteLine($"➡️ Step Task:{currentTask.Step}\n");

            result = await _openAIClient.GetChatCompletionAsync(prompt);
        }
    }
}

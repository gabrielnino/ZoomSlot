using Models;

namespace Services
{
    internal static class PrompHelpers
    {

        public static Prompt GetParseJobOfferPrompt(string jobOfferDescription)
        {
            if (string.IsNullOrWhiteSpace(jobOfferDescription))
            {
                throw new ArgumentNullException(nameof(jobOfferDescription), "Job description cannot be null or empty.");
            }

            const string JsonSchema = @"
            {
              ""Company Name"": ""string"",
              ""Job Offer Title"": ""string"",
              ""Job Offer Summary"": ""string"",
              ""Email Contact"": ""string"",
              ""Key Skills Required"": [""string""],
              ""Essential Qualifications"": [""string""],
              ""Essential Technical Skill Qualifications"": [""string""],
              ""Other Technical Skill Qualifications"": [""string""],
              ""Salary or Budget Offered"": ""string""
            }";

            const string SystemContent = @"
            You are an expert recruiter specializing in the selection and evaluation of software developers, 
            focusing on identifying top talent with the required technical skills, qualifications, 
            and experience to meet specific job requirements in the software development industry.";

            const string TaskDescription = @"
            Analyze the following job description and extract specific information. 
            For the properties ""Essential Technical Skill Qualifications"" and ""Other Technical Skill Qualifications"", 
            include only the names of the technical skills without specifying time or additional comments. 
            The output should be in a structured JSON format, adhering to the schema below.";

            string userContent = $@"
            {TaskDescription}

            Job Description:
            {jobOfferDescription}

            Output Requirements:
            Present the extracted information in the following JSON schema:

            JSON Schema:
            {JsonSchema}";
            return new Prompt
            {
                SystemContent = SystemContent,
                UserContent = userContent
            };
        }

        public static Prompt GetParseResumePrompt(string resumeText)
        {
            if (string.IsNullOrWhiteSpace(resumeText))
            {
                throw new ArgumentException("Resume text cannot be null or empty.", nameof(resumeText));
            }

            const string JsonSchema = @"
            {
              ""Name"": ""string"",
              ""Title"": ""string"",
              ""Location"": ""string"",
              ""Contact Information"": {
                ""Phone"": ""string"",
                ""Email"": ""string"",
                ""LinkedIn"": ""string""
              },
              ""Professional Summary"": ""string"",
              ""Bullet Points"": [""string""],
              ""Technical Skills"": [""string""],
              ""Soft Skills"": [""string""],
              ""Languages"": [""string""],
              ""Professional Experience"": [
                {
                  ""Role"": ""string"",
                  ""Company"": ""string"",
                  ""Location"": ""string"",
                  ""Duration"": ""string"",
                  ""Responsibilities"": [""string""],
                  ""Tech Stack"": [""string""]
                }
              ],
              ""Additional Qualifications"": [""string""],
              ""Education"": {
                ""Institution"": ""string"",
                ""Location"": ""string"",
                ""Degree"": ""string"",
                ""Graduation Date"": ""string""
              }
            }";

            const string SystemContent = @"
            You are an expert recruiter specializing in technical talent evaluation.
            Your task is to extract and structure resume information into a standardized JSON format.
            Focus on identifying:
            - Clear technical skills (without proficiency levels)
            - Relevant experience with specific technologies
            - Key achievements and responsibilities
            - Clean educational background";

            const string TaskInstructions = @"
            Analyze the following resume text and extract the requested information.
            Follow these guidelines:
            1. Include only skill names for technical skills (no durations or comments)
            2. Keep descriptions concise and achievement-oriented
            3. Maintain consistent formatting for dates and locations
            4. Output must strictly adhere to the provided JSON schema";

            string userContent = $@"
            Resume Text to Process:
            {resumeText}

            Output Requirements:
            {TaskInstructions}

            Required JSON Structure:
            {JsonSchema}";

            return new Prompt
            {
                SystemContent = SystemContent,
                UserContent = userContent
            };
        }


        public static Prompt GenerateResumeJsonPrompt(string jobOfferString, string resumeString)
        {
            if (string.IsNullOrWhiteSpace(jobOfferString))
            {
                throw new ArgumentNullException(nameof(jobOfferString), "Job description cannot be null or empty.");
            }

            const string JsonSchema = @"
            {
              ""Name"": ""string"",
              ""Title"": ""string"",
              ""Location"": ""string"",
              ""Contact Information"": {
                ""Phone"": ""string"",
                ""Email"": ""string"",
                ""LinkedIn"": ""string""
              },
              ""Professional Summary"": ""string"",
              ""Technical Skills"": [""string""],
              ""Soft Skills"": [""string""],
              ""Languages"": [""string""],
              ""Professional Experience"": [
                {
                  ""Role"": ""string"",
                  ""Company"": ""string"",
                  ""Location"": ""string"",
                  ""Duration"": ""string"",
                  ""Responsibilities"": [""string""],
                  ""Tech Stack"": [""string""]
                }
              ],
              ""Additional Qualifications"": [""string""],
              ""Education"": {
                ""Institution"": ""string"",
                ""Location"": ""string"",
                ""Degree"": ""string"",
                ""Graduation Date"": ""string""
              }
            }";

            const string SystemContent = @"
            You are a professional resume assistant specializing in tailoring resumes to highlight relevant technical qualifications.";

            const string TaskDescription = @"
                Your task is to align a given resume with the provided job offer. The input will consist of a JSON-formatted job offer and resume. You must:
                1. Update the Professional Summary to align with the job description and highlight relevant skills, achievements, and experiences. Incorporate key skills and keywords from the job offer into the summary.
                2. Update the Tech Stack in each professional experience to reflect the technologies listed under ""Essential Technical Skill Qualifications"" and ""Other Technical Skill Qualifications.""
                3. Tailor the Responsibilities in each professional experience to align with the job description while retaining the candidate's original achievements and quantifiable impacts.
                4. Update the Role Titles in each professional experience to reflect and align with the job offer’s language, ensuring consistency with the target position (e.g., ""Software Developer – AI Trainer""). Titles should remain truthful to the experience level and responsibilities but should use language and phrasing from the job offer when applicable.

                ### Guidelines:
                - **Professional Summary**:
                  - Extract key themes, skills, and qualifications from the job offer and integrate them into the summary.
                  - Highlight years of experience, key technical skills (e.g., Oracle, SQL, ETL, Agile), and significant achievements.
                  - Emphasize alignment with the role's requirements, such as database development, Agile SDLC, and leadership experience.
                  - Ensure the summary reflects the candidate's ability to meet the job offer's expectations and contribute value to the company.

                  #### Example:
                  **Job Offer Keywords**: ""10+ years of database development, Agile SDLC, Oracle, SQL, leadership.""
  
                  #### Before:
                  ""Innovative Full Stack Developer with over 17 years of experience designing and implementing enterprise-level solutions in .NET Framework, Angular, and Azure.""

                  #### After:
                  ""Experienced Senior Database Developer with over 17 years of expertise in designing and managing enterprise-level databases, specializing in Oracle, SQL, and ETL processes. Proven track record of leading Agile teams to deliver high-impact solutions while optimizing database performance and scalability. Adept at aligning database architecture with modern SDLC methodologies to support critical business operations.""

                - **Tech Stack Updates**:
                  - Replace outdated technologies with modern equivalents, ensuring consistency with the timeframe of each role.
                    - Example: Replace "".NET Framework"" with "".NET Core"" for roles after 2016, where applicable.
                  - Retain or add only technologies mentioned in the job offer (Essential/Other Technical Skills) unless the resume indicates otherwise.
                  - Ensure older technologies, such as ""Web Forms,"" are updated to modern equivalents for recent experiences.

                - **Responsibilities Tailoring**:
                  - Align the descriptions of responsibilities to reflect keywords and themes in the job offer.
                  - Include measurable outcomes (e.g., ""Improved database performance by 30%"" or ""Reduced deployment time by 35%"").
                  - Reflect domain-specific skills from the job offer where relevant (e.g., ""ETL development,"" ""Agile,"" or ""Stored procedures"").

                - **Preserve Resume Integrity**:
                  - Do not invent achievements; base changes on provided data and job offer context.
                  - Ensure descriptions remain true to the candidate's experience while highlighting relevant skills.

                - **Example of Alignment**:
                  #### Before:
                  **Responsibilities**: ""Developed APIs and optimized queries for better performance.""
                  **Tech Stack**: "".NET Framework, SQL Server.""

                  #### After:
                  **Responsibilities**: ""Developed RESTful APIs for CRM-EMR integration, reducing data inconsistencies by 20%. Optimized SQL Server queries, enhancing database performance by 25%. Aligned database design with Agile development practices.""
                  **Tech Stack**: "".NET Core, SQL Server, RESTful APIs, Agile.""

                ### Input:
                Provide the job offer and resume in JSON format. Ensure the output is presented as a JSON object matching this schema:

            ";

            string userContent = $@"
            {TaskDescription}

            Job Description:
            {jobOfferString}
            
            Resume:
            {resumeString}

            Output Requirements:
            Present the extracted information in the following JSON schema:

            JSON Schema:
            {JsonSchema}";
            return new Prompt
            {
                SystemContent = SystemContent,
                UserContent = userContent
            };
        }

        public static Prompt GenerateCoverLetterPrompt(string jobOfferString, string resumeString)
        {
            if (string.IsNullOrWhiteSpace(jobOfferString))
            {
                throw new ArgumentNullException(nameof(jobOfferString), "Job description cannot be null or empty.");
            }
            if (string.IsNullOrWhiteSpace(resumeString))
            {
                throw new ArgumentNullException(nameof(resumeString), "Resume cannot be null or empty.");
            }

            const string JsonSchema = @"
            {
              ""Name"": ""string"",
              ""Title"": ""string"",
              ""Location"": ""string"",
              ""Contact Information"": {
                ""Phone"": ""string"",
                ""Email"": ""string"",
                ""LinkedIn"": ""string""
              },
              ""Professional Summary"": ""string"",
              ""Bullet Points"": [""string""],
              ""Closing Paragraph"": ""string"",
              ""Technical Skills"": [""string""],
              ""Soft Skills"": [""string""],
              ""Languages"": [""string""],
              ""Professional Experience"": [
                {
                  ""Role"": ""string"",
                  ""Company"": ""string"",
                  ""Location"": ""string"",
                  ""Duration"": ""string"",
                  ""Responsibilities"": [""string""],
                  ""Tech Stack"": [""string""]
                }
              ],
              ""Additional Qualifications"": [""string""],
              ""Education"": {
                ""Institution"": ""string"",
                ""Location"": ""string"",
                ""Degree"": ""string"",
                ""Graduation Date"": ""string""
              }
            }";

            const string SystemContent = @"
            You are a professional career assistant specializing in crafting compelling and personalized cover letters.
            Your expertise lies in tailoring each cover letter to highlight the most relevant essential technical skills qualifications,
            other essential technical skills qualifications, and accomplishments, ensuring alignment with the job description and industry standards.
            Your goal is to present the candidate as the ideal fit for the role,
            showcasing their value and enthusiasm in a professional and engaging manner.";

            const string TaskDescription = @"
            You will receive two JSON-formatted inputs: a job offer and a resume. 
            Your task is to generate a tailored and professional cover letter that aligns with the information provided in both inputs.";

            string userContent = $@"
            {TaskDescription}

            Job Description:
            {jobOfferString}
            
            Resume:
            {resumeString}

            Output Requirements:
            Present the extracted information in the following JSON schema:

            JSON Schema:
            {JsonSchema}";
            return new Prompt
            {
                SystemContent = SystemContent,
                UserContent = userContent
            };
        }
    }
}

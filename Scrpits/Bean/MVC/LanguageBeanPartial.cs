using System;
using System.Collections.Generic;
public partial class LanguageBean
{
}
public partial class LanguageCfg
{
    public static void ChangeLanguageData(LanguageEnum languageType)
    {
        ChangeLanguageData(languageType.GetEnumName());
    }
    
    public static void ChangeLanguageData(string languageType)
	{
        currentLanguage = languageType;
	}
}

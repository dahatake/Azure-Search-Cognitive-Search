# [Custom Skill] Microsoft Translator Text on Azure Function (C#)

Custom Skill は、Azure Search の Cognitive Skill 機能とのやり取りのため、入出力の JSON 形式は、それに適用したものにする必要があります。

ご参考: https://docs.microsoft.com/ja-jp/azure/search/cognitive-search-custom-skill-interface

1. Setup

- Microsoft Translator API v3 のキー

Azure Function の「環境変数」に設定します。

| 項目 | 値 |
| --- | --- |
| 環境変数名 | TRANSLATOR_TEXT_KEY | 
  - https://docs.microsoft.com/ja-jp/azure/azure-functions/functions-reference-csharp#environment-variables

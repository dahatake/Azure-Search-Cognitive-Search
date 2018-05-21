# Azure-Search-Cognitive-Search

Azure Search の Cognitive Search のサンプルです。

参考: 
https://docs.microsoft.com/ja-jp/azure/search/cognitive-search-quickstart-blob

## AzureSearch-Object-REST
REST での、Index, DataSource, Skillset, Indexer の作成/更新を行います。

- Tool
    - シンプルなので、[POSTMAN](https://www.getpostman.com/ "POSTMAN") などの REST Client ツールが便利です。
    - Azure Portal: Index, DataSource, Indexer についてはオブジェクトの状況を確認できます。
    - REST のみ: Skillset (2018/05/21 現在)

## dahatakeTranslatorText
Custom skillset のサンプルです。
開発中は Azure Functions がお勧めです。ログ出力が容易です。
- Translator
    - Microsoft Translator Text を呼び出し
    - 単独文字列のみ

## 作成手順
1. Azure Blob Storage にファイルアップロード
2. Azure Functions 作成
3. Azure Functions のコードを発行
4. REST にて、Index, DataSource, Skillset, Indexer を作成
5. Indexer はデフォルトだと作成時に実行されます。
6. Index を Search して、結果確認

## Debug Tips
- ファイル数は 1-2 個で
- "enriched" field を Index に追加する。本番では外すこと.
    ```JSON
    {
        "fields": [
            // other fields go here.
            {
                "name": "enriched",
                "type": "Edm.String",
                "searchable": false,
                "sortable": false,
                "filterable": false,
                "facetable": false
            }
        ]
    }
    ```

    参考: https://docs.microsoft.com/ja-jp/azure/search/cognitive-search-concept-troubleshooting

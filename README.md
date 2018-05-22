# Azure-Search-Cognitive-Search

Azure Search の Cognitive Search のサンプルです。

参考: 
https://docs.microsoft.com/ja-jp/azure/search/cognitive-search-quickstart-blob

## AzureSearch-Object-REST
REST での、Index, DataSource, Skillset, Indexer の作成/更新を行います。

| 設定項目 | ファイル | 説明 |
| --- | --- | --- |
| Azure Search Key | 全て | Azure Search のアクセスキー |
| Azure Function Authentication Code | skillset-images.txt | Azure Function 上の関数にアクセスするためのコード。プロダクション利用にはもう少し良い方法があります (理解のしやすさを優先しました)。The JFK Files のコードが参考になります。|

- Tool
    - シンプルなので、[POSTMAN](https://www.getpostman.com/ "POSTMAN") などの REST Client ツールが便利です。
    - Azure Portal: Index, DataSource, Indexer についてはオブジェクトの状況を確認できます。
    - REST のみ: Skillset (2018/05/21 現在)

## CognitiveSearchCustomSkill
Custom skillset のサンプルです。いずれも Azure Cognitive Services を使っています。
開発中は Azure Functions がお勧めです。ログ出力が容易です。
- Translator
    - Microsoft Translator Text を呼び出し
    - 単独文字列のみ
- Face API
    - Face API の Indentify にて、登録済みの人がいるかを検索
    - 一番精度の良かった 1人のみを返します (複数名も取得きるよう作業中)

環境変数を設定する必要があります。

| 項目 | 説明 |
| --- | --- |
| TRANSLATOR_TEXT_KEY | Microsoft Translator Text v3 へのアクセスキー |
| FACEAPI_KEY | Face API へのアクセスキー |
| FACEAPI_REGION | Face API エンドポイントのRegion部分のみ |
| FACEAPI_GROUPID | Face API の PersonGroupId です。小文字のみです |

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
            ...
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

## RegisterFaceAPIPersonWithGroup

Face API に写真登録をするシンプルなコンソールアプリケーションです。

- .NET Framework のみ (.NET Standard に後日対応予定)


## 参考

JFK Files: https://github.com/Microsoft/AzureSearch_JFK_Files

Azure Search - Cognitive Search ドキュメント: https://docs.microsoft.com/ja-jp/azure/search/cognitive-search-concept-troubleshooting
